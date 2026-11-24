using System;
using System.Collections.Generic;
using RimWorld;
using SmashTools;
using SmashTools.Rendering;
using UnityEngine.Assertions;
using Verse;
using Verse.Sound;

namespace Vehicles.Rendering;

public sealed class LightController
{
  private readonly VehiclePawn vehicle;

  private readonly List<PointLight> lights = [];
  
  public LightController(VehiclePawn vehicle)
  {
    this.vehicle = vehicle;

    vehicle.AddEvent(VehicleEventDefOf.Spawned, CreateLights);
    vehicle.AddEvent(VehicleEventDefOf.Despawned, DestroyLights);
  }

  private VehicleDef VehicleDef => vehicle.VehicleDef;

  private bool BelowLightLevel
  {
    get;
    set
    {
      if (field == value)
        return;

      field = value;
      DirtyAll();
    }
  } = true;

  private void CreateLights()
  {
    if (VehicleDef.lighting is not { lights.Count: > 0 })
      return;

    Assert.IsTrue(lights.Count == 0);
    Assert.IsTrue(vehicle.Spawned);

    Map map = vehicle.Map;
    GlowGridExtender extender = map.GetDetachedMapComponent<GlowGridExtender>();
    foreach (PointLightProperties props in VehicleDef.lighting.lights)
    {
      PointLight light = new(vehicle, props);
      extender.RegisterGlowEmitter(light.Spawn());
      lights.Add(light);
    }

    if (lights.Count > 0)
    {
      vehicle.AddEvent(this, VehicleEventDefOf.PositionChanged, DirtyAll);
      vehicle.AddEvent(this, VehicleEventDefOf.RotationChanged, DirtyAll);
      vehicle.AddEvent(this, VehicleEventDefOf.AngleChanged, DirtyAll);
      vehicle.AddEvent(this, VehicleEventDefOf.IgnitionOn, DirtyAll);
      vehicle.AddEvent(this, VehicleEventDefOf.IgnitionOff, DirtyAll);
      vehicle.AddEvent(this, VehicleEventDefOf.ScanRare, CheckLightLevel);
    }
    CheckLightLevel();
    UpdateLightStates();
  }

  private void DestroyLights()
  {
    if (lights.NullOrEmpty())
      return;

    Assert.IsTrue(vehicle.Spawned);
    foreach (PointLight light in lights)
    {
      try
      {
        light.Dispose();
      }
      catch (Exception ex)
      {
        Log.Error($"Exception thrown destroying {light}.\n{ex}");
      }
    }
    lights.Clear();
    vehicle.RemoveAllEvents(this);
  }

  private void DirtyAll()
  {
    if (UpdateLightStates())
    {
      SoundDefOf.FlickSwitch.PlayOneShot(new TargetInfo(vehicle.Position, vehicle.Map));
    }
  }

  private bool UpdateLightStates()
  {
    bool anyToggled = false;
    bool enable = vehicle.Drafted && BelowLightLevel;
    foreach (PointLight light in lights)
    {
      bool oldState = light.IsOn;
      light.SetVisible(enable);
      light.MarkDirty();
      anyToggled |= oldState != light.IsOn;
    }
    return anyToggled;
  }

  private void CheckLightLevel()
  {
    BelowLightLevel = VehicleMod.settings.main.headlights &&
                      vehicle.Map.skyManager.CurSkyGlow <= VehicleMod.settings.main.headlightLightLevel;
  }
}
