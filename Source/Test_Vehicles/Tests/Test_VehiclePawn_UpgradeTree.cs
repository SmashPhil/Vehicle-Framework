using System.Collections.Generic;
using System.Linq;
using DevTools.Testing;
using RimWorld;
using SmashTools;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles.Tests;

[TestFixture(TestType.Playing)]
[TestCategory(TestCategoryNames.VehiclePawn, TestCategoryNames.Events)]
[TestDescription("Upgrade tree node requirements and unlock behavior.")]
internal sealed class Test_VehiclePawn_UpgradeTree
{
  [Test]
  private void RootNodeNeedsNoPrerequisites()
  {
    UpgradeNode root = Node("root");
    using VehicleGroup group = CreateGroup(root);

    Assert.IsTrue(group.vehicle.CompUpgradeTree.PrerequisitesMet(root));
  }

  [Test]
  private void NodeNeedsEveryPrerequisiteUnlocked()
  {
    UpgradeNode first = Node("first");
    UpgradeNode second = Node("second");
    UpgradeNode child = Node("child");
    child.prerequisiteNodes = [first.key, second.key];
    using VehicleGroup group = CreateGroup(first, second, child);
    CompUpgradeTree tree = group.vehicle.CompUpgradeTree;

    Assert.IsFalse(tree.PrerequisitesMet(child));
    tree.FinishUnlock(first);
    Assert.IsFalse(tree.PrerequisitesMet(child));
    tree.FinishUnlock(second);
    Assert.IsTrue(tree.PrerequisitesMet(child));
  }

  [Test]
  private void NodeNeedsResearchEvenWithoutNodePrerequisites()
  {
    UpgradeNode node = Node("research");
    node.researchPrerequisites = [UnfinishedResearch()];
    using VehicleGroup group = CreateGroup(node);

    Assert.IsFalse(group.vehicle.CompUpgradeTree.PrerequisitesMet(node));
  }

  [Test]
  private void NodeNeedsResearchAfterNodePrerequisites()
  {
    UpgradeNode root = Node("root");
    UpgradeNode child = Node("child");
    child.prerequisiteNodes = [root.key];
    child.researchPrerequisites = [UnfinishedResearch()];
    using VehicleGroup group = CreateGroup(root, child);
    CompUpgradeTree tree = group.vehicle.CompUpgradeTree;

    tree.FinishUnlock(root);

    Assert.IsFalse(tree.PrerequisitesMet(child));
  }

  [Test]
  private void SingleConflictingNodeDisablesUpgrade()
  {
    UpgradeNode selected = Node("selected");
    UpgradeNode candidate = Node("candidate");
    candidate.disableIfUpgradeNodeEnabled = selected.key;
    using VehicleGroup group = CreateGroup(selected, candidate);
    CompUpgradeTree tree = group.vehicle.CompUpgradeTree;

    Assert.IsFalse(tree.Disabled(candidate));
    tree.FinishUnlock(selected);
    Assert.IsTrue(tree.Disabled(candidate));
  }

  [Test]
  private void AnyConflictingNodeDisablesUpgrade()
  {
    UpgradeNode first = Node("first");
    UpgradeNode second = Node("second");
    UpgradeNode candidate = Node("candidate");
    candidate.disableIfUpgradeNodesEnabled = [first.key, second.key];
    using VehicleGroup group = CreateGroup(first, second, candidate);
    CompUpgradeTree tree = group.vehicle.CompUpgradeTree;

    Assert.IsFalse(tree.Disabled(candidate));
    tree.FinishUnlock(second);
    Assert.IsTrue(tree.Disabled(candidate));
  }

  [Test]
  private void RootNodeFindsTopOfPrerequisiteChain()
  {
    UpgradeNode root = Node("root");
    UpgradeNode middle = Node("middle");
    UpgradeNode leaf = Node("leaf");
    middle.prerequisiteNodes = [root.key];
    leaf.prerequisiteNodes = [middle.key];
    using VehicleGroup group = CreateGroup(root, middle, leaf);

    Assert.IsTrue(ReferenceEquals(root, group.vehicle.CompUpgradeTree.RootNode(leaf)));
  }

