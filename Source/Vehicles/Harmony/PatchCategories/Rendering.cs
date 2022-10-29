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
		public static MethodInfo TrueCenter_Thing { get; private set; }
		public static MethodInfo TrueCenter_Baseline { get; private set; }

		public void PatchMethods()
		{
			TrueCenter_Thing = AccessTools.Method(typeof(GenThing), nameof(GenThing.TrueCenter), parameters: new Type[] { typeof(Thing) });
			TrueCenter_Baseline = AccessTools.Method(typeof(GenThing), nameof(GenThing.TrueCenter), parameters: new Type[] { typeof(IntVec3), typeof(Rot4), typeof(IntVec2), typeof(float) });

			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Pawn_RotationTracker), nameof(Pawn_RotationTracker.UpdateRotation)),
				prefix: new HarmonyMethod(typeof(Rendering),
				nameof(UpdateVehicleRotation)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(ColonistBarColonistDrawer), "DrawIcons"), prefix: null,
				postfix: new HarmonyMethod(typeof(Rendering),
				nameof(DrawIconsVehicles)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(SelectionDrawer), "DrawSelectionBracketFor"),
				prefix: new HarmonyMethod(typeof(Rendering),
				nameof(DrawSelectionBracketsVehicles)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Pawn), nameof(Pawn.ProcessPostTickVisuals)),
				prefix: new HarmonyMethod(typeof(Rendering),
				nameof(ProcessVehiclePostTickVisuals)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(GhostDrawer), nameof(GhostDrawer.DrawGhostThing)),
				postfix: new HarmonyMethod(typeof(Rendering),
				nameof(DrawGhostVehicle)));
			VehicleHarmony.Patch(original: TrueCenter_Thing,
				prefix: new HarmonyMethod(typeof(Rendering),
				nameof(TrueCenterVehicle)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Targeter), nameof(Targeter.TargeterOnGUI)),
				postfix: new HarmonyMethod(typeof(Rendering),
				nameof(DrawTargeters)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Targeter), nameof(Targeter.ProcessInputEvents)),
				postfix: new HarmonyMethod(typeof(Rendering),
				nameof(ProcessTargeterInputEvents)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Targeter), nameof(Targeter.TargeterUpdate)),
				postfix: new HarmonyMethod(typeof(Rendering),
				nameof(TargeterUpdate)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Targeter), nameof(Targeter.StopTargeting)),
				postfix: new HarmonyMethod(typeof(Rendering),
				nameof(TargeterStop)));

			//VehicleHarmony.Patch(original: AccessTools.Method(typeof(OverlayDrawer), "RenderOutOfFuelOverlay"),
			//	prefix: new HarmonyMethod(typeof(Rendering),
			//	nameof(RenderVehicleOutOfFuelOverlay)));
			//VehicleHarmony.Patch(original: AccessTools.Method(typeof(OverlayDrawer), "RenderPulsingOverlay", parameters: new Type[] { typeof(Thing), typeof(Material), typeof(int), typeof(Mesh), typeof(bool) }),
			//	transpiler: new HarmonyMethod(typeof(Rendering),
			//	nameof(RenderOverlaysCenterVehicle)));
		}

		/// <summary>
		/// Use own Vehicle rotation to disallow moving rotation for various tasks such as Drafted
		/// </summary>
		/// <param name="__instance"></param>
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

		public static bool ProcessVehiclePostTickVisuals(Pawn __instance, int ticksPassed, CellRect viewRect)
		{
			if (__instance is VehiclePawn vehicle)
			{
				if (!vehicle.Suspended && vehicle.Spawned)
				{
					if (Current.ProgramState != ProgramState.Playing || viewRect.Contains(vehicle.Position))
					{
						vehicle.Drawer.ProcessPostTickVisuals(ticksPassed);
					}
					vehicle.rotationTracker.ProcessPostTickVisuals(ticksPassed);
				}
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

		public static bool RenderVehicleOutOfFuelOverlay(OverlayDrawer __instance, Thing t)
		{
			if (t is VehiclePawn vehicle)
			{
				//Material material = MaterialPool.MatFrom(vehicle.CompFueledTravel?.Props.FuelIcon ?? ThingDefOf.Chemfuel.uiIcon, ShaderDatabase.MetaOverlay, Color.white);
				//RenderPulsingOverlay.Invoke(__instance, new object[] { t, material, 5, false });
				//RenderPulsingOverlay.Invoke(__instance, new object[] { t, OutOfFuelMat, 6, true });
				//return false;
			}
			return true;
		}

		public static IEnumerable<CodeInstruction> RenderOverlaysCenterVehicle(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> instructionList = instructions.ToList();
			for (int i = 0; i < instructionList.Count; i++)
			{
				CodeInstruction instruction = instructionList[i];

				if (instruction.opcode == OpCodes.Stloc_0 && instructionList[i - 1].Calls(TrueCenter_Baseline))
				{
					yield return new CodeInstruction(opcode: OpCodes.Ldarg_1);
					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(Rendering), nameof(Rendering.VehicleTrueCenterReroute)));
				}

				yield return instruction;
			}
		}

		public static Vector3 VehicleTrueCenterReroute(Vector3 trueCenter, Thing thing)
		{
			if (thing is VehiclePawn vehicle)
			{
				return vehicle.OverlayCenter;
			}
			return trueCenter;
		}

		public static bool TrueCenterVehicle(Thing t, ref Vector3 __result)
		{
			if (t is VehiclePawn vehicle)
			{
				__result = vehicle.TrueCenter();
				return false;
			}
			return true;
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

		public static void TargeterStop()
		{
			Targeters.StopAllTargeters();
		}
		/* ----------------------------------------------------------- */
	}
}
