using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;
using Verse.Sound;
using SmashTools;
using static UnityEngine.Scripting.GarbageCollector;
using Verse.Noise;

namespace Vehicles
{
	[StaticConstructorOnStartup]
	public class CompUpgradeTree : VehicleComp
	{
		private static readonly Material UnderfieldMat = MaterialPool.MatFrom("Things/Building/BuildingFrame/Underfield", ShaderDatabase.Transparent);
		private static readonly Texture2D CornerTex = ContentFinder<Texture2D>.Get("Things/Building/BuildingFrame/Corner", true);
		private static readonly Texture2D TileTex = ContentFinder<Texture2D>.Get("Things/Building/BuildingFrame/Tile", true);

		private Material cachedCornerMat;
		private Material cachedTileMat;

		private HashSet<string> upgrades = new HashSet<string>();

		private string nodeUnlocking;
		
		public UpgradeInProgress upgrade;

		public ThingOwner<Thing> upgradeContainer = new ThingOwner<Thing>();

		private Dictionary<string, List<UpgradeState>> states { get; set; } = new Dictionary<string, List<UpgradeState>>();

		public CompProperties_UpgradeTree Props => (CompProperties_UpgradeTree)props;

		public bool Upgrading => NodeUnlocking != null;

		public UpgradeNode NodeUnlocking => upgrade?.node;

		public Color FrameColor => new Color(0.6f, 0.6f, 0.6f);

		public float PercentComplete
		{
			get
			{
				if (Upgrading && NodeUnlocking.work > 0)
				{
					return 1 - upgrade.WorkLeft / NodeUnlocking.work;
				}
				return 0;
			}
		}

		private Material CornerMat
		{
			get
			{
				if (cachedCornerMat == null)
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
				if (cachedTileMat == null)
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
				{
					return false;
				}
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
			if (!node.prerequisiteNodes.NullOrEmpty())
			{
				foreach (string prerequisiteKey in node.prerequisiteNodes)
				{
					if (!upgrades.Contains(prerequisiteKey))
					{
						return false;
					}
				}
			}
			return true;
		}

		public bool Disabled(UpgradeNode node)
		{
			if (!node.disableIfUpgradeNodeEnabled.NullOrEmpty() && upgrades.Contains(node.disableIfUpgradeNodeEnabled))
			{
				return true;
			}
			if (!node.disableIfUpgradeNodesEnabled.NullOrEmpty() && node.disableIfUpgradeNodesEnabled.Any(key => upgrades.Contains(key)))
			{
				return true;
			}
			return false;
		}

		public bool LastNodeUnlocked(UpgradeNode node)
		{
			List<UpgradeNode> unlocksNodes = Vehicle.CompUpgradeTree.Props.def.nodes.FindAll(x => x.prerequisiteNodes.Contains(node.key));
			return !unlocksNodes.NotNullAndAny(preReqNode => Vehicle.CompUpgradeTree.NodeUnlocked(preReqNode));
		}

		/// <summary>
		/// Resets unlock and triggers refund event
		/// </summary>
		public void ResetUnlock(UpgradeNode node)
		{
			if (node is null || !upgrades.Contains(node.key))
			{
				return;
			}
			if (upgrades.Remove(node.key))
			{
				if (!node.upgrades.NullOrEmpty())
				{
					foreach (Upgrade upgrade in node.upgrades)
					{
						try
						{
							upgrade.Refund(Vehicle);
						}
						catch (Exception ex)
						{
							Log.Error($"{VehicleHarmony.LogLabel} Unable to reset {GetType()} to {Vehicle.LabelShort}. \nException: {ex}");
						}
					}
				}
				RefundNode(node);
				node.RemoveOverlays(Vehicle);
				node.resetSound?.PlayOneShot(new TargetInfo(Vehicle.Position, Vehicle.Map));
				Vehicle.EventRegistry[VehicleEventDefOf.VehicleUpgradeRefundCompleted].ExecuteEvents();
			}
		}

		private void RefundNode(UpgradeNode node)
		{
			if (!node.ingredients.NullOrEmpty())
			{
				List<ThingDefCount> refundList = new List<ThingDefCount>();
				foreach (ThingDefCountClass thingDefCountClass in node.ingredients)
				{
					(ThingDef thingDef, float count) = (thingDefCountClass.thingDef, thingDefCountClass.count);
					int refundCount;
					if (!node.refundLeavings.NullOrEmpty() && node.refundLeavings.TryGetValue(thingDef, out float fraction))
					{
						refundCount = Mathf.RoundToInt(count * fraction);
					}
					else
					{
						refundCount = Mathf.RoundToInt(count * node.refundFraction);
					}
					if (refundCount > 0)
					{
						refundList.Add(new ThingDefCount(thingDef, refundCount));
					}
				}
				if (!refundList.NullOrEmpty())
				{
					ThingOwner<Thing> thingOwner = new ThingOwner<Thing>();
					foreach (ThingDefCount thingDefCount in refundList)
					{
						Thing thing = ThingMaker.MakeThing(thingDefCount.ThingDef);
						thing.stackCount = thingDefCount.Count;
						thingOwner.TryAdd(thing);
					}
					thingOwner.TryDropAllOutsideVehicle(Vehicle.Map, Vehicle.OccupiedRect());
				}
			}
		}

		/// <summary>
		/// Unlocks node and triggers unlock event for all upgrades of that node
		/// </summary>
		public void FinishUnlock(UpgradeNode node)
		{
			if (upgrade != null && upgrade.Removal)
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
				foreach (Upgrade upgrade in node.upgrades)
				{
					try
					{
						upgrade.Unlock(Vehicle, false);
					}
					catch (Exception ex)
					{
						Log.Error($"{VehicleHarmony.LogLabel} Unable to unlock {GetType()} for {Vehicle.LabelShort}. \nException: {ex}");
					}
				}
			}
			node.AddOverlays(Vehicle);
			node.unlockSound?.PlayOneShot(new TargetInfo(Vehicle.Position, Vehicle.Map));
			upgrades.Add(node.key);

			upgradeContainer.ClearAndDestroyContents();

			Vehicle.EventRegistry[VehicleEventDefOf.VehicleUpgradeCompleted].ExecuteEvents();
		}