  [Test]
  private void UnlockedChildPreventsRemovingParent()
  {
    UpgradeNode parent = Node("parent");
    UpgradeNode child = Node("child");
    child.prerequisiteNodes = [parent.key];
    using VehicleGroup group = CreateGroup(parent, child);
    CompUpgradeTree tree = group.vehicle.CompUpgradeTree;

    tree.FinishUnlock(parent);
    Assert.IsTrue(tree.LastNodeUnlocked(parent));
    tree.FinishUnlock(child);
    Assert.IsFalse(tree.LastNodeUnlocked(parent));
    tree.ResetUnlock(child);
    Assert.IsTrue(tree.LastNodeUnlocked(parent));
  }

  [Test]
  private void StartUnlockEnqueuesWork()
  {
    UpgradeNode node = Node("node");
    node.work = 12;
    using VehicleGroup group = CreateGroup(node);
    CompUpgradeTree tree = group.vehicle.CompUpgradeTree;
    using EventListener<VehicleEventDef> listener = new(group.vehicle, VehicleEventDefOf.UpgradeEnqueued);

    tree.StartUnlock(node);

    Assert.IsTrue(tree.Upgrading);
    Assert.IsTrue(ReferenceEquals(node, tree.NodeUnlocking));
    Assert.IsFalse(tree.upgrade.Removal);
    Assert.AreEqual(12f, tree.upgrade.WorkLeft);
    Assert.IsFalse(tree.NodeUnlocked(node));
    Assert.AreEqual(1, listener.CountRaised);
  }

  [Test]
  private void RemoveUnlockEnqueuesRefundWork()
  {
    UpgradeNode node = Node("node");
    using VehicleGroup group = CreateGroup(node);
    CompUpgradeTree tree = group.vehicle.CompUpgradeTree;
    tree.FinishUnlock(node);
    using EventListener<VehicleEventDef> listener = new(group.vehicle, VehicleEventDefOf.UpgradeRefundEnqueued);

    tree.RemoveUnlock(node);

    Assert.IsTrue(ReferenceEquals(node, tree.NodeUnlocking));
    Assert.IsTrue(tree.upgrade.Removal);
    Assert.IsTrue(tree.NodeUnlocked(node));
    Assert.AreEqual(1, listener.CountRaised);
  }

  [Test]
  private void ClearUpgradeCancelsQueuedWork()
  {
    UpgradeNode node = Node("node");
    using VehicleGroup group = CreateGroup(node);
    CompUpgradeTree tree = group.vehicle.CompUpgradeTree;
    tree.StartUnlock(node);
    using EventListener<VehicleEventDef> listener = new(group.vehicle, VehicleEventDefOf.UpgradeCanceled);

    tree.ClearUpgrade();

    Assert.IsFalse(tree.Upgrading);
    Assert.IsNull(tree.NodeUnlocking);
    Assert.IsFalse(tree.NodeUnlocked(node));
    Assert.AreEqual(1, listener.CountRaised);
  }

  [Test]
  private void QueuedUpgradePreventsDrafting()
  {
    UpgradeNode node = Node("node");
    using VehicleGroup group = CreateGroup(node);
    CompUpgradeTree tree = group.vehicle.CompUpgradeTree;

    Assert.IsTrue(tree.CanDraft().Accepted);
    tree.StartUnlock(node);
    Assert.IsFalse(tree.CanDraft().Accepted);
  }

  [Test]
  private void CancelingUpgradeReturnsStoredMaterials()
  {
    UpgradeNode node = Node("node");
    using VehicleGroup group = CreateGroup(node);
    CompUpgradeTree tree = group.vehicle.CompUpgradeTree;
    tree.StartUnlock(node);
    Thing steel = ThingMaker.MakeThing(ThingDefOf.Steel);
    using ScopeEntity scopeSteel = new(steel);
    tree.AddToContainer(steel, 1);

    tree.ClearUpgrade();

    Assert.AreEqual(0, tree.upgradeContainer.Count);
    Assert.IsTrue(steel.Spawned);
  }

  [Test]
  private void FinishingNodeAppliesMockUpgrade()
  {
    MockUpgrade upgrade = new();
    UpgradeNode node = Node("node");
    node.upgrades = [upgrade];
    using VehicleGroup group = CreateGroup(node);
    group.vehicle.CompUpgradeTree.StartUnlock(node);
    using EventListener<VehicleEventDef> listener = new(group.vehicle, VehicleEventDefOf.UpgradeCompleted);

    group.vehicle.CompUpgradeTree.FinishUnlock(node);

    Assert.IsTrue(group.vehicle.CompUpgradeTree.NodeUnlocked(node));
    Assert.AreEqual(1, upgrade.UnlockCalls);
    Assert.IsTrue(ReferenceEquals(group.vehicle, upgrade.LastVehicle));
    Assert.IsFalse(upgrade.LastUnlockingPostLoad);
    Assert.AreEqual(1, listener.CountRaised);
  }

