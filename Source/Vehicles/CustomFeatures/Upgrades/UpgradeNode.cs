using System.Collections.Generic;
using JetBrains.Annotations;
using SmashTools;
using UnityEngine;
using Verse;

namespace Vehicles;

[PublicAPI]
public class UpgradeNode
{
	// ReSharper disable once UnassignedReadonlyField
	public string key;
	public string label;
	public string description;
	public string upgradeExplanation;

	public bool displayLabel;
	public string icon;
	public float work = 1;

	// Hidden nodes are upgrades which can only be unlocked programmatically
	public bool hidden;

	public SoundDef unlockSound;
	public SoundDef resetSound;

	public IntVec2 gridCoordinate;

	public Vector2 drawSize = new(ITab_Vehicle_Upgrades.UpgradeNodeDim,
		ITab_Vehicle_Upgrades.UpgradeNodeDim);

	public Color? drawColorOne;
	public Color? drawColorTwo;
	public Color? drawColorThree;

	public List<Upgrade> upgrades;

	public List<string> replaces;

	// TODO - Remove in 1.7 in favor of disable conditions
	public string disableIfUpgradeNodeEnabled;

	// TODO - Remove in 1.7 in favor of disable conditions
	public List<string> disableIfUpgradeNodesEnabled;

	public List<ResearchProjectDef> researchPrerequisites = [];
	public List<string> prerequisiteNodes = [];

	[LoadAlias("costList")] // TODO 1.7 - switch to costList
	public List<ThingDefCountClass> ingredients = [];

	public float refundFraction = 0.5f; //Default in vanilla deconstructing is 50%
	public SimpleDictionary<ThingDef, float> refundLeavings = [];

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
				Icon = ContentFinder<Texture2D>.Get(icon);
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
				vehicle.DrawTracker.overlayRenderer.AddOverlay(key, graphicOverlay);
			}
		}

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

	public void RemoveOverlays(VehiclePawn vehicle)
	{
		vehicle.DrawTracker.overlayRenderer.RemoveOverlays(key);
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
		return key.GetHashCode();
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
		return vehicle.CompUpgradeTree.upgradeContainer.InnerListForReading.Count(thing => thing.def == def);
	}

	public int TotalItemCountRequired(ThingDef def)
	{
		return ingredients.FirstOrDefault(thingDefCount => thingDefCount.thingDef == def).count;
	}

	public virtual bool AvailableSpace(VehiclePawn vehicle, Thing item)
	{
		return IsIngredient(item.def) &&
			MatchedItemCount(vehicle, item.def) < TotalItemCountRequired(item.def);
	}

	public IEnumerable<ThingDefCountClass> MaterialsRequired(VehiclePawn vehicle)
	{
		foreach (ThingDefCountClass thingDefCountClass in ingredients)
		{
			if (!vehicle.CompUpgradeTree.upgradeContainer.Contains(thingDefCountClass.thingDef))
			{
				yield return new ThingDefCountClass(thingDefCountClass.thingDef,
					thingDefCountClass.count);
			}
			else if (vehicle.CompUpgradeTree.upgradeContainer.TotalStackCountOfDef(thingDefCountClass
				 .thingDef) < thingDefCountClass.count)
			{
				yield return new ThingDefCountClass(thingDefCountClass.thingDef,
					thingDefCountClass.count -
					vehicle.CompUpgradeTree.upgradeContainer.TotalStackCountOfDef(thingDefCountClass
					 .thingDef));
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

	public void PostLoad()
	{
		if (!upgrades.NullOrEmpty())
		{
			foreach (Upgrade upgrade in upgrades)
			{
				upgrade.PostLoad();
			}
		}
	}
}