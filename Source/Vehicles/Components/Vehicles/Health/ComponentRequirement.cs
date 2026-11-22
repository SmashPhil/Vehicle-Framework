using System;
using JetBrains.Annotations;
using SmashTools;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles;

[PublicAPI]
public class ComponentRequirement : IDisposable
{
  public const ComparisonType DefaultComparison = ComparisonType.GreaterThan;

  private string key;
  private float healthPercent;
  private ComparisonType comparison = DefaultComparison;

  [Unsaved]
  private VehiclePawn vehicle;

  /// <summary>
  /// For Xml-Deserialization only
  /// </summary>
  [UsedWithReflection]
  public ComponentRequirement()
  {
  }

  public ComponentRequirement(string key, float healthPercent, ComparisonType comparison = DefaultComparison)
  {
    this.key = key;
    this.healthPercent = healthPercent;
    this.comparison = comparison;
  }

  public VehicleComponent Component { get; private set; }

  public string Label => Component.props.label;

  public bool MeetsRequirements { get; private set; } = true;

  // TODO 1.8 - Remove
  [Obsolete("Use zero-argument overload instead.", error: true)]
  public void RecacheComponent(VehiclePawn vehicle)
  {
    Assert.AreEqual(vehicle, this.vehicle);
    RecacheComponent();
  }

  public void RecacheComponent()
  {
    Assert.IsNotNull(vehicle);
    Component = vehicle.statHandler.GetComponent(key);
  }

  public void Init(VehiclePawn vehicle)
  {
    if (this.vehicle != null)
    {
      Trace.Fail("Trying to double init ComponentRequirement.");
      return;
    }
    this.vehicle = vehicle;
    vehicle.AddEvent(VehicleEventDefOf.HealthChanged, OnHealthChanged);
    RecacheComponent();
    OnHealthChanged();
  }

  private void OnHealthChanged()
  {
    MeetsRequirements =
      Component != null && comparison.Compare(Component.HealthPercent, healthPercent);
  }

  public static ComponentRequirement CopyFrom(ComponentRequirement reference)
  {
    return new ComponentRequirement(reference.key, reference.healthPercent, reference.comparison);

  }

  public void Dispose()
  {
    vehicle?.RemoveEvent(VehicleEventDefOf.HealthChanged, OnHealthChanged);
    vehicle = null;
  }
}