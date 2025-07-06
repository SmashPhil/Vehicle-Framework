using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Vehicles;

public partial class VehiclePawn
{
  public const int MaxTickInterval = GenTicks.TickRareInterval;

  [Unsaved]
  public VehicleSustainers sustainers;

  private List<TimedExplosion> explosives = [];

  // Vehicles should never be suspended since there is no logic for handling passengers in a
  // suspended vehicle. Suspending the vehicle would also suspend all passengers by proxy.
  public override bool Suspended => false;

  public int AttachedExplosives => explosives.Count;

  // Pawn has null held things
  bool IThingHolderTickable.ShouldTickContents => false;

  protected override int MaxTickIntervalRate => MaxTickInterval;

  public override int UpdateRateTicks
  {
    get
    {
      if (AllPawnsAboard.Count == 0 && compTickers.Count == 0)
        return MaxTickInterval;
      return base.UpdateRateTicks;
    }
  }

  public void AddTimedExplosion(TimedExplosion exploder)
  {
    explosives.Add(exploder);
    DrawTracker.AddRenderer(exploder);
  }

  public TimedExplosion AddTimedExplosion(TimedExplosion.Data explosionData,
    DrawOffsets drawOffsets = null)
  {
    TimedExplosion exploder = new(this, explosionData, drawOffsets: drawOffsets);
    AddTimedExplosion(exploder);
    return exploder;
  }

  protected override void Tick()
  {
    BaseTickOptimized();
    TickAllComps();
    if (Faction != Faction.OfPlayer)
    {
      vehicleAI?.AITick();
    }
  }

  public bool RequestTickStart<T>(T comp) where T : ThingComp
  {
    if (!compTickers.Contains(comp))
    {
      compTickers.Add(comp);
      return true;
    }
    return false;
  }

  public bool RequestTickStop<T>(T comp) where T : ThingComp
  {
    if (!VehicleMod.settings.main.opportunisticTicking)
    {
      // If opportunistic ticking is off, disallow removal from ticker list.
      // VehicleComp should then always tick.
      return false;
    }
    return compTickers.Remove(comp);
  }

  private void TickExplosives()
  {
    for (int i = explosives.Count - 1; i >= 0; i--)
    {
      TimedExplosion timedExplosion = explosives[i];
      if (!timedExplosion.Tick())
      {
        explosives.Remove(timedExplosion);
        DrawTracker.RemoveRenderer(timedExplosion);
      }
    }
  }

  protected virtual void TickAllComps()
  {
    for (int i = compTickers.Count - 1; i >= 0; i--)
    {
      // Must run back to front in case CompTick methods trigger their own removal
      compTickers[i].CompTick();
    }
    // TODO - should check leaking when vehicle takes damage
    // Leak tick is separate from tick by request so the fuel can continue to leak even if
    // the comp itself does not need to be ticking.
    CompFueledTravel?.LeakTick();
  }

  public override void TickRare()
  {
    base.TickRare();
    EventRegistry[VehicleEventDefOf.ScanRare].ExecuteEvents();
  }

  private void TickShort()
  {
    EventRegistry[VehicleEventDefOf.ScanShort].ExecuteEvents();
  }

  protected override void TickInterval(int delta)
  {
    ageTracker.AgeTickInterval(delta);
    records.RecordsTickInterval(delta);
    if (!this.IsWorldPawn())
      jobs.JobTrackerTickInterval(delta);

    // TODO
    //if (currentlyFishing && Find.TickManager.TicksGame % 240 == 0)
    //{
    //  if (AllPawnsAboard.Count == 0)
    //  {
    //    currentlyFishing = false;
    //  }
    //  else
    //  {
    //    IntVec3 cell = this.OccupiedRect().ExpandedBy(1).EdgeCells.RandomElement();
    //    MoteMaker.MakeStaticMote(cell, Map, ThingDefOf_VehicleMotes.Mote_FishingNet);
    //  }
    //}
  }

  protected virtual void BaseTickOptimized()
  {
    const int ScanShortTicks = 60;
    const int ScanRareTicks = GenTicks.TickRareInterval;

    if (this.IsHashIntervalTick(ScanShortTicks))
      TickShort();
    if (this.IsHashIntervalTick(ScanRareTicks))
      TickRare();

    sustainers.Tick();
    TickHandlers();

    if (Spawned)
    {
      //animator?.AnimationTick();
      vehiclePather.PatherTick();
      stances.StanceTrackerTick();
      if (Drafted || CompVehicleTurrets is { Deploying: true })
      {
        jobs.JobTrackerTick();
      }
      TickExplosives();
    }

    //abilities?.AbilitiesTick();
    inventory.innerContainer.DoTick();
    inventory.InventoryTrackerTick();
  }
}