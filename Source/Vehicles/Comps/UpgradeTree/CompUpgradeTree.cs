using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using SmashTools;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Vehicles;

[StaticConstructorOnStartup]
public class CompUpgradeTree : VehicleComp, IRefundable
{
	private static readonly Material UnderfieldMat =
		MaterialPool.MatFrom("Things/Building/BuildingFrame/Underfield", ShaderDatabase.Transparent);

	private static readonly Texture2D CornerTex =
		ContentFinder<Texture2D>.Get("Things/Building/BuildingFrame/Corner");

	private static readonly Texture2D TileTex =
		ContentFinder<Texture2D>.Get("Things/Building/BuildingFrame/Tile");

	private static readonly Texture2D CancelIcon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");

	private static readonly StringBuilder InspectStringBuilder = new();
	private static readonly Color FrameColor = new(0.6f, 0.6f, 0.6f);

	private Material cachedCornerMat;
	private Material cachedTileMat;

	private HashSet<string> upgrades = [];
	private string nodeUnlocking;

	public UpgradeInProgress upgrade;

	public ThingOwner<Thing> upgradeContainer = [];

	private Dictionary<string, List<UpgradeState>> States { get; } = [];

	public CompProperties_UpgradeTree Props => (CompProperties_UpgradeTree)props;

	public bool Upgrading => NodeUnlocking != null;

	public UpgradeNode NodeUnlocking => upgrade?.node;

	private float PercentComplete => Upgrading && NodeUnlocking.work > 0 ? 1 - upgrade.WorkLeft / NodeUnlocking.work : 0;

	private Material CornerMat
	{
		get
		{
			if (!cachedCornerMat)
			{
				cachedCornerMat = MaterialPool.MatFrom(CornerTex, ShaderDatabase.MetaOverlay, FrameColor);
			}
			return cachedCornerMat;
		}
	}

	private Material TileMat
	{
		get
		{
			if (!cachedTileMat)
			{
				cachedTileMat = MaterialPool.MatFrom(TileTex, ShaderDatabase.MetaOverlay, FrameColor);
			}
			return cachedTileMat;
		}
	}

	public bool StoredCostSatisfied
	{
		get
		{
			if (NodeUnlocking == null)
				return false;

			foreach (ThingDefCountClass thingDefCount in NodeUnlocking.ingredients)
			{
				if (upgradeContainer.TotalStackCountOfDef(thingDefCount.thingDef) < thingDefCount.count)
				{
					return false;
				}
			}
			return true;
		}
	}

	IEnumerable<(ThingDef thingDef, float count)> IRefundable.Refunds
	{
		get
		{
			if (!upgradeContainer.NullOrEmpty())
			{
				foreach (Thing thing in upgradeContainer)
				{
					yield return (thing.def, thing.stackCount);
				}
			}
			if (!upgrades.NullOrEmpty())
			{
				foreach (string upgradeKey in upgrades)
				{
					UpgradeNode node = Props.def.GetNode(upgradeKey);
					if (node != null && !node.ingredients.NullOrEmpty())
					{
						foreach (ThingDefCountClass thingDefCountClass in node.ingredients)
						{
							yield return (thingDefCountClass.thingDef, thingDefCountClass.count);
						}
					}
				}
			}
		}
	}

	public bool NodeUnlocked(UpgradeNode node)
	{
		return upgrades.Contains(node.key);
	}

	public UpgradeNode RootNode(UpgradeNode child)
	{
		UpgradeNode parentOfChild = child;
		while (!parentOfChild.prerequisiteNodes.NullOrEmpty())
		{
			parentOfChild = Props.def.GetNode(parentOfChild.prerequisiteNodes.First());
		}
		return parentOfChild;
	}

	public bool PrerequisitesMet(UpgradeNode node)
	{
		if (node.prerequisiteNodes.NullOrEmpty())
			return true;

		foreach (string prerequisiteKey in node.prerequisiteNodes)
		{
			if (!upgrades.Contains(prerequisiteKey))
				return false;
		}
		return true;
	}

	public bool Disabled(UpgradeNode node)
	{
		if (!node.disableIfUpgradeNodeEnabled.NullOrEmpty() &&
			upgrades.Contains(node.disableIfUpgradeNodeEnabled))
		{
			return true;
		}
		return !node.disableIfUpgradeNodesEnabled.NullOrEmpty() &&
			node.disableIfUpgradeNodesEnabled.Any(key => upgrades.Contains(key));
	}

	public bool LastNodeUnlocked(UpgradeNode node)
	{
		List<UpgradeNode> unlocksNodes =
			Vehicle.CompUpgradeTree.Props.def.nodes.FindAll(x =>
				x.prerequisiteNodes.Contains(node.key));
		return !unlocksNodes.NotNullAndAny(preReqNode =>
			Vehicle.CompUpgradeTree.NodeUnlocked(preReqNode));
	}

