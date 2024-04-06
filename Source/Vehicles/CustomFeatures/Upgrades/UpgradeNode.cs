using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class UpgradeNode
	{
		public string key;
		public string label;
		public string description;

		public bool displayLabel = false;
		public string icon;
		public float work = 1;

		public bool hidden = false; //Hidden nodes are upgrades which only get unlocked via code

		public SoundDef unlockSound;
		public SoundDef resetSound;

		public IntVec2 gridCoordinate;
		public Vector2 drawSize = new Vector2(ITab_Vehicle_Upgrades.UpgradeNodeDim, ITab_Vehicle_Upgrades.UpgradeNodeDim);

		public Color? drawColorOne;
		public Color? drawColorTwo;
		public Color? drawColorThree;
		//TODO - Add texture overlays from upgrade

		public List<Upgrade> upgrades;

		public List<string> replaces;

		public string disableIfUpgradeNodeEnabled;

		public List<ResearchProjectDef> researchPrerequisites = new List<ResearchProjectDef>();
		public List<string> prerequisiteNodes = new List<string>();
		public List<ThingDefCountClass> ingredients = new List<ThingDefCountClass>();

		public List<GraphicDataOverlay> graphicOverlays;

		public Texture2D Icon { get; private set; }

		public virtual IntVec2 GridCoordinate => gridCoordinate;

		public virtual Texture2D UpgradeImage
		{
			get
			{
				if (string.IsNullOrEmpty(icon))
				{
					return BaseContent.BadTex;
				}
				if (!Icon)
				{
					Icon = ContentFinder<Texture2D>.Get(icon, true);
				}
				return Icon;
			}
		}

		/// <summary>
		/// Draw extra elements on the Upgrade ITab GUI
		/// </summary>
		/// <param name="rect"></param>
		public virtual void DrawExtraOnGUI(Rect rect)
		{
		}

		/// <summary>
		/// Apply texture overlays and colors
		/// </summary>
		public void AddOverlays(VehiclePawn vehicle)
		{
			if (!graphicOverlays.NullOrEmpty())
			{
				foreach (GraphicDataOverlay graphicDataOverlay in graphicOverlays)
				{
					GraphicOverlay graphicOverlay = GraphicOverlay.Create(graphicDataOverlay, vehicle);
					vehicle.graphicOverlay.AddOverlay(key, graphicOverlay);
				}
			}
			if (VehicleMod.settings.main.overrideDrawColors)
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

		public void RemoveOverlays(VehiclePawn vehicle)
		{
			vehicle.graphicOverlay.RemoveOverlays(key);
		}

		public override bool Equals(object obj)
		{
			return obj is UpgradeNode node && Equals(node);
		}

		private bool Equals(UpgradeNode node)
		{
			return key == node.key;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public static bool operator ==(UpgradeNode lhs, UpgradeNode rhs)
		{
			return lhs?.key == rhs?.key;
		}

		public static bool operator !=(UpgradeNode lhs, UpgradeNode rhs)
		{
			return lhs?.key != rhs?.key;
		}

		public override string ToString()
		{
			return $"{key}_{GetType()}";
		}

		public bool IsIngredient(ThingDef def)
		{
			for (int i = 0; i < ingredients.Count; i++)
			{
				if (ingredients[i].thingDef == def)
				{
					return true;
				}
			}
			return false;
		}

		public bool IsStuffable(ThingDef def)
		{
			return false;
			//foreach (IngredientFilter ingredient in ingredients)
			//{
			//	if (ingredient.StuffableDef(def))
			//	{
			//		return true;
			//	}
			//}
			//return false;
		}

		public IEnumerable<ThingDefCountClass> PotentiallyMissingIngredients(Pawn pawn, Map map)
		{
			foreach (ThingDefCountClass thingDefCount in ingredients)
			{
				bool flag = false;
				List<Thing> list = map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableEver);
				for (int j = 0; j < list.Count; j++)
				{
					Thing thing = list[j];
					if ((pawn is null || !thing.IsForbidden(pawn)) && !thing.Position.Fogged(map))
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					yield return thingDefCount;
					//if (ingredient.IsFixedIngredient)
					//{
					//	yield return ingredient.CountClass;
					//}
					//else
					//{
					//	ThingDef thingDef = (from x in ingredient.filter.AllowedThingDefs
					//	orderby x.BaseMarketValue
					//	select x).FirstOrDefault((ThingDef x) => ingredient.filter.Allows(x));
					//	if (thingDef != null)
					//	{
					//		yield return new ThingDefCountClass(thingDef, ingredient.count);
					//	}
					//}
				}
			}
			yield break;
		}

		public int MatchedItemCount(VehiclePawn vehicle, ThingDef def)
		{
			if (IsIngredient(def) && IsStuffable(def))
			{
				return vehicle.CompUpgradeTree.upgradeContainer.InnerListForReading.Where(t => t.def.stuffProps.categories.NotNullAndAny(c => def.stuffProps.categories.Contains(c))).Count();
			}
			return vehicle.CompUpgradeTree.upgradeContainer.InnerListForReading.Where(t => t.def == def).Count();
		}

		public int TotalItemCountRequired(ThingDef def)
		{
			//if (IsIngredient(def) && IsStuffable(def))
			//{
			//	return ingredients.FirstOrDefault(i => i.stuffableDefs.Contains(def)).count;
			//}
			return ingredients.FirstOrDefault(thingDefCount => thingDefCount.thingDef == def).count;
		}

		public virtual bool AvailableSpace(VehiclePawn vehicle, Thing item)
		{
			return IsIngredient(item.def) && MatchedItemCount(vehicle, item.def) < TotalItemCountRequired(item.def);
		}

		public IEnumerable<ThingDefCountClass> MaterialsRequired(VehiclePawn vehicle)
		{
			foreach (ThingDefCountClass thingDefCountClass in ingredients)
			{
				if (!vehicle.CompUpgradeTree.upgradeContainer.Contains(thingDefCountClass.thingDef))
				{
					yield return new ThingDefCountClass(thingDefCountClass.thingDef, thingDefCountClass.count);
				}
				else if (vehicle.CompUpgradeTree.upgradeContainer.TotalStackCountOfDef(thingDefCountClass.thingDef) < thingDefCountClass.count)
				{
					yield return new ThingDefCountClass(thingDefCountClass.thingDef, thingDefCountClass.count - vehicle.CompUpgradeTree.upgradeContainer.TotalStackCountOfDef(thingDefCountClass.thingDef));
				}
			}
		}

		public void ResolveReferences()
		{
			if (!upgrades.NullOrEmpty())
			{
				foreach (Upgrade upgrade in upgrades)
				{
					upgrade.Init(this);
				}
			}
		}

		public virtual void SettingsWindow(VehicleDef def, Listing_Settings listing)
		{
		}
	}
}
