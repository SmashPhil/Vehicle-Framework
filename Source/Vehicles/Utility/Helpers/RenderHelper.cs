using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	[StaticConstructorOnStartup]
	public static class RenderHelper
	{
		private static readonly List<int> cachedEdgeTiles = new List<int>();
		private static int cachedEdgeTilesForCenter = -1;
		private static int cachedEdgeTilesForRadius = -1;
		private static int cachedEdgeTilesForWorldSeed = -1;

		public static bool draggingCP;
		public static bool draggingHue;
		public static bool draggingDisplacement;

		public static Texture2D ColorChart = new Texture2D(255, 255);
		public static Texture2D HueChart = new Texture2D(1, 255);

		public static Color Blackist = new Color(0.06f, 0.06f, 0.06f);
		public static Color Greyist = new Color(0.2f, 0.2f, 0.2f);

		static RenderHelper()
		{
			for (int i = 0; i < 255; i++)
			{
				HueChart.SetPixel(0, i, Color.HSVToRGB(Mathf.InverseLerp(0f, 255f, i), 1f, 1f));
			}
			HueChart.Apply(false);
			for (int j = 0; j < 255; j++)
			{
				for (int k = 0; k < 255; k++)
				{
					Color color = Color.clear;
					Color c = Color.Lerp(color, Color.white, Mathf.InverseLerp(0f, 255f, j));
					color = Color32.Lerp(Color.black, c, Mathf.InverseLerp(0f, 255f, k));
					ColorChart.SetPixel(j, k, color);
				}
			}
			ColorChart.Apply(false);
		}

		public static void DrawLinesBetweenTargets(VehiclePawn pawn, Job curJob, JobQueue jobQueue)
		{
			Vector3 a = pawn.Position.ToVector3Shifted();
			if (pawn.vPather.curPath != null)
			{
				a = pawn.vPather.Destination.CenterVector3;
			}
			else if (curJob != null && curJob.targetA.IsValid && (!curJob.targetA.HasThing || (curJob.targetA.Thing.Spawned && curJob.targetA.Thing.Map == pawn.Map)))
			{
				GenDraw.DrawLineBetween(a, curJob.targetA.CenterVector3, AltitudeLayer.Item.AltitudeFor());
				a = curJob.targetA.CenterVector3;
			}
			for (int i = 0; i < jobQueue.Count; i++)
			{
				if (jobQueue[i].job.targetA.IsValid)
				{
					if (!jobQueue[i].job.targetA.HasThing || (jobQueue[i].job.targetA.Thing.Spawned && jobQueue[i].job.targetA.Thing.Map == pawn.Map))
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
							if (!targetQueueA[j].HasThing || (targetQueueA[j].Thing.Spawned && targetQueueA[j].Thing.Map == pawn.Map))
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
		/// <seealso cref="MoteThrown"/>
		/// <param name="loc"></param>
		/// <param name="map"></param>
		/// <param name="mote"></param>
		/// <param name="overrideSaturation"></param>
		public static Mote ThrowMoteEnhanced(Vector3 loc, Map map, MoteThrown mote, bool overrideSaturation = false)
		{
			if(!loc.ShouldSpawnMotesAt(map) || (overrideSaturation && map.moteCounter.Saturated))
			{
				return null;
			}

			GenSpawn.Spawn(mote, loc.ToIntVec3(), map, WipeMode.Vanish);
			return mote;
		}

		/// <summary>
		/// Draw ColorPicker and HuePicker
		/// </summary>
		/// <param name="fullRect"></param>
		public static Rect DrawColorPicker(Rect fullRect, ref float hue, ref float saturation, ref float value, Action<float, float, float> colorSetter)
		{
			Rect rect = fullRect.ContractedBy(10f);
			rect.width = 15f;
			if (Input.GetMouseButtonDown(0) && Mouse.IsOver(rect) && !draggingHue)
			{
				draggingHue = true;
			}
			if (draggingHue && Event.current.isMouse)
			{
				float num = hue;
				hue = Mathf.InverseLerp(rect.height, 0f, Event.current.mousePosition.y - rect.y);
				if (hue != num)
				{
					colorSetter(hue, saturation, value);
				}
			}
			if (Input.GetMouseButtonUp(0))
			{
				draggingHue = false;
			}
			Widgets.DrawBoxSolid(rect.ExpandedBy(1f), Color.grey);
			Widgets.DrawTexturePart(rect, new Rect(0f, 0f, 1f, 1f), HueChart);
			Rect rect2 = new Rect(0f, 0f, 16f, 16f)
			{
				center = new Vector2(rect.center.x, rect.height * (1f - hue) + rect.y).Rounded()
			};

			Widgets.DrawTextureRotated(rect2, VehicleTex.ColorHue, 0f);
			rect = fullRect.ContractedBy(10f);
			rect.x = rect.xMax - rect.height;
			rect.width = rect.height;
			if (Input.GetMouseButtonDown(0) && Mouse.IsOver(rect) && !draggingCP)
			{
				draggingCP = true;
			}
			if (draggingCP)
			{
				saturation = Mathf.InverseLerp(0f, rect.width, Event.current.mousePosition.x - rect.x);
				value = Mathf.InverseLerp(rect.width, 0f, Event.current.mousePosition.y - rect.y);
				colorSetter(hue, saturation, value);
			}
			if (Input.GetMouseButtonUp(0))
			{
				draggingCP = false;
			}
			Widgets.DrawBoxSolid(rect.ExpandedBy(1f), Color.grey);
			Widgets.DrawBoxSolid(rect, Color.white);
			GUI.color = Color.HSVToRGB(hue, 1f, 1f);
			Widgets.DrawTextureFitted(rect, ColorChart, 1f);
			GUI.color = Color.white;
			GUI.BeginClip(rect);
			rect2.center = new Vector2(rect.width * saturation, rect.width * (1f - value));
			if (value >= 0.4f && (hue <= 0.5f || saturation <= 0.5f))
			{
				GUI.color = Blackist;
			}
			Widgets.DrawTextureFitted(rect2, VehicleTex.ColorPicker, 1f);
			GUI.color = Color.white;
			GUI.EndClip();
			return rect;
		}

		/// <summary>
		/// Create rotated Mesh where <paramref name="rot"/> [1:3] indicates number of 90 degree rotations
		/// </summary>
		/// <param name="size"></param>
		/// <param name="rot"></param>
		public static Mesh NewPlaneMesh(Vector2 size, int rot)
		{
			Vector3[] vertices = new Vector3[4];
			Vector2[] uv = new Vector2[4];
			int[] triangles = new int[6];
			vertices[0] = new Vector3(-0.5f * size.x, 0f, -0.5f * size.y);
			vertices[1] = new Vector3(-0.5f * size.x, 0f, 0.5f * size.y);
			vertices[2] = new Vector3(0.5f * size.x, 0f, 0.5f * size.y);
			vertices[3] = new Vector3(0.5f * size.x, 0f, -0.5f * size.y);
			switch (rot)
			{
				case 1:
					uv[0] = new Vector2(1f, 0f);
					uv[1] = new Vector2(0f, 0f);
					uv[2] = new Vector2(0f, 1f);
					uv[3] = new Vector2(1f, 1f);
					break;
				case 2:
					uv[0] = new Vector2(1f, 1f);
					uv[1] = new Vector2(1f, 0f);
					uv[2] = new Vector2(0f, 0f);
					uv[3] = new Vector2(0f, 1f);
					break;
				case 3:
					uv[0] = new Vector2(0f, 1f);
					uv[1] = new Vector2(1f, 1f);
					uv[2] = new Vector2(1f, 0f);
					uv[3] = new Vector2(0f, 0f);
					break;
				default:
					uv[0] = new Vector2(0f, 0f);
					uv[1] = new Vector2(0f, 1f);
					uv[2] = new Vector2(1f, 1f);
					uv[3] = new Vector2(1f, 0f);
					break;
			}
			triangles[0] = 0;
			triangles[1] = 1;
			triangles[2] = 2;
			triangles[3] = 0;
			triangles[4] = 2;
			triangles[5] = 3;
			Mesh mesh = new Mesh();
			mesh.name = "NewPlaneMesh()";
			mesh.vertices = vertices;
			mesh.uv = uv;
			mesh.SetTriangles(triangles, 0);
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
			return mesh;
		}

		/// <summary>
		/// Create mesh with varying length of vertices rather than being restricted to 4
		/// </summary>
		/// <param name="size"></param>
		public static Mesh NewTriangleMesh(Vector2 size)
		{
			Vector3[] vertices = new Vector3[3];
			Vector2[] uv = new Vector2[3];
			int[] triangles = new int[3];

			vertices[0] = new Vector3(-0.5f * size.x, 0, 1 * size.y);
			vertices[1] = new Vector3(0.5f * size.x, 0, 1 * size.y);
			vertices[2] = new Vector3(0, 0, 0);

			uv[0] = vertices[0];
			uv[1] = vertices[1];
			uv[2] = vertices[2];

			triangles[0] = 0;
			triangles[1] = 1;
			triangles[2] = 2;

			Mesh mesh = new Mesh();
			mesh.name = "TriangleMesh";
			mesh.vertices = vertices;
			mesh.uv = uv;
			mesh.SetTriangles(triangles, 0);
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
			return mesh;
		}
		
		/// <summary>
		/// Create triangle mesh with a cone like arc for an FOV effect
		/// </summary>
		/// <remarks><paramref name="arc"/> should be within [0:360]</remarks>
		/// <param name="size"></param>
		/// <param name="arc"></param>
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
		/// Reroute Draw method call to dynamic object's Draw method
		/// </summary>
		/// <param name="worldObject"></param>
		public static bool RenderDynamicWorldObjects(WorldObject worldObject)
		{
			//Doesn't guarantee preventing dynamic drawing if def doesn't have expanding icon, second check required in DynamicDrawnWorldObject.Draw
			if (VehicleMod.settings.main.dynamicWorldDrawing && worldObject is DynamicDrawnWorldObject dynamicObject)
			{ 
				//dynamicObject.Draw();
				//return true;
			}
			return false;
		}

		/// <summary>
		/// Draw ring around edge tile cells given <paramref name="center"/> and <paramref name="radius"/>
		/// </summary>
		/// <param name="center"></param>
		/// <param name="radius"></param>
		/// <param name="material"></param>
		public static void DrawWorldRadiusRing(int center, int radius, Material material)
		{
			if (radius < 0)
			{
				return;
			}
			if (cachedEdgeTilesForCenter != center || cachedEdgeTilesForRadius != radius || cachedEdgeTilesForWorldSeed != Find.World.info.Seed)
			{
				cachedEdgeTilesForCenter = center;
				cachedEdgeTilesForRadius = radius;
				cachedEdgeTilesForWorldSeed = Find.World.info.Seed;
				cachedEdgeTiles.Clear();
				Find.WorldFloodFiller.FloodFill(center, (int tile) => true, delegate (int tile, int dist)
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
				}, int.MaxValue, null);
				WorldGrid worldGrid = Find.WorldGrid;
				Vector3 c = worldGrid.GetTileCenter(center);
				Vector3 n = c.normalized;
				cachedEdgeTiles.Sort(delegate (int a, int b)
				{
					float num = Vector3.Dot(n, Vector3.Cross(worldGrid.GetTileCenter(a) - c, worldGrid.GetTileCenter(b) - c));
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
	}
}