	/// <summary>
	/// Resets unlock and triggers refund event
	/// </summary>
	public void ResetUnlock(UpgradeNode node)
	{
		if (node is null || !upgrades.Contains(node.key))
			return;

		// Should always be false since the hashset is checked for the key beforehand.
		if (!upgrades.Remove(node.key))
		{
			Trace.Fail($"Unable to reset {node.key}. Upgrade doesn't exist.");
			return;
		}

		if (!node.upgrades.NullOrEmpty())
		{
			for (int i = node.upgrades.Count - 1; i >= 0; i--)
			{
				Upgrade nodeUpgrade = node.upgrades[i];
				try
				{
					nodeUpgrade.Refund(Vehicle);
				}
				catch (Exception ex)
				{
					Log.Error(
						$"{VehicleHarmony.LogLabel} Unable to reset {GetType()} to {Vehicle.LabelShort}. \nException: {ex}");
				}
			}
		}
		RefundIngredients(node);
		node.RemoveOverlays(Vehicle);
		node.resetSound?.PlayOneShot(new TargetInfo(Vehicle.Position, Vehicle.Map));
		Vehicle.EventRegistry[VehicleEventDefOf.UpgradeRefundCompleted].ExecuteEvents();
	}

	private void RefundIngredients(UpgradeNode node)
	{
		if (!node.ingredients.NullOrEmpty())
		{
			ThingOwner<Thing> thingOwner = [];
			foreach (ThingDefCountClass thingDefCount in node.ingredients)
			{
				Thing thing = ThingMaker.MakeThing(thingDefCount.thingDef);
				float multiplier =
					Ext_Vehicles.RefundMaterialCount(Vehicle.VehicleDef, DestroyMode.Deconstruct);
				thing.stackCount = GenMath.RoundRandom(thingDefCount.count * multiplier);
				thingOwner.TryAdd(thing);
			}
			thingOwner.TryDropAllOutsideVehicle(Vehicle.Map, Vehicle.OccupiedRect());
		}
	}

	/// <summary>
	/// Unlocks node and triggers unlock event for all upgrades of that node
	/// </summary>
	public void FinishUnlock(UpgradeNode node)
	{
		if (upgrade is { Removal: true })
		{
			ResetUnlock(node);
			return;
		}
		if (!node.replaces.NullOrEmpty())
		{
			foreach (string replaceKey in node.replaces)
			{
				UpgradeNode replaceNode = Props.def.GetNode(replaceKey);
				ResetUnlock(replaceNode);
			}
		}
		if (!node.upgrades.NullOrEmpty())
		{
			foreach (Upgrade nodeUpgrade in node.upgrades)
			{
				try
				{
					nodeUpgrade.Unlock(Vehicle, false);
				}
				catch (Exception ex)
				{
					Log.Error(
						$"{VehicleHarmony.LogLabel} Unable to unlock {GetType()} for {Vehicle.LabelShort}. \nException: {ex}");
				}
			}
		}
		node.AddOverlays(Vehicle);
		node.unlockSound?.PlayOneShot(new TargetInfo(Vehicle.Position, Vehicle.Map));
		upgrades.Add(node.key);

		upgradeContainer.ClearAndDestroyContents();

		Vehicle.EventRegistry[VehicleEventDefOf.UpgradeCompleted].ExecuteEvents();
	}

	public void ClearUpgrade()
	{
		Vehicle.Map.GetCachedMapComponent<VehicleReservationManager>().ClearReservedFor(Vehicle);
		upgradeContainer.TryDropAll(Vehicle.Position, Vehicle.Map, ThingPlaceMode.Near);
		upgrade = null;
		Vehicle.EventRegistry[VehicleEventDefOf.UpgradeCanceled].ExecuteEvents();
	}

	/// <summary>
	/// Notifies job pool that vehicle has upgrade that wants to be unlocked, still requires job to be performed
	/// </summary>
	public void StartUnlock(UpgradeNode node)
	{
		upgrade = new UpgradeInProgress(Vehicle, node, false);
		upgradeContainer.TryDropAll(Vehicle.Position, Vehicle.Map, ThingPlaceMode.Near);
		Vehicle.EventRegistry[VehicleEventDefOf.UpgradeEnqueued].ExecuteEvents();
	}

	/// <summary>
	/// Notifies job pool that vehicle has upgrade that wants to be removed, still requires job to be performed
	/// </summary>
	public void RemoveUnlock(UpgradeNode node)
	{
		upgrade = new UpgradeInProgress(Vehicle, node, true);
		upgradeContainer.TryDropAll(Vehicle.Position, Vehicle.Map, ThingPlaceMode.Near);
		Vehicle.EventRegistry[VehicleEventDefOf.UpgradeRefundEnqueued].ExecuteEvents();
	}

