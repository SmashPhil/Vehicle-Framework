using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Sound;
using RimWorld;
using Vehicles.Defs;

namespace Vehicles
{
    public class CompUpgradeTree : ThingComp
    {
        public CompProperties_UpgradeTree Props => (CompProperties_UpgradeTree)props;

        public VehiclePawn Vehicle => parent as VehiclePawn;

        public List<UpgradeNode> upgradeList = new List<UpgradeNode>();

        public int TimeLeftUpgrading => CurrentlyUpgrading ? NodeUnlocking.Ticks : 0;
        public bool CurrentlyUpgrading => NodeUnlocking != null && NodeUnlocking.upgradePurchased && !NodeUnlocking.upgradeActive;
        public UpgradeNode NodeUnlocking { get; set; }

        public UpgradeNode RootNode(UpgradeNode child)
        {
            UpgradeNode parentOfChild = child;
            while(parentOfChild.prerequisiteNodes.AnyNullified())
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
            UpgradeNode matchedNode = upgradeList.Find(x => x.upgradeID == node.upgradeID);
            if(matchedNode is null)
            {
                Log.Error($"Unable to locate node {node.upgradeID} in upgrade list. Cross referencing comp upgrades?");
                return null;
            }
            return matchedNode;
        }

        public UpgradeNode NodeListed(string upgradeID)
        {
            UpgradeNode matchedNode = upgradeList.Find(x => x.upgradeID == upgradeID);
            if(matchedNode is null)
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
            return !unlocksNodes.AnyNullified(x => x.upgradeActive);
        }

        public void RefundUnlock(UpgradeNode node)
        {
            if (node is null)
                return;
            if (!node.upgradeActive)
                return;
            node.itemContainer.TryDropAll(Vehicle.Position, Vehicle.Map, ThingPlaceMode.Near);
            node.Refund(Vehicle);
            node.ResetNode();
        }

        public void CancelUpgrade()
        {
            NodeUnlocking.itemContainer.TryDropAll(Vehicle.Position, Vehicle.Map, ThingPlaceMode.Near);
            NodeUnlocking.ResetNode();
            Vehicle.Map.GetComponent<VehicleReservationManager>().ClearReservedFor(Vehicle);
        }

        public void StartUnlock(UpgradeNode node)
        {
            NodeUnlocking = NodeListed(node);
            NodeUnlocking.ResetTimer();
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            NodeUnlocking = upgradeList.Find(x => x.upgradePurchased && !x.upgradeActive);
            if(!respawningAfterLoad)
            {
                upgradeList = new List<UpgradeNode>();
                foreach(UpgradeNode node in Props.upgradesAvailable)
                {
                    UpgradeNode permanentNode = (UpgradeNode)Activator.CreateInstance(node.GetType(), new object[] { node, Vehicle });
                    permanentNode.OnInit();
                    upgradeList.Add(permanentNode);
                }

                if(upgradeList.Select(x => x.upgradeID).GroupBy(y => y).Where(y => y.Count() > 1).Select(z => z.Key).AnyNullified())
                {
                    Log.Error(string.Format("Duplicate UpgradeID's detected on def {0}. This is not supported.", parent.def.defName));
                    if(Prefs.DevMode)
                    {
                        Log.Message("====== Duplicate UpgradeID's for this Vehicle ======");
                        foreach(UpgradeNode errorNode in upgradeList.GroupBy(grp => grp).Where(g => g.Count() > 1))
                        {
                            Log.Message($"UpgradeID: {errorNode.upgradeID} UniqueID: {errorNode.GetUniqueLoadID()} Location: {errorNode.gridCoordinate}");
                        }
                        Log.Message("===========================================");
                    }
                }
            }
            foreach(UpgradeNode node in upgradeList)
            {
                node.ResolveReferences();
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if(NodeUnlocking != null && !NodeUnlocking.upgradeActive && NodeUnlocking.StoredCostSatisfied)
            {
                //NodeUnlocking.Ticks--;
                if(NodeUnlocking.Ticks <= 0)
                {
                    NodeUnlocking.Upgrade(Vehicle);
                    NodeUnlocking.upgradeActive = true;
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
