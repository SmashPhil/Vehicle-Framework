using System;
using SmashTools;
using SmashTools.Rendering;
using UnityEngine.Assertions;

namespace Vehicles.Rendering;

internal sealed class PointLight : IDisposable
{
  private readonly VehiclePawn vehicle;
  private readonly PointLightProperties props;

  private ComponentRequirement component;

  private GlowEmitter emitter;

  public PointLight(VehiclePawn vehicle, PointLightProperties props)
  {
    this.vehicle = vehicle;
    this.props = props;
  }

  public bool IsOn => IsVisible && emitter is { IsOn: true };

  public bool IsVisible { get; private set; }

  public void SetVisible(bool visible)
  {
    IsVisible = visible;
    emitter.IsOn = visible && component is not { MeetsRequirements: false };
  }

  public void MarkDirty()
  {
    emitter.MarkDirty();
  }

  public GlowEmitter Spawn()
  {
    Assert.IsNull(emitter);
    emitter = props.CreateNew(vehicle);
    if (props.component != null)
    {
      component = ComponentRequirement.CopyFrom(props.component);
      component.Init(vehicle);
      vehicle.AddEvent(VehicleEventDefOf.HealthChanged, OnHealthChange);
    }
    return emitter;
  }

  private void OnHealthChange()
  {
    SetVisible(IsVisible);
  }

  public void Dispose()
  {
    if (component != null)
    {
      vehicle.RemoveEvent(VehicleEventDefOf.HealthChanged, OnHealthChange);
      component.Dispose();
      component = null;
    }
    emitter?.Destroy();
    emitter = null;
  }

  public override string ToString()
  {
    string label = $"[{vehicle}]-{emitter?.GetType().ToString() ?? "NULL"}";
    return $"{label} ({props.position})";
  }
}