	private void PrepVehicleForWork()
	{
		Vehicle.ignition.Drafted = false;
		Vehicle.Angle = 0;
		if (Vehicle.AllPawnsAboard.Count > 0)
		{
			Vehicle.DisembarkAll();
		}
	}

	internal void ReactivateComps()
	{
		if (upgrades.NullOrEmpty())
			return;

		foreach (string key in upgrades)
		{
			UpgradeNode node = Props.def.GetNode(key);
			if (node == null || node.upgrades.NullOrEmpty())
				continue;

			foreach (Upgrade nodeUpgrade in node.upgrades)
			{
				if (nodeUpgrade is not CompUpgrade compUpgrade)
					continue;
				try
				{
					compUpgrade.Unlock(Vehicle, unlockingPostLoad: true);
				}
				catch (Exception ex)
				{
					Log.Error(
						$"{VehicleHarmony.LogLabel} Unable to unlock {GetType()} post-load for {Vehicle.LabelShort}. \nException: {ex}");
				}
			}
		}
	}

	internal void ReloadUnlocks()
	{
		if (upgrades.NullOrEmpty())
			return;

		foreach (string key in upgrades)
		{
			UpgradeNode node = Props.def.GetNode(key);
			if (node == null)
				continue;

			node.AddOverlays(Vehicle);
			if (node.upgrades.NullOrEmpty())
				continue;

			foreach (Upgrade nodeUpgrade in node.upgrades)
			{
				if (!nodeUpgrade.UnlockOnLoad)
					continue;

				try
				{
					nodeUpgrade.Unlock(Vehicle, unlockingPostLoad: true);
				}
				catch (Exception ex)
				{
					Log.Error(
						$"{VehicleHarmony.LogLabel} Unable to unlock {GetType()} post-load for {Vehicle.LabelShort}. \nException: {ex}");
				}
			}
		}
	}

	public void AddToContainer(Thing thing, int count)
	{
		upgradeContainer.TryAddOrTransfer(thing, count);
		ValidateListers();
	}

	public void AddSettings(UpgradeState state)
	{
		if (!States.ContainsKey(state.key))
		{
			States[state.key] = new List<UpgradeState>();
		}
		States[state.key].Add(state);
	}

	public bool TryGetStates(string key, out List<UpgradeState> outList)
	{
		return States.TryGetValue(key, out outList);
	}

	public void RemoveSettings(UpgradeState state)
	{
		if (!States.TryGetValue(state.key, out List<UpgradeState> innerList))
		{
			Log.Error($"Unable to locate {state.key} in state cache.");
			return;
		}
		innerList.Remove(state);
	}

	public override void PostDraw()
	{
		base.PostDraw();
		if (Upgrading)
		{
			const float SizeScaleFactor = 1.15f;

			Vector3 drawPos = Vehicle.DrawPos;
			Vector3 size = new(Vehicle.VehicleDef.Size.x * SizeScaleFactor, 1f, Vehicle.VehicleDef.Size.z * SizeScaleFactor);
			Matrix4x4 matrix = Matrix4x4.TRS(drawPos, Vehicle.Rotation.AsQuat, size);
			Graphics.DrawMesh(MeshPool.plane10, matrix, UnderfieldMat, 0);

			const int TilesPerCell = 4;
			const int Rotations = 4;

			for (int i = 0; i < Rotations; i++)
			{
				float num2 = Mathf.Min(Vehicle.RotatedSize.x, Vehicle.RotatedSize.z) * 0.38f;
				IntVec3 intVec = i switch
				{
					0 => new IntVec3(-1, 0, -1), // North
					1 => new IntVec3(-1, 0, 1), // East,
					2 => new IntVec3(1, 0, 1), // South
					3 => new IntVec3(1, 0, -1), // West
					_ => throw new InvalidOperationException(nameof(Rot4))
				};

				Vector3 b = default;
				b.x = intVec.x * (Vehicle.RotatedSize.x / 2f - num2 / 2f);
				b.z = intVec.z * (Vehicle.RotatedSize.z / 2f - num2 / 2f);
				Vector3 s2 = new(num2, 1f, num2);
				Matrix4x4 matrix2 = default;
				matrix2.SetTRS(drawPos + Vector3.up * 0.03f + b, new Rot4(i).AsQuat, s2);
				Graphics.DrawMesh(MeshPool.plane10, matrix2, CornerMat, 0);
			}

			int tiles =
				Mathf.CeilToInt(PercentComplete * Vehicle.RotatedSize.x * Vehicle.RotatedSize.z * TilesPerCell);
			IntVec2 intVec2 = Vehicle.RotatedSize * 2;
			for (int j = 0; j < tiles; j++)
			{
				IntVec2 intVec3 = default;
				intVec3.z = j / intVec2.x;
				intVec3.x = j - intVec3.z * intVec2.x;
				Vector3 a = new Vector3(intVec3.x * 0.5f, 0f, intVec3.z * 0.5f) + drawPos;
				a.x -= Vehicle.RotatedSize.x * 0.5f - 0.25f;
				a.z -= Vehicle.RotatedSize.z * 0.5f - 0.25f;
				Vector3 s3 = new(0.5f, 1f, 0.5f);
				Matrix4x4 matrix3 = default;
				matrix3.SetTRS(a + Vector3.up * 0.02f, Quaternion.identity, s3);
				Graphics.DrawMesh(MeshPool.plane10, matrix3, TileMat, 0);
			}
		}
	}

