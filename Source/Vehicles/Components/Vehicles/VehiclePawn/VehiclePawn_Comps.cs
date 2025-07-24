using System;
using System.Collections.Generic;
using SmashTools;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles;

public partial class VehiclePawn
{
  [Unsaved]
  private bool fetchedCompVehicleTurrets;

  [Unsaved]
  private bool fetchedCompFuel;

  [Unsaved]
  private bool fetchedCompUpgradeTree;

  [Unsaved]
  private bool fetchedCompVehicleLauncher;

  [Unsaved]
  private CompVehicleTurrets compVehicleTurrets;

  [Unsaved]
  private CompFueledTravel compFuel;

  [Unsaved]
  private CompUpgradeTree compUpgradeTree;

  [Unsaved]
  private CompVehicleLauncher compVehicleLauncher;

  [Unsaved]
  private SelfOrderingList<ThingComp> cachedComps = [];

  [Unsaved]
  private List<ThingComp> compTickers = [];

  internal List<ThingComp> deactivatedComps = [];
  internal List<ActivatableThingComp> activatableComps = [];
  internal List<Type> deactivatedCompTypes = [];

  public CompVehicleTurrets CompVehicleTurrets
  {
    get
    {
      if (!fetchedCompVehicleTurrets)
      {
        compVehicleTurrets = GetCachedComp<CompVehicleTurrets>();
        fetchedCompVehicleTurrets = true;
      }
      return compVehicleTurrets;
    }
  }

  public CompFueledTravel CompFueledTravel
  {
    get
    {
      if (!fetchedCompFuel)
      {
        compFuel = GetCachedComp<CompFueledTravel>();
        fetchedCompFuel = true;
      }
      return compFuel;
    }
  }

  public CompUpgradeTree CompUpgradeTree
  {
    get
    {
      if (!fetchedCompUpgradeTree)
      {
        compUpgradeTree = GetCachedComp<CompUpgradeTree>();
        fetchedCompUpgradeTree = true;
      }
      return compUpgradeTree;
    }
  }

  public CompVehicleLauncher CompVehicleLauncher
  {
    get
    {
      if (!fetchedCompVehicleLauncher)
      {
        compVehicleLauncher = GetCachedComp<CompVehicleLauncher>();
        fetchedCompVehicleLauncher = true;
      }
      return compVehicleLauncher;
    }
  }

  public void AddComp(ThingComp thingComp)
  {
    AllComps.Add(thingComp);
    RecacheComponents();
  }

  public bool RemoveComp(ThingComp thingComp)
  {
    bool result = AllComps.Remove(thingComp);
    if (result)
    {
      RecacheComponents();
    }
    return result;
  }

  public void ActivateComp(ThingComp comp)
  {
    ActivatableThingComp activatableComp =
      activatableComps.FirstOrDefault(activatableComp => activatableComp.Type == comp.GetType());
    if (activatableComp == null)
    {
      activatableComp = new ActivatableThingComp(this);
      activatableComp.Init(comp);
      activatableComps.Add(activatableComp);
    }
    activatableComp.Owners++;
  }

  public void DeactivateComp(ThingComp comp)
  {
    foreach (ActivatableThingComp activatableComp in activatableComps)
    {
      if (activatableComp.Type == comp.GetType())
      {
        activatableComp.Owners--;
        return;
      }
    }
  }

  public T GetCachedComp<T>() where T : ThingComp
  {
    for (int i = 0; i < cachedComps.Count; i++)
    {
      if (cachedComps[i] is T t)
      {
        cachedComps.Touch(i);
        return t;
      }
    }
    return null;
  }

  public ThingComp GetComp(Type type)
  {
    // AllComps should always be initialized to new instance list, and never be null
    foreach (ThingComp thingComp in AllComps)
    {
      if (thingComp.GetType().SameOrSubclass(type))
        return thingComp;
    }
    return null;
  }

  public ThingComp GetDeactivatedComp(Type type)
  {
    // AllComps should always be initialized to new instance list, and never be null
    foreach (ThingComp thingComp in deactivatedComps)
    {
      if (thingComp.GetType().SameOrSubclass(type))
        return thingComp;
    }
    return null;
  }

  protected virtual void RecacheComponents()
  {
    fetchedCompVehicleTurrets = false;
    fetchedCompFuel = false;
    fetchedCompUpgradeTree = false;
    fetchedCompVehicleLauncher = false;

    cachedComps.Clear();
    if (!AllComps.NullOrEmpty())
    {
      cachedComps.AddRange(AllComps);
    }
    RecacheCompTickers();
  }

  private void RecacheCompTickers()
  {
    compTickers.Clear();
    foreach (ThingComp thingComp in AllComps)
    {
      if (!(thingComp is VehicleComp vehicleComp) || !vehicleComp.TickByRequest)
      {
        compTickers.Add(thingComp);
      }
    }
  }

  private void LoadVarsActivatableComps()
  {
    Assert.IsTrue(Scribe.mode == LoadSaveMode.LoadingVars);
    if (CompUpgradeTree == null || activatableComps.NullOrEmpty())
      return;

    foreach (ActivatableThingComp activatableComp in activatableComps)
    {
      if (activatableComp.Comp == null)
      {
        Log.Error($"Unable to load variables from {activatableComp.Type?.Name ?? "NULL"}");
        return;
      }
      activatableComp.Comp.PostExposeData();
    }
  }
}