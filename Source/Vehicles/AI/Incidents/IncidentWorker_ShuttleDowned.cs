using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public class IncidentWorker_ShuttleDowned : IncidentWorker
	{
		public static void Execute(AerialVehicleInFlight aerialVehicle, string[] reasons, WorldObject culprit = null, IntVec3? cell = null)
		{
			(VehicleIncidentDefOf.ShuttleCrashed.Worker as IncidentWorker_ShuttleDowned).TryExecuteEvent(aerialVehicle, reasons, culprit: culprit, cell: cell);
		}

		protected virtual string GetLetterText(AerialVehicleInFlight aerialVehicle, string[] reasons, WorldObject culprit)
		{
			StringBuilder letterText = new StringBuilder();
			if (culprit != null)
			{
				letterText.AppendLine("VF_IncidentCrashedSite_ShotDown".Translate(aerialVehicle, culprit));
			}
			else
			{
				letterText.AppendLine("VF_IncidentCrashedSite_Crashing".Translate(aerialVehicle));
				if (!reasons.NullOrEmpty())
				{
					letterText.AppendLine("VF_IncidentReasonLister".Translate(string.Join(Environment.NewLine, reasons)));
				}
			}
			return letterText.ToString();
		}

		protected virtual string GetLetterLabel(AerialVehicleInFlight aerialVehicle, WorldObject culprit)
		{
			return culprit is null ? "VF_IncidentCrashedSiteLabel_Crashing".Translate(aerialVehicle.vehicle) : "VF_IncidentCrashedSiteLabel_ShotDown".Translate(aerialVehicle.vehicle, culprit);
		}

		public virtual bool TryExecuteEvent(AerialVehicleInFlight aerialVehicle, string[] reasons, WorldObject culprit = null, IntVec3? cell = null)
		{
			try
			{
				int ticksTillArrival = GenerateMapAndReinforcements(aerialVehicle, culprit, out Map crashSite);
				IntVec3 crashingCell = cell ?? RandomCrashingCell(aerialVehicle, crashSite);
				if (crashingCell == IntVec3.Invalid)
				{
					return false;
				}
				AerialVehicleArrivalAction_CrashSpecificCell arrivalAction = new AerialVehicleArrivalAction_CrashSpecificCell(aerialVehicle.vehicle, crashSite.Parent, crashSite.Tile, crashingCell, Rot4.East);
				arrivalAction.Arrived(crashSite.Tile);
				aerialVehicle.Destroy();
				string settlementLabel = culprit?.Label ?? string.Empty;
				if (ticksTillArrival > 0)
				{
					string hoursTillArrival = Ext_Math.RoundTo(ticksTillArrival / 2500f, 1).ToString();
					SendCrashSiteLetter(culprit, crashSite.Parent, GetLetterLabel(aerialVehicle, culprit), GetLetterText(aerialVehicle, reasons, culprit), 
						def.letterDef, crashSite.Parent, new NamedArgument[] { aerialVehicle.Label, settlementLabel, hoursTillArrival});
				}
				else
				{
					SendCrashSiteLetter(culprit, crashSite.Parent, GetLetterLabel(aerialVehicle, culprit), GetLetterText(aerialVehicle, reasons, culprit), 
						def.letterDef, crashSite.Parent, new NamedArgument[] { aerialVehicle.Label, settlementLabel});
				} 
				return true;
			}
			catch (Exception ex)
			{
				Log.Error($"Failed to execute incident {GetType()}. Exception=\"{ex.Message}\"");
				return false;
			}
		}
		
		protected virtual IntVec3 RandomCrashingCell(AerialVehicleInFlight aerialVehicle, Map crashSite)
		{
			Predicate<IntVec3> validator = (IntVec3 c) => aerialVehicle.vehicle.PawnOccupiedCells(c, Rot4.East).All(c2 => c2.Standable(crashSite) && !c.Roofed(crashSite) && !c.Fogged(crashSite) && c.InBounds(crashSite));
			RCellFinder.TryFindRandomCellNearTheCenterOfTheMapWith(validator, crashSite, out IntVec3 result);
			return result;
		}

		protected virtual int GenerateMapAndReinforcements(AerialVehicleInFlight aerialVehicle, WorldObject culprit, out Map crashSite)
		{
			int ticksTillArrival = -1;
			if (Find.WorldObjects.MapParentAt(aerialVehicle.Tile) is MapParent mapParent && mapParent.Map != null)
			{
				crashSite = mapParent.Map;
			}
			else
			{
				int num = CaravanIncidentUtility.CalculateIncidentMapSize(aerialVehicle.vehicle.AllPawnsAboard, aerialVehicle.vehicle.AllPawnsAboard);
				crashSite = GetOrGenerateMapUtility.GetOrGenerateMap(aerialVehicle.Tile, new IntVec3(num, 1, num), WorldObjectDefOfVehicles.CrashedShipSite);
				if (culprit is Settlement settlement)
				{
					ticksTillArrival = (crashSite.Parent as CrashSite).InitiateReinforcementsRequest(settlement);
				}
			}
			return ticksTillArrival;
		}

		protected virtual void SendCrashSiteLetter(WorldObject shotDownBy, MapParent crashSite, TaggedString baseLetterLabel, TaggedString baseLetterText, LetterDef letterDef, LookTargets lookTargets, params NamedArgument[] textArgs)
		{
			if (baseLetterLabel.NullOrEmpty() || baseLetterText.NullOrEmpty())
			{
				Log.Error("Sending standard incident letter with no label or text.");
			}
			ChoiceLetter choiceLetter = LetterMaker.MakeLetter(baseLetterLabel.Formatted(textArgs), baseLetterText.Formatted(textArgs), letterDef, lookTargets, shotDownBy?.Faction);
			List<HediffDef> list3 = new List<HediffDef>();
			if (!def.letterHyperlinkHediffDefs.NullOrEmpty())
			{
				list3.AddRange(def.letterHyperlinkHediffDefs);
			}
			choiceLetter.hyperlinkHediffDefs = list3;
			Find.LetterStack.ReceiveLetter(choiceLetter, null);
		}

		protected override bool TryExecuteWorker(IncidentParms parms)
		{
			throw new NotImplementedException("Shuttle downed event cannot be called through the Worker");
		}
	}
}
