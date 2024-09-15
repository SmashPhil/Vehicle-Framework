using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Vehicles
{
	public class VehicleRaidParamsDef : Def
	{
		public List<FactionDef> factions;

		public List<PawnInventoryOption> inventory;

		public List<PawnsArrivalModeDef> arrivalModes;

		public bool Allows(Faction faction, PawnsArrivalModeDef arrivalModeDef)
		{
			if (factions.NullOrEmpty() || !factions.Contains(faction.def))
			{
				return false;
			}
			if (arrivalModeDef != null)
			{
				if (!arrivalModes.NullOrEmpty())
				{
					if (!arrivalModes.Contains(arrivalModeDef))
					{
						return false;
					}
				}
			}
			return true;
		}
	}
}
