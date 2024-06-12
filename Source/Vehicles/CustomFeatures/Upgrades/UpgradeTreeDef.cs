using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Vehicles
{
	public class UpgradeTreeDef : Def
	{
		public List<UpgradeNode> nodes;

		private Dictionary<string, UpgradeNode> lookup = new Dictionary<string, UpgradeNode>();

		public override IEnumerable<string> ConfigErrors()
		{
			foreach (string error in base.ConfigErrors())
			{
				yield return error;
			}
			if (!nodes.NullOrEmpty())
			{
				foreach (UpgradeNode node in nodes)
				{
					if (!node.prerequisiteNodes.NullOrEmpty())
					{
						foreach (string key in node.prerequisiteNodes)
						{
							if (!lookup.TryGetValue(key, out UpgradeNode prerequisiteNode))
							{
								yield return $"Unable to find key {key} in prerequisiteNodes for {node}";
							}
						}
					}
					if (!node.upgrades.NullOrEmpty())
					{
						foreach (Upgrade upgrade in node.upgrades)
						{
							foreach (string error in upgrade.ConfigErrors)
							{
								yield return $"(UpgradeNode={node.key} Type={upgrade.GetType()}) {error}";
							}
						}
					}
				}
			}
		}

		public override void ResolveReferences()
		{
			base.ResolveReferences();
			if (!nodes.NullOrEmpty())
			{
				foreach (UpgradeNode node in nodes)
				{
					node.ResolveReferences();
					if (!lookup.ContainsKey(node.key))
					{
						lookup[node.key] = node;
					}
					else
					{
						Log.Error($"Duplicate keys in upgrade tree {defName}.");
					}
				}
			}
		}

		public UpgradeNode GetNode(string key)
		{
			if (key.NullOrEmpty())
			{
				return null;
			}
			return lookup.TryGetValue(key, null);
		}
	}
}
