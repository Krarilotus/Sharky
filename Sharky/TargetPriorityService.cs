﻿using Sharky.Managers;
using System.Collections.Generic;
using System.Linq;

namespace Sharky
{
    public class TargetPriorityService
    {
        UnitDataManager UnitDataManager;

        public TargetPriorityService(UnitDataManager unitDataManager)
        {
            UnitDataManager = unitDataManager;
        }

        public TargetPriorityCalculation CalculateTargetPriority(UnitCalculation unitCalculation)
        {
            var calculation = new TargetPriorityCalculation
            {
                TargetPriority = TargetPriority.Attack
            };

            var allies = unitCalculation.NearbyAllies.Where(e => e.UnitClassifications.Contains(UnitClassification.DefensiveStructure) || e.UnitClassifications.Contains(UnitClassification.ArmyUnit));
            var enemies = unitCalculation.NearbyEnemies.Where(e => e.UnitClassifications.Contains(UnitClassification.DefensiveStructure) || e.UnitClassifications.Contains(UnitClassification.ArmyUnit));

            var allyHealth = allies.Sum(e => e.SimulatedHitpoints);
            var enemyHealth = enemies.Sum(e => e.SimulatedHitpoints);

            var allyAttributes = allies.SelectMany(e => e.Attributes).Distinct();
            var enemyAttributes = enemies.SelectMany(e => e.Attributes).Distinct();

            var allyDps = allies.Sum(e => e.SimulatedDamagePerSecond(enemyAttributes, true, true));
            var enemyDps = enemies.Sum(e => e.SimulatedDamagePerSecond(allyAttributes, true, true));
           
            var allyHps = allies.Sum(e => e.SimulatedHealPerSecond);
            var enemyHps = enemies.Sum(e => e.SimulatedHealPerSecond);

            var secondsToKillEnemies = 600f;
            if (allyDps - enemyHps > 0)
            {
                secondsToKillEnemies = enemyHealth / (allyDps - enemyHps);
            }

            var secondsToKillAllies = 600f;
            if (enemyDps - allyHps > 0)
            {
                secondsToKillAllies = allyHealth / (enemyDps - allyHps);
            }

            calculation.OverallWinnability = secondsToKillAllies / secondsToKillEnemies; // higher the number the better
            calculation.Overwhelm = calculation.OverallWinnability > 20;

            var airAttackingEnemies = enemies.Where(e => e.DamageAir || e.Unit.UnitType == (uint)UnitTypes.PROTOSS_SHIELDBATTERY);
            var airKillSeconds = 600f;
            if (allies.Count(a => a.Unit.IsFlying && a.DamageAir) > 0)
            {
                var seconds = GetKillSeconds(airAttackingEnemies, allies);
                if (seconds > 0)
                {
                    airKillSeconds = seconds;
                }
            }

            calculation.AirWinnability = secondsToKillAllies / airKillSeconds;

            var groundAttackingEnemies = enemies.Where(e => e.DamageGround || e.Unit.UnitType == (uint)UnitTypes.TERRAN_MEDIVAC || e.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISM || e.Unit.UnitType == (uint)UnitTypes.PROTOSS_SHIELDBATTERY);
            var groundKillSeconds = 600f;
            if (allies.Count(a => !a.Unit.IsFlying && a.DamageGround) > 0)
            {
                var seconds = GetKillSeconds(groundAttackingEnemies, allies);
                if (seconds > 0)
                {
                    groundKillSeconds = seconds;
                }
            }

            calculation.GroundWinnability = secondsToKillAllies / groundKillSeconds;

            if (calculation.OverallWinnability < 1 && calculation.AirWinnability < 1 && calculation.GroundWinnability < 1)
            {
                if (enemyHps > enemyDps && groundAttackingEnemies.Count(u => u.Unit.UnitType == (uint)UnitTypes.TERRAN_SCV) > 2)
                {
                    var lowerHps = enemyHealth / (allyDps - (enemyHps / 5));
                    if (secondsToKillAllies / lowerHps > 1)
                    {
                        calculation.TargetPriority = TargetPriority.KillWorkers; // can win by targetting repairing scvs
                        return calculation;
                    }
                }

                calculation.TargetPriority = TargetPriority.Retreat;
                return calculation;
            }

            if (ShouldTargetDetection(unitCalculation))
            {
                calculation.TargetPriority = TargetPriority.KillDetection;
                return calculation;
            }

            if (calculation.GroundWinnability > calculation.AirWinnability)
            {
                var bunker = groundAttackingEnemies.Where(b => b.Unit.UnitType == (uint)UnitTypes.TERRAN_BUNKER).FirstOrDefault();
                if (bunker != null && groundAttackingEnemies.Count() < 10)
                {
                    if (bunker.Unit.BuildProgress < 1 || (bunker.Unit.Health > 100 && enemyHps > enemyDps && groundAttackingEnemies.Count(u => u.Unit.UnitType == (uint)UnitTypes.TERRAN_SCV) > 2))
                    {
                        calculation.TargetPriority = TargetPriority.KillWorkers;
                        return calculation;
                    }

                    calculation.TargetPriority = TargetPriority.KillBunker;
                    return calculation;
                }
                calculation.TargetPriority = TargetPriority.WinGround;
            }
            else if (calculation.AirWinnability > calculation.GroundWinnability)
            {
                calculation.TargetPriority = TargetPriority.WinAir;
            }

            return calculation;
        }

