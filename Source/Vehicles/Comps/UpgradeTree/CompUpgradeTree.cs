using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class CompUpgradeTree : VehicleAIComp
	{
		private HashSet<string> upgrades = new HashSet<string>();

		private string nodeUnlocking;

		public UpgradeInProgress upgrade;

		public ThingOwner<Thing> upgradeContainer = new ThingOwner<Thing>();

		public CompProperties_UpgradeTree Props => (CompProperties_UpgradeTree)props;

		public int WorkLeftUpgrading => CurrentlyUpgrading ? Mathf.CeilToInt(upgrade.WorkLeft) : 0;

		public bool CurrentlyUpgrading => NodeUnlocking != null;

		public UpgradeNode NodeUnlocking => upgrade?.node;

		public bool StoredCostSatisfied
		{
			get
			{
				if (NodeUnlocking == null)
				{
					return false;
				}
				foreach (ThingDefCountClass thingDefCount in NodeUnlocking.ingredients)
				{
					if (upgradeContainer.TotalStackCountOfDef(thingDefCount.thingDef) < thingDefCount.count)
					{
						Log.Message($"Def: {thingDefCount} | {upgradeContainer.TotalStackCountOfDef(thingDefCount.thingDef)} vs. {thingDefCount.count} Count: {upgradeContainer.TotalStackCount}");
						return false;
					}
				}
				return true;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool NodeUnlocked(UpgradeNode node)
		{
			return upgrades.Contains(node.key);
		}

		public UpgradeNode RootNode(UpgradeNode child)
		{
			UpgradeNode parentOfChild = child;
			while (!parentOfChild.prerequisiteNodes.NullOrEmpty())
			{
				parentOfChild = Props.def.GetNode(parentOfChild.prerequisiteNodes.First());
			}
			return parentOfChild;
		}

		public bool PrerequisitesMet(UpgradeNode node)
		{
			if (!node.prerequisiteNodes.NullOrEmpty())
			{
				foreach (string prerequisiteKey in node.prerequisiteNodes)
				{
					if (!upgrades.Contains(prerequisiteKey))
					{
						return false;
					}
				}
			}
			return true;
		}

		public bool Disabled(UpgradeNode node)
		{
			if (!node.disableIfUpgradeNodeEnabled.NullOrEmpty())
			{
				return upgrades.Contains(node.disableIfUpgradeNodeEnabled);
			}
			return false;
		}

		public bool LastNodeUnlocked(UpgradeNode node)
		{
			List<UpgradeNode> unlocksNodes = Vehicle.CompUpgradeTree.Props.def.nodes.FindAll(x => x.prerequisiteNodes.Contains(node.key));
			return !unlocksNodes.NotNullAndAny(preReqNode => Vehicle.CompUpgradeTree.NodeUnlocked(preReqNode));
		}

		public void ResetUnlock(UpgradeNode node)
		{
			if (node is null || !upgrades.Contains(node.key))
			{
				return;
			}
			if (upgrades.Remove(node.key))
			{
				foreach (Upgrade upgrade in node.upgrades)
				{
					upgrade.Refund(Vehicle);
				}
			}
		}

		public void ClearUpgrade()
		{
			Vehicle.Map.GetCachedMapComponent<VehicleReservationManager>().ClearReservedFor(Vehicle);
			upgradeContainer.TryDropAll(Vehicle.Position, Vehicle.Map, ThingPlaceMode.Near);
			upgrade = null;
		}

		public void StartUnlock(UpgradeNode node)
		{
			upgrade = new UpgradeInProgress(Vehicle, node);
			upgradeContainer.TryDropAll(Vehicle.Position, Vehicle.Map, ThingPlaceMode.Near);
			Vehicle.ignition.Drafted = false;
		}

		public void FinishUnlock(UpgradeNode node)
		{
			if (!node.replaces.NullOrEmpty())
			{
				foreach (string replaceKey in node.replaces)
				{
					UpgradeNode replaceNode = Props.def.GetNode(replaceKey);
					ResetUnlock(replaceNode);
				}
			}
			foreach (Upgrade upgrade in node.upgrades)
			{
				upgrade.Unlock(Vehicle);
			}
			node.ApplyPattern(Vehicle);
			upgrades.Add(node.key);

			upgradeContainer.ClearAndDestroyContents();
		}

		public void InitializeUpgradeTree()
		{
		}

		public override bool CanDraft(out string failReason)
		{
			if (CurrentlyUpgrading)
			{
				failReason = "VF_UpgradeInProgress".Translate();
				return false;
			}
			return base.CanDraft(out failReason);
		}

		public override void PostGeneration()
		{
			InitializeUpgradeTree();
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
		}

		public override void CompTickRare()
		{
			base.CompTickRare();
			ValidateListers();
		}

		public void ValidateListers()
		{
			if (NodeUnlocking != null)
			{
				if (StoredCostSatisfied)
				{
					Vehicle.Map.GetCachedMapComponent<VehicleReservationManager>().RegisterLister(Vehicle, ReservationType.Upgrade);
					Vehicle.Map.GetCachedMapComponent<VehicleReservationManager>().RemoveLister(Vehicle, ReservationType.LoadUpgradeMaterials);
				}
				else
				{
					Vehicle.Map.GetCachedMapComponent<VehicleReservationManager>().RegisterLister(Vehicle, ReservationType.LoadUpgradeMaterials);
					Vehicle.Map.GetCachedMapComponent<VehicleReservationManager>().RemoveLister(Vehicle, ReservationType.Upgrade);
				}
			}
			else
			{
				Vehicle.Map.GetCachedMapComponent<VehicleReservationManager>().RemoveLister(Vehicle, ReservationType.Upgrade);
				Vehicle.Map.GetCachedMapComponent<VehicleReservationManager>().RemoveLister(Vehicle, ReservationType.LoadUpgradeMaterials);
			}
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Collections.Look(ref upgrades, nameof(upgrades), LookMode.Value);
			Scribe_Values.Look(ref nodeUnlocking, nameof(nodeUnlocking));
			Scribe_Deep.Look(ref upgrade, nameof(upgrade));
			Scribe_Deep.Look(ref upgradeContainer, nameof(upgradeContainer), new object[] { this });
		}
	}
}
