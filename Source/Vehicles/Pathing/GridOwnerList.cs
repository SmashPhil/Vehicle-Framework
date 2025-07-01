using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using SmashTools;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public abstract class GridOwnerList<T> where T : IPathConfig
{
  protected int[] piggyToOwner;

  protected T[] configs;

  protected object gridOwnerLock = new();

  public event OwnershipTransferred OnOwnershipTransfer;

  public bool AnyOwners { get; private set; }

  public VehicleDef[] AllOwners { get; private set; }

  public IEnumerable<VehicleDef> AllPiggies
  {
    get
    {
      foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefsListForReading)
      {
        if (!IsOwner(vehicleDef))
          yield return vehicleDef;
      }
    }
  }

  /// <summary>
  /// Ownership of region grid is being transferred from <paramref name="fromVehicleDef"/> to
  /// <paramref name="toVehicleDef"/>
  /// </summary>
  /// <param name="fromVehicleDef"></param>
  /// <param name="toVehicleDef"></param>
  public delegate void OwnershipTransferred(VehicleDef fromVehicleDef, VehicleDef toVehicleDef);

  // NOTE - Initialized on startup, there should never be any values in the owner lookup that
  // point to invalid indices.
  internal virtual void Init()
  {
    piggyToOwner ??= new int[DefDatabase<VehicleDef>.DefCount];
    piggyToOwner.Populate(-1);

    List<VehicleDef> owners = [];
    GenerateConfigs();
    SeparateIntoGroups(owners);

    // Reference assignment is atomic, but the lists need to be populated separately since many
    // callers will be accessing these through getters and locking every single time they're
    // accessed is an unnecessary performance drain if they are rarely ever going to change.
    AllOwners = [.. owners];
    AnyOwners = owners.Count > 0;
  }

  protected abstract void GenerateConfigs();

  protected abstract bool CanTransferOwnershipTo(VehicleDef vehicleDef);

  protected void SeparateIntoGroups(List<VehicleDef> owners, bool compress = true)
  {
    foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefsListForReading)
    {
      if (TryGetOwner(owners, vehicleDef, out int ownerId) && compress)
      {
        piggyToOwner[vehicleDef.DefIndex] = ownerId;
      }
      else
      {
        piggyToOwner[vehicleDef.DefIndex] = vehicleDef.DefIndex;
        owners.Add(vehicleDef);
      }
    }
  }

  protected bool TryGetOwner(List<VehicleDef> owners, VehicleDef vehicleDef, out int ownerId)
  {
    T config = configs[vehicleDef.DefIndex];
    foreach (VehicleDef checkingOwner in owners)
    {
      ownerId = checkingOwner.DefIndex;
      if (config.UsesRegions == configs[ownerId].UsesRegions &&
        config.MatchesReachability(configs[ownerId]))
      {
        return true;
      }
    }

    ownerId = -1;
    return false;
  }

  public bool TryForfeitOwnership(VehicleDef ownerDef)
  {
    Assert.IsTrue(IsOwner(ownerDef));
    foreach (VehicleDef piggyDef in GetPiggies(ownerDef))
    {
      if (CanTransferOwnershipTo(piggyDef))
      {
        TransferOwnership(piggyDef);
        return true;
      }
    }
    return false;
  }

  public void TransferOwnership(VehicleDef vehicleDef)
  {
    VehicleDef ownerDef = GetOwner(vehicleDef);
    if (vehicleDef == ownerDef)
      return; // Already has ownership

    // Point all piggies of the previous owner over to this new owner
    foreach (VehicleDef piggyDef in GetPiggies(ownerDef))
    {
      Interlocked.Exchange(ref piggyToOwner[piggyDef.DefIndex], vehicleDef.DefIndex);
    }
    Interlocked.Exchange(ref piggyToOwner[ownerDef.DefIndex], vehicleDef.DefIndex);

    // Might be a bit too safe but since the swap is technically 2 operations (index lookup + atomic CAS)
    // the lock prevents multiple transferships from occurring at the same time. Outside of this we only
    // care that the array write is atomic.
    lock (gridOwnerLock)
    {
      int index = Array.IndexOf(AllOwners, ownerDef);
      Interlocked.Exchange(ref AllOwners[index], vehicleDef);
    }

    OnOwnershipTransfer?.Invoke(ownerDef, vehicleDef);
  }

  public bool IsOwner(VehicleDef vehicleDef)
  {
    return IsOwner(vehicleDef.DefIndex);
  }

  public bool IsOwner(int id)
  {
    return piggyToOwner[id] == id;
  }

  public VehicleDef GetOwner(VehicleDef vehicleDef)
  {
    if (IsOwner(vehicleDef.DefIndex))
      return vehicleDef;
    int id = piggyToOwner[vehicleDef.DefIndex];
    return GetOwner(id);
  }

  private VehicleDef GetOwner(int ownerId)
  {
    foreach (VehicleDef vehicleDef in AllOwners)
    {
      if (vehicleDef.DefIndex == ownerId)
        return vehicleDef;
    }
    Log.Error($"Unable to find owner by id {ownerId}");
    return null;
  }

  public IEnumerable<VehicleDef> GetPiggies(VehicleDef vehicleDef)
  {
    Assert.IsTrue(IsOwner(vehicleDef));
    foreach (VehicleDef piggyDef in AllPiggies)
    {
      VehicleDef ownerDef = GetOwner(piggyDef);
      if (ownerDef == vehicleDef)
        yield return piggyDef;
    }
  }
}