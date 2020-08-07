using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Xml;
using HarmonyLib;

namespace Vehicles
{
    public abstract class UpgradeNode : IExposable, ILoadReferenceable, IThingHolder
    {
        /// <summary>
        /// Only use for XML parse step. Must include w/ child classes
        /// </summary>
        public UpgradeNode() 
        { 
        }

        /// <summary>
        /// Self-assign values, may encounter bugs if certain fields not populated
        /// </summary>
        /// <param name="parent"></param>
        public UpgradeNode(VehiclePawn parent)
        {
            nodeID = Current.Game.GetComponent<VehicleIdManager>().GetNextUpgradeId();
            this.parent = parent;

            itemContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
        }

        /// <summary>
        /// preferred method of node instantiation. Copy over from XML defined values
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="parent"></param>
        public UpgradeNode(UpgradeNode reference, VehiclePawn parent)
        {
            nodeID = Current.Game.GetComponent<VehicleIdManager>().GetNextUpgradeId();
            this.parent = parent;

            label = reference.label;
            upgradeID = reference.upgradeID;
            rootNodeLabel = reference.rootNodeLabel;
            informationHighlighted = reference.informationHighlighted;
            disableIfUpgradeNodeEnabled = reference.disableIfUpgradeNodeEnabled;

            ingredients = reference.ingredients;
            researchPrerequisites = reference.researchPrerequisites;
            prerequisiteNodes = reference.prerequisiteNodes;
            imageFilePath = reference.imageFilePath;
            gridCoordinate = reference.gridCoordinate;
            upgradeTime = reference.upgradeTime;

            itemContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
        }

        public string label;

        public string upgradeID;

        public string rootNodeLabel;

        public string informationHighlighted;

        public string disableIfUpgradeNodeEnabled;

        public List<ResearchProjectDef> researchPrerequisites = new List<ResearchProjectDef>();

        public List<string> prerequisiteNodes = new List<string>();

        public List<IngredientFilter> ingredients = new List<IngredientFilter>();

        public string imageFilePath;

        public IntVec2 gridCoordinate;

        public RimworldTime upgradeTime;

        public VehiclePawn parent;

        protected int upgradeTicksLeft; //Post-purchase

        public ThingOwner<Thing> itemContainer; //Post-purchase

        protected bool cachedStoredCostSatisfied = false;

        public bool upgradeActive;

        public bool upgradePurchased;

        public int nodeID;

        protected Texture2D upgradeImage;

        public bool NodeUpgrading => upgradePurchased && !upgradeActive;

        /// <summary>
        /// Id of Upgrade applied to front of UniqueLoadId. Should represent unique name of class
        /// </summary>
        public abstract string UpgradeIdName { get; }

        /// <summary>
        /// Called when node has upgraded fully, after upgrade build ticks hits 0 or triggered by god mode
        /// </summary>
        /// <param name="vehicle"></param>
        public abstract void Upgrade(VehiclePawn vehicle);

        /// <summary>
        /// Undo Upgrade action. Should be polar opposite of Upgrade functionality to revert changes
        /// </summary>
        /// <param name="vehicle"></param>
        public abstract void Refund(VehiclePawn vehicle);

        /// <summary>
        /// Called when Node is first initialized (when Vehicle is initially spawned / created)
        /// </summary>
        public virtual void OnInit()
        {
        }

        /// <summary>
        /// Draw extra elements on the Upgrade ITab GUI
        /// </summary>
        /// <param name="rect"></param>
        public virtual void DrawExtraOnGUI(Rect rect)
        {
        }

        public int Ticks
        {
            get
            {
                return upgradeTicksLeft;
            }
            set
            {
                if (upgradeTicksLeft - value < 0)
                {
                    upgradeTicksLeft = 0;
                }
                else
                {
                    upgradeTicksLeft = value;
                }
            }
        }

