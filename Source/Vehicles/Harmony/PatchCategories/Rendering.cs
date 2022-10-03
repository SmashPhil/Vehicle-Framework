using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using OpCodes = System.Reflection.Emit.OpCodes;
using UnityEngine;
using HarmonyLib;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	internal class Rendering : IPatchCategory
	{
		public void PatchMethods()
		{
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Pawn_RotationTracker), nameof(Pawn_RotationTracker.UpdateRotation)),
				prefix: new HarmonyMethod(typeof(Rendering),
				nameof(UpdateVehicleRotation)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(ColonistBarColonistDrawer), "DrawIcons"), prefix: null,
				postfix: new HarmonyMethod(typeof(Rendering),
				nameof(DrawIconsVehicles)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(SelectionDrawer), "DrawSelectionBracketFor"),
				prefix: new HarmonyMethod(typeof(Rendering),
				nameof(DrawSelectionBracketsVehicles)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(PawnFootprintMaker), nameof(PawnFootprintMaker.FootprintMakerTick)),
				prefix: new HarmonyMethod(typeof(Rendering),
				nameof(BoatWakesTicker)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(PawnTweener), "TweenedPosRoot"),
				prefix: new HarmonyMethod(typeof(Rendering),
				nameof(VehicleTweenedPosRoot)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(GhostDrawer), nameof(GhostDrawer.DrawGhostThing)),
				postfix: new HarmonyMethod(typeof(Rendering),
				nameof(DrawGhostVehicle)));

			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Targeter), nameof(Targeter.TargeterOnGUI)),
				postfix: new HarmonyMethod(typeof(Rendering),
				nameof(DrawTargeters)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Targeter), nameof(Targeter.ProcessInputEvents)),
				postfix: new HarmonyMethod(typeof(Rendering),
				nameof(ProcessTargeterInputEvents)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Targeter), nameof(Targeter.TargeterUpdate)),
				postfix: new HarmonyMethod(typeof(Rendering),
				nameof(TargeterUpdate)));
		}

		/// <summary>
		/// Use own Vehicle rotation to disallow moving rotation for various tasks such as Drafted
		/// </summary>
		/// <param name="__instance"></param>
		/// <returns></returns>
		public static bool UpdateVehicleRotation(Pawn_RotationTracker __instance, Pawn ___pawn)
		{
			if (___pawn is VehiclePawn vehicle)
			{
				if (vehicle.Destroyed || vehicle.jobs.HandlingFacing)
				{
					return false;
				}
				if (vehicle.vPather.Moving)
				{
					if (vehicle.vPather.curPath == null || vehicle.vPather.curPath.NodesLeftCount < 1)
					{
						return false;
					}
					vehicle.UpdateRotationAndAngle();
				}
				return false;
			}
			return true;
		}

		/// <summary>
		/// Render small vehicle icon on colonist bar picture rect if they are currently onboard a vehicle
		/// </summary>
		/// <param name="rect"></param>
		/// <param name="colonist"></param>
		public static void DrawIconsVehicles(Rect rect, Pawn colonist)
		{
			if (colonist.Dead || !(colonist.ParentHolder is VehicleHandler handler))
			{
				return;
			}
			float num = 20f * Find.ColonistBar.Scale;
			Vector2 vector = new Vector2(rect.x + 1f, rect.yMax - num - 1f);

			Rect rect2 = new Rect(vector.x, vector.y, num, num);
			GUI.DrawTexture(rect2, VehicleTex.CachedTextureIcons[handler.vehicle.VehicleDef]);
			TooltipHandler.TipRegion(rect2, "ActivityIconOnBoardShip".Translate(handler.vehicle.Label)); 
			vector.x += num;
		}

		/// <summary>
		/// Draw diagonal and shifted brackets for Boats
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static bool DrawSelectionBracketsVehicles(object obj)
		{
			var vehicle = obj as VehiclePawn;
			var building = obj as VehicleBuilding;
			if (vehicle != null || building?.vehicle != null)
			{
				if (vehicle is null)
				{
					vehicle = building.vehicle;
				}
				Vector3[] brackets = new Vector3[4];
				float angle = vehicle.Angle;

				Vector3 newDrawPos = vehicle.DrawPosTransformed(vehicle.VehicleDef.drawProperties.selectionBracketsOffset, angle);

				FieldInfo info = AccessTools.Field(typeof(SelectionDrawer), "selectTimes");
				object o = info.GetValue(null);
				Ext_Pawn.CalculateSelectionBracketPositionsWorldForMultiCellPawns(brackets, vehicle, newDrawPos, vehicle.RotatedSize.ToVector2(), (Dictionary<object, float>)o, Vector2.one, angle, 1f);
				
				int num = Mathf.CeilToInt(angle);
				for (int i = 0; i < 4; i++)
				{
					Quaternion rotation = Quaternion.AngleAxis(num, Vector3.up);
					Graphics.DrawMesh(MeshPool.plane10, brackets[i], rotation, MaterialPresets.SelectionBracketMat, 0);
					num -= 90;
				}
				return false;
			}
			//Add for building too?
			return true;
		}

		/// <summary>
		/// Create custom water footprints, resembling a wake behind the boat
		/// </summary>
		/// <param name="___pawn"></param>
		/// <param name="___lastFootprintPlacePos"></param>
		/// <returns></returns>
		public static bool BoatWakesTicker(Pawn ___pawn, ref Vector3 ___lastFootprintPlacePos)
		{
			if (___pawn is VehiclePawn vehicle && vehicle.IsBoat())
			{
				if ((vehicle.Drawer.DrawPos - ___lastFootprintPlacePos).MagnitudeHorizontalSquared() > 0.1)
				{
					Vector3 drawPos = vehicle.Drawer.DrawPos;
					if (drawPos.ToIntVec3().InBounds(vehicle.Map) && !vehicle.beached)
					{
						FleckMaker.WaterSplash(drawPos, vehicle.Map, 7 * vehicle.VehicleDef.properties.wakeMultiplier, vehicle.VehicleDef.properties.wakeSpeed);
						___lastFootprintPlacePos = drawPos;
					}
				}
				else if (VehicleMod.settings.main.passiveWaterWaves && Find.TickManager.TicksGame % 360 == 0)
				{
					float offset = Mathf.PingPong(Find.TickManager.TicksGame / 10, vehicle.VehicleDef.graphicData.drawSize.y / 4);
					FleckMaker.WaterSplash(vehicle.Drawer.DrawPos - new Vector3(0, 0, offset), vehicle.Map, vehicle.VehicleDef.properties.wakeMultiplier, vehicle.VehicleDef.properties.wakeSpeed);
				}
				return false;
			}
			return true;
		}

		public static bool VehicleTweenedPosRoot(Pawn ___pawn, ref Vector3 __result)
		{
			if (___pawn is VehiclePawn vehicle)
			{
				if (!vehicle.Spawned || vehicle.vPather == null)
				{
					__result = vehicle.Position.ToVector3Shifted();
					return false;
				}
				float num = vehicle.VehicleMovedPercent;
				__result = vehicle.vPather.nextCell.ToVector3Shifted() * num + vehicle.Position.ToVector3Shifted() * (1f - num); //+ PawnCollisionOffset?
				return false;
			}
			return true;
		}

		public static void DrawGhostVehicle(IntVec3 center, Rot8 rot, ThingDef thingDef, Graphic baseGraphic, Color ghostCol, AltitudeLayer drawAltitude, Thing thing = null)
		{
			if (thingDef is VehicleBuildDef def)
			{
				VehicleDef vehicleDef = def.thingToSpawn;
				Vector3 loc = GenThing.TrueCenter(center, rot, def.Size, drawAltitude.AltitudeFor());
				float extraAngle;
				foreach (GraphicOverlay graphicOverlay in vehicleDef.GhostGraphicOverlaysFor(ghostCol))
				{
					extraAngle = graphicOverlay.rotation;
					graphicOverlay.graphic.DrawWorker(loc + baseGraphic.DrawOffsetFull(rot), rot, def, thing, rot.AsAngle + extraAngle);
				}
				if (vehicleDef.GetSortedCompProperties<CompProperties_VehicleTurrets>() is CompProperties_VehicleTurrets)
				{
					vehicleDef.DrawGhostTurretTextures(loc, rot, ghostCol);
				}
			}
		}

		/* ---------------- Hooks onto Targeter calls ---------------- */
		public static void DrawTargeters()
		{
			Targeters.OnGUITargeters();
		}

		public static void ProcessTargeterInputEvents()
		{
			Targeters.ProcessTargeterInputEvents();
		}

		public static void TargeterUpdate()
		{
			Targeters.UpdateTargeters();
		}
		/* ----------------------------------------------------------- */
	}
}
