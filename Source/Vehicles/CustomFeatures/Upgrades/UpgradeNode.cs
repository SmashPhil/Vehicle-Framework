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
		public string upgradeExplanation;

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

		public List<Upgrade> upgrades;

		public List<string> replaces;

		public string disableIfUpgradeNodeEnabled; //TODO - Remove in 1.6 in favor of disable conditions
		public List<string> disableIfUpgradeNodesEnabled; //TODO - Remove in 1.6 in favor of disable conditions

		public List<ResearchProjectDef> researchPrerequisites = new List<ResearchProjectDef>();
		public List<string> prerequisiteNodes = new List<string>();

		[LoadAlias("costList")] //TODO 1.6 - switch to costList
		public List<ThingDefCountClass> ingredients = new List<ThingDefCountClass>();

		public float refundFraction = 0.75f;
		public SimpleDictionary<ThingDef, float> refundLeavings = new SimpleDictionary<ThingDef, float>();

		public List<GraphicDataOverlay> graphicOverlays;

		public Texture2D Icon { get; private set; }

		public virtual IntVec2 GridCoordinate => gridCoordinate;

		public bool HasGraphics { get; private set; }

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
			if (!UnityData.IsInMainThread)
			{
				LongEventHandler.ExecuteWhenFinished(() => AddOverlaysInternal(vehicle));
			}
			else
			{
				AddOverlaysInternal(vehicle);
			}
		}

		private void AddOverlaysInternal(VehiclePawn vehicle)
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

		public int MatchedItemCount(VehiclePawn vehicle, ThingDef def)
		{
			return vehicle.CompUpgradeTree.upgradeContainer.InnerListForReading.Where(t => t.def == def).Count();
		}

		public int TotalItemCountRequired(ThingDef def)
		{
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
			HasGraphics = !graphicOverlays.NullOrEmpty();
			if (!upgrades.NullOrEmpty())
			{
				foreach (Upgrade upgrade in upgrades)
				{
					upgrade.Init(this);
					HasGraphics |= upgrade.HasGraphics;
				}
			}
		}
	}
}
