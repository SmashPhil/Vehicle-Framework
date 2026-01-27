using System;
using System.Collections.Generic;
using RimWorld;
using SmashTools;
using Verse;

namespace Vehicles;

// TODO - use RoleAssignment
public static class RoleHelper
{
  public static bool CanAddToCargo(this Pawn pawn, VehiclePawn vehicle)
  {
    if (!pawn.ShouldAlwaysTransferToVehiclesCargo())
      return false;
    if (MassUtility.IsOverEncumbered(vehicle))
      return false;

    float vehicleMass = MassUtility.InventoryMass(pawn);
    float mass = MassUtility.GearAndInventoryMass(pawn);
    return vehicleMass + mass <= vehicle.GetStatValue(VehicleStatDefOf.CargoCapacity);
  }

  public static bool CanBoardVehicle(this Pawn pawn, VehiclePawn vehicle)
  {
    return vehicle.GetHandlerFor(pawn) != null;
  }

  public static VehicleRoleHandler GetHandlerFor(this VehiclePawn vehicle, Pawn pawn)
  {
    foreach (VehicleRoleHandler handler in vehicle.Handlers)
    {
      if (handler.AreSlotsAvailable && handler.CanOperateRole(pawn))
        return handler;
    }
    return null;
  }

  public static void Distribute(in List<VehiclePawn> vehicles, List<Pawn> pawns)
  {
    if (vehicles.NullOrEmpty())
    {
      Trace.Fail("Trying to distribute to pawns with no vehicles listed.");
      return;
    }
    Distributor distributor = new(vehicles, pawns);
    distributor.DistributeOnPriority(HandlingType.Movement);
    distributor.DistributeOnPriority(HandlingType.Turret);
    distributor.DistributeToAnyRole();
  }

  public static void DistributeAll(in List<VehiclePawn> vehicles, List<Pawn> pawns)
  {
    if (vehicles.NullOrEmpty())
    {
      Trace.Fail("Trying to distribute to pawns with no vehicles listed.");
      return;
    }
    Distributor distributor = new(vehicles, pawns);
    distributor.DistributeNonColonistsToCargo();
    distributor.DistributeOnPriority(HandlingType.Movement);
    distributor.DistributeOnPriority(HandlingType.Turret);
    distributor.DistributeToAnyRole();
    distributor.DistributeFallbackToCargo();
  }

  private readonly struct Distributor
  {
    private readonly RotatingList<VehiclePawn> vehicles;
    private readonly List<Pawn> pawns;

    public Distributor(List<VehiclePawn> vehicles, List<Pawn> pawns)
    {
      pawns.RemoveAll(Ext_Vehicles.InVehicle);
      this.pawns = pawns;
      this.vehicles = [.. vehicles];
    }

    private VehiclePawn GetNextAvailableVehicle(Pawn pawn, Func<Pawn, VehiclePawn, bool> predicate)
    {
      int index = vehicles.Index;
      do
      {
        VehiclePawn vehicle = vehicles.Next;
        if (predicate(pawn, vehicle))
          return vehicle;
      }
      while (vehicles.Index != index);

      return null;
    }

    /// <summary>
    /// Board pawns that must always be loaded into cargo
    /// </summary>
    public void DistributeNonColonistsToCargo()
    {
      for (int i = pawns.Count - 1; i >= 0; i--)
      {
        Pawn pawn = pawns[i];
        if (!pawn.ShouldAlwaysTransferToVehiclesCargo())
          continue;

        if (GetNextAvailableVehicle(pawn, CanAddToCargo) is { } vehicle)
        {
          vehicle.AddOrTransfer(pawn);
          pawns.RemoveAt(i);
        }
      }
    }

    /// <summary>
    /// Add pawns to cargo if they are eligible as a fallback for situations where all pawns
    /// want to be boarded if possible. i.e. Aerial vehicles and boats
    /// </summary>
    public void DistributeFallbackToCargo()
    {
      for (int i = pawns.Count - 1; i >= 0; i--)
      {
        Pawn pawn = pawns[i];
        if (!pawn.ShouldAlwaysTransferToVehiclesCargo())
          continue;

        if (GetNextAvailableVehicle(pawn, CanAddToCargo) is { } vehicle)
        {
          vehicle.AddOrTransfer(pawn);
          pawns.RemoveAt(i);
        }
      }
    }

    /// <summary>
    /// Distribute pawns that can operate roles of this handling type to those roles.
    /// </summary>
    /// <param name="handlingType"></param>
    public void DistributeOnPriority(HandlingType handlingType)
    {
      for (int i = pawns.Count - 1; i >= 0; i--)
      {
        Pawn pawn = pawns[i];
        if (pawn.ShouldAlwaysTransferToVehiclesCargo())
          continue;

        VehicleRoleHandler handler = GetAvailableHandler(pawn, vehicles, handlingType);
        if (handler != null && handler.vehicle.TryAddPawn(pawn, handler))
        {
          pawns.RemoveAt(i);
        }
      }
      return;

      static VehicleRoleHandler GetAvailableHandler(Pawn pawn, List<VehiclePawn> vehicles, HandlingType handlingType)
      {
        foreach (VehiclePawn vehicle in vehicles)
        {
          VehicleRoleHandler handler = vehicle.GetNextAvailableHandler(pawn, handlingType);
          if (handler != null)
            return handler;
        }
        return null;
      }
    }

    /// <summary>
    /// Send all non-cargo pawns to unfilled roles.
    /// </summary>
    public void DistributeToAnyRole()
    {
      for (int i = pawns.Count - 1; i >= 0; i--)
      {
        Pawn pawn = pawns[i];
        if (GetNextAvailableVehicle(pawn, CanAddToVehicle) is { } vehicle && !vehicle.TryAddPawn(pawn))
        {
          Log.Error($"Unable to add {pawn} to vehicle {vehicle}.");
        }
      }
      return;

      static bool CanAddToVehicle(Pawn pawn, VehiclePawn vehicle)
      {
        foreach (VehicleRoleHandler handler in vehicle.handlers)
        {
          if ((handler.role.handlingTypes & HandlingType.Movement) != 0 && !handler.CanOperateRole(pawn))
            continue;

          if (handler.AreSlotsAvailable)
            return true;
        }
        return false;
      }
    }
  }
}