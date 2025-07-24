using System;
using System.Collections.Generic;
using Verse;

namespace Vehicles;

public class CompUpgrade : Upgrade
{
  public List<CompProperties> activate;
  public List<CompProperties> deactivate;

  public override bool UnlockOnLoad => true;

  public override void Unlock(VehiclePawn vehicle, bool unlockingPostLoad)
  {
    if (!activate.NullOrEmpty())
    {
      foreach (CompProperties compProperties in activate)
      {
        ActivateComp(vehicle, compProperties);
      }
    }

    if (!deactivate.NullOrEmpty())
    {
      foreach (CompProperties compProperties in deactivate)
      {
        DeactivateComp(vehicle, compProperties);
      }
    }
  }

  public override void Refund(VehiclePawn vehicle)
  {
    if (!activate.NullOrEmpty())
    {
      foreach (CompProperties compProperties in activate)
      {
        DeactivateComp(vehicle, compProperties);
      }
    }
    if (!deactivate.NullOrEmpty())
    {
      foreach (CompProperties compProperties in deactivate)
      {
        ActivateComp(vehicle, compProperties);
      }
    }
  }

  private static void ActivateComp(VehiclePawn vehicle, CompProperties compProperties)
  {
    ThingComp thingComp = vehicle.GetDeactivatedComp(compProperties.compClass);
    thingComp ??= vehicle.GetComp(compProperties.compClass);
    if (thingComp == null)
    {
      thingComp = CreateComp(vehicle, compProperties);
      vehicle.AddComp(thingComp);
      thingComp.Initialize(compProperties);
    }
    vehicle.ActivateComp(thingComp);
  }

  private static void DeactivateComp(VehiclePawn vehicle, CompProperties compProperties)
  {
    Type compType = compProperties.compClass;
    ThingComp comp = vehicle.GetComp(compType);
    vehicle.DeactivateComp(comp);
  }

  public static ThingComp CreateComp(ThingWithComps parent, CompProperties compProperties)
  {
    ThingComp thingComp = (ThingComp)Activator.CreateInstance(compProperties.compClass);
    thingComp.parent = parent;
    return thingComp;
  }
}