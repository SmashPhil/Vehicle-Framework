using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;
using SmashTools;

namespace Vehicles
{
	public class LaunchRestriction_Runway : LaunchRestriction
	{
		private static List<IntVec3> invalidCells = new List<IntVec3>();

		public Color colorValid = Color.white;
		public Color colorInvalid = Color.red;

		/// <summary>
		/// x = min, z = max
		/// </summary>
		public IntVec2 width = IntVec2.Zero;
		public IntVec2 height = IntVec2.Zero;

		public SimpleDictionary<ThingCategory, float> categories;
		
		private CellRect RunwayRect(IntVec3 position, Rot4 rot)
		{
			IntVec2 adjWidth = width;
			IntVec2 adjHeight = height;
			//Flip direction for west / south
			if (rot == Rot4.West)
			{
				adjWidth.x = -width.x;
				adjWidth.z = -width.z;
			}
			else if (rot == Rot4.South)
			{
				adjHeight.x = -height.x;
				adjHeight.z = -height.z;
			}

			CellRect cellRect = CellRect.FromLimits(position.x + adjWidth.x, position.z + adjHeight.x, position.x + adjWidth.z, position.z + adjHeight.z);
			return cellRect;
		}

		public override bool CanStartProtocol(VehiclePawn vehicle, Map map, IntVec3 position, Rot4 rot)
		{
			//Must null check map for immediately spawned & selected vehicles
			if (categories.NullOrEmpty() && map is null)
			{
				return true;
			}
			CellRect cellRect = RunwayRect(position, rot);
			foreach (IntVec3 cell in cellRect)
			{
				if (!cell.InBounds(map) || map.thingGrid.ThingsListAtFast(cell).Any(thing => InvalidFor(thing)))
				{
					return false;
				}
			}
			return true;
		}

		public override void DrawRestrictionsTargeter(VehiclePawn vehicle, Map map, IntVec3 position, Rot4 rot)
		{
			CellRect cellRect = RunwayRect(position, rot);

			//Must null check map for immediately spawned & selected vehicles
			if (!categories.NullOrEmpty() && map != null)
			{
				invalidCells.Clear();
				foreach (IntVec3 cell in cellRect)
				{
					if (cell.InBounds(map) && map.thingGrid.ThingsListAtFast(cell).Any(thing => InvalidFor(thing)))
					{
						invalidCells.Add(cell);
					}
				}
			}

			GenDraw.DrawFieldEdges(cellRect.ToList(), colorValid);
			if (!invalidCells.NullOrEmpty())
			{
				GenDraw.DrawFieldEdges(invalidCells, colorInvalid);
			}
		}

		private bool InvalidFor(Thing thing)
		{
			return categories.ContainsKey(thing.def.category) && thing.def.fillPercent >= categories[thing.def.category];
		}
	}
}
