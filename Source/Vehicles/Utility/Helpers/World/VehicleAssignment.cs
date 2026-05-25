using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SmashTools;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles;

[PublicAPI]
public sealed class VehicleAssignment
{
  private static readonly List<AssignedSeat> EmptyAssignments = [];

  private readonly Dictionary<VehiclePawn, List<AssignedSeat>> vehicleAssignments = [];
  private readonly Dictionary<Pawn, AssignedSeat> pawnAssignment = [];

  public Dictionary<Pawn, AssignedSeat> AllAssignments => pawnAssignment;

  public void Clear()
  {
    vehicleAssignments.Clear();
    pawnAssignment.Clear();
  }

  [Pure]
  public bool IsAssigned(Pawn pawn)
  {
    return pawnAssignment.ContainsKey(pawn);
  }

  [Pure]
  public AssignedSeat GetAssignment(Pawn pawn)
  {
    return pawnAssignment.TryGetValue(pawn);
  }

  [Pure]
  public List<AssignedSeat> GetAssignments(VehiclePawn vehicle)
  {
    return vehicleAssignments.TryGetValue(vehicle, fallback: EmptyAssignments);
  }

  public void RemoveAll(Predicate<Pawn> validator)
  {
    bool anyRemoved = false;
    // Easier to just copy list and remove directly from dict
    foreach (Pawn pawn in pawnAssignment.Keys.ToList())
    {
      if (validator(pawn))
        anyRemoved |= pawnAssignment.Remove(pawn);
    }
    if (anyRemoved)
      UpdateVehicleAssignments();
  }

  public void RemoveAssignment(Pawn pawn)
  {
    Assert.IsFalse(pawn is VehiclePawn);
    pawnAssignment.Remove(pawn);
    UpdateVehicleAssignments();
  }

  public void RemoveAssignments(VehiclePawn vehicle)
  {
    vehicleAssignments.Remove(vehicle);
    UpdatePawnAssignments();
  }

  public void SetAssignment(AssignedSeat assignment)
  {
    pawnAssignment.Remove(assignment.pawn);
    pawnAssignment[assignment.pawn] = assignment;
    UpdateVehicleAssignments();
  }

  public void SetAssignments(VehiclePawn vehicle, List<AssignedSeat> assignments)
  {
    vehicleAssignments[vehicle] = assignments;
    UpdatePawnAssignments();
  }

  private void UpdatePawnAssignments()
  {
    pawnAssignment.Clear();
    foreach (AssignedSeat assignment in vehicleAssignments.SelectMany(kvp => kvp.Value))
      pawnAssignment[assignment.pawn] = assignment;
  }

  private void UpdateVehicleAssignments()
  {
    vehicleAssignments.Clear();
    foreach (AssignedSeat seat in pawnAssignment.Values)
    {
      vehicleAssignments.AddOrAppend(seat.Vehicle, seat);
    }
  }
}