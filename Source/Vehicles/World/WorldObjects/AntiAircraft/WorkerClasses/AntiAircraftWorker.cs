using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Vehicles.World;

public abstract class AntiAircraftWorker
{
  protected AntiAircraftDef def;
  protected AirDefense airDefense;

  public AntiAircraftWorker(AirDefense airDefense, AntiAircraftDef def)
  {
    this.airDefense = airDefense;
    this.def = def;
  }

  public virtual AerialVehicleInFlight CurrentTarget => airDefense.activeTargets.FirstOrDefault();

  public virtual bool ShouldDrawSearchLight => true;

  public abstract void Launch();

  public virtual void Tick()
  {
  }

  public virtual void TickRare()
  {
  }

  public virtual void TickLong()
  {
  }

  public virtual bool CanUseAirDefense(WorldObject worldObject)
  {
    return worldObject is MapParent mapParent && mapParent.Faction != null &&
      mapParent.Faction.def.techLevel >= TechLevel.Industrial;
  }
}