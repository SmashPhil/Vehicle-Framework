using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class CompUpgradeTree : VehicleAIComp
	{
		public CompProperties_UpgradeTree Props => (CompProperties_UpgradeTree)props;

		public List<UpgradeNode> upgradeList = new List<UpgradeNode>();

		public int WorkLeftUpgrading => CurrentlyUpgrading ? Mathf.CeilToInt(NodeUnlocking.WorkLeft) : 0;

		public VehiclePawn Vehicle => parent as VehiclePawn;

		public bool CurrentlyUpgrading => NodeUnlocking != null && NodeUnlocking.upgradePurchased && !NodeUnlocking.upgradeActive;

		public UpgradeNode NodeUnlocking { get; set; }

		public UpgradeNode RootNode(UpgradeNode child)
		{
			UpgradeNode parentOfChild = child;
			while(parentOfChild.prerequisiteNodes.NotNullAndAny())
			{
				parentOfChild = NodeListed(parentOfChild.prerequisiteNodes.First());
			}
			return parentOfChild;
		}

		public bool PrerequisitesMet(UpgradeNode node)
		{
			return upgradeList.Where(x => node.prerequisiteNodes.Contains(x.upgradeID)).All(y => y.upgradeActive);
		}

		public bool Disabled(UpgradeNode node)
		{
			return !string.IsNullOrEmpty(node.disableIfUpgradeNodeEnabled) && upgradeList.FirstOrDefault(x => x.upgradeID == node.disableIfUpgradeNodeEnabled).upgradeActive;
		}

		public UpgradeNode NodeListed(UpgradeNode node)
		{
			UpgradeNode matchedNode = upgradeList.Find(x => x == node);
			if (matchedNode is null)
			{
				Log.Error($"Unable to locate node {node.upgradeID} in upgrade list. Cross referencing comp upgrades?");
				return null;
			}
			return matchedNode;
		}

		public UpgradeNode NodeListed(string upgradeID)
		{
			UpgradeNode matchedNode = upgradeList.Find(x => x.upgradeID == upgradeID);
			if (matchedNode is null)
			{
				Log.Error($"Unable to locate node {upgradeID} in upgrade list. Cross referencing comp upgrades?");
				return null;
			}
			return matchedNode;
		}

		public bool LastNodeUnlocked(UpgradeNode node)
		{
			UpgradeNode nodeListed = NodeListed(node);
			List<UpgradeNode> unlocksNodes = upgradeList.FindAll(x => x.prerequisiteNodes.Contains(nodeListed.upgradeID));
			return !unlocksNodes.NotNullAndAny(x => x.upgradeActive);
		}

		public void RefundUnlock(UpgradeNode node)
		{
			if (node is null || !node.upgradeActive) return;
			node.itemContainer.TryDropAll(Vehicle.Position, Vehicle.Map, ThingPlaceMode.Near);
			node.Refund();
			node.ResetNode();
		}

		public void CancelUpgrade()
		{
			NodeUnlocking.itemContainer.TryDropAll(Vehicle.Position, Vehicle.Map, ThingPlaceMode.Near);
			NodeUnlocking.ResetNode();
			Vehicle.Map.GetCachedMapComponent<VehicleReservationManager>().ClearReservedFor(Vehicle);
		}

		public void StartUnlock(UpgradeNode node)
		{
			NodeUnlocking = NodeListed(node);
			NodeUnlocking.ResetTimer();
		}

		public void FinishUnlock(UpgradeNode node)
		{
			var actualNode = NodeListed(node);
			if (!actualNode.replaces.NullOrEmpty())
			{
				actualNode.replaces.ForEach(u =>
				{
					UpgradeNode node = NodeListed(u);
					if (node.upgradeActive)
					{
						RefundUnlock(node);
					}
				});
			}
			actualNode.Upgrade();
			actualNode.TextureAndColor();
			actualNode.upgradeActive = true;
			actualNode.upgradePurchased = true;
		}

		public void InitializeUpgradeTree()
		{
			upgradeList = new List<UpgradeNode>();
			foreach (UpgradeNode node in Props.upgrades)
			{
				try
				{
					UpgradeNode permanentNode = (UpgradeNode)Activator.CreateInstance(node.GetType(), new object[] { node, Vehicle });
					permanentNode.OnInit();
					upgradeList.Add(permanentNode);
				}
				catch (Exception ex)
				{
					SmashLog.Error($"Exception thrown while generating <text>{node.upgradeID}</text> of type <type>{node.GetType()}</type> for {Vehicle.LabelShort}\nException=\"{ex}\"");
					upgradeList.Add(UpgradeNode.BlankUpgrade(node, Vehicle));
				}
			}

			if (upgradeList.Select(x => x.upgradeID).GroupBy(y => y).Where(y => y.Count() > 1).Select(z => z.Key).NotNullAndAny())
			{
				Log.Error(string.Format("Duplicate UpgradeID's detected on def {0}. This is not supported.", parent.def.defName));
				Debug.Message("====== Duplicate UpgradeID's for this Vehicle ======");
				foreach(UpgradeNode errorNode in upgradeList.GroupBy(grp => grp).Where(g => g.Count() > 1))
				{
					Debug.Message($"UpgradeID: {errorNode.upgradeID} UniqueID: {errorNode.GetUniqueLoadID()} Location: {errorNode.gridCoordinate}");
				}
				Debug.Message("===========================================");
			}
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
			NodeUnlocking = upgradeList.Find(x => x.upgradePurchased && !x.upgradeActive);
			foreach(UpgradeNode node in upgradeList)
			{
				node.ResolveReferences();
			}
		}

		public void DoWork()
		{
			if (NodeUnlocking != null && !NodeUnlocking.upgradeActive && NodeUnlocking.StoredCostSatisfied)
			{
				NodeUnlocking.WorkLeft--;
				if (NodeUnlocking.WorkLeft <= 0)
				{
					FinishUnlock(NodeUnlocking);
				}
			}
		}

		public override void CompTickRare()
		{
			base.CompTickRare();
			if (Vehicle.Spawned)
			{
				if (NodeUnlocking != null)
				{
					if (NodeUnlocking.StoredCostSatisfied)
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
					Vehicle.Map.GetCachedMapComponent<VehicleReservationManager>().RemoveLister(Vehicle, ReservationType.LoadUpgradeMaterials);
					Vehicle.Map.GetCachedMapComponent<VehicleReservationManager>().RemoveLister(Vehicle, ReservationType.LoadUpgradeMaterials);
				}
			}
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Collections.Look(ref upgradeList, "upgradeList", LookMode.Deep);
		}
	}
}
