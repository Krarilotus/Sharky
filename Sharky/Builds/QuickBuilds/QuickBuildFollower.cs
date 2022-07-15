﻿using SC2APIProtocol;
using Sharky.DefaultBot;
using System;

namespace Sharky.Builds.QuickBuilds
{
    public enum QuickBuildStepStatus
    {
        WaitingForWorkers,
        WaitingForProduction,
    }

    /// <summary>
    /// Simple class that follows quickbuild and builds what is needed accordingly.
    /// </summary>
    public class QuickBuildFollower
    {
        protected MacroData MacroData;
        protected BuildOptions BuildOptions;
        protected UnitCountService UnitCountService;
        protected EnemyData EnemyData;
        protected UnitTypeBuildClassifications UnitTypeBuildClassifications;
        protected SharkyUnitData SharkyUnitData;
        protected DebugService DebugService;

        protected QuickBuild Build;
        protected int InitialUnitCount = 0;
        protected QuickBuildStepStatus QuickBuildStepStatus = QuickBuildStepStatus.WaitingForWorkers;

        public QuickBuildFollower(DefaultSharkyBot defaultSharkyBot)
        {
            MacroData = defaultSharkyBot.MacroData;
            BuildOptions = defaultSharkyBot.BuildOptions;
            UnitCountService = defaultSharkyBot.UnitCountService;
            EnemyData = defaultSharkyBot.EnemyData;
            UnitTypeBuildClassifications = defaultSharkyBot.UnitTypeBuildClassifications;
            SharkyUnitData = defaultSharkyBot.SharkyUnitData;
            DebugService = defaultSharkyBot.DebugService;
        }

        public void Start(QuickBuild build)
        {
            Build = build;
        }

        public void BuildFrame(int frame)
        {
            var step = Build?.CurrentStep;

            if (step == null)
                return;

            DebugService.DrawText($"QuickBuild step: ({step.Value.Item1}) {step.Value.Item2} {step.Value.Item3} ({(QuickBuildStepStatus == QuickBuildStepStatus.WaitingForWorkers ? "waiting for workers" : "waiting for production")})");

            
            if (QuickBuildStepStatus == QuickBuildStepStatus.WaitingForWorkers)
            {
                if (MacroData.FoodUsed >= step.Value.Item1)
                {
                    AddStep(step);
                    QuickBuildStepStatus = QuickBuildStepStatus.WaitingForProduction;
                }
                else
                {
                    // TODO: race agnostic
                    MacroData.DesiredUnitCounts[UnitTypes.ZERG_DRONE] = UnitCountService.UnitsDoneAndInProgressCount(UnitTypes.ZERG_DRONE) + (step.Value.Item1 - MacroData.FoodUsed);
                }
            }

            if (QuickBuildStepStatus == QuickBuildStepStatus.WaitingForProduction)
            {
                if (IsProductionMet(step))
                {
                    QuickBuildStepStatus = QuickBuildStepStatus.WaitingForWorkers;
                    Build.Advance();
                }
                else
                {
                    if (step.Value.Item2 is UnitTypes unitType)
                    {
                        var unitCount = step.Value.Item3;
                        if (unitType == UnitTypes.ZERG_EXTRACTOR)
                        {
                            MacroData.DesiredGases = InitialUnitCount + unitCount;
                        } 
                        else if (UnitTypeBuildClassifications.ProducedUnits.Contains(step.Value.Item2))
                        {
                            MacroData.DesiredUnitCounts[unitType] = InitialUnitCount + unitCount;
                        }
                        else if (UnitTypeBuildClassifications.TechUnits.Contains(step.Value.Item2))
                        {
                            MacroData.DesiredTechCounts[unitType] = InitialUnitCount + unitCount;
                        }
                        else if (UnitTypeBuildClassifications.ProductionUnits.Contains(step.Value.Item2))
                        {
                            MacroData.DesiredProductionCounts[unitType] = InitialUnitCount + unitCount;
                        }
                        else if (UnitTypeBuildClassifications.MorphUnits.Contains(step.Value.Item2))
                        {
                            MacroData.DesiredMorphCounts[unitType] = InitialUnitCount + unitCount;
                        }
                    }
                    else if (step.Value.Item2 is Upgrades upgrade)
                    {
                        MacroData.DesiredUpgrades[upgrade] = true;
                    }
                }
            }
        }

        /*protected int WorkerCount
        {
            get
            {
                var workerType = UnitTypes.ZERG_DRONE;
                if (EnemyData.SelfRace == Race.Protoss)
                {
                    workerType = UnitTypes.PROTOSS_PROBE;
                }
                else if (EnemyData.SelfRace == Race.Terran)
                {
                    workerType = UnitTypes.TERRAN_SCV;
                }

                return UnitCountService.UnitsDoneAndInProgressCount(workerType);
            }
        }*/

        protected bool IsProductionMet((int, dynamic, int)? step)
        {
            if (step == null)
                return false;

            if (step.Value.Item2 is UnitTypes unitType)
            {
                var count = CountUnits(unitType);
                return count >= InitialUnitCount + (step.Value.Item3);
            }
            else if (step.Value.Item2 is Upgrades upgrade)
            {
                return UnitCountService.UpgradeDoneOrInProgress(upgrade);
            }

            return false;
        }

        protected void AddStep((int, dynamic, int)? step)
        {
            if (step == null)
                return;

            if (step.Value.Item2 is UnitTypes unitType)
            {
                InitialUnitCount = CountUnits(unitType);
            }
            else if (step.Value.Item2 is Upgrades upgrade)
            {
                MacroData.DesiredUpgrades[upgrade] = true;
            }
        }

        protected int CountUnits(UnitTypes unitType)
        {
            if (SharkyUnitData.TrainingData.ContainsKey(unitType))
                return UnitCountService.UnitsInProgressCount(unitType) + UnitCountService.EquivalentTypeCount(unitType);
            else
                return UnitCountService.BuildingsInProgressCount(unitType) + UnitCountService.EquivalentTypeCount(unitType);
        }
    }
}
