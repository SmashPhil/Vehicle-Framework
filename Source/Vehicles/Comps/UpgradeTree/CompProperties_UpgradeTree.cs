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

		[Unsaved]
		private Dictionary<UpgradeNode, List<GraphicOverlay>> overlays;

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
			else if (!def.nodes.NullOrEmpty())
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

		public override void ResolveReferences(ThingDef parentDef)
		{
			base.ResolveReferences(parentDef);
			if (!(parentDef is VehicleDef vehicleDef))
			{
				Log.Error($"Attaching {GetType()} on non-vehicle def. This is not allowed.");
				return;
			}

			if (def != null && !def.nodes.NullOrEmpty())
			{
				LongEventHandler.ExecuteWhenFinished(delegate ()
				{
					overlays = new Dictionary<UpgradeNode, List<GraphicOverlay>>();
					foreach (UpgradeNode node in def.nodes)
					{
						if (!node.graphicOverlays.NullOrEmpty())
						{
							foreach (GraphicDataOverlay graphicDataOverlay in node.graphicOverlays)
							{
								GraphicOverlay graphicOverlay = GraphicOverlay.Create(graphicDataOverlay, vehicleDef);
								overlays.AddOrInsert(node, graphicOverlay);
							}
						}
					}
				});
			}
		}

		public List<GraphicOverlay> TryGetOverlays(UpgradeNode node)
		{ 
			if (!overlays.NullOrEmpty())
			{
				return overlays.TryGetValue(node, null);
			}
			return null;
		}
	}
}
