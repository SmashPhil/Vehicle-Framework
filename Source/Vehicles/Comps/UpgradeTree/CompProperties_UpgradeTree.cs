using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
	public class CompProperties_UpgradeTree : CompProperties
	{
		public List<UpgradeNode> upgrades;

		public CompProperties_UpgradeTree()
		{
			compClass = typeof(CompUpgradeTree);
		}

		public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
		{
			foreach (string error in base.ConfigErrors(parentDef))
			{
				yield return error;
			}
			if (upgrades is null)
			{
				yield return "null upgrades. Consider removing CompProperties_UpgradeTree if you don't plan on using upgrades.";
			}
		}
	}
}
