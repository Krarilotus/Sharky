﻿using SC2APIProtocol;
using Sharky.Managers;
using System.Linq;
using System.Numerics;

namespace Sharky.Builds.BuildingPlacement
{
    public class BuildingPlacement : IBuildingPlacement
    {
        IBuildingPlacement ProtossBuildingPlacement;
        IBaseManager BaseManager;
        UnitManager UnitManager;
        BuildingService BuildingService;

        public BuildingPlacement(IBuildingPlacement protossBuildingPlacement, IBaseManager baseManager, UnitManager unitManager, BuildingService buildingService)
        {
            ProtossBuildingPlacement = protossBuildingPlacement;
            BaseManager = baseManager;
            UnitManager = unitManager;
            BuildingService = buildingService;
        }

        public Point2D FindPlacement(Point2D target, UnitTypes unitType, int size)
        {
            if (unitType == UnitTypes.PROTOSS_NEXUS || unitType == UnitTypes.TERRAN_COMMANDCENTER || unitType == UnitTypes.ZERG_HATCHERY)
            {
                return GetResourceCenterLocation();
            }

            return ProtossBuildingPlacement.FindPlacement(target, unitType, size);
        }

        private Point2D GetResourceCenterLocation()
        {
            var resourceCenters = UnitManager.SelfUnits.Values.Where(u => u.UnitClassifications.Contains(UnitClassification.ResourceCenter));
            var openBases = BaseManager.BaseLocations.Where(b => !resourceCenters.Any(r => Vector2.DistanceSquared(new Vector2(r.Unit.Pos.X, r.Unit.Pos.Y), new Vector2(b.Location.X, b.Location.Y)) < 25));

            foreach (var openBase in openBases)
            {
                if (BuildingService.AreaBuildable(openBase.Location.X, openBase.Location.Y, 2) && !BuildingService.Blocked(openBase.Location.X, openBase.Location.Y, 2))
                {
                    return openBase.Location;
                }
              
            }
            return null;
        }
    }
}