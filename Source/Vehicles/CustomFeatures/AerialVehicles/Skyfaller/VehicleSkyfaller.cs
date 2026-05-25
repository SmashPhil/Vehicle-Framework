using System.Collections.Generic;
using RimWorld.Planet;
using SmashTools;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Vehicles;

[StaticConstructorOnStartup]
public abstract class VehicleSkyfaller : Thing, IThingHolderTickable, IRoofCollapseAlert, ISustainerTarget
{
  protected static MaterialPropertyBlock shadowPropertyBlock = new();

  public float angle;
  protected Vector3 launchProtocolDrawPos;

  protected Material cachedShadowMaterial;

  protected bool anticipationSoundPlayed;

  public VehiclePawn vehicle;

  // Hook for vanilla ticking and ParentHolder resolving, just holds a reference to the inner vehicle.
  private readonly ThingOwner<VehiclePawn> innerContainer = [];

  public override Vector3 DrawPos => launchProtocolDrawPos;

  protected Vector3 RootPos => Ext_Vehicles.TrueCenter(Position, Rotation, vehicle.VehicleDef.size, base.DrawPos.y);

  public ThingWithComps Thing => vehicle;

  bool IThingHolderTickable.ShouldTickContents => true;

  TargetInfo ISustainerTarget.Target => this;

  MaintenanceType ISustainerTarget.MaintenanceType => MaintenanceType.PerTick;

  private Material ShadowMaterial
  {
    get
    {
      if (cachedShadowMaterial is null && !def.skyfaller.shadow.NullOrEmpty())
      {
        cachedShadowMaterial =
          MaterialPool.MatFrom(def.skyfaller.shadow, ShaderDatabase.Transparent);
      }
      return cachedShadowMaterial;
    }
  }

  protected override void Tick()
  {
    vehicle.CompVehicleLauncher.launchProtocol.Tick();
  }

  protected virtual void LeaveMap()
  {
    Destroy();
  }

  public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
  {
    innerContainer.Remove(vehicle);
    // NOTE - Vehicle can be spawned right before the skyfaller despawns, we only want to release claims for outgoing
    // skyfallers, releasing the claim of a spawned vehicle would enable map removal and game ender conditions.
    if (!vehicle.Spawned)
      Map.GetDetachedMapComponent<VehiclePositionManager>().ReleaseClaimed(vehicle);
    base.DeSpawn(mode);
    vehicle.ReleaseSustainerTarget();
  }

  protected virtual void DrawDropSpotShadow()
  {
    Material shadowMaterial = ShadowMaterial;
    if (shadowMaterial is null)
    {
      return;
    }
    //TODO - draw shadow at DrawPos but z-axis is left on ground and size decreases through curve
    DrawDropSpotShadow(DrawPos, Rotation, shadowMaterial, def.skyfaller.shadowSize,
      vehicle.CompVehicleLauncher.launchProtocol.TicksPassed);
  }

  public static void DrawDropSpotShadow(Vector3 center, Rot4 rot, Material material,
    Vector2 shadowSize, int ticksToLand)
  {
    if (rot.IsHorizontal)
    {
      Gen.Swap(ref shadowSize.x, ref shadowSize.y);
    }
    ticksToLand = Mathf.Max(ticksToLand, 0);
    Vector3 pos = center;
    pos.y = AltitudeLayer.Shadows.AltitudeFor();
    float num = 1f + ticksToLand / 100f;
    Vector3 s = new(num * shadowSize.x, 1f, num * shadowSize.y);
    Color white = Color.white;
    if (ticksToLand > 150)
    {
      white.a = Mathf.InverseLerp(200f, 150f, ticksToLand);
    }
    shadowPropertyBlock.SetColor(ShaderPropertyIDs.Color, white);
    Matrix4x4 matrix = default;
    matrix.SetTRS(pos, rot.AsQuat, s);
    Graphics.DrawMesh(MeshPool.plane10Back, matrix, material, 0, null, 0, shadowPropertyBlock);
  }

  // TODO 1.7 - Remove in favor of compact generator class
  private void PackVehicle()
  {
    if (vehicle.Spawned)
      vehicle.DeSpawn();
    innerContainer.TryAddOrTransfer(vehicle);
    Map.GetDetachedMapComponent<VehiclePositionManager>().ClaimPosition(vehicle, Position, Rotation);
    // Needs updating if spawning full colonist list into generated map with no targeter
    Find.GameEnder.CheckOrUpdateGameOver();
  }

  public override void SpawnSetup(Map map, bool respawningAfterLoad)
  {
    base.SpawnSetup(map, respawningAfterLoad);
    launchProtocolDrawPos = RootPos;
    if (vehicle.IsWorldPawn())
    {
      Find.WorldPawns.RemovePawn(vehicle);
      foreach (Pawn pawn in vehicle.AllPawnsAboard)
      {
        if (pawn.IsWorldPawn())
        {
          Find.WorldPawns.RemovePawn(pawn);
        }
      }
    }
    vehicle.SetSustainerTarget(this);
    // Reset required for recaching handler lists. Loading save file will not recache these since
    // vehicle will be despawned initially
    vehicle.ResetRenderStatus();
    PackVehicle();
  }

  public override void ExposeData()
  {
    base.ExposeData();

    Scribe_Values.Look(ref angle, nameof(angle));
    Scribe_Deep.Look(ref vehicle, nameof(vehicle));
  }

  RoofCollapseResponse IRoofCollapseAlert.Notify_OnBeforeRoofCollapse()
  {
    return RoofCollapseResponse.None;
  }

  void IThingHolder.GetChildHolders(List<IThingHolder> outChildren)
  {
  }

  ThingOwner IThingHolder.GetDirectlyHeldThings()
  {
    return innerContainer;
  }
}