	public override AcceptanceReport CanDraft()
	{
		if (Upgrading)
			return "VF_DisabledByVehicleUpgrading".Translate(Vehicle.LabelCap);

		return true;
	}

	public override void CompTickRare()
	{
		base.CompTickRare();
		if (Vehicle.Spawned)
		{
			ValidateListers();
		}
	}

	private void ValidateListers()
	{
		if (!Vehicle.Spawned)
			return;

		if (Upgrading)
		{
			if (upgrade.Removal || StoredCostSatisfied)
			{
				Vehicle.Map.GetCachedMapComponent<VehicleReservationManager>()
				 .RegisterLister(Vehicle, ReservationType.Upgrade);
				Vehicle.Map.GetCachedMapComponent<VehicleReservationManager>()
				 .RemoveLister(Vehicle, ReservationType.LoadUpgradeMaterials);
			}
			else
			{
				Vehicle.Map.GetCachedMapComponent<VehicleReservationManager>()
				 .RegisterLister(Vehicle, ReservationType.LoadUpgradeMaterials);
				Vehicle.Map.GetCachedMapComponent<VehicleReservationManager>()
				 .RemoveLister(Vehicle, ReservationType.Upgrade);
			}
		}
		else
		{
			Vehicle.Map.GetCachedMapComponent<VehicleReservationManager>()
			 .RemoveLister(Vehicle, ReservationType.Upgrade);
			Vehicle.Map.GetCachedMapComponent<VehicleReservationManager>()
			 .RemoveLister(Vehicle, ReservationType.LoadUpgradeMaterials);
		}
	}

	public override void EventRegistration()
	{
		base.EventRegistration();
		Vehicle.AddEvent(VehicleEventDefOf.UpgradeEnqueued, PrepVehicleForWork, ValidateListers);
		Vehicle.AddEvent(VehicleEventDefOf.UpgradeRefundEnqueued, PrepVehicleForWork, ValidateListers);
		Vehicle.AddEvent(VehicleEventDefOf.UpgradeCanceled, ValidateListers);
		Vehicle.AddEvent(VehicleEventDefOf.UpgradeCompleted, ValidateListers);
		Vehicle.AddEvent(VehicleEventDefOf.UpgradeRefundCompleted, ValidateListers);
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		if (Upgrading)
		{
			Command_Action cancelLoad = new()
			{
				defaultLabel = "DesignatorCancel".Translate(),
				defaultDesc = "VF_CancelUpgrade".Translate(Vehicle.LabelCap),
				icon = CancelIcon,
				activateSound = SoundDefOf.Tick_Low,
				hotKey = KeyBindingDefOf.Designator_Cancel,
				action = ClearUpgrade
			};
			yield return cancelLoad;
		}
	}

	public override string CompInspectStringExtra()
	{
		if (upgrade != null)
		{
			if (StoredCostSatisfied)
				return $"{"WorkLeft".Translate()}: {Vehicle.CompUpgradeTree.upgrade.WorkLeft.ToStringWorkAmount()}";

			using ClearStringOnDispose csd = new(InspectStringBuilder);
			foreach (ThingDefCountClass thingDefCount in NodeUnlocking.ingredients)
			{
				int count = upgradeContainer.TotalStackCountOfDef(thingDefCount.thingDef);
				if (count > 0)
				{
					InspectStringBuilder.AppendWithComma(thingDefCount.Summary);
				}
			}
			return InspectStringBuilder.ToString();
		}
		return null;
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Collections.Look(ref upgrades, nameof(upgrades), LookMode.Value);
		Scribe_Values.Look(ref nodeUnlocking, nameof(nodeUnlocking));
		Scribe_Deep.Look(ref upgrade, nameof(upgrade));
		Scribe_Deep.Look(ref upgradeContainer, nameof(upgradeContainer));

		upgrades ??= [];
		upgradeContainer ??= [];
	}
}