		public void ClearUpgrade()
		{
			Vehicle.Map.GetCachedMapComponent<VehicleReservationManager>().ClearReservedFor(Vehicle);
			upgradeContainer.TryDropAll(Vehicle.Position, Vehicle.Map, ThingPlaceMode.Near);
			upgrade = null;
		}

		/// <summary>
		/// Notifies job pool that vehicle has upgrade that wants to be unlocked, still requires job to be performed
		/// </summary>
		public void StartUnlock(UpgradeNode node)
		{
			upgrade = new UpgradeInProgress(Vehicle, node, false);
			upgradeContainer.TryDropAll(Vehicle.Position, Vehicle.Map, ThingPlaceMode.Near);
			Vehicle.EventRegistry[VehicleEventDefOf.VehicleUpgradeEnqueued].ExecuteEvents();
		}

		/// <summary>
		/// Notifies job pool that vehicle has upgrade that wants to be removed, still requires job to be performed
		/// </summary>
		public void RemoveUnlock(UpgradeNode node)
		{
			upgrade = new UpgradeInProgress(Vehicle, node, true);
			upgradeContainer.TryDropAll(Vehicle.Position, Vehicle.Map, ThingPlaceMode.Near);
			Vehicle.EventRegistry[VehicleEventDefOf.VehicleUpgradeRefundEnqueued].ExecuteEvents();
		}

		private void PrepVehicleForWork()
		{
			Vehicle.ignition.Drafted = false;
			Vehicle.Angle = 0;
			Vehicle.DisembarkAll();
		}

		private void ReloadUnlocks()
		{
			if (!upgrades.NullOrEmpty())
			{
				foreach (string key in upgrades)
				{
					UpgradeNode node = Props.def.GetNode(key);
					if (node != null)
					{
						node.AddOverlays(Vehicle);
						if (!node.upgrades.NullOrEmpty())
						{
							foreach (Upgrade upgrade in node.upgrades)
							{
								if (upgrade.UnlockOnLoad)
								{
									try
									{
										upgrade.Unlock(Vehicle, true);
									}
									catch (Exception ex)
									{
										Log.Error($"{VehicleHarmony.LogLabel} Unable to unlock {GetType()} post-load for {Vehicle.LabelShort}. \nException: {ex}");
									}
								}
							}
						}
					}
				}
			}
		}

		public void InitializeUpgradeTree()
		{
		}

		public void AddToContainer(ThingOwner<Thing> holder, Thing thing, int count)
		{
			holder.TryTransferToContainer(thing, upgradeContainer, count);
			ValidateListers();
		}

		public void AddSettings(UpgradeState state)
		{
			if (!states.ContainsKey(state.key))
			{
				states[state.key] = new List<UpgradeState>();
			}
			states[state.key].Add(state);
		}

		public bool TryGetStates(string key, out List<UpgradeState> outList)
		{
			return states.TryGetValue(key, out outList);
		}

