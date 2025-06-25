using System;
using System.Collections.Generic;
using RimWorld;
using SmashTools;
using UnityEngine;
using Verse;

namespace Vehicles;

public partial class VehiclePawn
{
  public List<TransferableOneWay> cargoToLoad;
  private bool outOfFoodNotified;
  public bool showAllItemsOnMap;

  public IntVec3 FollowerCell { get; protected set; }

  public int AddOrTransfer(Thing thing)
  {
    return AddOrTransfer(thing, thing.stackCount);
  }

  public int AddOrTransfer(Thing thing, int count)
  {
    int result = inventory.innerContainer.TryAddOrTransfer(thing, count);
    EventRegistry[VehicleEventDefOf.CargoAdded].ExecuteEvents();
    return result;
  }

  public Thing TakeFromInventory(Thing thing)
  {
    return inventory.innerContainer.Take(thing, thing.stackCount);
  }

  public Thing TakeFromInventory(Thing thing, int count)
  {
    Thing removedThing = inventory.innerContainer.Take(thing, count);
    EventRegistry[VehicleEventDefOf.CargoRemoved].ExecuteEvents();
    return removedThing;
  }

  protected IntVec3 CalculateOffset(Rot8 rot)
  {
    int offset = VehicleDef.Size.z; //Not reduced by half to avoid overlaps with vehicle tracks
    int offsetX = Mathf.CeilToInt(offset * Mathf.Cos(Mathf.Deg2Rad * 45));
    int offsetZ = Mathf.CeilToInt(offset * Mathf.Sin(Mathf.Deg2Rad * 45));
    IntVec3 root = PositionHeld;
    return rot.AsByte switch
    {
      Rot8.NorthInt     => new IntVec3(root.x, root.y, root.z - offset),
      Rot8.EastInt      => new IntVec3(root.x - offset, root.y, root.z),
      Rot8.SouthInt     => new IntVec3(root.x, root.y, root.z + offset),
      Rot8.WestInt      => new IntVec3(root.x + offset, root.y, root.z),
      Rot8.NorthEastInt => new IntVec3(root.x - offsetX, root.y, root.z - offsetZ),
      Rot8.SouthEastInt => new IntVec3(root.x - offsetX, root.y, root.z + offsetZ),
      Rot8.SouthWestInt => new IntVec3(root.x + offsetX, root.y, root.z + offsetZ),
      Rot8.NorthWestInt => new IntVec3(root.x + offsetX, root.y, root.z - offsetZ),
      _                 => throw new NotImplementedException()
    };
  }

  public void RecalculateFollowerCell()
  {
    IntVec3 result = IntVec3.Invalid;
    if (Map != null)
    {
      Rot8 rot = FullRotation;
      for (int i = 0; i < 8; i++)
      {
        result = CalculateOffset(rot);
        if (result.InBounds(Map))
        {
          break;
        }
        rot.Rotate(RotationDirection.Clockwise);
      }
    }
    FollowerCell = result;
  }
}