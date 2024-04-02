using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class CompProperties_UpgradeTree : CompProperties
	{
		public UpgradeTreeDef def;

		//public List<UpgradeNode> upgrades;

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
			if (def is null)
			{
				yield return "<field>def</field> is null. Consider removing CompProperties_UpgradeTree if you don't plan on using upgrades.".ConvertRichText();
			}
			else
			{
				foreach (UpgradeNode node in def.nodes)
				{
					if (node.GridCoordinate.x < 0 || node.GridCoordinate.x >= ITab_Vehicle_Upgrades.totalLinesAcross)
					{
						yield return $"Maximum grid coordinate width={ITab_Vehicle_Upgrades.totalLinesAcross - 1}. Larger coordinates are not supported, consider going downward. Coord=({node.GridCoordinate.x},{node.GridCoordinate.z})".ConvertRichText();
					}
				}
			}
		}
	}
}
