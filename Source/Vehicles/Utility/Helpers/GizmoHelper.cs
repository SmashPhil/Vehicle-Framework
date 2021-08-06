using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;
using RimWorld;
using RimWorld.Planet;

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
					Find.WindowStack.Add(new Dialog_TradeAerialVehicle(aerialVehicle, bestNegotiator, settlement, false));
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
		/// Trade dialog for AerialVehicle located on a Settlement
		/// </summary>
		/// <param name="vehicle"></param>
		/// <param name="settlement"></param>
		public static Command ShuttleTradeCommand(AerialVehicleInFlight vehicle, Settlement settlement)
		{
			Pawn bestNegotiator = WorldHelper.FindBestNegotiator(vehicle.vehicle, settlement.Faction, settlement.TraderKind);
			Command_Action command_Action = new Command_Action
			{
				defaultLabel = "CommandTrade".Translate(),
				defaultDesc = "CommandTradeDesc".Translate(),
				icon = VehicleTex.TradeCommandTex,
				action = delegate ()
				{
					if (settlement != null && settlement.CanTradeNow)
					{
						Find.WindowStack.Add(new Dialog_Trade(bestNegotiator, settlement, false));
						PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter_Send(settlement.Goods.OfType<Pawn>(), "LetterRelatedPawnsTradingWithSettlement".Translate(Faction.OfPlayer.def.pawnsPlural), LetterDefOf.NeutralEvent, false, true);
					}
				}
			};
			if (bestNegotiator is null)
			{
				if (settlement.TraderKind != null && settlement.TraderKind.permitRequiredForTrading != null && !vehicle.vehicle.AllPawnsAboard.Any((Pawn p) => p.royalty != null && p.royalty.HasPermit(settlement.TraderKind.permitRequiredForTrading, settlement.Faction)))
				{
					command_Action.Disable("CommandTradeFailNeedPermit".Translate(settlement.TraderKind.permitRequiredForTrading.LabelCap));
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
		/// Draft gizmos for VehiclePawn
		/// </summary>
		/// <param name="drafter"></param>
		public static IEnumerable<Gizmo> DraftGizmos(Pawn_DraftController drafter)
		{
			Command_Toggle command_Toggle = new Command_Toggle();
			command_Toggle.hotKey = KeyBindingDefOf.Command_ColonistDraft;
			command_Toggle.isActive = (() => drafter.Drafted);
			command_Toggle.toggleAction = delegate()
			{
				drafter.Drafted = !drafter.Drafted;
				PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Drafting, KnowledgeAmount.SpecificInteraction);
				if (drafter.Drafted)
				{
					LessonAutoActivator.TeachOpportunity(ConceptDefOf.QueueOrders, OpportunityType.GoodToKnow);
				}
			};
			command_Toggle.defaultDesc = "CommandToggleDraftDesc".Translate();
			command_Toggle.icon = TexCommand.Draft;
			command_Toggle.turnOnSound = SoundDefOf.DraftOn;
			command_Toggle.turnOffSound = SoundDefOf.DraftOff;
			if (!drafter.Drafted)
			{
				command_Toggle.defaultLabel = "CommandDraftLabel".Translate();
			}
			if (drafter.pawn.Downed)
			{
				command_Toggle.Disable("IsIncapped".Translate(drafter.pawn.LabelShort, drafter.pawn));
			}
			if (!drafter.Drafted)
			{
				command_Toggle.tutorTag = "Draft";
			}
			else
			{
				command_Toggle.tutorTag = "Undraft";
			}
			yield return command_Toggle;
			if (drafter.Drafted && drafter.pawn.equipment.Primary != null && drafter.pawn.equipment.Primary.def.IsRangedWeapon)
			{
				yield return new Command_Toggle
				{
					hotKey = KeyBindingDefOf.Misc6,
					isActive = (() => drafter.FireAtWill),
					toggleAction = delegate()
					{
						drafter.FireAtWill = !drafter.FireAtWill;
					},
					icon = TexCommand.FireAtWill,
					defaultLabel = "CommandFireAtWillLabel".Translate(),
					defaultDesc = "CommandFireAtWillDesc".Translate(),
					tutorTag = "FireAtWillToggle"
				};
			}
			yield break;
		}

		/// <summary>
		/// Resolve designators when changes have been made to <paramref name="designationCategoryDef"/>
		/// </summary>
		/// <param name="designationCategoryDef"></param>
		public static void DesignatorsChanged(DesignationCategoryDef designationCategoryDef)
		{
			AccessTools.Method(typeof(DesignationCategoryDef), "ResolveDesignators").Invoke(designationCategoryDef, new object[] { });
		}
	}
}
