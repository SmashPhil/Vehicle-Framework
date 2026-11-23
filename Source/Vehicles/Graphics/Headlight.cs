using CoreLib.Performance;
using LudeonTK;
using SmashTools;
using SmashTools.Rendering;
using Unity.Collections;
using UnityEngine;
using Verse;

namespace Vehicles.Rendering;

public class Headlight : LightCone
{
  private readonly VehiclePawn vehicle;
  private readonly HeadlightProperties props;

  public Headlight(VehiclePawn vehicle, HeadlightProperties props) : base(vehicle.Map, new Configuration
  {
    position = IntVec3.Zero,
    angle = props.angle,
    direction = props.direction,
    length = props.length,
    intensity = props.intensity,
    overlightRadius = props.overlightRadius,
    color = props.color
  })
  {
    this.vehicle = vehicle;
    this.props = props;
  }

  private float VehicleRotation => vehicle.Rotation.AsAngle + vehicle.Angle;

  public override IntVec3 Position
  {
    get
    {
      Rot8 rot = vehicle.FullRotation;
      IntVec2 size = vehicle.VehicleDef.Size;
      IntVec2 offset = props.RealPosition(rot, size);
      offset += base.Position.ToIntVec2.RotatedBy(rot, size);
      return vehicle.Position +  new IntVec3(offset.x, 0, offset.z);
    }
    set => base.Position = value;
  }

  protected override CellRect AffectedRect
  {
    get
    {
      return CalculateAffectedRect(Position, Direction + VehicleRotation);
    }
  }

  protected override void Compute([ReadOnly] NativeBitArray blockers)
  {
    ComputeCone(Position, Direction + VehicleRotation, blockers);
  }

  [DebugAction(VehicleHarmony.VehiclesLabel, "Random cone light", actionType = DebugActionType.ToolMap,
    allowedGameStates = AllowedGameStates.PlayingOnMap)]
  private static void DebugRandomConeLight()
  {
    Map currentMap = Find.CurrentMap;
    if (currentMap == null)
      return;

    IntVec3 cell = UI.MouseCell();
    if (!cell.InBounds(currentMap))
      return;

    float angle = Rand.RangeInclusive(30, 120);
    float direction = Rand.RangeInclusive(0, 359);
    int length = Rand.RangeInclusive(8, 25);
    LightCone emitter = new(currentMap, new Configuration
    {
      position = cell,
      angle = angle,
      direction = direction,
      length = length,
      overlightRadius = 2f,
      color = new Color32(255, 225, 170, 0)
    });
    currentMap.GetDetachedMapComponent<GlowGridExtender>().RegisterGlowEmitter(emitter);
    new Debouncer(delegate
    {
      if (!currentMap.Disposed)
      {
        emitter.Destroy();
      }
    }, milliseconds: 1000).Invoke();
  }
}
