using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace Vehicles
{
    public class CompVehicleTracks : ThingComp
    {
        public CompProperties_VehicleTracks Props => (CompProperties_VehicleTracks)props;
        public VehiclePawn ParentVehicle => (VehiclePawn)parent;

        public override void CompTick()
        {
            base.CompTick();

            if (ParentVehicle.vPather.MovingNow && !Props.tracks.NullOrEmpty())
            {
                if(ParentVehicle.Position != previousCell)
                {
                    previousCell = ParentVehicle.Position;
                    foreach(VehicleTrack track in Props.tracks)
                    {
                        IntVec2 rotationalSign = new IntVec2(1,1);

                        if (ParentVehicle.Rotation == Rot4.South)
                            rotationalSign.z = -1;
                        else if (ParentVehicle.Rotation == Rot4.West)
                            rotationalSign.x = -1;
                        else if (ParentVehicle.Rotation == Rot4.East)
                            rotationalSign.z = -1;

                        int xFirst = rotationalSign.x * (ParentVehicle.Rotation.IsHorizontal ? track.trackPoint.First.z : track.trackPoint.First.x);
                        int zFirst = rotationalSign.z * (ParentVehicle.Rotation.IsHorizontal ? track.trackPoint.First.x : track.trackPoint.First.z);
                        IntVec3 first = new IntVec3(ParentVehicle.Position.x - xFirst, ParentVehicle.Position.y, ParentVehicle.Position.z - zFirst);
                        int xSecond = rotationalSign.x * (ParentVehicle.Rotation.IsHorizontal ? track.trackPoint.Second.z : track.trackPoint.Second.x);
                        int zSecond = rotationalSign.z * (ParentVehicle.Rotation.IsHorizontal ? track.trackPoint.Second.x : track.trackPoint.Second.z);
                        IntVec3 second = new IntVec3(ParentVehicle.Position.x - xSecond, ParentVehicle.Position.y, ParentVehicle.Position.z - zSecond);

                        DebugVehicleTracksDrawer(first,second);
                        foreach(IntVec3 cell in CellRect.FromLimits(first, second).Cells)
                        {
                            List<Thing> things = ParentVehicle.Map.thingGrid.ThingsListAt(cell);
                            for(int i = things.Count - 1; i >= 0; i--)
                            {
                                Thing thing = things[i];
                                if (thing == ParentVehicle)
                                    continue;
                                if(track.destroyableCategories.Contains(thing.def.category))
                                {
                                    try
                                    {
                                        if (thing.def.category == ThingCategory.Pawn)
                                            thing.TakeDamage(new DamageInfo(DamageDefOf.Crush, 200));
                                        else
                                            thing.Destroy();
                                    }
                                    catch
                                    {
                                        //do nothing right now
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        internal void DebugVehicleTracksDrawer(IntVec3 c1, IntVec3 c2)
        {
            if(RimShipMod.mod.settings.debugDrawVehicleTracks)
            {
                foreach(VehicleTrack track in Props.tracks)
                {
                    foreach(IntVec3 cell in CellRect.FromLimits(c1, c2).Cells)
                    {
                        GenDraw.DrawFieldEdges(new List<IntVec3>() { cell }, Color.red);
                    }
                }
            }
        }

        private IntVec3 previousCell;
    }
}