		public void RemoveSettings(UpgradeState state)
		{
			if (!states.TryGetValue(state.key, out List<UpgradeState> innerList))
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
				Vector3 drawPos = Vehicle.DrawPos;
				Vector2 size = new Vector2(Vehicle.VehicleDef.Size.x, Vehicle.VehicleDef.Size.z);
				size.x *= 1.15f;
				size.y *= 1.15f;
				Vector3 s = new Vector3(size.x, 1f, size.y);
				Matrix4x4 matrix = default;
				matrix.SetTRS(drawPos, Vehicle.Rotation.AsQuat, s);
				Graphics.DrawMesh(MeshPool.plane10, matrix, UnderfieldMat, 0);
				int corners = 4;
				for (int i = 0; i < corners; i++)
				{
					float num2 = Mathf.Min(Vehicle.RotatedSize.x, Vehicle.RotatedSize.z) * 0.38f;
					IntVec3 intVec = default;
					if (i == 0)
					{
						intVec = new IntVec3(-1, 0, -1);
					}
					else if (i == 1)
					{
						intVec = new IntVec3(-1, 0, 1);
					}
					else if (i == 2)
					{
						intVec = new IntVec3(1, 0, 1);
					}
					else if (i == 3)
					{
						intVec = new IntVec3(1, 0, -1);
					}
					Vector3 b = default;
					b.x = intVec.x * (Vehicle.RotatedSize.x / 2f - num2 / 2f);
					b.z = intVec.z * (Vehicle.RotatedSize.z / 2f - num2 / 2f);
					Vector3 s2 = new Vector3(num2, 1f, num2);
					Matrix4x4 matrix2 = default;
					matrix2.SetTRS(drawPos + Vector3.up * 0.03f + b, new Rot4(i).AsQuat, s2);
					Graphics.DrawMesh(MeshPool.plane10, matrix2, CornerMat, 0);
				}
				int tiles = Mathf.CeilToInt(PercentComplete * Vehicle.RotatedSize.x * Vehicle.RotatedSize.z * 4); //4 tiles per cell
				IntVec2 intVec2 = Vehicle.RotatedSize * 2;
				for (int j = 0; j < tiles; j++)
				{
					IntVec2 intVec3 = default;
					intVec3.z = j / intVec2.x;
					intVec3.x = j - intVec3.z * intVec2.x;
					Vector3 a = new Vector3(intVec3.x * 0.5f, 0f, intVec3.z * 0.5f) + drawPos;
					a.x -= Vehicle.RotatedSize.x * 0.5f - 0.25f;
					a.z -= Vehicle.RotatedSize.z * 0.5f - 0.25f;
					Vector3 s3 = new Vector3(0.5f, 1f, 0.5f);
					Matrix4x4 matrix3 = default;
					matrix3.SetTRS(a + Vector3.up * 0.02f, Quaternion.identity, s3);
					Graphics.DrawMesh(MeshPool.plane10, matrix3, TileMat, 0);
				}
			}
		}

		public override bool CanDraft(out string failReason, out bool allowDevMode)
		{
			allowDevMode = false;
			if (Upgrading)
			{
				failReason = "VF_DisabledByVehicleUpgrading".Translate(Vehicle.LabelCap);
				return false;
			}
			return base.CanDraft(out failReason, out allowDevMode);
		}

		public override void PostGeneration()
		{
			InitializeUpgradeTree();
		}

		public override void PostLoad()
		{
			base.PostLoad();
			ReloadUnlocks();
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
		}

		public override void CompTickRare()
		{
			base.CompTickRare();
			if (Vehicle.Spawned)
			{
				ValidateListers();
			}
		}

		public void ValidateListers()
		{
			if (NodeUnlocking != null)
			{
				if (upgrade.Removal || StoredCostSatisfied)
				{
					Vehicle.Map.GetCachedMapComponent<VehicleReservationManager>().RegisterLister(Vehicle, ReservationType.Upgrade);
					Vehicle.Map.GetCachedMapComponent<VehicleReservationManager>().RemoveLister(Vehicle, ReservationType.LoadUpgradeMaterials);
				}
				else
				{
					Vehicle.Map.GetCachedMapComponent<VehicleReservationManager>().RegisterLister(Vehicle, ReservationType.LoadUpgradeMaterials);
					Vehicle.Map.GetCachedMapComponent<VehicleReservationManager>().RemoveLister(Vehicle, ReservationType.Upgrade);
				}
			}
			else
			{
				Vehicle.Map.GetCachedMapComponent<VehicleReservationManager>().RemoveLister(Vehicle, ReservationType.Upgrade);
				Vehicle.Map.GetCachedMapComponent<VehicleReservationManager>().RemoveLister(Vehicle, ReservationType.LoadUpgradeMaterials);
			}
		}

		public override void EventRegistration()
		{
			base.EventRegistration();
			Vehicle.AddEvent(VehicleEventDefOf.VehicleUpgradeEnqueued, PrepVehicleForWork);
			Vehicle.AddEvent(VehicleEventDefOf.VehicleUpgradeRefundEnqueued, PrepVehicleForWork);
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Collections.Look(ref upgrades, nameof(upgrades), LookMode.Value);
			Scribe_Values.Look(ref nodeUnlocking, nameof(nodeUnlocking));
			Scribe_Deep.Look(ref upgrade, nameof(upgrade));
			Scribe_Deep.Look(ref upgradeContainer, nameof(upgradeContainer));
		}
	}
}
