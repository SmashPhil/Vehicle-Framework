using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
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

			VehicleHarmony.Patch(original: AccessTools.Method(typeof(CameraJumper), nameof(CameraJumper.TryShowWorld)),
				prefix: new HarmonyMethod(typeof(WorldHandling),
				nameof(ForcedTargetingDontShowWorld)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(MainButtonWorker_ToggleWorld), nameof(MainButtonWorker_ToggleWorld.Activate)),
				prefix: new HarmonyMethod(typeof(WorldHandling),
				nameof(ForcedTargetingDontToggleWorld)));

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
				if (p is VehiclePawn aerialVehicle && (aerialVehicle.CompVehicleLauncher?.inFlight ?? false))
				{
					__result = WorldPawnSituation.InTravelingTransportPod;
					return;
				}
				foreach (Map map in Find.Maps)
				{
					foreach (VehiclePawn vehicle in map.mapPawns.AllPawnsSpawned.Where(v => v is VehiclePawn vehicle && vehicle.Faction == Faction.OfPlayer))
					{
						if (vehicle.AllPawnsAboard.Contains(p))
						{
							__result = WorldPawnSituation.InTravelingTransportPod;
							return;
						}
					}
				}
				foreach (VehiclePawn vehicle in Find.WorldPawns.AllPawnsAlive.Where(v => v is VehiclePawn vehicle))
				{
					if (vehicle.AllPawnsAboard.Contains(p))
					{
						__result = WorldPawnSituation.InTravelingTransportPod;
						return;
					}
				}
			}
		}

		/// <summary>
		/// Prevent RimWorld Garbage Collection from removing DockedBoats as well as pawns onboard DockedBoat WorldObjects
		/// </summary>
		/// <param name="p"></param>
		public static bool DoNotRemoveVehicleObjects(Pawn p)
		{
			foreach (DockedBoat obj in Find.WorldObjects.AllWorldObjects.Where(x => x is DockedBoat))
			{
				if (obj.dockedBoats.Contains(p))
				{
					return false;
				}
			}
			foreach (VehicleCaravan vehicleCaravan in Find.WorldObjects.AllWorldObjects.Where(x => x is VehicleCaravan))
			{
				foreach (Pawn innerPawn in vehicleCaravan.PawnsListForReading)
				{
					if (innerPawn is VehiclePawn innerVehicle && (innerVehicle == p || innerVehicle.AllPawnsAboard.Contains(p)))
					{
						return false;
					}
				}
			}
			foreach (AerialVehicleInFlight aerialVehicle in Find.WorldObjects.AllWorldObjects.Where(x => x is AerialVehicleInFlight))
			{
				if (aerialVehicle.vehicle == p || aerialVehicle.vehicle.AllPawnsAboard.Contains(p))
				{
					return false;
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
