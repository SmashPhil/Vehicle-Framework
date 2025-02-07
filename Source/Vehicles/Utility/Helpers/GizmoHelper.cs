using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public static class GizmoHelper
	{
		/// <summary>
		/// Trade dialog for AerialVehicle WorldObject
		/// </summary>
		/// <param name="aerialVehicle"></param>
		/// <param name="faction"></param>
		/// <param name="trader"></param>
		public static Command_Action AerialVehicleTradeCommand(this AerialVehicleInFlight aerialVehicle, Faction faction = null, TraderKindDef trader = null)
		{
			Pawn bestNegotiator = WorldHelper.FindBestNegotiator(aerialVehicle.vehicle, faction, trader);
			Command_Action commandAction = new()
			{
				defaultLabel = "CommandTrade".Translate(),
				defaultDesc = "CommandTradeDesc".Translate(),
				icon = VehicleTex.TradeCommandTex,
				action = delegate ()
				{
					if (Find.WorldObjects.SettlementAt(aerialVehicle.Tile) is { CanTradeNow: true } settlement)
					{
						Find.WindowStack.Add(new Dialog_Trade(bestNegotiator, settlement));
						PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter_Send(settlement.Goods.OfType<Pawn>(), 
							"LetterRelatedPawnsTradingWithSettlement".Translate(Faction.OfPlayer.def.pawnsPlural), 
							LetterDefOf.NeutralEvent);
					}
				}
			};
			if (bestNegotiator is null)
			{
				if (trader != null && trader.permitRequiredForTrading != null && !aerialVehicle.vehicle.AllPawnsAboard.Any(
				     (pawn) => pawn.royalty != null && pawn.royalty.HasPermit(trader.permitRequiredForTrading, faction)))
				{
					commandAction.Disable("CommandTradeFailNeedPermit".Translate(trader.permitRequiredForTrading.LabelCap));
				}
				else
				{
					commandAction.Disable("CommandTradeFailNoNegotiator".Translate());
				}
			}
			if (bestNegotiator != null && bestNegotiator.skills.GetSkill(SkillDefOf.Social).TotallyDisabled)
			{
				commandAction.Disable("CommandTradeFailSocialDisabled".Translate());
			}
			return commandAction;
		}

		/// <summary>
		/// Resolve designators when changes have been made to <paramref name="designationCategoryDef"/>
		/// </summary>
		/// <param name="designationCategoryDef"></param>
		public static void DesignatorsChanged(DesignationCategoryDef designationCategoryDef)
		{
			AccessTools.Method(typeof(DesignationCategoryDef), "ResolveDesignators").Invoke(designationCategoryDef, []);
		}

		public static void ResetDesignatorStatuses()
		{
			Assert.IsTrue(Current.ProgramState == ProgramState.Playing);
			foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefsListForReading)
			{
				VehicleEnabled.For enabled = SettingsCache.TryGetValue(vehicleDef, typeof(VehicleDef), 
					nameof(VehicleDef.enabled), vehicleDef.enabled);
				bool allowed = enabled is VehicleEnabled.For.Player or VehicleEnabled.For.Everyone;
				Current.Game.Rules.SetAllowBuilding(vehicleDef.buildDef, allowed);
			}
		}
	}
}
