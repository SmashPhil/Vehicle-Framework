using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public static class DebugHelper
	{
		public static readonly PathDebugData<DebugRegionType> Local = new PathDebugData<DebugRegionType>();
		public static readonly PathDebugData<WorldPathingDebugType> World = new PathDebugData<WorldPathingDebugType>();

		/// <summary>
		/// Draw settlement debug lines that show original locations before settlement was pushed to the coastline
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		public static void DebugDrawSettlement(int from, int to)
		{
			PeaceTalks o = (PeaceTalks)WorldObjectMaker.MakeWorldObject(WorldObjectDefOfVehicles.DebugSettlement);
			o.Tile = from;
			o.SetFaction(Faction.OfMechanoids);
			Find.WorldObjects.Add(o);
			if (VehicleHarmony.drawPaths)
			{
				VehicleHarmony.debugLines.Add(Find.WorldPathFinder.FindPath(from, to, null, null));
			}
		}

		public static List<Toggle> DebugToggles<T>(VehicleDef vehicleDef, PathDebugData<T> debugData) where T : Enum
		{
			List<Toggle> toggles = new List<Toggle>();
			if (Enum.GetUnderlyingType(typeof(T)) != typeof(int))
			{
				Log.Error($"Cannot generate DebugToggles for enum type {typeof(T)}. Must be 32bit int to avoid overflow.");
				return toggles;
			}
			
			foreach (T @enum in Enum.GetValues(typeof(T)))
			{
				bool flags = typeof(T).IsDefined(typeof(FlagsAttribute), false);
				Toggle toggle = new Toggle(@enum.ToString(), stateGetter: delegate ()
				{
					bool matchingDef = debugData.VehicleDef == vehicleDef;
					return matchingDef && ((flags && debugData.DebugType.HasFlag(@enum)) || debugData.DebugType.Equals(@enum));
				}, stateSetter: delegate (bool value)
				{
					debugData.VehicleDef = vehicleDef;
					if (flags)
					{
						if (value)
						{
							debugData.DebugType = (T)Enum.ToObject(typeof(T), Convert.ToInt32(debugData.DebugType) | Convert.ToInt32(@enum));
						}
						else
						{
							debugData.DebugType = (T)Enum.ToObject(typeof(T), Convert.ToInt32(debugData.DebugType) & ~Convert.ToInt32(@enum));
						}
					}
					else if (value)
					{
						debugData.DebugType = @enum;
					}
				});

				toggles.Add(toggle);
			}

			return toggles;
		}

		public static IEnumerable<Toggle> RegionToggles(VehicleDef vehicleDef)
		{
			yield break;
			//yield return new Toggle(DebugRegionType.Regions.ToString(), () => drawRegionsFor == vehicleDef && debugRegionType.HasFlag(DebugRegionType.Regions), delegate (bool value)
			//{
			//	drawRegionsFor = vehicleDef;
			//	if (value)
			//	{
			//		debugRegionType |= DebugRegionType.Regions;
			//	}
			//	else
			//	{
			//		debugRegionType &= ~DebugRegionType.Regions;
			//	}
			//});
			//yield return new Toggle(DebugRegionType.Links.ToString(), () => drawRegionsFor == vehicleDef && debugRegionType.HasFlag(DebugRegionType.Links), delegate (bool value)
			//{
			//	drawRegionsFor = vehicleDef;
			//	if (value)
			//	{
			//		debugRegionType |= DebugRegionType.Links;
			//	}
			//	else
			//	{
			//		debugRegionType &= ~DebugRegionType.Links;
			//	}
			//});
			//yield return new Toggle(DebugRegionType.Things.ToString(), () => drawRegionsFor == vehicleDef && debugRegionType.HasFlag(DebugRegionType.Things), delegate (bool value)
			//{
			//	drawRegionsFor = vehicleDef;
			//	if (value)
			//	{
			//		debugRegionType |= DebugRegionType.Things;
			//	}
			//	else
			//	{
			//		debugRegionType &= ~DebugRegionType.Things;
			//	}
			//});
			//yield return new Toggle(DebugRegionType.PathCosts.ToString(), () => drawRegionsFor == vehicleDef && debugRegionType.HasFlag(DebugRegionType.PathCosts), delegate (bool value)
			//{
			//	drawRegionsFor = vehicleDef;
			//	if (value)
			//	{
			//		debugRegionType |= DebugRegionType.PathCosts;
			//	}
			//	else
			//	{
			//		debugRegionType &= ~DebugRegionType.PathCosts;
			//	}
			//});
		}

		/// <summary>
		/// Draw water regions to show if they are valid and initialized
		/// </summary>
		/// <param name="map"></param>
		public static void DebugDrawVehicleRegion(Map map)
		{
			if (Local.VehicleDef != null)
			{
				map.GetCachedMapComponent<VehicleMapping>()[Local.VehicleDef].VehicleRegionGrid.DebugDraw(Local.DebugType);
			}
		}

		/// <summary>
		/// Draw path costs overlay on GUI
		/// </summary>
		/// <param name="map"></param>
		public static void DebugDrawVehiclePathCostsOverlay(Map map)
		{
			if (Local.VehicleDef != null)
			{
				map.GetCachedMapComponent<VehicleMapping>()[Local.VehicleDef].VehicleRegionGrid.DebugOnGUI(Local.DebugType);
			}
		}

		public class PathDebugData<T> where T : Enum
		{
			private VehicleDef vehicleDef;
			private T debugType;

			public VehicleDef VehicleDef { get => vehicleDef; set => vehicleDef = value; }

			public T DebugType { get => debugType; set => debugType = value; }
		}
	}
}
