using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;
using Vehicles.UI;

namespace Vehicles
{
	public abstract class UpgradeNode : IExposable, ILoadReferenceable, IThingHolder
	{
		public string label;
		public bool displayLabel = false;
		public string icon;
		private float work = 1;
		public IntVec2 gridCoordinate;
		public Color? drawColorOne;
		public Color? drawColorTwo;
		public Color? drawColorThree;
		//TODO - Add texture overlays from upgrade

		public string upgradeID;
		public int nodeID;

		public List<string> replaces;

		public string informationHighlighted;
		public string disableIfUpgradeNodeEnabled;

		public List<ResearchProjectDef> researchPrerequisites = new List<ResearchProjectDef>();
		public List<string> prerequisiteNodes = new List<string>();
		public List<IngredientFilter> ingredients = new List<IngredientFilter>();

		public VehiclePawn vehicle;

		protected float workLeft;
		public ThingOwner<Thing> itemContainer;
		protected Texture2D upgradeImage;
		protected bool cachedStoredCostSatisfied = false;
		public bool upgradeActive;
		public bool upgradePurchased;

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
		public UpgradeNode(VehiclePawn vehicle)
		{
			nodeID = VehicleIdManager.Instance.GetNextUpgradeId();
			this.vehicle = vehicle;

			itemContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
		}

		/// <summary>
		/// preferred method of node instantiation. Copy over from XML defined values
		/// </summary>
		/// <param name="reference"></param>
		/// <param name="parent"></param>
		public UpgradeNode(UpgradeNode reference, VehiclePawn vehicle)
		{
			nodeID = VehicleIdManager.Instance.GetNextUpgradeId();
			this.vehicle = vehicle;

			label = reference.label;
			upgradeID = reference.upgradeID;
			displayLabel = reference.displayLabel;
			informationHighlighted = reference.informationHighlighted;
			disableIfUpgradeNodeEnabled = reference.disableIfUpgradeNodeEnabled;
			replaces = reference.replaces;

			ingredients = reference.ingredients;
			researchPrerequisites = reference.researchPrerequisites;
			prerequisiteNodes = reference.prerequisiteNodes;
			icon = reference.icon;
			gridCoordinate = reference.gridCoordinate;
			work = reference.work;
			drawColorOne = reference.drawColorOne;
			drawColorTwo = reference.drawColorTwo;
			drawColorThree = reference.drawColorThree;

			itemContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
		}

		/// <summary>
		/// Id of Upgrade applied to front of UniqueLoadId. Should represent unique name of class
		/// </summary>
		public abstract string UpgradeIdName { get; }

		public virtual int ListerCount { get; }

		public bool NodeUpgrading => upgradePurchased && !upgradeActive;

		public float Work => work;

		public virtual IntVec2 GridCoordinate => gridCoordinate;

		public float WorkLeft
		{
			get
			{
				return workLeft;
			}
			set
			{
				workLeft = value;
			}
		}

		public virtual bool StoredCostSatisfied
		{
			get
			{
				if (itemContainer is null)
					return false;
				if (cachedStoredCostSatisfied)
					return true;
				foreach (IngredientFilter ingredient in ingredients)
				{
					if (itemContainer.TotalStackCountOfDef(ingredient.FixedIngredient) < ingredient.count)
						return false;
				}
				cachedStoredCostSatisfied = true;
				return true;
			}
		}

		public virtual Texture2D UpgradeImage
		{
			get
			{
				if (string.IsNullOrEmpty(icon))
				{
					return BaseContent.BadTex;
				}
				if (upgradeImage is null)
				{
					upgradeImage = ContentFinder<Texture2D>.Get(icon, true);
				}
				return upgradeImage;
			}
		}

		public static StatUpgrade BlankUpgrade(UpgradeNode failedNode, VehiclePawn vehicle) => new StatUpgrade(vehicle)
		{
			values = new Dictionary<StatUpgradeCategoryDef, float>(),
			label = failedNode.label,
			upgradeID = failedNode.upgradeID + "_FAILED",
			displayLabel = true,
			informationHighlighted = failedNode.informationHighlighted,
			disableIfUpgradeNodeEnabled = string.Empty,

			ingredients = new List<IngredientFilter>(),
			researchPrerequisites = new List<ResearchProjectDef>(),
			prerequisiteNodes = new List<string>(),
			icon = BaseContent.BadTexPath,
			gridCoordinate = failedNode.gridCoordinate,
			workLeft = RimWorldTime.ParseToTicks("999y")
		};

		/// <summary>
		/// Called when node has upgraded fully, after upgrade build ticks hits 0 or triggered by god mode
		/// </summary>
		public abstract void Upgrade();

		/// <summary>
		/// Undo Upgrade action. Should be polar opposite of Upgrade functionality to revert changes
		/// </summary>
		public abstract void Refund();

		/// <summary>
		/// Apply texture overlays and colors
		/// </summary>
		public virtual void TextureAndColor()
		{
			if (VehicleMod.settings.main.overrideDrawColors && vehicle.Pattern == PatternDefOf.Default)
			{
				bool colorChanged = false;
				if (drawColorOne != null)
				{
					vehicle.DrawColor = drawColorOne.Value;
					colorChanged = true;
				}
				if (drawColorTwo != null)
				{
					vehicle.DrawColorTwo = drawColorTwo.Value;
					colorChanged = true;
				}
				if (drawColorThree != null)
				{
					vehicle.DrawColorThree = drawColorThree.Value;
					colorChanged = true;
				}
				if (colorChanged)
				{
					vehicle.Notify_ColorChanged();
				}
			}
		}

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
				return itemContainer.InnerListForReading.Where(t => t.def.stuffProps.categories.NotNullAndAny(c => def.stuffProps.categories.Contains(c))).Count();
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
			workLeft = work;
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

		public virtual void SettingsWindow(VehicleDef def, Listing_Settings listing)
		{
		}

		public virtual void ExposeData()
		{
			Scribe_Values.Look(ref label, "label");
			Scribe_Values.Look(ref upgradeID, "upgradeID");
			Scribe_Values.Look(ref displayLabel, "displayLabel");
			Scribe_Values.Look(ref informationHighlighted, "informationHighlighted");
			Scribe_Values.Look(ref disableIfUpgradeNodeEnabled, "disableIfUpgradeNodeEnabled");
			Scribe_References.Look(ref vehicle, "vehicle");
			Scribe_Values.Look(ref work, "work");
			Scribe_Values.Look(ref cachedStoredCostSatisfied, "cachedStoredCostSatisfied");
			Scribe_Values.Look(ref drawColorOne, "drawColorOne");
			Scribe_Values.Look(ref drawColorOne, "drawColorTwo");
			Scribe_Values.Look(ref drawColorOne, "drawColorThree");

			/* Post-purchase */
			Scribe_Values.Look(ref workLeft, "workLeft");
			Scribe_Deep.Look(ref itemContainer, "itemContainer");

			Scribe_Values.Look(ref upgradeActive, "upgradeActive");
			Scribe_Values.Look(ref upgradePurchased, "upgradePurchased");

			Scribe_Collections.Look(ref ingredients, "ingredients");

			Scribe_Collections.Look(ref researchPrerequisites, "researchPrerequisites", LookMode.Def);

			Scribe_Collections.Look(ref prerequisiteNodes, "prerequisiteNodes", LookMode.Value);
			Scribe_Values.Look(ref icon, "icon");
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
				return vehicle;
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
