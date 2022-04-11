using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles.AI
{
	public class IncidentWorker_ShuttleDowned : IncidentWorker
	{
		public IncidentCrashedSiteDef CrashSiteDef => def as IncidentCrashedSiteDef;

		public virtual bool TryExecuteEvent(AerialVehicleInFlight aerialVehicle, WorldObject shotDownBy, IntVec3? precalculatedCell = null)
		{
			try
			{
				Map crashSite;
				int ticksTillArrival = -1;
				if (Find.WorldObjects.MapParentAt(aerialVehicle.Tile) is MapParent mapParent)
				{
					crashSite = mapParent.Map;
				}
				else
				{
					int num = CaravanIncidentUtility.CalculateIncidentMapSize(aerialVehicle.vehicle.AllPawnsAboard, aerialVehicle.vehicle.AllPawnsAboard);
					crashSite = GetOrGenerateMapUtility.GetOrGenerateMap(aerialVehicle.Tile, new IntVec3(num, 1, num), WorldObjectDefOfVehicles.CrashedShipSite);
					if (shotDownBy is Settlement settlement)
					{
						ticksTillArrival = (crashSite.Parent as CrashSite).InitiateReinforcementsRequest(settlement);
					}
				}
				bool validator(IntVec3 c)
				{
					bool flag = aerialVehicle.vehicle.PawnOccupiedCells(c, Rot4.East).All(c2 => c2.Standable(crashSite) && !c.Roofed(crashSite) && !c.Fogged(crashSite) && c.InBounds(crashSite));
					return flag;
				}
				IntVec3 RandomCentercell()
				{
					RCellFinder.TryFindRandomCellNearTheCenterOfTheMapWith(validator, crashSite, out IntVec3 result);
					return result;
				}
				IntVec3 cell = precalculatedCell ?? RandomCentercell();
				if (cell == IntVec3.Invalid)
				{
					return false;
				}
				AerialVehicleArrivalAction_CrashSpecificCell arrivalAction = new AerialVehicleArrivalAction_CrashSpecificCell(aerialVehicle.vehicle, crashSite.Parent, crashSite.Tile,
					aerialVehicle.vehicle.CompVehicleLauncher.launchProtocol, cell, Rot4.East);
				arrivalAction.Arrived(crashSite.Tile);
				aerialVehicle.Destroy();
				string settlementLabel = shotDownBy?.Label ?? string.Empty;
				if (ticksTillArrival > 0)
				{
					string hoursTillArrival = Ext_Math.RoundTo(ticksTillArrival / 2500f, 1).ToString();
					SendCrashSiteLetter(shotDownBy, crashSite.Parent, CrashSiteDef.letterLabel, CrashSiteDef.letterTexts[1], CrashSiteDef.letterDef, crashSite.Parent, new NamedArgument[] { aerialVehicle.Label, settlementLabel, hoursTillArrival});
				}
				else
				{
					SendCrashSiteLetter(shotDownBy, crashSite.Parent, CrashSiteDef.letterLabel, CrashSiteDef.letterTexts[0], CrashSiteDef.letterDef, crashSite.Parent, new NamedArgument[] { aerialVehicle.Label, settlementLabel});
				} 
				return true;
			}
			catch (Exception ex)
			{
				Log.Error($"Failed to execute incident {GetType()}. Exception=\"{ex.Message}\"");
				return false;
			}
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
