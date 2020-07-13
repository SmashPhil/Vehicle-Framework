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

        public VehiclePawn Pawn => parent as VehiclePawn;

        public List<UpgradeNode> upgradeList = new List<UpgradeNode>();

        public int TimeLeftUpgrading => CurrentlyUpgrading ? NodeUnlocking.GetSetTicks() : 0;
        public bool CurrentlyUpgrading => NodeUnlocking != null && NodeUnlocking.upgradePurchased && !NodeUnlocking.upgradeActive;
        public UpgradeNode NodeUnlocking { get; set; }

        public UpgradeNode RootNode(UpgradeNode child)
        {
            UpgradeNode parentOfChild = child;
            while(parentOfChild.prerequisiteNodes.Any())
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
                return node;
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
            return !unlocksNodes.Any(x => x.upgradeActive);
        }

        public void RefundUnlock()
        {
            if (NodeUnlocking is null)
                return;
            NodeUnlocking.itemContainer.TryDropAll(Pawn.Position, Pawn.Map, ThingPlaceMode.Near);
            NodeUnlocking.ResetNode();
        }

        public void RefundUnlock(UpgradeNode node)
        {
            if (node is null)
                return;
            if (!node.upgradeActive)
                return;
            node.itemContainer.TryDropAll(Pawn.Position, Pawn.Map, ThingPlaceMode.Near);
            foreach(KeyValuePair<StatUpgrade, float> stat in node.values)
            {
                switch(stat.Key)
                {
                    case StatUpgrade.Armor:
                        Pawn.GetComp<CompVehicle>().ArmorPoints -= stat.Value;
                        break;
                    case StatUpgrade.Speed:
                        Pawn.GetComp<CompVehicle>().MoveSpeedModifier -= stat.Value;
                        break;
                    case StatUpgrade.CargoCapacity:
                        Pawn.GetComp<CompVehicle>().CargoCapacity -= stat.Value;
                        break;
                    case StatUpgrade.FuelConsumptionRate:
                        Pawn.GetComp<CompFueledTravel>().FuelEfficiency -= stat.Value;
                        break;
                    case StatUpgrade.FuelCapacity:
                        Pawn.GetComp<CompFueledTravel>().FuelCapacity -= stat.Value;
                        break;
                    case StatUpgrade.Cannon:
                        break;
                    default:
                        throw new NotImplementedException("StatUpgrade Not Valid");
                }
            }
            node.ResetNode();
        }

        public void StartUnlock(UpgradeNode node)
        {
            NodeUnlocking = NodeListed(node);
            NodeUnlocking.ResetTimer();
        }

        public bool FinishUnlock()
        {
            try
            {
                foreach(KeyValuePair<StatUpgrade, float> stat in NodeUnlocking.values)
                {
                    switch(stat.Key)
                    {
                        case StatUpgrade.Armor:
                            Pawn.GetComp<CompVehicle>().ArmorPoints += stat.Value;
                            break;
                        case StatUpgrade.Speed:
                            Pawn.GetComp<CompVehicle>().MoveSpeedModifier += stat.Value;
                            break;
                        case StatUpgrade.CargoCapacity:
                            Pawn.GetComp<CompVehicle>().CargoCapacity += stat.Value;
                            break;
                        case StatUpgrade.FuelConsumptionRate:
                            Pawn.GetComp<CompFueledTravel>().FuelEfficiency += stat.Value;
                            break;
                        case StatUpgrade.FuelCapacity:
                            Pawn.GetComp<CompFueledTravel>().FuelCapacity += stat.Value;
                            break;
                        case StatUpgrade.Cannon:
                            break;
                        default:
                            throw new NotImplementedException("StatUpgrade Not Valid");
                    }
                }
            }
            catch(Exception ex)
            {
                Log.Error($"Unable to add stat values to {Pawn.LabelShort}. Report on Boats workshop page. \nException: {ex.Message} \nStackTrace: {ex.StackTrace}");
                return false;
            }
            
            try
            {
                Pawn.GetComp<CompCannons>().AddCannons(NodeUnlocking.cannonsUnlocked.Keys.ToList());
                Pawn.GetComp<CompVehicle>().AddHandlers(NodeUnlocking.cannonsUnlocked.Values.ToList());
                NodeUnlocking.upgradeActive = true;
            }
            catch(Exception ex)
            {
                Log.Error($"Unable to add cannon to {Pawn.LabelShort}. Report on Boats workshop page. \nException: {ex.Message} \nStackTrace: {ex.StackTrace}");
                return false;
            }
            Pawn.GetComp<CompVehicle>().Props.buildDef.GetModExtension<SpawnThingBuilt>().soundFinished?.PlayOneShot(new TargetInfo(Pawn.Position, Pawn.Map, false));
            return true;
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
                    UpgradeNode permanentNode = new UpgradeNode(node, Pawn);
                    permanentNode.InitializeLists();
                    upgradeList.Add(permanentNode);
                }

                if(upgradeList.Select(x => x.upgradeID).GroupBy(y => y).Where(y => y.Count() > 1).Select(z => z.Key).Any())
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
        }

        public override void CompTick()
        {
            base.CompTick();
            if(NodeUnlocking != null && !NodeUnlocking.upgradeActive && NodeUnlocking.StoredCostSatisfied)
            {
                NodeUnlocking.GetSetTicks(-1);
                if(NodeUnlocking.GetSetTicks() <= 0)
                {
                    FinishUnlock();
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
