using System;
using UnityEngine;
using Verse;

namespace Vehicles;

public class ActivatableThingComp
{
  private readonly VehiclePawn vehicle;

  private ThingComp comp;
  private int owners;
  private Type type;

  public ActivatableThingComp(VehiclePawn vehicle)
  {
    this.vehicle = vehicle;
  }

  private bool Deactivated => owners == 0;

  public Type Type => type;

  public ThingComp Comp => comp;

  public int Owners
  {
    get { return owners; }
    set
    {
      if (owners != value)
      {
        owners = Mathf.Clamp(value, 0, int.MaxValue);
        RevalidateCompStatus();
      }
    }
  }

  private void RevalidateCompStatus()
  {
    if (Deactivated)
    {
      if (vehicle.RemoveComp(comp))
      {
        vehicle.deactivatedComps.Add(comp);
        vehicle.deactivatedCompTypes.Add(comp.GetType());
        vehicle.activatableComps.Remove(this);
      }
    }
    else if (!vehicle.AllComps.Contains(comp))
    {
      vehicle.AddComp(comp);
    }
  }

  public void Init(ThingComp comp)
  {
    this.comp = comp;
    type = comp.GetType();
  }
}