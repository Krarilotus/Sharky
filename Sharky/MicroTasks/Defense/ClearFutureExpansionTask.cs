﻿using SC2APIProtocol;
using Sharky.Builds.BuildingPlacement;
using Sharky.DefaultBot;
using Sharky.MicroControllers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    public class ClearFutureExpansionTask : MicroTask
    {
        TargetingData TargetingData;
        BaseData BaseData;
        MacroData MacroData;
        EnemyData EnemyData;
        MicroTaskData MicroTaskData;
        BuildingService BuildingService;

        IMicroController MicroController;

        Point2D NextBaseLocation;
        int BaseCountDuringLocation;

        public List<DesiredUnitsClaim> DesiredUnitsClaims { get; set; }
        
        /// <summary>
        /// only uses units when the desired bases is less than the current, claims them from the attack class then unclaims them when done
        /// </summary>
        public bool OnlyActiveWhenNeeded { get; set; }
        bool Needed;

        public ClearFutureExpansionTask(DefaultSharkyBot defaultSharkyBot,
            List<DesiredUnitsClaim> desiredUnitsClaims, float priority, bool enabled = true)
        {
            TargetingData = defaultSharkyBot.TargetingData;
            BaseData = defaultSharkyBot.BaseData;
            MacroData = defaultSharkyBot.MacroData;
            EnemyData = defaultSharkyBot.EnemyData;
            MicroTaskData = defaultSharkyBot.MicroTaskData;
            BuildingService = defaultSharkyBot.BuildingService;

            MicroController = defaultSharkyBot.MicroController;

            DesiredUnitsClaims = desiredUnitsClaims;
            Priority = priority;
            Enabled = enabled;
            UnitCommanders = new List<UnitCommander>();

            Enabled = true;

            OnlyActiveWhenNeeded = false;
            Needed = false;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            if (OnlyActiveWhenNeeded && !Needed) { return; }

            foreach (var commander in commanders)
            {
                if (!commander.Value.Claimed)
                {
                    var unitType = commander.Value.UnitCalculation.Unit.UnitType;
                    foreach (var desiredUnitClaim in DesiredUnitsClaims)
                    {
                        if ((uint)desiredUnitClaim.UnitType == unitType && !commander.Value.UnitCalculation.Unit.IsHallucination && UnitCommanders.Count(u => u.UnitCalculation.Unit.UnitType == (uint)desiredUnitClaim.UnitType) < desiredUnitClaim.Count)
                        {
                            commander.Value.Claimed = true;
                            commander.Value.UnitRole = UnitRole.Defend;
                            UnitCommanders.Add(commander.Value);
                        }
                    }
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var actions = new List<SC2APIProtocol.Action>();

            UpdateNeeded();

            if (OnlyActiveWhenNeeded && !Needed) { return actions; }

            if (UpdateBaseLocation())
            {
                var detectors = UnitCommanders.Where(c => c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Detector) || c.UnitCalculation.UnitClassifications.Contains(UnitClassification.DetectionCaster));
                var nonDetectors = UnitCommanders.Where(c => !c.UnitCalculation.UnitClassifications.Contains(UnitClassification.Detector) && !c.UnitCalculation.UnitClassifications.Contains(UnitClassification.DetectionCaster));

                var vector = new Vector2(NextBaseLocation.X, NextBaseLocation.Y);

                foreach (var nonDetector in nonDetectors)
                {
                    if (nonDetector.UnitCalculation.EnemiesThreateningDamage.Any() || Vector2.DistanceSquared(nonDetector.UnitCalculation.Position, vector) < 25)
                    {
                        actions.AddRange(MicroController.Attack(new List<UnitCommander> { nonDetector }, NextBaseLocation, TargetingData.ForwardDefensePoint, NextBaseLocation, frame));
                    }
                    else
                    {
                        actions.AddRange(nonDetector.Order(frame, Abilities.MOVE, NextBaseLocation));
                    }
                }

                foreach (var detector in detectors)
                {
                    if (detector.UnitCalculation.EnemiesThreateningDamage.Any() || Vector2.DistanceSquared(detector.UnitCalculation.Position, vector) < 4)
                    {
                        actions.AddRange(MicroController.Support(new List<UnitCommander> { detector }, nonDetectors, NextBaseLocation, TargetingData.ForwardDefensePoint, NextBaseLocation, frame));
                    }
                    else
                    {
                        actions.AddRange(detector.Order(frame, Abilities.MOVE, NextBaseLocation));
                    }
                }
            }
            else
            {
                actions.AddRange(MicroController.Attack(UnitCommanders, TargetingData.ForwardDefensePoint, TargetingData.MainDefensePoint, null, frame));
            }

            return actions;
        }

        private void UpdateNeeded()
        {
            if (!OnlyActiveWhenNeeded) { return; }

            if ((EnemyData.SelfRace == Race.Zerg && MacroData.DesiredProductionCounts[UnitTypes.ZERG_HATCHERY] > BaseData.SelfBases.Count()) ||
                (EnemyData.SelfRace == Race.Terran && MacroData.DesiredProductionCounts[UnitTypes.TERRAN_COMMANDCENTER] > BaseData.SelfBases.Count()) ||
                 (EnemyData.SelfRace == Race.Protoss && MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_NEXUS] > BaseData.SelfBases.Count()))
            {
                if (!Needed)
                {
                    StealFromAttackTask();
                }
                Needed = true;
            }
            else
            {
                Needed = false;
                foreach (var commander in UnitCommanders)
                {
                    commander.UnitRole = UnitRole.None;
                    commander.Claimed = false;
                }
                UnitCommanders.Clear();
            }
        }

        void StealFromAttackTask()
        {
            if (NextBaseLocation == null) { return; }
            var vector = new Vector2(NextBaseLocation.X, NextBaseLocation.Y);
            if (MicroTaskData.ContainsKey(typeof(AttackTask).Name))
            {
                foreach (var commander in MicroTaskData[typeof(AttackTask).Name].UnitCommanders.OrderBy(c => Vector2.DistanceSquared(c.UnitCalculation.Position, vector)))
                {              
                    var unitType = commander.UnitCalculation.Unit.UnitType;
                    foreach (var desiredUnitClaim in DesiredUnitsClaims)
                    {
                        if ((uint)desiredUnitClaim.UnitType == unitType && !commander.UnitCalculation.Unit.IsHallucination && UnitCommanders.Count(u => u.UnitCalculation.Unit.UnitType == (uint)desiredUnitClaim.UnitType) < desiredUnitClaim.Count)
                        {
                            commander.Claimed = true;
                            commander.UnitRole = UnitRole.Defend;
                            UnitCommanders.Add(commander);
                        }
                    }
                }

                foreach (var commander in UnitCommanders)
                {
                    MicroTaskData[typeof(AttackTask).Name].StealUnit(commander);
                }
            }
            
        }

        private bool UpdateBaseLocation()
        {
            var baseCount = BaseData.SelfBases.Count();
            if (NextBaseLocation == null || BaseCountDuringLocation != baseCount)
            {
                var nextBase = BuildingService.GetNextBaseLocation();
                if (nextBase != null)
                {
                    NextBaseLocation = nextBase.Location;
                    BaseCountDuringLocation = baseCount;
                    return true;
                }
                return false;
            }
            else
            {
                return true;
            }
        }

        public override void RemoveDeadUnits(List<ulong> deadUnits)
        {
            foreach (var tag in deadUnits)
            {
                UnitCommanders.RemoveAll(c => c.UnitCalculation.Unit.Tag == tag);
            }
        }
    }
}