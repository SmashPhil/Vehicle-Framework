using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
	public struct VehicleGenerationRequest
	{
		public VehicleDef VehicleDef { get; set; }
		public Faction Faction { get; set; }
		public Color ColorOne { get; set; }
		public Color ColorTwo { get; set; }
		public Color ColorThree { get; set; }
		public bool RandomizeMask { get; set; }
		public int Upgrades { get; set; }
		public bool CleanSlate { get; set; }

		public VehicleGenerationRequest(VehicleDef vehicleDef, Faction faction, bool randomizeColors = false, bool randomizeMask = false, bool cleanSlate = true)
		{
			Rand.PushState();

			VehicleDef = vehicleDef;
			Faction = faction;
			
			if (randomizeColors)
			{
				float r1 = Rand.Range(0.25f, .75f);
				float g1 = Rand.Range(0.25f, .75f);
				float b1 = Rand.Range(0.25f, .75f);
				ColorOne = new Color(r1, g1, b1, 1);
				float r2 = Rand.Range(0.25f, .75f);
				float g2 = Rand.Range(0.25f, .75f);
				float b2 = Rand.Range(0.25f, .75f);
				ColorTwo = new Color(r2, g2, b2, 1);
				float r3 = Rand.Range(0.25f, .75f);
				float g3 = Rand.Range(0.25f, .75f);
				float b3 = Rand.Range(0.25f, .75f);
				ColorThree = new Color(r3, g3, b3, 1);
			}
			else
			{
				ColorOne = vehicleDef.graphicData.color;
				ColorTwo = vehicleDef.graphicData.colorTwo;
				ColorThree = vehicleDef.graphicData.colorThree;
			}

			Upgrades = 0;
			CleanSlate = cleanSlate;
			if (!CleanSlate && vehicleDef.GetSortedCompProperties<CompProperties_UpgradeTree>() is CompProperties_UpgradeTree compProperties_UpgradeTree)
			{
				Upgrades = Rand.Range(0, compProperties_UpgradeTree.upgrades.Count);
			}
			
			RandomizeMask = randomizeMask;

			Rand.PopState();
		}
	}
}
