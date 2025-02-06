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
			Command_Action command_Action = new Command_Action();
			command_Action.defaultLabel = "CommandTrade".Translate();
			command_Action.defaultDesc = "CommandTradeDesc".Translate();
			command_Action.icon = VehicleTex.TradeCommandTex;
			command_Action.action = delegate()
			{
				Settlement settlement = Find.WorldObjects.SettlementAt(aerialVehicle.Tile);
				if (settlement != null && settlement.CanTradeNow)
				{
					Find.WindowStack.Add(new Dialog_Trade(bestNegotiator, settlement, false));
					PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter_Send(settlement.Goods.OfType<Pawn>(), "LetterRelatedPawnsTradingWithSettlement".Translate(Faction.OfPlayer.def.pawnsPlural), LetterDefOf.NeutralEvent, false, true);
				}
			};
			if (bestNegotiator is null)
			{
				if (trader != null && trader.permitRequiredForTrading != null && !aerialVehicle.vehicle.AllPawnsAboard.Any((Pawn p) => p.royalty != null && p.royalty.HasPermit(trader.permitRequiredForTrading, faction)))
				{
					command_Action.Disable("CommandTradeFailNeedPermit".Translate(trader.permitRequiredForTrading.LabelCap));
				}
				else
				{
					command_Action.Disable("CommandTradeFailNoNegotiator".Translate());
				}
			}
			if (bestNegotiator != null && bestNegotiator.skills.GetSkill(SkillDefOf.Social).TotallyDisabled)
			{
				command_Action.Disable("CommandTradeFailSocialDisabled".Translate());
			}
			return command_Action;
		}

		/// <summary>
		/// Resolve designators when changes have been made to <paramref name="designationCategoryDef"/>
		/// </summary>
		/// <param name="designationCategoryDef"></param>
		public static void DesignatorsChanged(DesignationCategoryDef designationCategoryDef)
		{
			AccessTools.Method(typeof(DesignationCategoryDef), "ResolveDesignators").Invoke(designationCategoryDef, new object[] { });
		}

		public static void ResetDesignatorStatuses()
		{
			Assert.IsTrue(Current.ProgramState == ProgramState.Playing);
			foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefsListForReading)
			{
				VehicleEnabled.For enabled = SettingsCache.TryGetValue(vehicleDef, typeof(VehicleDef), 
					nameof(VehicleDef.enabled), vehicleDef.enabled);
				bool allowed = enabled == VehicleEnabled.For.Player || enabled == VehicleEnabled.For.Everyone;
				Current.Game.Rules.SetAllowBuilding(vehicleDef.buildDef, allowed);
			}
		}
	}
}
