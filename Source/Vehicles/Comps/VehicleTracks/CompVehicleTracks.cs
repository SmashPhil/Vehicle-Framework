using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace Vehicles
{
	public class CompVehicleTracks : VehicleAIComp
	{
		public CompProperties_VehicleTracks Props => (CompProperties_VehicleTracks)props;
		public VehiclePawn ParentVehicle => (VehiclePawn)parent;

		private List<Pair<IntVec3, IntVec3>> drawTracks = new List<Pair<IntVec3, IntVec3>>();

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			drawTracks = new List<Pair<IntVec3, IntVec3>>();
		}

		public void TakeStep()
		{
			drawTracks.Clear();
			foreach(VehicleTrack track in Props.tracks)
			{
				IntVec2 rotationalSign = new IntVec2(1,1);

				if (ParentVehicle.Rotation == Rot4.South)
					rotationalSign.z = -1;
				else if (ParentVehicle.Rotation == Rot4.West)
					rotationalSign.x = -1;
				else if (ParentVehicle.Rotation == Rot4.East)
					rotationalSign.z = -1;

				IntVec2 start = track.trackPoint.First;
				IntVec2 end = track.trackPoint.Second;
				start.z -= 1;
				end.z -= 1;

				int xFirst = rotationalSign.x * (ParentVehicle.Rotation.IsHorizontal ? start.z : start.x);
				int zFirst = rotationalSign.z * (ParentVehicle.Rotation.IsHorizontal ? start.x : start.z);
				IntVec3 first = new IntVec3(ParentVehicle.Position.x - xFirst, ParentVehicle.Position.y, ParentVehicle.Position.z - zFirst);
				int xSecond = rotationalSign.x * (ParentVehicle.Rotation.IsHorizontal ? end.z : end.x);
				int zSecond = rotationalSign.z * (ParentVehicle.Rotation.IsHorizontal ? end.x : end.z);
				IntVec3 second = new IntVec3(ParentVehicle.Position.x - xSecond, ParentVehicle.Position.y, ParentVehicle.Position.z - zSecond);

				drawTracks.Add(new Pair<IntVec3, IntVec3>(first, second));

				foreach (IntVec3 cell in CellRect.FromLimits(first, second).Cells)
				{
					if (!cell.InBounds(ParentVehicle.Map))
					{
						continue;
					}

					List<Thing> things = ParentVehicle.Map.thingGrid.ThingsListAtFast(cell);
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
								{
									thing.TakeDamage(new DamageInfo(DamageDefOf.Crush, 200));
								}
								else
								{
									thing.Destroy();
								}
							}
							catch
							{
								//do nothing right now
							}
						}
					}
				}

				foreach (IntVec3 cell in WarningRect().Cells)
				{
					if (!cell.InBounds(ParentVehicle.Map))
					{
						continue;
					}
					if (ParentVehicle.Map.thingGrid.ThingsListAtFast(cell).Any(t => t is Pawn))
					{
						GenVehicleTracks.NotifyNearbyPawnsPathOfVehicleTrack(ParentVehicle);
					}
				}
			}
		}

		public override void CompTick()
		{
			base.CompTick();

			if (ParentVehicle.Spawned && !Props.tracks.NullOrEmpty())
			{
				if(VehicleMod.settings.debug.debugDrawVehicleTracks)
				{
					foreach(var trackPoints in drawTracks)
					{
						DebugVehicleTracksDrawer(trackPoints.First, trackPoints.Second);
					}
				}
			}
		}

		private void DebugVehicleTracksDrawer(IntVec3 c1, IntVec3 c2)
		{
			foreach(VehicleTrack track in Props.tracks)
			{
				foreach(IntVec3 cell in CellRect.FromLimits(c1, c2).Cells)
				{
					GenDraw.DrawFieldEdges(new List<IntVec3>() { cell }, Color.red);
				}
			}

			foreach (IntVec3 cell in WarningRect().Cells)
			{
				GenDraw.DrawFieldEdges(new List<IntVec3>() { cell }, Color.red);
			}
		}

		private CellRect WarningRect()
		{
			int opDirection = 1;
			if (ParentVehicle.Rotation == Rot4.South)
				opDirection = -1;
			else if (ParentVehicle.Rotation == Rot4.West)
				opDirection = -1;

			int x = ParentVehicle.Position.x;
			int z = ParentVehicle.Position.z;

			if (ParentVehicle.Rotation.IsHorizontal)
			{
				x += ParentVehicle.def.Size.z * opDirection;
			}
			else
			{
				z += ParentVehicle.def.Size.z * opDirection;
			}

			int sizeX = ParentVehicle.def.Size.x;
			int sizeZ = ParentVehicle.def.Size.z;
			if (ParentVehicle.Rotation.IsHorizontal)
			{
				int tmp = sizeX;
				sizeX = sizeZ;
				sizeZ = tmp;
			}
			return CellRect.CenteredOn(new IntVec3(x, 0, z), sizeX, sizeZ);
		}
	}
}
