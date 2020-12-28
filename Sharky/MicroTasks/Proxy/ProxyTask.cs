﻿using SC2APIProtocol;
using Sharky.Managers;
using Sharky.MicroControllers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sharky.MicroTasks
{
    public class ProxyTask : MicroTask
    {
        UnitDataManager UnitDataManager;
        MacroData MacroData;
        MicroManager MicroManager;
        DebugManager DebugManager;
        IIndividualMicroController IndividualMicroController;

        public string ProxyName { get; set; }

        bool started { get; set; }

        public ProxyTask(UnitDataManager unitDataManager, bool enabled, float priority, MacroData macroData, string proxyName, MicroManager microManager, DebugManager debugManager, IIndividualMicroController individualMicroController)
        {
            UnitDataManager = unitDataManager;
            Priority = priority;
            MacroData = macroData;
            ProxyName = proxyName;
            MicroManager = microManager;
            DebugManager = debugManager;
            IndividualMicroController = individualMicroController;

            UnitCommanders = new List<UnitCommander>();
            Enabled = enabled;
        }

        public override void Enable()
        {
            if (MicroManager.MicroTasks.ContainsKey("MiningTask"))
            {
                MicroManager.MicroTasks["MiningTask"].ResetClaimedUnits();
            }
            Enabled = true;
            started = false;
        }

        public override void Disable()
        {
            foreach (var commander in UnitCommanders)
            {
                commander.Claimed = false;
            }
            UnitCommanders = new List<UnitCommander>();

            Enabled = false;
            MacroData.Proxies[ProxyName].Enabled = false;
        }

        public override void ClaimUnits(ConcurrentDictionary<ulong, UnitCommander> commanders)
        {
            if (UnitCommanders.Count() == 0)
            {
                if (started)
                {
                    Disable();
                    return;
                }

                foreach (var commander in commanders.OrderBy(c => c.Value.UnitCalculation.Unit.BuffIds.Count()))
                {
                    if (!commander.Value.Claimed && commander.Value.UnitCalculation.UnitClassifications.Contains(UnitClassification.Worker))
                    {
                        if (commander.Value.UnitCalculation.Unit.Orders.Any(o => !UnitDataManager.MiningAbilities.Contains((Abilities)o.AbilityId)))
                        {
                        }
                        else
                        {
                            commander.Value.Claimed = true;
                            UnitCommanders.Add(commander.Value);
                            started = true;
                            return;
                        }
                    }
                }
            }
        }

        public override IEnumerable<SC2APIProtocol.Action> PerformActions(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();

            if (MacroData.Proxies.ContainsKey(ProxyName))
            {
                commands.AddRange(MoveToProxyLocation(frame));
            }

            return commands;
        }

        IEnumerable<SC2APIProtocol.Action> MoveToProxyLocation(int frame)
        {
            var commands = new List<SC2APIProtocol.Action>();

            foreach (var commander in UnitCommanders.Where(c => !c.UnitCalculation.Unit.Orders.Any(o => UnitDataManager.BuildingData.Values.Any(b => (uint)b.Ability == o.AbilityId))))
            {
                if (Vector2.DistanceSquared(new Vector2(MacroData.Proxies[ProxyName].Location.X, MacroData.Proxies[ProxyName].Location.Y), new Vector2(commander.UnitCalculation.Unit.Pos.X, commander.UnitCalculation.Unit.Pos.Y)) > MacroData.Proxies[ProxyName].MaximumBuildingDistance)
                {
                    SC2APIProtocol.Action action;
                    if (IndividualMicroController.NavigateToTarget(commander, MacroData.Proxies[ProxyName].Location, null, null, Formation.Normal, frame, out action))
                    {
                        if (action != null)
                        {
                            commands.Add(action);
                        }
                    }
                }
            }

            return commands;
        }
    }
}