        bool ShouldTargetDetection(UnitCalculation unitCalculation)
        {
            if (unitCalculation.NearbyAllies.Any(e => UnitDataManager.CloakableAttackers.Contains((UnitTypes)e.Unit.UnitType) || e.Unit.UnitType == (uint)UnitTypes.PROTOSS_MOTHERSHIP))
            {
                if (unitCalculation.NearbyEnemies.Any(e => e.Unit.UnitType == (uint)UnitTypes.PROTOSS_OBSERVER || e.Unit.UnitType == (uint)UnitTypes.TERRAN_RAVEN || e.Unit.UnitType == (uint)UnitTypes.ZERG_OVERSEER || e.Unit.UnitType == (uint)UnitTypes.PROTOSS_ORACLE))
                {
                    return true;
                }

                if (unitCalculation.NearbyEnemies.Any(e => e.Unit.UnitType == (uint)UnitTypes.TERRAN_MISSILETURRET || e.Unit.UnitType == (uint)UnitTypes.PROTOSS_PHOTONCANNON || e.Unit.UnitType == (uint)UnitTypes.ZERG_SPORECRAWLER || e.Unit.UnitType == (uint)UnitTypes.ZERG_SPORECRAWLERUPROOTED || e.Unit.UnitType == (uint)UnitTypes.TERRAN_GHOST || e.Unit.UnitType == (uint)UnitTypes.ZERG_INFESTOR))
                {
                    return true;
                }
            }
            return false;
        }

        float GetKillSeconds(IEnumerable<UnitCalculation> enemies, IEnumerable<UnitCalculation> allies)
        {
            var groundAttackingEnemiesAttributes = enemies.SelectMany(e => e.Attributes).Distinct();

            var flyingGroundAttackingEnemies = enemies.Where(e => e.Unit.IsFlying);
            var flyingGroundAttackingEnemiesAttributes = enemies.SelectMany(e => e.Attributes).Distinct();
            var airHps = flyingGroundAttackingEnemies.Sum(e => e.SimulatedHealPerSecond);
            var airHealth = flyingGroundAttackingEnemies.Sum(e => e.SimulatedHitpoints);

            var groundedGroundAttackingEnemies = enemies.Where(e => !e.Unit.IsFlying);
            var groundedGroundAttackingEnemiesAttributes = enemies.SelectMany(e => e.Attributes).Distinct();
            var groundHps = groundedGroundAttackingEnemies.Sum(e => e.SimulatedHealPerSecond);
            var groundHealth = groundedGroundAttackingEnemies.Sum(e => e.SimulatedHitpoints);

            var groundAttackingAllies = allies.Where(e => e.DamageGround || e.Unit.UnitType == (uint)UnitTypes.PROTOSS_WARPPRISM || e.Unit.UnitType == (uint)UnitTypes.TERRAN_MEDIVAC);
            var airAttackingAllies = allies.Where(e => e.DamageAir);
            var bothAttackingAllies = allies.Where(e => e.DamageAir || e.DamageGround);
            var groundAttackDps = groundAttackingAllies.Sum(e => e.SimulatedDamagePerSecond(groundedGroundAttackingEnemiesAttributes, false, true));
            var airAttackDps = airAttackingAllies.Sum(e => e.SimulatedDamagePerSecond(flyingGroundAttackingEnemiesAttributes, true, false));
            var bothAttackingDps = bothAttackingAllies.Sum(e => e.SimulatedDamagePerSecond(groundAttackingEnemiesAttributes, true, true));

            var secondsToKillAirGroundAttackingEnemies = 600f;
            if (airAttackDps - airHps > 0)
            {
                secondsToKillAirGroundAttackingEnemies = airHealth / (airAttackDps - airHps);
            }
            var secondsToKillGroundGroundAttackingEnemies = 600f;
            if (groundAttackDps - groundHps > 0)
            {
                secondsToKillGroundGroundAttackingEnemies = groundHealth / (groundAttackDps - groundHps);
            }

            var secondsBothToKillAirGroundAttackingEnemies = 600f;
            if (bothAttackingDps - airHps > 0)
            {
                secondsBothToKillAirGroundAttackingEnemies = airHealth / (bothAttackingDps - airHps);
            }
            var secondsBothToKillGroundGroundAttackingEnemies = 600f;
            if (bothAttackingDps - groundHps > 0)
            {
                secondsBothToKillGroundGroundAttackingEnemies = groundHealth / (bothAttackingDps - groundHps);
            }

            var airSeconds = secondsToKillAirGroundAttackingEnemies - secondsBothToKillGroundGroundAttackingEnemies;
            var groundSeconds = secondsToKillGroundGroundAttackingEnemies - secondsBothToKillAirGroundAttackingEnemies;

            var groundKillSeconds = airSeconds;
            if (groundSeconds > airSeconds)
            {
                groundKillSeconds = groundSeconds;
            }

            return groundKillSeconds;
        }
    }
}