        public int UpgradeTimeParsed
        {
            get
            {
                return upgradeTime.ticks;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is UpgradeNode node && Equals(node);
        }

        private bool Equals(UpgradeNode u)
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

        public override string ToString()
        {
            return $"({label}, {GetType()}, {upgradeID})";
        }

        public virtual bool StoredCostSatisfied
        {
            get
            {
                if (itemContainer is null)
                    return false;
                if (cachedStoredCostSatisfied)
                    return true;
                foreach(IngredientFilter ingredient in ingredients)
                {
                    if (itemContainer.TotalStackCountOfDef(ingredient.FixedIngredient) < ingredient.count)
                        return false;
                }
                cachedStoredCostSatisfied = true;
                return true;
            }
        }

        public virtual IntVec2 GridCoordinate
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

        public virtual Texture2D UpgradeImage
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

        public bool IsIngredient(ThingDef def)
        {
	        for (int i = 0; i < ingredients.Count; i++)
	        {
		        if (ingredients[i].filter.Allows(def) && ingredients[i].IsFixedIngredient )
		        {
			        return true;
		        }
	        }
	        return false;
        }

        public bool IsStuffable(ThingDef def)
        {
            foreach(IngredientFilter ingredient in ingredients)
            {
                if(ingredient.StuffableDef(def))
                {
                    return true;
                }
            }
            return false;
        }

        public int MatchedItemCount(ThingDef def)
        {
            if(IsIngredient(def) && IsStuffable(def))
            {
                return itemContainer.InnerListForReading.Where(t => t.def.stuffProps.categories.AnyNullified(c => def.stuffProps.categories.Contains(c))).Count();
            }
            return itemContainer.InnerListForReading.Where(t => t.def == def).Count();
        }

        public int TotalItemCountRequired(ThingDef def)
        {
            if(IsIngredient(def) && IsStuffable(def))
            {
                return ingredients.FirstOrDefault(i => i.stuffableDefs.Contains(def)).count;
            }
            return ingredients.FirstOrDefault(i => i.FixedIngredient == def).count;
        }

        public void ResetTimer()
        {
            upgradeTicksLeft = UpgradeTimeParsed;
        }

        public virtual void ResetNode()
        {
            cachedStoredCostSatisfied = false;
            upgradeActive = false;
            upgradePurchased = false;
        }

        public virtual bool AvailableSpace(Thing item)
        {
            return IsIngredient(item.def) && MatchedItemCount(item.def) < TotalItemCountRequired(item.def);
        }

        public IEnumerable<ThingDefCountClass> PotentiallyMissingIngredients(Pawn pawn, Map map)
		{
			foreach(IngredientFilter ingredient in ingredients)
			{
				bool flag = false;
				List<Thing> list = map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableEver);
				for (int j = 0; j < list.Count; j++)
				{
					Thing thing = list[j];
					if ((pawn is null || !thing.IsForbidden(pawn)) && !thing.Position.Fogged(map) && ingredient.IsFixedIngredient || ingredient.filter.Allows(thing))
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					if (ingredient.IsFixedIngredient)
					{
                        yield return ingredient.CountClass;
					}
					else
					{
						ThingDef thingDef = (from x in ingredient.filter.AllowedThingDefs
						orderby x.BaseMarketValue
						select x).FirstOrDefault((ThingDef x) => ingredient.filter.Allows(x));
						if (thingDef != null)
						{
							yield return new ThingDefCountClass(thingDef, ingredient.count);
						}
					}
				}
			}
			yield break;
		}

        public IEnumerable<ThingDefCountClass> MaterialsRequired()
        {
            foreach (IngredientFilter ingredient in ingredients)
            {
                if (ingredient.IsFixedIngredient)
                {
                    ThingDef thingDef = ingredient.FixedIngredient;
                    if (!itemContainer.Contains(thingDef))
                    {
                        yield return new ThingDefCountClass(thingDef, ingredient.count);
                    }
                    else if(itemContainer.TotalStackCountOfDef(thingDef) < ingredient.count)
                    {
                        yield return new ThingDefCountClass(thingDef, ingredient.count - itemContainer.TotalStackCountOfDef(thingDef));
                    }
                }
            }
        }

        public void ResolveReferences()
        {
            foreach(IngredientFilter ingredient in ingredients)
            {
                ingredient.ResolveReferences();
            }
        }

        public virtual void ExposeData()
        {
            Scribe_Values.Look(ref label, "label");
            Scribe_Values.Look(ref upgradeID, "upgradeID");
            Scribe_Values.Look(ref rootNodeLabel, "rootNodeLabel");
            Scribe_Values.Look(ref informationHighlighted, "informationHighlighted");
            Scribe_Values.Look(ref disableIfUpgradeNodeEnabled, "disableIfUpgradeNodeEnabled");
            Scribe_References.Look(ref parent, "parent");
            Scribe_Values.Look(ref upgradeTime, "upgradeTime");
            Scribe_Values.Look(ref cachedStoredCostSatisfied, "cachedStoredCostSatisfied");

            /* Post-purchase */
            Scribe_Values.Look(ref upgradeTicksLeft, "upgradeTicksLeft");
            Scribe_Deep.Look(ref itemContainer, "itemContainer");

            Scribe_Values.Look(ref upgradeActive, "upgradeActive");
            Scribe_Values.Look(ref upgradePurchased, "upgradePurchased");

            Scribe_Collections.Look(ref ingredients, "ingredients");

            Scribe_Collections.Look(ref researchPrerequisites, "researchPrerequisites", LookMode.Def);

            Scribe_Collections.Look(ref prerequisiteNodes, "prerequisiteNodes", LookMode.Value);
            Scribe_Values.Look(ref imageFilePath, "imageFilePath");
            Scribe_Values.Look(ref gridCoordinate, "gridCoordinate");
            Scribe_Values.Look(ref nodeID, "nodeID");
        }

        public string GetUniqueLoadID()
        {
            return $"{UpgradeIdName}_{upgradeID}-{nodeID}";
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
    }
}
