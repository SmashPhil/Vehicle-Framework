using DevTools.Testing;
using SmashTools;

namespace Vehicles.Tests;

[TestFixture(TestType.Playing)]
[TestCategory(TestCategoryNames.VehiclePawn, TestCategoryNames.Events)]
[TestDescription("Component requirements that track vehicle component health.")]
internal class Test_ComponentRequirement
{
  private const string ComponentKey = "Engine";
  private const string ComponentLabel = "Engine component";
  private const string OtherKey = "Armor";
  private const string OtherLabel = "Armor component";
  private const int MaxHealth = 100;

  [Test]
  private void InitCachesComponentAndLabel()
  {
    using VehicleGroup group = CreateGroup();
    VehicleComponent component = group.vehicle.statHandler.GetComponent(ComponentKey);
    using ComponentRequirement requirement = new(ComponentKey, healthPercent: 0.5f);
    Expect.IsTrue(requirement.MeetsRequirements);

    requirement.Init(group.vehicle);

    Expect.ReferencesAreEqual(component, requirement.Component);
    Expect.AreEqual(ComponentLabel, requirement.Label);
    Expect.IsTrue(requirement.MeetsRequirements);
  }

  [Test]
  private void MissingComponentDoesNotMeetRequirement()
  {
    using VehicleGroup group = CreateGroup();
    using ComponentRequirement requirement = new("Missing", healthPercent: 0.5f);
    requirement.Init(group.vehicle);

    Expect.IsNull(requirement.Component);
    Expect.IsFalse(requirement.MeetsRequirements);
  }

  [Test]
  private void HealthChangedUpdatesRequirement()
  {
    using VehicleGroup group = CreateGroup();
    VehicleComponent component = group.vehicle.statHandler.GetComponent(ComponentKey);
    using ComponentRequirement requirement = new(ComponentKey, healthPercent: 0.5f);
    requirement.Init(group.vehicle);

    component.SetHealth(health: 50);
    Expect.IsFalse(requirement.MeetsRequirements);

    component.SetHealth(health: 75);
    Expect.IsTrue(requirement.MeetsRequirements);
  }

  [Test]
  private void ConfiguredComparisonIsUsed()
  {
    using VehicleGroup group = CreateGroup();
    VehicleComponent component = group.vehicle.statHandler.GetComponent(ComponentKey);
    using ComponentRequirement requirement = new(ComponentKey, healthPercent: 0.5f,
      comparison: ComparisonType.LessThanOrEqual);
    requirement.Init(group.vehicle);
    Expect.IsFalse(requirement.MeetsRequirements);

    component.SetHealth(health: 50);
    Expect.IsTrue(requirement.MeetsRequirements);

    component.SetHealth(health: 51);
    Expect.IsFalse(requirement.MeetsRequirements);
  }

  [Test]
  private void RecacheComponentUsesConfiguredKey()
  {
    using VehicleGroup group = CreateGroup();
    using ComponentRequirement requirement = new(OtherKey, healthPercent: 0.5f);
    requirement.Init(group.vehicle);
    VehicleComponent other = group.vehicle.statHandler.GetComponent(OtherKey);

    requirement.RecacheComponent();

    Expect.ReferencesAreEqual(other, requirement.Component);
    Expect.AreEqual(OtherLabel, requirement.Label);
  }

  [Test]
  private void CopyFromCopiesConfiguration()
  {
    using VehicleGroup group = CreateGroup();
    using ComponentRequirement reference = new(OtherKey, healthPercent: 0.6f,
      comparison: ComparisonType.LessThanOrEqual);
    reference.Init(group.vehicle);
    using ComponentRequirement copy = ComponentRequirement.CopyFrom(reference);
    Expect.ReferencesAreNotEqual(reference, copy);
    Expect.IsNull(copy.Component);

    copy.Init(group.vehicle);
    Expect.ReferencesAreEqual(group.vehicle.statHandler.GetComponent(OtherKey), copy.Component);

    VehicleComponent other = copy.Component;
    other.SetHealth(health: 60);
    Expect.IsTrue(copy.MeetsRequirements);
    other.SetHealth(health: 70);
    Expect.IsFalse(copy.MeetsRequirements);
  }

  [Test]
  private void DisposeUnsubscribes()
  {
    using VehicleGroup group = CreateGroup();
    VehicleComponent component = group.vehicle.statHandler.GetComponent(ComponentKey);
    int count = HealthListenerCount(group.vehicle);
    ComponentRequirement requirement = new(ComponentKey, healthPercent: 0.5f);
    try
    {
      requirement.Init(group.vehicle);
      Expect.AreEqual(count + 1, HealthListenerCount(group.vehicle));
      component.SetHealth(health: 50);
      Expect.IsFalse(requirement.MeetsRequirements);
    }
    finally
    {
      requirement.Dispose();
    }

    component.SetHealth(health: 75);

    Expect.AreEqual(count, HealthListenerCount(group.vehicle));
    Expect.IsFalse(requirement.MeetsRequirements);
  }

  [Test]
  private void CopiesDisposeIndependently()
  {
    using VehicleGroup group = CreateGroup();
    VehicleComponent component = group.vehicle.statHandler.GetComponent(ComponentKey);
    int count = HealthListenerCount(group.vehicle);
    using ComponentRequirement reference = new(ComponentKey, healthPercent: 0.5f);
    ComponentRequirement first = ComponentRequirement.CopyFrom(reference);
    using ComponentRequirement second = ComponentRequirement.CopyFrom(reference);
    try
    {
      first.Init(group.vehicle);
      second.Init(group.vehicle);
      Expect.AreEqual(count + 2, HealthListenerCount(group.vehicle));
    }
    finally
    {
      first.Dispose();
    }

    component.SetHealth(health: 50);

    Expect.AreEqual(count + 1, HealthListenerCount(group.vehicle));
    Expect.IsTrue(first.MeetsRequirements);
    Expect.IsFalse(second.MeetsRequirements);
  }

  private static int HealthListenerCount(VehiclePawn vehicle)
  {
    return vehicle.EventRegistry[VehicleEventDefOf.HealthChanged].TotalEventCount;
  }

  private static VehicleGroup CreateGroup()
  {
    return VehicleGroup.CreateBasicVehicleGroup(new VehicleGroup.MockSettings
    {
      permissions = VehiclePermissions.Mobile | VehiclePermissions.Autonomous,
      components =
      [
        new VehicleComponentProperties
        {
          key = ComponentKey,
          label = ComponentLabel,
          health = MaxHealth
        },
        new VehicleComponentProperties
        {
          key = OtherKey,
          label = OtherLabel,
          health = MaxHealth
        }
      ]
    });
  }
}
