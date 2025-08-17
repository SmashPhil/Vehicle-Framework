using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Vehicles.World;

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
		if (caravan.vehiclePather.Moving)
		{
			float cost = caravan.vehiclePather.IsNextTilePassable() ?
				1f - caravan.vehiclePather.nextTileCostLeft /
				caravan.vehiclePather.nextTileCostTotal :
				0;
			int tileID;
			if (caravan.vehiclePather.NextTile == caravan.Tile &&
				caravan.vehiclePather.previousTileForDrawingIfInDoubt != -1)
			{
				tileID = caravan.vehiclePather.previousTileForDrawingIfInDoubt;
			}
			else
			{
				tileID = caravan.Tile;
			}
			return worldGrid.GetTileCenter(caravan.vehiclePather.NextTile) * cost +
				worldGrid.GetTileCenter(tileID) * (1f - cost);
		}
		return worldGrid.GetTileCenter(caravan.Tile);
	}

	public static Vector3 CaravanCollisionPosOffsetFor(VehicleCaravan caravan)
	{
		if (!caravan.Spawned)
			return Vector3.zero;

		bool spawnedAndMoving = caravan.Spawned && caravan.vehiclePather.Moving;
		float d = BaseRadius * Find.WorldGrid.AverageTileSize;
		if (!spawnedAndMoving || caravan.vehiclePather.NextTile == caravan.vehiclePather.Destination)
		{
			PlanetTile tile = spawnedAndMoving ? caravan.vehiclePather.NextTile : caravan.Tile;
			GetCaravansStandingAtOrAboutToStandAt(tile, out int caravansCount, out int vertexIndex,
				caravan);
			if (caravansCount == 0)
				return Vector3.zero;
			return WorldRendererUtility.ProjectOnQuadTangentialToPlanet(
				Find.WorldGrid.GetTileCenter(tile),
				GenGeo.RegularPolygonVertexPosition(caravansCount, vertexIndex) * d);
		}

		if (DrawPosCollides(caravan))
		{
			Rand.PushState();
			Rand.Seed = caravan.ID;
			float f = Rand.Range(0f, 360f);
			Rand.PopState();
			Vector2 point = new Vector2(Mathf.Cos(f), Mathf.Sin(f)) * d;
			return WorldRendererUtility.ProjectOnQuadTangentialToPlanet(PatherTweenedPosRoot(caravan),
				point);
		}
		return Vector3.zero;
	}

	private static void GetCaravansStandingAtOrAboutToStandAt(int tile, out int caravansCount,
		out int caravansWithLowerIdCount, VehicleCaravan forCaravan)
	{
		caravansCount = 0;
		caravansWithLowerIdCount = 0;

		foreach (Caravan caravan in Find.WorldObjects.Caravans)
		{
			if (caravan is not VehicleCaravan vehicleCaravan)
				continue;

			if (vehicleCaravan.Tile != tile)
			{
				if (!vehicleCaravan.vehiclePather.Moving ||
					vehicleCaravan.vehiclePather.NextTile != vehicleCaravan.vehiclePather.Destination ||
					vehicleCaravan.vehiclePather.Destination != tile)
				{
					continue;
				}
			}
			else if (vehicleCaravan.vehiclePather.Moving)
			{
				continue;
			}
			caravansCount++;
			if (caravan.ID < forCaravan.ID)
				caravansWithLowerIdCount++;
		}
	}

	private static bool DrawPosCollides(VehicleCaravan caravan)
	{
		Vector3 a = PatherTweenedPosRoot(caravan);
		float num = Find.WorldGrid.AverageTileSize * BaseDistToCollide;
		foreach (Caravan caravanOnWorld in Find.WorldObjects.Caravans)
		{
			if (caravanOnWorld is not VehicleCaravan vehicleCaravan)
				continue;

			if (vehicleCaravan != caravan &&
				Vector3.Distance(a, PatherTweenedPosRoot(vehicleCaravan)) < num)
			{
				return true;
			}
		}
		return false;
	}
}