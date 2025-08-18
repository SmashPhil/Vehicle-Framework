using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld.Planet;
using SmashTools;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Vehicles.Rendering;

public static class RenderHelper
{
	private static readonly List<PlanetTile> cachedEdgeTiles = [];

	private static readonly Dictionary<(Vector2 size, Rot4 rot), Mesh> rotatedMeshes = [];

	private static int cachedEdgeTilesForCenter = -1;
	private static int cachedEdgeTilesForRadius = -1;
	private static int cachedEdgeTilesForWorldSeed = -1;

	public static void DrawLinesBetweenTargets(VehiclePawn vehicle, Job curJob, JobQueue jobQueue)
	{
		Vector3 a = vehicle.Position.ToVector3Shifted();
		if (vehicle.vehiclePather.curPath != null)
		{
			a = vehicle.vehiclePather.Destination.CenterVector3;
		}
		else if (curJob != null && curJob.targetA.IsValid && (!curJob.targetA.HasThing ||
			(curJob.targetA.Thing.Spawned && curJob.targetA.Thing != vehicle &&
				curJob.targetA.Thing.Map == vehicle.Map)))
		{
			GenDraw.DrawLineBetween(a, curJob.targetA.CenterVector3, AltitudeLayer.Item.AltitudeFor());
			a = curJob.targetA.CenterVector3;
		}
		for (int i = 0; i < jobQueue.Count; i++)
		{
			if (jobQueue[i].job.targetA.IsValid)
			{
				if (!jobQueue[i].job.targetA.HasThing || (jobQueue[i].job.targetA.Thing.Spawned &&
					jobQueue[i].job.targetA.Thing.Map == vehicle.Map))
				{
					Vector3 centerVector = jobQueue[i].job.targetA.CenterVector3;
					GenDraw.DrawLineBetween(a, centerVector, AltitudeLayer.Item.AltitudeFor());
					a = centerVector;
				}
			}
			else
			{
				List<LocalTargetInfo> targetQueueA = jobQueue[i].job.targetQueueA;
				if (targetQueueA != null)
				{
					for (int j = 0; j < targetQueueA.Count; j++)
					{
						if (!targetQueueA[j].HasThing || (targetQueueA[j].Thing.Spawned &&
							targetQueueA[j].Thing.Map == vehicle.Map))
						{
							Vector3 centerVector2 = targetQueueA[j].CenterVector3;
							GenDraw.DrawLineBetween(a, centerVector2, AltitudeLayer.Item.AltitudeFor());
							a = centerVector2;
						}
					}
				}
			}
		}
	}

	/// <summary>
	/// Allow for optional overriding of mote saturation on map while being able to throw any MoteThrown <paramref name="mote"/>
	/// </summary>
	[Obsolete("Use MoteGenerator instead.")]
	public static Mote ThrowMoteEnhanced(Vector3 loc, Map map, MoteThrown mote,
		bool overrideSaturation = false)
	{
		if (!loc.ShouldSpawnMotesAt(map) || (overrideSaturation && map.moteCounter.Saturated))
		{
			return null;
		}

		GenSpawn.Spawn(mote, loc.ToIntVec3(), map);
		return mote;
	}

	/// <summary>
	/// Create triangle mesh with a cone like arc for an FOV effect
	/// </summary>
	/// <remarks><paramref name="arc"/> should be within [0:360]</remarks>
	public static Mesh NewConeMesh(float distance, int arc)
	{
		float currentAngle = arc / -2f;
		Vector3[] vertices = new Vector3[arc + 2];
		Vector2[] uv = new Vector2[vertices.Length];
		int[] triangles = new int[arc * 3];

		vertices[0] = Vector3.zero;
		uv[0] = Vector3.zero;
		int t = 0;
		for (int i = 1; i <= arc; i++)
		{
			vertices[i] = vertices[0].PointFromAngle(distance, currentAngle);
			uv[i] = vertices[i];
			currentAngle += 1;

			triangles[t] = 0;
			triangles[t + 1] = i;
			triangles[t + 2] = i + 1;
			t += 3;
		}

		Mesh mesh = new Mesh();
		mesh.name = "ConeMesh";
		mesh.vertices = vertices;
		mesh.uv = uv;
		mesh.SetTriangles(triangles, 0);
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		return mesh;
	}

	/// <summary>
	/// Draw ring around edge tile cells given <paramref name="center"/> and <paramref name="radius"/>
	/// </summary>
	/// <param name="center"></param>
	/// <param name="radius"></param>
	/// <param name="material"></param>
	public static void DrawWorldRadiusRing(PlanetTile center, int radius, Material material)
	{
		if (radius < 0)
		{
			return;
		}
		if (cachedEdgeTilesForCenter != center || cachedEdgeTilesForRadius != radius ||
			cachedEdgeTilesForWorldSeed != Find.World.info.Seed)
		{
			cachedEdgeTilesForCenter = center;
			cachedEdgeTilesForRadius = radius;
			cachedEdgeTilesForWorldSeed = Find.World.info.Seed;
			cachedEdgeTiles.Clear();
			center.Layer.Filler.FloodFill(center, _ => true, delegate(PlanetTile tile, int dist)
			{
				if (dist > radius + 1)
				{
					return true;
				}
				if (dist == radius + 1)
				{
					cachedEdgeTiles.Add(tile);
				}
				return false;
			});

			WorldGrid worldGrid = Find.WorldGrid;
			Vector3 c = worldGrid.GetTileCenter(center);
			Vector3 n = c.normalized;
			cachedEdgeTiles.Sort(delegate(PlanetTile a, PlanetTile b)
			{
				float num = Vector3.Dot(n,
					Vector3.Cross(worldGrid.GetTileCenter(a) - c, worldGrid.GetTileCenter(b) - c));
				if (Mathf.Abs(num) < 0.0001f)
				{
					return 0;
				}
				if (num < 0f)
				{
					return -1;
				}
				return 1;
			});
		}
		GenDraw.DrawWorldLineStrip(cachedEdgeTiles, material, 5f);
	}

	public static bool ShouldShow(Map map, IntVec3 cell, RenderConditions shouldShow)
	{
		if ((shouldShow & RenderConditions.CurrentMap) == RenderConditions.CurrentMap && map != Find.CurrentMap)
		{
			return false;
		}
		if ((shouldShow & RenderConditions.OnScreen) == RenderConditions.OnScreen &&
			!Find.CameraDriver.CurrentViewRect.ExpandedBy(5).Contains(cell))
		{
			return false;
		}
		return cell.InBounds(map);
	}
}