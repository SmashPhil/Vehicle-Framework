using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles;

public class StashedVehicle : DynamicDrawnWorldObject, IThingHolder
{
  private ThingOwner<Thing> stash = [];

  private static readonly StringBuilder inspectStringBuilder = new();
  private static readonly Dictionary<VehicleDef, int> vehicleCounts = [];

  private Material cachedMaterial;

  public IEnumerable<VehiclePawn> Vehicles => stash.InnerListForReading
   .Where(thing => thing is VehiclePawn).Cast<VehiclePawn>();

  public override Material Material
  {
    get
    {
      if (cachedMaterial is null)
      {
        Color color = Faction?.Color ?? Color.white;
        VehiclePawn largestVehicle = Vehicles.MaxBy(vehicle => vehicle.VehicleDef.Size.Magnitude);
        string texPath = VehicleTex.CachedTextureIconPaths.TryGetValue(largestVehicle.VehicleDef,
          VehicleTex.DefaultVehicleIconTexPath);
        cachedMaterial = MaterialPool.MatFrom(texPath, ShaderDatabase.WorldOverlayTransparentLit,
          color, WorldMaterials.WorldObjectRenderQueue);
      }
      return cachedMaterial;
    }
  }

  public override void Draw()
  {
    if (!this.HiddenBehindTerrainNow())
    {
      WorldHelper.DrawQuadTangentialToPlanet(DrawPos, 0.7f * Find.WorldGrid.AverageTileSize, 0.015f,
        Material);
    }
  }

  public VehicleCaravan Notify_CaravanArrived(Caravan caravan)
  {
    if (caravan is VehicleCaravan vehicleCaravan)
    {
      if (vehicleCaravan.AerialVehicle || Vehicles.Any(vehicle => !vehicle.VehicleDef.canCaravan))
      {
        Messages.Message(
          "Unable to retrieve vehicle, aerial vehicles can't merge with other vehicle caravans.",
          MessageTypeDefOf.RejectInput, historical: false);
        return null;
      }
    }

    // Use separate list, caravan must relinquish ownership of pawns in order to add them to a new caravan
    List<Pawn> pawns = caravan.pawns.InnerListForReading.ToList();
    caravan.RemoveAllPawns();

    List<VehiclePawn> vehicles = [];
    foreach (Thing thing in stash.InnerListForReading.ToList())
    {
      if (thing is VehiclePawn vehicle)
      {
        stash.Remove(thing);
        vehicles.Add(vehicle);
      }
    }
    RoleHelper.Distribute(vehicles, pawns);
    // Pawns that were distributed between vehicles will not be part of the formation, but are
    // instead nested within the vehicle. Any remaining dismounted pawns + vehicles must be joined
    pawns.AddRange(vehicles);

    VehicleCaravan mergedCaravan =
      CaravanHelper.MakeVehicleCaravan(pawns, caravan.Faction, caravan.Tile, true);

    for (int i = stash.Count - 1; i >= 0; i--)
    {
      mergedCaravan.AddPawnOrItem(stash[i], true);
    }
    stash.Clear();

    Destroy();
    caravan.Destroy();

    return mergedCaravan;
  }

  public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
  {
    foreach (FloatMenuOption floatMenuOption in base.GetFloatMenuOptions(caravan))
    {
      yield return floatMenuOption;
    }
    foreach (FloatMenuOption floatMenuOption in CaravanArrivalAction_StashedVehicle
     .GetFloatMenuOptions(caravan, this))
    {
      yield return floatMenuOption;
    }
  }

  public override string GetInspectString()
  {
    inspectStringBuilder.Clear();
    vehicleCounts.Clear();
    {
      foreach (VehiclePawn vehicle in Vehicles)
      {
        if (!vehicleCounts.TryAdd(vehicle.VehicleDef, 1))
          vehicleCounts[vehicle.VehicleDef]++;
      }

      foreach ((VehicleDef vehicleDef, int count) in vehicleCounts)
      {
        inspectStringBuilder.AppendLine($"{count} {vehicleDef.LabelCap}");
      }
    }
    vehicleCounts.Clear();

    inspectStringBuilder.Append(base.GetInspectString());
    return inspectStringBuilder.ToString();
  }

  public override void Destroy()
  {
    base.Destroy();
    stash.ClearAndDestroyContentsOrPassToWorld();
    foreach (VehiclePawn vehicle in Vehicles)
      Find.WorldPawns.RemoveAndDiscardPawnViaGC(vehicle);
  }

