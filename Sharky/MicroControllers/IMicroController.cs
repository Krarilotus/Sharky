﻿using SC2APIProtocol;
using System.Collections.Generic;

namespace Sharky.MicroControllers
{
    public interface IMicroController
    {
        List<Action> Attack(List<UnitCommander> commanders, Point2D target, Point2D defensivePoint);
        List<Action> Retreat(List<UnitCommander> commanders, Point2D defensivePoint);
    }
}