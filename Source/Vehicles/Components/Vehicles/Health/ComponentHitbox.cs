using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Verse;

namespace Vehicles;

[PublicAPI]
public class ComponentHitbox
{
  public VehicleComponentPosition side = VehicleComponentPosition.Empty;

  public IntVec2 from = IntVec2.Invalid;
  public IntVec2 to = IntVec2.Invalid;

  public List<IntVec2> cells = [];

  public bool fallthrough = true;

  public List<IntVec2> Hitbox { get; set; } = [];

  public bool Empty => Hitbox.Count == 0;

  public bool Contains(IntVec2 cell)
  {
    if (Hitbox.NullOrEmpty())
      return false;

    return Hitbox.Contains(cell);
  }

  public IntVec2 NearestTo(IntVec2 cell)
  {
    if (Hitbox.Count == 1)
      return Hitbox[0];

    return Hitbox.MinBy(hb => (hb - cell).Magnitude);
  }

  public void Initialize(VehicleDef def)
  {
    // Defined cells
    if (!cells.NullOrEmpty())
    {
      Hitbox.AddRange(cells);
      return;
    }
    // Limit based rect
    if (from.IsValid && to.IsValid)
    {
      foreach (IntVec3 cell in CellRect.FromLimits(from.ToIntVec3, to.ToIntVec3))
        Hitbox.Add(cell.ToIntVec2);
      return;
    }

    if (side == VehicleComponentPosition.Empty)
    {
      // If no hitbox provided, default to root position. This only matters in the case of
      // non-hitbox external components otherwise they would be invulnerable.
      Hitbox.Add(IntVec2.Zero);
      return;
    }

    // Enum based rect
    CellRect rect = def.VehicleRect(new IntVec3(0, 0, 0), Rot4.North);
    if (side == VehicleComponentPosition.Body)
    {
      foreach (IntVec3 cell in rect.Cells)
        Hitbox.Add(cell.ToIntVec2);
    }
    else
    {
      foreach (IntVec3 cell in rect.GetEdgeCells(RotationFromSide(side)))
        Hitbox.Add(cell.ToIntVec2);
    }
  }

  public static Rot4 RotationFromSide(VehicleComponentPosition pos)
  {
    return pos switch
    {
      VehicleComponentPosition.Front => Rot4.North,
      VehicleComponentPosition.Right => Rot4.East,
      VehicleComponentPosition.Back  => Rot4.South,
      VehicleComponentPosition.Left  => Rot4.West,
      _                              => Rot4.Invalid
    };
  }
}