  [Test]
  private void ResetNodeRefundsMockUpgrade()
  {
    MockUpgrade upgrade = new();
    UpgradeNode node = Node("node");
    node.upgrades = [upgrade];
    using VehicleGroup group = CreateGroup(node);
    CompUpgradeTree tree = group.vehicle.CompUpgradeTree;
    tree.FinishUnlock(node);
    using EventListener<VehicleEventDef> listener = new(group.vehicle, VehicleEventDefOf.UpgradeRefundCompleted);

    tree.ResetUnlock(node);

    Assert.IsFalse(tree.NodeUnlocked(node));
    Assert.AreEqual(1, upgrade.RefundCalls);
    Assert.IsTrue(ReferenceEquals(group.vehicle, upgrade.LastVehicle));
    Assert.AreEqual(1, listener.CountRaised);
  }

  [Test]
  private void ResetOfLockedNodeDoesNothing()
  {
    MockUpgrade upgrade = new();
    UpgradeNode node = Node("node");
    node.upgrades = [upgrade];
    using VehicleGroup group = CreateGroup(node);
    using EventListener<VehicleEventDef> listener = new(group.vehicle, VehicleEventDefOf.UpgradeRefundCompleted);

    group.vehicle.CompUpgradeTree.ResetUnlock(node);

    Assert.AreEqual(0, upgrade.RefundCalls);
    Assert.AreEqual(0, listener.CountRaised);
  }

  [Test]
  private void FinishingRemovalRefundsInsteadOfApplying()
  {
    MockUpgrade upgrade = new();
    UpgradeNode node = Node("node");
    node.upgrades = [upgrade];
    using VehicleGroup group = CreateGroup(node);
    CompUpgradeTree tree = group.vehicle.CompUpgradeTree;
    tree.FinishUnlock(node);
    tree.RemoveUnlock(node);

    tree.FinishUnlock(node);

    Assert.IsFalse(tree.NodeUnlocked(node));
    Assert.AreEqual(1, upgrade.UnlockCalls);
    Assert.AreEqual(1, upgrade.RefundCalls);
  }

  [Test]
  private void ReplacementRefundsPreviousNode()
  {
    MockUpgrade oldUpgrade = new();
    UpgradeNode oldNode = Node("old");
    oldNode.upgrades = [oldUpgrade];
    UpgradeNode replacement = Node("replacement");
    replacement.replaces = [oldNode.key];
    using VehicleGroup group = CreateGroup(oldNode, replacement);
    CompUpgradeTree tree = group.vehicle.CompUpgradeTree;
    tree.FinishUnlock(oldNode);

    tree.FinishUnlock(replacement);

    Assert.IsFalse(tree.NodeUnlocked(oldNode));
    Assert.IsTrue(tree.NodeUnlocked(replacement));
    Assert.AreEqual(1, oldUpgrade.RefundCalls);
  }

  [Test]
  private void MaterialCostNeedsEveryIngredient()
  {
    UpgradeNode node = Node("node");
    node.ingredients =
    [
      new ThingDefCountClass(ThingDefOf.Steel, 3),
      new ThingDefCountClass(ThingDefOf.ComponentIndustrial, 1)
    ];
    using VehicleGroup group = CreateGroup(node);
    CompUpgradeTree tree = group.vehicle.CompUpgradeTree;
    Assert.IsFalse(tree.StoredCostSatisfied);
    tree.StartUnlock(node);

    AddMaterial(tree, ThingDefOf.Steel, 3);
    Assert.IsFalse(tree.StoredCostSatisfied);
    AddMaterial(tree, ThingDefOf.ComponentIndustrial, 1);
    Assert.IsTrue(tree.StoredCostSatisfied);
  }

