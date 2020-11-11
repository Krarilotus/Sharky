﻿using SC2APIProtocol;
using Sharky.Managers;
using Sharky.Managers.Protoss;
using System.Collections.Generic;

namespace Sharky.Builds.Protoss
{
    public class Robo : ProtossSharkyBuild
    {
        Race EnemyRace = Race.Terran;

        public Robo(BuildOptions buildOptions, MacroData macroData, UnitManager unitManager, AttackData attackData, IChatManager chatManager, NexusManager nexusManager) : base(buildOptions, macroData, unitManager, attackData, chatManager, nexusManager)
        {
        }

        public override void StartBuild(int frame)
        {
            base.StartBuild(frame);

            BuildOptions.StrictGasCount = true;
            MacroData.DesiredGases = 2;

            NexusManager.ChronodUpgrades = new HashSet<Upgrades>
            {
                Upgrades.WARPGATERESEARCH
            };

            NexusManager.ChronodUnits = new HashSet<UnitTypes>
            {
                UnitTypes.PROTOSS_PROBE,
            };

            MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_NEXUS] = 1;

            // TODO: be able to add MicroTasks
            //if (EnemyRace == SC2APIProtocol.Race.Protoss)
            //{
            //    var defenseTask = new DefenseSquadTask(build.Main, UnitTypes.STALKER)
            //}
            //else
            //{
            //    var defenseTask = new DefenseSquadTask(build.Main, UnitTypes.ADEPT)
            //}
            // TODO: EnemyRace
        }

        public override void OnFrame(ResponseObservation observation)
        {
            if (EnemyRace == SC2APIProtocol.Race.Protoss)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] = 1;
                }
            }
            else
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ADEPT] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ADEPT] = 1;
                }
            }

            if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] < 1)
            {
                MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] = 1;
            }
            if (UnitManager.Completed(UnitTypes.PROTOSS_GATEWAY) > 0)
            {
                if (MacroData.DesiredTechCounts[UnitTypes.PROTOSS_CYBERNETICSCORE] < 1)
                {
                    MacroData.DesiredTechCounts[UnitTypes.PROTOSS_CYBERNETICSCORE] = 1;
                }
            }

            if (UnitManager.Count(UnitTypes.PROTOSS_ROBOTICSFACILITY) > 0 && UnitManager.Count(UnitTypes.PROTOSS_NEXUS) >= 2)
            {
                // TODO: MacroData.PylonsAtEveryExpansion = true; 
                if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] < 2)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] = 2;
                }

                if (UnitManager.Count(UnitTypes.PROTOSS_STALKER) + UnitManager.Count(UnitTypes.PROTOSS_ADEPT) > 0)
                {
                    MacroData.DesiredUpgrades[Upgrades.WARPGATERESEARCH] = true;
                    if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] < 3)
                    {
                        MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_STALKER] = 3;
                    }
                }
            }

            if (UnitManager.Count(UnitTypes.PROTOSS_ROBOTICSFACILITY) < 1 && UnitManager.Count(UnitTypes.PROTOSS_STALKER) + UnitManager.Count(UnitTypes.PROTOSS_ADEPT) > 0)
            {
                MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_ROBOTICSFACILITY] = 1;
            }

            if (EnemyRace == SC2APIProtocol.Race.Terran)
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_OBSERVER] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_OBSERVER] = 1;
                }
            }
            else
            {
                if (MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_IMMORTAL] < 1)
                {
                    MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_IMMORTAL] = 1;
                }
            }

            if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_NEXUS] < 2)
            {
                MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_NEXUS] = 2;
            }
        }

        public override bool Transition()
        {
            return UnitManager.Completed(UnitTypes.PROTOSS_ROBOTICSFACILITY) > 0 && UnitManager.Completed(UnitTypes.PROTOSS_NEXUS) > 0;
        }
    }
}