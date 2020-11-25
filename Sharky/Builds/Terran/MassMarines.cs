﻿using SC2APIProtocol;
using Sharky.Managers;

namespace Sharky.Builds.Terran
{
    public class MassMarines : TerranSharkyBuild
    {
        public MassMarines(BuildOptions buildOptions, MacroData macroData, UnitManager unitManager, AttackData attackData, IChatManager chatManager) : base(buildOptions, macroData, unitManager, attackData, chatManager)
        {

        }

        public override void StartBuild(int frame)
        {
            base.StartBuild(frame);

            BuildOptions.StrictGasCount = true;
            MacroData.DesiredGases = 0;

            AttackData.ArmyFoodAttack = 5;

            MacroData.DesiredUnitCounts[UnitTypes.TERRAN_MARINE] = 150;
        }

        public override void OnFrame(ResponseObservation observation)
        {
            if (MacroData.FoodUsed >= 15)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.TERRAN_BARRACKS] < 5)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.TERRAN_BARRACKS] = 5;
                }
            }

            if (MacroData.FoodUsed >= 50)
            {
                if (MacroData.DesiredProductionCounts[UnitTypes.TERRAN_COMMANDCENTER] < 2)
                {
                    MacroData.DesiredProductionCounts[UnitTypes.TERRAN_COMMANDCENTER] = 2;
                }
            }
        }

        public override bool Transition(int frame)
        {
            return MacroData.FoodUsed > 50 && UnitManager.EquivalentTypeCompleted(UnitTypes.TERRAN_COMMANDCENTER) > 1;
        }
    }
}