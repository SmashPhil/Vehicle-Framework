using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Vehicles;

public class AirdropManager : MapComponent
{
  private List<DropShip> dropShips = [];

  private readonly EdgePoints[] dropZones = new EdgePoints[4];

  public AirdropManager(Map map) : base(map)
  {
  }

  public void Spawn(DropShip dropShip)
  {
    dropShips.Add(dropShip);
    dropShip.OnSpawned();
  }

  public void Remove(DropShip dropShip)
  {
    dropShips.Remove(dropShip);
  }

  public DropZone GetDropZoneFor(Rot4 edge, int points)
  {
    IntVec3 from = dropZones[edge.AsInt].RandomPoint;
    IntVec3 to = dropZones[edge.Opposite.AsInt].RandomPoint;
    return new DropZone(from, to, points);
  }

  private void CalculateDropZones(Map map)
  {
    dropZones[Rot4.NorthInt] = new EdgePoints(Rot4.North, map.Size.x);
    dropZones[Rot4.EastInt] = new EdgePoints(Rot4.East, map.Size.z);
    dropZones[Rot4.SouthInt] = new EdgePoints(Rot4.South, map.Size.x);
    dropZones[Rot4.WestInt] = new EdgePoints(Rot4.West, map.Size.z);
  }

  public override void FinalizeInit()
  {
    CalculateDropZones(map);
  }

  public override void MapComponentTick()
  {
    for (int i = dropShips.Count - 1; i >= 0; i--)
    {
      try
      {
        dropShips[i].Tick();
      }
      catch
      {
        // Remove the drop ship so it doesn't spam errors indefinitely. These are serialized so a broken
        // dropship would be game-ending for a player's run due to lag.
        dropShips.RemoveAt(i);
        throw;
      }
    }
  }

  public override void ExposeData()
  {
    Scribe_Collections.Look(ref dropShips, nameof(dropShips), saveDestroyedThings: false, LookMode.Deep);
  }

  private sealed record EdgePoints
  {
    private const int CellsPerAnchor = 50;

    private readonly Rot4 edge;
    private readonly IntVec3[] anchors;

    public EdgePoints(Rot4 edge, int length)
    {
      this.edge = edge;
      int count = Mathf.CeilToInt((float)length / CellsPerAnchor) - 1;
      anchors = new IntVec3[count + 2];
      CalculateAnchors(count, length);
    }

    public IntVec3 RandomPoint => anchors[Rand.Range(0, anchors.Length)];

    private void CalculateAnchors(int count, int length)
    {
      for (int i = 0; i <= count; i++)
      {
        float t = i / (float)count;
        int pos = Mathf.CeilToInt(Mathf.Lerp(0, length, t));
        SetPos(anchors, edge, i, pos, length);
      }
      return;

      static void SetPos(IntVec3[] array, Rot4 rot, int index, int pos, int length)
      {
        array[index] = rot.AsInt switch
        {
          0 => new IntVec3(pos, 0, length),
          1 => new IntVec3(length, 0, pos),
          2 => new IntVec3(pos, 0, 0),
          3 => new IntVec3(0, 0, pos),
          _ => throw new NotImplementedException(nameof(rot)),
        };
      }
    }
  }
}