using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Harmony;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using UnityEngine;
using UnityEngine.AI;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;


namespace RimShips.AI
{
    public sealed class MapExtension : IExposable
    {
        public MapExtension(Map map)
        {
            this.map = map;
        }

        public MapExtension MapExt
        {
            get
            {
                return this;
            }
        }

        public ShipPathGrid getShipPathGrid
        {
            get
            {
                return shipPathGrid;
            }
        }

        public ShipPathFinder getShipPathFinder
        {
            get
            {
                return shipPathFinder;
            }
        }

        public ShipReachability getShipReachability
        {
            get
            {
                return shipReachability;
            }
        }



        public void ConstructComponents()
        {
            this.shipPathGrid = new ShipPathGrid(this.map);
            this.shipPathFinder = new ShipPathFinder(this.map, this);
            this.shipReachability = new ShipReachability(this.map, this);
        }

        public void ExposeData()
        {
            Scribe_Deep.Look<ShipPathGrid>(ref this.shipPathGrid, "shipPathGrid", this.map, false);
            Scribe_Deep.Look<ShipPathFinder>(ref this.shipPathFinder, "shipPathFinder", this.map, this, false);
            Scribe_Deep.Look<ShipReachability>(ref this.shipReachability, "shipReachability", this.map, this, false);
            if(Scribe.mode == LoadSaveMode.LoadingVars)
            {
                this.ConstructComponents();
            }
        }

        private ShipPathGrid shipPathGrid;

        private ShipPathFinder shipPathFinder;

        private ShipReachability shipReachability;

        private readonly Map map;
    }
}
