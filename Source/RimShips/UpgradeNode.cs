using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;

namespace Vehicles
{
    public class UpgradeNode : IExposable, ILoadReferenceable, IUpgradeable, IThingHolder
    {
        public string label;

        public string upgradeID;

        public string rootNodeLabel;

        public string informationHighlighted;

        public string disableIfUpgradeNodeEnabled;

        public StatUpgrade upgradeCategory;

        public Dictionary<StatUpgrade, float> values = new Dictionary<StatUpgrade, float>();

        public Dictionary<CannonHandler, VehicleRole> cannonUpgrades = new Dictionary<CannonHandler, VehicleRole>();

        internal Dictionary<CannonHandler, VehicleHandler> cannonsUnlocked = new Dictionary<CannonHandler, VehicleHandler>();
        
        public Dictionary<ThingDef, int> cost = new Dictionary<ThingDef,int>();

        public List<ResearchProjectDef> researchPrerequisites = new List<ResearchProjectDef>();

        public List<string> prerequisiteNodes = new List<string>();

        public string imageFilePath;

        public IntVec2 gridCoordinate;

        public string upgradeTime;

        private int upgradeTicksLeft; //Post-purchase

        private VehiclePawn parent;
        public ThingOwner itemContainer; //Post-purchase
        public List<ThingDefCountClass> cachedMaterialsNeeded = new List<ThingDefCountClass>();
        private bool cachedStoredCostSatisfied = false;

        public UpgradeNode()
        {
        }

        public UpgradeNode(UpgradeNode reference, VehiclePawn parent)
        {
            nodeID = Find.UniqueIDsManager.GetNextThingID();
            this.parent = parent;

            label = reference.label;
            upgradeID = reference.upgradeID;
            rootNodeLabel = reference.rootNodeLabel;
            informationHighlighted = reference.informationHighlighted;
            disableIfUpgradeNodeEnabled = reference.disableIfUpgradeNodeEnabled;
            upgradeCategory = reference.upgradeCategory;
            values = reference.values;

            cannonUpgrades = reference.cannonUpgrades;
            foreach(KeyValuePair<CannonHandler, VehicleRole> cu in cannonUpgrades)
            {
                cannonsUnlocked.Add(cu.Key, new VehicleHandler(parent, cu.Value));
            }

            cost = reference.cost;
            researchPrerequisites = reference.researchPrerequisites;
            prerequisiteNodes = reference.prerequisiteNodes;
            imageFilePath = reference.imageFilePath;
            gridCoordinate = reference.gridCoordinate;
            upgradeTime = reference.upgradeTime;

            itemContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
        }

        public int UpgradeTimeParsed
        {
            get
            {
                if (string.IsNullOrEmpty(upgradeTime))
                    return 0;
                int totalTicks = 0;
			    foreach (string timeStamp in upgradeTime.Split(','))
			    {
				    bool parsed = int.TryParse(string.Concat(timeStamp.Where(char.IsDigit)), out int numeric);
				    totalTicks += numeric * HelperMethods.GetTickMultiplier(timeStamp[timeStamp.Length - 1]);
			    }
			    return totalTicks;
            }
        }

        public bool StoredCostSatisfied
        {
            get
            {
                if (itemContainer is null)
                    return false;
                if (cachedStoredCostSatisfied)
                    return true;
                foreach(KeyValuePair<ThingDef, int> item in cost)
                {
                    if (itemContainer.TotalStackCountOfDef(item.Key) < item.Value)
                        return false;
                }
                cachedStoredCostSatisfied = true;
                return true;
            }
        }

        public void ResetNode()
        {
            cachedStoredCostSatisfied = false;
            upgradeActive = false;
            upgradePurchased = false;
        }

        public bool AvailableSpace(Thing item)
        {
            return ExtractRequiredMaterials().AnyNullified(x => x.thingDef == item.def) ? MaterialsNeeded().Find(x => x.thingDef == item.def)?.count > 0 : false;
        }

        public List<ThingDefCountClass> MaterialsNeeded()
        {
            cachedMaterialsNeeded.Clear();
            List<ThingDefCountClass> itemsNeeded = ExtractRequiredMaterials().ToList();

            foreach(ThingDefCountClass item in itemsNeeded)
            {
                int num = itemContainer.TotalStackCountOfDef(item.thingDef);
                int num2 = item.count - num;
                if (num2 > 0)
                    cachedMaterialsNeeded.Add(new ThingDefCountClass(item.thingDef, num2));
            }
            return cachedMaterialsNeeded;
        }

        private IEnumerable<ThingDefCountClass> ExtractRequiredMaterials()
        {
            foreach(KeyValuePair<ThingDef, int> upgCost in cost)
            {
                yield return new ThingDefCountClass(upgCost.Key, upgCost.Value);
            }
        }

