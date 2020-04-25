using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Sound;
using RimWorld;
using RimShips.Defs;

namespace RimShips
{
    public class CompUpgradeTree : ThingComp
    {
        public CompProperties_UpgradeTree Props => (CompProperties_UpgradeTree)props;

        public Pawn Pawn => parent as Pawn;

        public List<UpgradeNode> upgradeList = new List<UpgradeNode>();

        public UpgradeNode nodeUnlocking;

        public UpgradeNode RootNode(UpgradeNode child)
        {
            UpgradeNode parentOfChild = child;
            while(parentOfChild.prerequisiteNodes.Any())
            {
                parentOfChild = upgradeList.Find(x => x.upgradeID == parentOfChild.prerequisiteNodes.First());
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

        public void StartUnlock(UpgradeNode node)
        {
            nodeUnlocking = upgradeList.Find(x => x.upgradeID == node.upgradeID);
            nodeUnlocking.ResetTimer();
        }

        public bool FinishUnlock()
        {
            try
            {
                foreach(KeyValuePair<StatUpgrade, float> stat in nodeUnlocking.values)
                {
                    switch(stat.Key)
                    {
                        case StatUpgrade.Armor:
                            Pawn.GetComp<CompShips>().ArmorPoints += stat.Value;
                            break;
                        case StatUpgrade.Speed:
                            Pawn.GetComp<CompShips>().MoveSpeedModifier += stat.Value;
                            break;
                        case StatUpgrade.CargoCapacity:
                            Pawn.GetComp<CompShips>().CargoCapacity += stat.Value;
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
                Pawn.GetComp<CompCannons>().AddCannons(nodeUnlocking.cannonsUnlocked);
                nodeUnlocking.upgradeActive = true;
            }
            catch(Exception ex)
            {
                Log.Error($"Unable to add cannon to {Pawn.LabelShort}. Report on Boats workshop page. \nException: {ex.Message} \nStackTrace: {ex.StackTrace}");
                return false;
            }
            Pawn.GetComp<CompShips>().Props.buildDef.GetModExtension<SpawnThingBuilt>().soundFinished?.PlayOneShot(new TargetInfo(Pawn.Position, Pawn.Map, false));
            return true;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if(!respawningAfterLoad)
            {
                upgradeList = new List<UpgradeNode>();
                foreach(UpgradeNode node in Props.upgradesAvailable)
                {
                    UpgradeNode permanentNode = new UpgradeNode(node);
                    permanentNode.InitializeLists();
                    upgradeList.Add(permanentNode);
                }

                if(upgradeList.Select(x => x.upgradeID).GroupBy(y => y).Where(y => y.Count() > 1).Select(z => z.Key).Any())
                {
                    Log.Error(string.Format("Duplicate UpgradeID's detected on def {0}. This is not supported.", parent.def.defName));
                    if(Prefs.DevMode)
                    {
                        Log.Message("====== Duplicate UpgradeID's for this Boat ======");
                        foreach(UpgradeNode errorNode in upgradeList.GroupBy(grp => grp).SelectMany(n => n.Skip(1)))
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
            if(nodeUnlocking != null && !nodeUnlocking.upgradeActive)
            {
                nodeUnlocking.upgradeTicksLeft--;
                if(nodeUnlocking.upgradeTicksLeft <= 0)
                {
                    FinishUnlock();
                }
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref upgradeList, "upgradeList", LookMode.Deep);
            Scribe_Deep.Look(ref nodeUnlocking, "nodeUnlocking");
        }
    }
}
