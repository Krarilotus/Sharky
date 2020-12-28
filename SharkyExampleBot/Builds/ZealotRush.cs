﻿using SC2APIProtocol;
using Sharky;
using Sharky.Builds;
using Sharky.Builds.BuildChoosing;
using Sharky.Chat;
using Sharky.Managers;
using Sharky.MicroTasks;
using System.Collections.Generic;

namespace SharkyExampleBot.Builds
{
    public class ZealotRush : ProtossSharkyBuild
    {
        MicroManager MicroManager;
        WorkerScoutTask WorkerScoutTask;
        ProxyScoutTask ProxyScoutTask;

        bool OpeningAttackChatSent;
        bool Scouted;

        public ZealotRush(BuildOptions buildOptions, MacroData macroData, ActiveUnitData activeUnitData, AttackData attackData, ChatService chatService, ChronoData chronoData, ICounterTransitioner counterTransitioner, MicroManager microManager, UnitCountService unitCountService) 
            : base(buildOptions, macroData, activeUnitData, attackData, chatService, chronoData, counterTransitioner, unitCountService)
        {
            MicroManager = microManager;
            OpeningAttackChatSent = false;
        }

        public override void StartBuild(int frame)
        {
            base.StartBuild(frame);

            BuildOptions.StrictGasCount = true;

            MacroData.DesiredUnitCounts[UnitTypes.PROTOSS_ZEALOT] = 100;

            ChronoData.ChronodUnits = new HashSet<UnitTypes>
            {
                UnitTypes.PROTOSS_ZEALOT
            };

            if (MicroManager.MicroTasks.ContainsKey("WorkerScoutTask"))
            {
                WorkerScoutTask = (WorkerScoutTask)MicroManager.MicroTasks["WorkerScoutTask"];
            }
            if (MicroManager.MicroTasks.ContainsKey("ProxyScoutTask"))
            {
                ProxyScoutTask = (ProxyScoutTask)MicroManager.MicroTasks["ProxyScoutTask"];
            }
        }

        public override void OnFrame(ResponseObservation observation)
        {
            if (UnitCountService.Completed(UnitTypes.PROTOSS_PYLON) > 0)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] < 2)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] = 2;
                }

                if (!Scouted)
                {
                    if (WorkerScoutTask != null)
                    {
                        WorkerScoutTask.Enable();
                    }
                    if (ProxyScoutTask != null)
                    {
                        ProxyScoutTask.Enable();
                    }
                    Scouted = true;
                }
            }
            if (UnitCountService.Completed(UnitTypes.PROTOSS_PYLON) >= 2)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] < 4)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.PROTOSS_GATEWAY] = 4;
                }
            }

            if (!OpeningAttackChatSent && MacroData.FoodArmy > 10)
            {
                ChatService.SendChatType("ZealotRush-FirstAttack");
                OpeningAttackChatSent = true;
            }
        }

        public override List<string> CounterTransition(int frame)
        {
            return new List<string>();
        }
    }
}