  [Test]
  private void MaterialsRequiredReturnsMissingAmount()
  {
    UpgradeNode node = Node("node");
    node.ingredients = [new ThingDefCountClass(ThingDefOf.Steel, 5)];
    using VehicleGroup group = CreateGroup(node);
    CompUpgradeTree tree = group.vehicle.CompUpgradeTree;
    tree.StartUnlock(node);

    Assert.AreEqual(5, node.MaterialsRequired(group.vehicle).Single().count);
    AddMaterial(tree, ThingDefOf.Steel, 2);
    Assert.AreEqual(3, node.MaterialsRequired(group.vehicle).Single().count);
    AddMaterial(tree, ThingDefOf.Steel, 3);
    Assert.IsFalse(node.MaterialsRequired(group.vehicle).Any());
  }

  [Test]
  private void FinishingUpgradeConsumesStoredMaterials()
  {
    UpgradeNode node = Node("node");
    node.ingredients = [new ThingDefCountClass(ThingDefOf.Steel, 1)];
    using VehicleGroup group = CreateGroup(node);
    CompUpgradeTree tree = group.vehicle.CompUpgradeTree;
    tree.StartUnlock(node);
    AddMaterial(tree, ThingDefOf.Steel, 1);

    tree.FinishUnlock(node);

    Assert.IsTrue(tree.NodeUnlocked(node));
    Assert.AreEqual(0, tree.upgradeContainer.Count);
  }

  [Test]
  private void NodeRecognizesRequiredIngredient()
  {
    UpgradeNode node = Node("node");
    node.ingredients = [new ThingDefCountClass(ThingDefOf.Steel, 1)];

    Assert.IsTrue(node.IsIngredient(ThingDefOf.Steel));
    Assert.IsFalse(node.IsIngredient(ThingDefOf.WoodLog));
  }

  [Test]
  private void AvailableSpaceStopsAtRequiredItemCount()
  {
    UpgradeNode node = Node("node");
    node.ingredients = [new ThingDefCountClass(ThingDefOf.Steel, 1)];
    using VehicleGroup group = CreateGroup(node);
    CompUpgradeTree tree = group.vehicle.CompUpgradeTree;
    tree.StartUnlock(node);
    Thing steel = ThingMaker.MakeThing(ThingDefOf.Steel);
    using ScopeEntity scopeSteel = new(steel);
    Thing wood = ThingMaker.MakeThing(ThingDefOf.WoodLog);
    using ScopeEntity scopeWood = new(wood);

    Assert.IsTrue(node.AvailableSpace(group.vehicle, steel));
    Assert.IsFalse(node.AvailableSpace(group.vehicle, wood));
    tree.AddToContainer(steel, 1);
    Assert.IsFalse(node.AvailableSpace(group.vehicle, steel));
  }

  [Test]
  private void AvailableSpaceAccountsForStackSize()
  {
    UpgradeNode node = Node("node");
    node.ingredients = [new ThingDefCountClass(ThingDefOf.Steel, 2)];
    using VehicleGroup group = CreateGroup(node);
    CompUpgradeTree tree = group.vehicle.CompUpgradeTree;
    tree.StartUnlock(node);
    AddMaterial(tree, ThingDefOf.Steel, 2);
    Thing extraSteel = ThingMaker.MakeThing(ThingDefOf.Steel);
    using ScopeEntity scopeSteel = new(extraSteel);

    Assert.IsFalse(node.AvailableSpace(group.vehicle, extraSteel));
  }

  [Test]
  private void NodeColorsApplyOnCompletion()
  {
    UpgradeNode node = Node("node");
    node.drawColorOne = Color.red;
    node.drawColorTwo = Color.green;
    node.drawColorThree = Color.blue;
    using VehicleGroup group = CreateGroup(node);

    group.vehicle.CompUpgradeTree.FinishUnlock(node);

    Assert.AreEqual(Color.red, group.vehicle.DrawColor);
    Assert.AreEqual(Color.green, group.vehicle.DrawColorTwo);
    Assert.AreEqual(Color.blue, group.vehicle.DrawColorThree);
  }

  [Test]
  private void UpgradeNodeIdentityUsesKey()
  {
    UpgradeNode first = Node("same");
    UpgradeNode second = Node("same");
    UpgradeNode different = Node("different");

    Assert.IsTrue(first.Equals(second));
    Assert.IsTrue(first == second);
    Assert.IsFalse(first == different);
    Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
  }

