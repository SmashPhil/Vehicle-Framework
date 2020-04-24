using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;

namespace RimShips
{
    public enum StatUpgrade { Other, Cannon, Armor, Speed, CargoCapacity, FuelConsumptionRate, FuelCapacity}
    public class CompProperties_UpgradeTree : CompProperties
    {
        public CompProperties_UpgradeTree()
        {
            this.compClass = typeof(CompUpgradeTree);
        }

        public List<UpgradeNode> upgradesAvailable;

        public Vector2 displayUICoord;
        public Vector2 displayUISize;

    }
}
