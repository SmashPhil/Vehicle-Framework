using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using HarmonyLib;
using Verse;
using Verse.Sound;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	internal class WorldHandling : IPatchCategory
	{
		public void PatchMethods()
		{
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(WorldPawns), nameof(WorldPawns.GetSituation)), prefix: null,
				postfix: new HarmonyMethod(typeof(WorldHandling),
				nameof(SituationBoardedVehicle)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(WorldPawns), nameof(WorldPawns.RemoveAndDiscardPawnViaGC)),
				prefix: new HarmonyMethod(typeof(WorldHandling),
				nameof(DoNotRemoveVehicleObjects)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(WorldObjectsHolder), "AddToCache"),
				postfix: new HarmonyMethod(typeof(WorldHandling),
				nameof(AddVehicleObjectToCache)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(WorldObjectsHolder), "RemoveFromCache"),
				postfix: new HarmonyMethod(typeof(WorldHandling),
				nameof(RemoveVehicleObjectToCache)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(WorldObjectsHolder), "Recache"),
				prefix: new HarmonyMethod(typeof(WorldHandling),
				nameof(RecacheVehicleObjectCache)));
			VehicleHarmony.Patch(original: AccessTools.PropertyGetter(typeof(PawnsFinder), nameof(PawnsFinder.AllCaravansAndTravelingTransportPods_AliveOrDead)),
				postfix: new HarmonyMethod(typeof(WorldHandling),
				nameof(AllAerialVehicles_AliveOrDead)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(PawnBanishUtility), nameof(PawnBanishUtility.Banish)),
				prefix: new HarmonyMethod(typeof(WorldHandling),
				nameof(BanishPawnFromAerialVehicle)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(PawnUtility), nameof(PawnUtility.IsTravelingInTransportPodWorldObject)),
				postfix: new HarmonyMethod(typeof(WorldHandling),
				nameof(AerialVehiclesDontRandomizePrisoners)));

			VehicleHarmony.Patch(original: AccessTools.Method(typeof(CameraJumper), nameof(CameraJumper.TryShowWorld)),
				prefix: new HarmonyMethod(typeof(WorldHandling),
				nameof(ForcedTargetingDontShowWorld)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(MainButtonWorker_ToggleWorld), nameof(MainButtonWorker_ToggleWorld.Activate)),
				prefix: new HarmonyMethod(typeof(WorldHandling),
				nameof(ForcedTargetingDontToggleWorld)));
			VehicleHarmony.Patch(original: AccessTools.Constructor(typeof(Dialog_Trade), parameters: new Type[] { typeof(Pawn), typeof(ITrader), typeof(bool) }),
				postfix: new HarmonyMethod(typeof(WorldHandling),
				nameof(SetupPlayerAerialVehicleVariables)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Dialog_Trade), nameof(Dialog_Trade.DoWindowContents)),
				prefix: new HarmonyMethod(typeof(WorldHandling),
				nameof(DrawAerialVehicleInfo)));

			/* World Targeter Event Handling */
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(WorldTargeter), nameof(WorldTargeter.TargeterUpdate)),
				postfix: new HarmonyMethod(typeof(WorldHandling),
				nameof(WorldTargeterUpdate)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(WorldTargeter), nameof(WorldTargeter.TargeterOnGUI)),
				postfix: new HarmonyMethod(typeof(WorldHandling),
				nameof(WorldTargeterOnGUI)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(WorldTargeter), nameof(WorldTargeter.ProcessInputEvents)),
				postfix: new HarmonyMethod(typeof(WorldHandling),
				nameof(WorldTargeterProcessInputEvents)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(WorldTargeter), nameof(WorldTargeter.StopTargeting)),
				postfix: new HarmonyMethod(typeof(WorldHandling),
				nameof(WorldTargeterStop)));
		}

		/// <summary>
		/// Prevent RimWorld Garbage Collection from snatching up VehiclePawn inhabitants and VehicleCaravan's VehiclePawn inhabitants by changing
		/// the WorldPawnSituation of pawns onboard vehicles
		/// </summary>
		/// <param name="p"></param>
		/// <param name="__result"></param>
		public static void SituationBoardedVehicle(Pawn p, ref WorldPawnSituation __result)
		{
			if (__result == WorldPawnSituation.Free && p.Faction != null && p.Faction == Faction.OfPlayerSilentFail)
			{
				if (p is VehiclePawn)
				{
					__result = WorldPawnSituation.CaravanMember;
					return;
				}
				if (p.ParentHolder?.ParentHolder is VehiclePawn)
				{
					__result = WorldPawnSituation.CaravanMember;
				}
				if (p.GetAerialVehicle() != null)
				{
					__result = WorldPawnSituation.InTravelingTransportPod;
					return;
				}
			}
		}

		/// <summary>
		/// Prevent RimWorld Garbage Collection from removing pawns inside vehicles on the world map
		/// </summary>
		/// <param name="p"></param>
		public static bool DoNotRemoveVehicleObjects(Pawn p)
		{
			foreach (WorldObject worldObject in Find.WorldObjects.AllWorldObjects)
			{
				if (worldObject is StashedVehicle stashedVehicle)
				{
					if (stashedVehicle.Vehicles.Contains(p))
					{
						return false;
					}
				}
				else if (worldObject is VehicleCaravan vehicleCaravan)
				{
					foreach (Pawn innerPawn in vehicleCaravan.PawnsListForReading)
					{
						if (innerPawn is VehiclePawn vehicle)
						{
							if (vehicle == p || vehicle.AllPawnsAboard.Contains(p))
							{
								return false;
							}
							foreach (Thing thing in vehicle.inventory.innerContainer)
							{
								if (thing == p)
								{
									return false;
								}
							}
						}
					}
				}
				else if (worldObject is AerialVehicleInFlight aerialVehicle)
				{
					if (aerialVehicle.vehicle == p || aerialVehicle.vehicle.AllPawnsAboard.Contains(p))
					{
						return false;
					}
					foreach (Thing thing in aerialVehicle.vehicle.inventory.innerContainer)
					{
						if (thing == p)
						{
							return false;
						}
					}
				}
			}
			return true;
		}

		/// <summary>
		/// Expanding Icon dynamic drawer for AerialVehicle dynamic textures
		/// </summary>
		/// <param name="instructions"></param>
		/// <param name="ilg"></param>
		public static IEnumerable<CodeInstruction> ExpandableIconDetourAerialVehicleTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
		{
			List<CodeInstruction> instructionList = instructions.ToList();
			Label jumpLabel = ilg.DefineLabel();

			for (int i = 0; i < instructionList.Count; i++)
			{
				CodeInstruction instruction = instructionList[i];

				if (instruction.opcode == OpCodes.Ldloc_2 && instructionList[i + 1].opcode == OpCodes.Ldc_I4_1)
				{
					//Jump label, for loop
					instruction.labels.Add(jumpLabel);
				}
				if (instruction.Calls(AccessTools.Property(type: typeof(WorldObject), name: nameof(WorldObject.ExpandingIconColor)).GetGetMethod()))
				{
					Label label = ilg.DefineLabel();

					yield return new CodeInstruction(opcode: OpCodes.Ldloc_3);
					yield return new CodeInstruction(opcode: OpCodes.Call, AccessTools.Method(typeof(RenderHelper), nameof(RenderHelper.RenderDynamicWorldObjects)));
					yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);

					yield return new CodeInstruction(opcode: OpCodes.Leave, jumpLabel);

					instruction.labels.Add(label);
				}

				yield return instruction;
			}
		}

		public static void AddVehicleObjectToCache(WorldObject o)
		{
			VehicleWorldObjectsHolder.Instance.AddToCache(o);
		}
		
		public static void RemoveVehicleObjectToCache(WorldObject o)
		{
			VehicleWorldObjectsHolder.Instance.RemoveFromCache(o);
		}

		public static void RecacheVehicleObjectCache()
		{
			VehicleWorldObjectsHolder.Instance.Recache();
		}

		public static void AllAerialVehicles_AliveOrDead(ref List<Pawn> __result)
		{
			foreach (AerialVehicleInFlight aerialVehicle in VehicleWorldObjectsHolder.Instance.AerialVehicles)
			{
				__result.AddRange(aerialVehicle.vehicle.AllPawnsAboard);
			}
		}

		public static void BanishPawnFromAerialVehicle(Pawn pawn, ref int tile)
		{
			if (pawn.GetAerialVehicle() is AerialVehicleInFlight aerialVehicle)
			{
				if (tile == -1)
				{
					tile = pawn.Tile;
				}
				bool leftToDie = PawnBanishUtility.WouldBeLeftToDie(pawn, tile);
				//if (!pawn.IsQuestLodger())
				//{
				//	PawnDiedOrDownedThoughtsUtility.TryGiveThoughts(pawn, null, leftToDie ? PawnDiedOrDownedThoughtsKind.BanishedToDie : PawnDiedOrDownedThoughtsKind.Banished);
				//}

				CaravanInventoryUtility.MoveAllInventoryToSomeoneElse(pawn, aerialVehicle.vehicle.AllPawnsAboard.Append(aerialVehicle.vehicle).ToList(), null);
				aerialVehicle.vehicle.RemovePawn(pawn);
				if (leftToDie)
				{
					if (Rand.Value < 0.8f)
					{
						pawn.Kill(null);
					}
					else
					{
						HealIfPossible(pawn);
					}
				}
			}
		}

		private static void HealIfPossible(Pawn p)
		{
			List<Hediff> hediffs = new List<Hediff>(p.health.hediffSet.hediffs);
			for (int i = 0; i < hediffs.Count; i++)
			{
				Hediff_Injury hediff_Injury = hediffs[i] as Hediff_Injury;
				if (hediff_Injury != null && !hediff_Injury.IsPermanent())
				{
					p.health.RemoveHediff(hediff_Injury);
				}
				else
				{
					ImmunityRecord immunityRecord = p.health.immunity.GetImmunityRecord(hediffs[i].def);
					if (immunityRecord != null)
					{
						immunityRecord.immunity = 1f;
					}
				}
			}
		}

		public static bool ForcedTargetingDontShowWorld(ref bool __result)
		{
			if (LandingTargeter.Instance.ForcedTargeting)
			{
				__result = false;
				return false;
			}
			return true;
		}

		public static bool ForcedTargetingDontToggleWorld()
		{
			if (LandingTargeter.Instance.ForcedTargeting)
			{
				SoundDefOf.ClickReject.PlayOneShotOnCamera(null);
				Messages.Message("MustTargetLanding".Translate(), MessageTypeDefOf.RejectInput);
				return false;
			}
			return true;
		}

		public static void SetupPlayerAerialVehicleVariables(ref List<Thing> ___playerCaravanAllPawnsAndItems)
		{
			AerialVehicleTraderHelper.SetupAerialVehicleTrade(ref ___playerCaravanAllPawnsAndItems);
		}

		public static void DrawAerialVehicleInfo(Dialog_Trade __instance, ref Rect inRect)
		{
			Rect rect = new Rect(12f, 0f, inRect.width - 24f, 40f);
			float yUsed = AerialVehicleTraderHelper.DrawAerialVehicleInfo(__instance, rect);
			inRect.yMin += yUsed;
		}

		public static void AerialVehiclesDontRandomizePrisoners(Pawn pawn, ref bool __result)
        {
			if (ThingOwnerUtility.AnyParentIs<VehiclePawn>(pawn) || ThingOwnerUtility.AnyParentIs<AerialVehicleInFlight>(pawn))
				__result = true;
		}

		/* -------------------- Launch Targeter -------------------- */
		public static void WorldTargeterUpdate()
		{
			Targeters.UpdateWorldTargeters();
		}

		public static void WorldTargeterOnGUI()
		{
			Targeters.OnGUIWorldTargeters();
		}

		public static void WorldTargeterProcessInputEvents()
		{
			Targeters.ProcessWorldTargeterInputEvents();
		}

		public static void WorldTargeterStop()
		{
			Targeters.StopAllWorldTargeters();
		}
		/* --------------------------------------------------------- */
	}
}