        //REDO
        public int GetSetTicks(int tickCount = 0)
        {
            upgradeTicksLeft += tickCount;
            return upgradeTicksLeft;
        }

        public bool NodeUpgrading => upgradePurchased && !upgradeActive;

        public void ResetTimer()
        {
            upgradeTicksLeft = UpgradeTimeParsed;
        }

        public IntVec2 GridCoordinate
        {
            get
            {
                if(gridCoordinate.x > 29 || gridCoordinate.z > 25)
                {
                    throw new NotSupportedException($"Maximum grid coordinate size is 29x25. Larger coordinates are not supported. GridCoord: ({gridCoordinate.x},{gridCoordinate.z})");
                }
                return gridCoordinate;
            }
        }

        public bool upgradeActive;

        public bool upgradePurchased;

        public int nodeID;

        private Texture2D upgradeImage;


        public Texture2D UpgradeImage
        {
            get
            {
                if(string.IsNullOrEmpty(imageFilePath))
                {
                    return HelperMethods.missingIcon;
                }
                if(upgradeImage is null)
                {
                    upgradeImage = ContentFinder<Texture2D>.Get(imageFilePath, true);
                }
                return upgradeImage;
            }
        }

        public void InitializeLists()
        {
            if (values is null)
                values = new Dictionary<StatUpgrade, float>();

            if(cannonsUnlocked is null)
                cannonsUnlocked = new Dictionary<CannonHandler, VehicleHandler>();

            foreach(KeyValuePair<CannonHandler,VehicleHandler> cannon in cannonsUnlocked)
            {
                if(cannon.Key.uniqueID < 0)
                {
                    cannon.Key.uniqueID = Find.UniqueIDsManager.GetNextThingID();
                }
                if(cannon.Value.uniqueID < 0)
                {
                    cannon.Value.uniqueID = Find.UniqueIDsManager.GetNextThingID();
                }
            }

            if(cost is null)
                cost = new Dictionary<ThingDef,int>();

            if(researchPrerequisites is null)
                researchPrerequisites = new List<ResearchProjectDef>();

            if(prerequisiteNodes is null)
                prerequisiteNodes = new List<string>();
        }

        public override bool Equals(object obj)
        {
            return obj is UpgradeNode && Equals((UpgradeNode)obj);
        }

        public bool Equals(UpgradeNode u)
        {
            return upgradeID == u.upgradeID;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(UpgradeNode u1, UpgradeNode u2)
        {
            return u1?.upgradeID == u2?.upgradeID;
        }

        public static bool operator !=(UpgradeNode u1, UpgradeNode u2)
        {
            return u1?.upgradeID != u2?.upgradeID;
        }

        public string GetUniqueLoadID()
        {
            return $"UpgradeNode_{upgradeID}-{nodeID}";
        }

        public IThingHolder ParentHolder
        {
            get
            {
                return parent;
            }
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return itemContainer;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref label, "label");
            Scribe_Values.Look(ref upgradeID, "upgradeID");
            Scribe_Values.Look(ref rootNodeLabel, "rootNodeLabel");
            Scribe_Values.Look(ref informationHighlighted, "informationHighlighted");
            Scribe_Values.Look(ref disableIfUpgradeNodeEnabled, "disableIfUpgradeNodeEnabled");
            Scribe_Values.Look(ref upgradeCategory, "upgradeCategory");
            Scribe_References.Look(ref parent, "parent");

            /* Post-purchase */
            Scribe_Values.Look(ref upgradeTicksLeft, "upgradeTicksLeft");
            Scribe_Deep.Look(ref itemContainer, "itemContainer");

            Scribe_Values.Look(ref upgradeActive, "upgradeActive");
            Scribe_Values.Look(ref upgradePurchased, "upgradePurchased");

            Scribe_Collections.Look(ref cost, "cost", LookMode.Def, LookMode.Value);
            Scribe_Collections.Look(ref values, "values", LookMode.Value, LookMode.Value);

            Scribe_Collections.Look(ref researchPrerequisites, "researchPrerequisites", LookMode.Def);
            
            Scribe_Collections.Look(ref cannonsUnlocked, "cannonsUnlocked", LookMode.Deep, LookMode.Deep);

            Scribe_Collections.Look(ref prerequisiteNodes, "prerequisiteNodes", LookMode.Value);
            Scribe_Values.Look(ref imageFilePath, "imageFilePath");
            Scribe_Values.Look(ref gridCoordinate, "gridCoordinate");
            Scribe_Values.Look(ref nodeID, "nodeID");
        }
    }
}
