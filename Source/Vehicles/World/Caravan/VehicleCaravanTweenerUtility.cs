using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.Planet;
using Verse;
using UnityEngine;

namespace Vehicles
{
	public static class VehicleCaravanTweenerUtility
	{
		private const float BaseRadius = 0.15f;
		private const float BaseDistToCollide = 0.2f;

		public static Vector3 PatherTweenedPosRoot(VehicleCaravan caravan)
		{
			WorldGrid worldGrid = Find.WorldGrid;
			if (!caravan.Spawned)
			{
				return worldGrid.GetTileCenter(caravan.Tile);
			}
			if (caravan.vPather.Moving)
			{
				float num;
				if (!caravan.vPather.IsNextTilePassable())
				{
					num = 0f;
				}
				else
				{
					num = 1f - caravan.vPather.nextTileCostLeft / caravan.vPather.nextTileCostTotal;
				}
				int tileID;
				if (caravan.vPather.nextTile == caravan.Tile && caravan.vPather.previousTileForDrawingIfInDoubt != -1)
				{
					tileID = caravan.vPather.previousTileForDrawingIfInDoubt;
				}
				else
				{
					tileID = caravan.Tile;
				}
				return worldGrid.GetTileCenter(caravan.vPather.nextTile) * num + worldGrid.GetTileCenter(tileID) * (1f - num);
			}
			return worldGrid.GetTileCenter(caravan.Tile);
		}

		public static Vector3 CaravanCollisionPosOffsetFor(VehicleCaravan caravan)
		{
			if (!caravan.Spawned)
			{
				return Vector3.zero;
			}
			bool flag = caravan.Spawned && caravan.vPather.Moving;
			float d = BaseRadius * Find.WorldGrid.averageTileSize;
			if (!flag || caravan.vPather.nextTile == caravan.vPather.Destination)
			{
				int num;
				if (flag)
				{
					num = caravan.vPather.nextTile;
				}
				else
				{
					num = caravan.Tile;
				}
				GetCaravansStandingAtOrAboutToStandAt(num, out int num2, out int vertexIndex, caravan);
				if (num2 == 0)
				{
					return Vector3.zero;
				}
				return WorldRendererUtility.ProjectOnQuadTangentialToPlanet(Find.WorldGrid.GetTileCenter(num), GenGeo.RegularPolygonVertexPosition(num2, vertexIndex) * d);
			}
			else
			{
				if (DrawPosCollides(caravan))
				{
					Rand.PushState();
					Rand.Seed = caravan.ID;
					float f = Rand.Range(0f, 360f);
					Rand.PopState();
					Vector2 point = new Vector2(Mathf.Cos(f), Mathf.Sin(f)) * d;
					return WorldRendererUtility.ProjectOnQuadTangentialToPlanet(PatherTweenedPosRoot(caravan), point);
				}
				return Vector3.zero;
			}
		}

		private static void GetCaravansStandingAtOrAboutToStandAt(int tile, out int caravansCount, out int caravansWithLowerIdCount, VehicleCaravan forCaravan)
		{
			caravansCount = 0;
			caravansWithLowerIdCount = 0;
			List<VehicleCaravan> caravans = Find.WorldObjects.Caravans.Where(v => v is VehicleCaravan).Cast<VehicleCaravan>().ToList();
			int i = 0;
			while (i < caravans.Count)
			{
				VehicleCaravan caravan = caravans[i];
				if (caravan.Tile != tile)
				{
					if (caravan.vPather.Moving && caravan.vPather.nextTile == caravan.vPather.Destination)
					{
						if (caravan.vPather.Destination == tile)
						{
							goto IL_68;
						}
					}
				}
				else if (!caravan.vPather.Moving)
				{
					goto IL_68;
				}
				IL_82:
				i++;
				continue;
				IL_68:
				caravansCount++;
				if (caravan.ID < forCaravan.ID)
				{
					caravansWithLowerIdCount++;
					goto IL_82;
				}
				goto IL_82;
			}
		}

		private static bool DrawPosCollides(VehicleCaravan caravan)
		{
			Vector3 a = PatherTweenedPosRoot(caravan);
			float num = Find.WorldGrid.averageTileSize * BaseDistToCollide;
			List<VehicleCaravan> caravans = Find.WorldObjects.Caravans.Where(c => c is VehicleCaravan).Cast<VehicleCaravan>().ToList();
			for (int i = 0; i < caravans.Count; i++)
			{
				VehicleCaravan caravan2 = caravans[i];
				if (caravan2 != caravan && Vector3.Distance(a, PatherTweenedPosRoot(caravan2)) < num)
				{
					return true;
				}
			}
			return false;
		}
	}
}