  [Test]
  private void RefundsIncludeStoredAndUnlockedMaterials()
  {
    UpgradeNode installed = Node("installed");
    installed.ingredients = [new ThingDefCountClass(ThingDefOf.Steel, 4)];
    UpgradeNode pending = Node("pending");
    pending.ingredients = [new ThingDefCountClass(ThingDefOf.ComponentIndustrial, 2)];
    using VehicleGroup group = CreateGroup(installed, pending);
    CompUpgradeTree tree = group.vehicle.CompUpgradeTree;
    tree.FinishUnlock(installed);
    tree.StartUnlock(pending);
    AddMaterial(tree, ThingDefOf.ComponentIndustrial, 2);

    var refunds = ((IRefundable)tree).Refunds.ToList();

    Assert.IsTrue(refunds.Any(refund =>
      refund.thingDef == ThingDefOf.Steel && Mathf.Approximately(refund.count, 4)));
    Assert.IsTrue(refunds.Any(refund =>
      refund.thingDef == ThingDefOf.ComponentIndustrial && Mathf.Approximately(refund.count, 2)));
  }

  [Test]
  private void SettingsCanBeAddedAndRemoved()
  {
    using VehicleGroup group = CreateGroup(Node("node"));
    CompUpgradeTree tree = group.vehicle.CompUpgradeTree;
    UpgradeState state = new() { key = "setting" };

    Assert.IsFalse(tree.TryGetStates(state.key, out _));
    tree.AddSettings(state);
    Assert.IsTrue(tree.TryGetStates(state.key, out List<UpgradeState> states));
    Assert.IsTrue(states.Contains(state));
    tree.RemoveSettings(state);
    Assert.IsFalse(states.Contains(state));
  }

  [Test]
  private void ResolvingNodeInitializesUpgrade()
  {
    MockUpgrade upgrade = new();
    UpgradeNode node = Node("node");
    node.upgrades = [upgrade];

    node.ResolveReferences();

    Assert.IsTrue(ReferenceEquals(node, upgrade.InitializedNode));
  }

  [Test]
  private void PostLoadingNodeNotifiesUpgrade()
  {
    MockUpgrade upgrade = new();
    UpgradeNode node = Node("node");
    node.upgrades = [upgrade];

    node.PostLoad();

    Assert.AreEqual(1, upgrade.PostLoadCalls);
  }

  [Test]
  private void NodeHasGraphicsWhenUpgradeDoes()
  {
    MockUpgrade upgrade = new() { Graphics = true };
    UpgradeNode node = Node("node");
    node.upgrades = [upgrade];

    node.ResolveReferences();

    Assert.IsTrue(node.HasGraphics);
  }

  private static UpgradeNode Node(string key)
  {
    return new UpgradeNode { key = key };
  }

  private static ResearchProjectDef UnfinishedResearch()
  {
    return new ResearchProjectDef
    {
      defName = "TestUnfinishedVehicleResearch",
      baseCost = 100
    };
  }

  private static VehicleGroup CreateGroup(params UpgradeNode[] nodes)
  {
    UpgradeTreeDef treeDef = new() { defName = "TestUpgradeTree", nodes = [.. nodes] };
    treeDef.ResolveReferences();
    VehicleGroup group = VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      comps = [new CompProperties_UpgradeTree { def = treeDef }]
    });
    group.Spawn();
    Assert.IsNotNull(group.vehicle.CompUpgradeTree);
    return group;
  }

  private static void AddMaterial(CompUpgradeTree tree, ThingDef def, int count)
  {
    Thing thing = ThingMaker.MakeThing(def);
    thing.stackCount = count;
    tree.AddToContainer(thing, count);
  }

  private sealed class MockUpgrade : Upgrade
  {
    public int UnlockCalls { get; private set; }
    public int RefundCalls { get; private set; }
    public int PostLoadCalls { get; private set; }
    public VehiclePawn LastVehicle { get; private set; }
    public UpgradeNode InitializedNode { get; private set; }
    public bool LastUnlockingPostLoad { get; private set; }
    public bool Graphics { get; init; }

    public override bool UnlockOnLoad => false;
    public override bool HasGraphics => Graphics;

    public override void Init(UpgradeNode upgradeNode)
    {
      base.Init(upgradeNode);
      InitializedNode = upgradeNode;
    }

    public override void PostLoad()
    {
      PostLoadCalls++;
    }

    public override void Unlock(VehiclePawn vehicle, bool unlockingPostLoad)
    {
      UnlockCalls++;
      LastVehicle = vehicle;
      LastUnlockingPostLoad = unlockingPostLoad;
    }

    public override void Refund(VehiclePawn vehicle)
    {
      RefundCalls++;
      LastVehicle = vehicle;
    }
  }
}