  public override void ExposeData()
  {
    base.ExposeData();

    Scribe_Deep.Look(ref stash, nameof(stash), this);
  }

  public void GetChildHolders(List<IThingHolder> outChildren)
  {
    ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
  }

  public ThingOwner GetDirectlyHeldThings()
  {
    return stash;
  }

  public static StashedVehicle Create(VehicleCaravan vehicleCaravan, out Caravan caravan,
    List<TransferableOneWay> transferables = null)
  {
    const float MinTimeoutDays = 15;
    const float MaxTimeoutDays = 30;
    const float MinVehicleSize = 1;
    const float MaxVehicleSize = 10;

    caravan = null;
    if (vehicleCaravan.VehiclesListForReading.NullOrEmpty())
    {
      Log.Error("No vehicles in vehicle caravan for stashed vehicle.");
      return null;
    }

    StashedVehicle stashedVehicle =
      (StashedVehicle)WorldObjectMaker.MakeWorldObject(WorldObjectDefOfVehicles.StashedVehicle);
    stashedVehicle.Tile = vehicleCaravan.Tile;

    // Calculate days before removal from world map
    VehiclePawn largestVehicle =
      vehicleCaravan.VehiclesListForReading.MaxBy(vehicle => vehicle.VehicleDef.Size.Magnitude);
    float t = Ext_Math.ReverseInterpolate(largestVehicle.VehicleDef.Size.Magnitude, MinVehicleSize,
      MaxVehicleSize);
    float timeoutDays = Mathf.Lerp(MinTimeoutDays, MaxTimeoutDays, t);
    stashedVehicle.GetComponent<TimeoutComp>()
     .StartTimeout(Mathf.CeilToInt(timeoutDays * GenDate.TicksPerDay));

    List<Pawn> inventoryCandidates = [];

    caravan = CaravanMaker.MakeCaravan([], vehicleCaravan.Faction, vehicleCaravan.Tile, true);

    foreach (VehiclePawn vehicle in vehicleCaravan.VehiclesListForReading)
    {
      foreach (VehicleRoleHandler handler in vehicle.handlers)
      {
        for (int i = handler.thingOwner.Count - 1; i >= 0; i--)
        {
          Pawn pawn = handler.thingOwner[i];
          inventoryCandidates.Add(pawn);
          // We need to remove then add, so that the vehicle registers that the pawn was taken
          // out of the vehicle handler (and also triggers the PawnRemoved event)
          vehicle.TryRemovePawn(pawn, handler);
          caravan.AddPawn(pawn, true);
        }
      }
      for (int i = vehicle.inventory.innerContainer.Count - 1; i >= 0; i--)
      {
        Thing thing = vehicle.inventory.innerContainer[i];
        if (thing is Pawn)
          vehicle.inventory.innerContainer.TryTransferToContainer(thing, caravan.pawns);
      }
    }

    if (!transferables.NullOrEmpty())
    {
      //Transfer all contents
      foreach (TransferableOneWay transferable in transferables)
      {
        TransferableUtility.TransferNoSplit(transferable.things, transferable.CountToTransfer,
          delegate(Thing thing, int numToTake)
          {
            Pawn ownerOf = CaravanInventoryUtility.GetOwnerOf(vehicleCaravan, thing);
            if (ownerOf is null)
            {
              Log.Error($"Error while stashing vehicle. {thing} has no owner.");
            }
            else
            {
              CaravanInventoryUtility.MoveInventoryToSomeoneElse(ownerOf, thing,
                inventoryCandidates, vehicleCaravan.pawns.InnerListForReading,
                numToTake);
            }
          });
      }
    }

    // Transfer vehicles to stashed vehicle object
    for (int i = vehicleCaravan.pawns.Count - 1; i >= 0; i--)
    {
      Pawn vehicle = vehicleCaravan.pawns[i];
      if (!vehicleCaravan.pawns.TryTransferToContainer(vehicle, stashedVehicle.stash,
        canMergeWithExistingStacks: false))
      {
        Trace.Fail($"Unable to transfer {vehicle} to stash. Moving to new caravan instead.");
        vehicleCaravan.RemovePawn(vehicle);
        caravan.AddPawn(vehicle, true);
      }
    }
    // Ensure cached vehicle list is up-to-date otherwise old references will get destroyed
    // with the caravan
    vehicleCaravan.RecacheVehicles();

    Find.WorldObjects.Add(stashedVehicle);
    Assert.IsTrue(vehicleCaravan.pawns.Count == 0);
    vehicleCaravan.Destroy();
    return stashedVehicle;
  }
}