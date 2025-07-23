using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld.Planet;
using SmashTools;
using UnityEngine;
using Verse;

namespace Vehicles.World;

// TODO - cleanup
[PublicAPI]
public class FlightPath : IExposable
{
  private IArrivalAction arrivalAction;
  private List<FlightNode> nodes = [];
  private List<PlanetTile> reconTiles = [];
  private AerialVehicleInFlight aerialVehicle;
  private bool circling;
  private bool currentlyInRecon;

  public FlightPath(AerialVehicleInFlight aerialVehicle)
  {
    this.aerialVehicle = aerialVehicle;
  }

  public List<FlightNode> Path => nodes;

  public FlightNode First => nodes[0];

  public FlightNode Last => nodes[^1];

  public int Count => nodes.Count;

  public bool Empty => nodes.NullOrEmpty();

  public FlightNode this[int index] => nodes[index];

  public bool Circling => circling;

  public bool InRecon => currentlyInRecon;

  public IArrivalAction ArrivalAction => arrivalAction;

  public float TotalDistance { get; private set; }

  public float DistanceLeft
  {
    get
    {
      float distance = 0;
      Vector3 start = aerialVehicle.DrawPos;
      foreach (FlightNode node in nodes)
      {
        Vector3 nextTile = WorldHelper.GetTilePos(node.Tile);
        distance += Ext_Math.SphericalDistance(start, nextTile);
        start = nextTile;
      }
      return distance;
    }
  }

  public void VerifyFlightPath()
  {
    First.RecalculateCenter();
  }

  public void RecacheCenters()
  {
    if (!nodes.NullOrEmpty())
    {
      for (int i = 0; i < nodes.Count; i++)
      {
        nodes[i].RecalculateCenter();
      }
    }
  }

  public void AddNode(PlanetTile tile)
  {
    nodes.Add(new FlightNode(tile));
    RecalculateDistance();
  }

  public void PushCircleAt(PlanetTile tile)
  {
    reconTiles.Clear();
    Ext_World.GetTileNeighbors(tile, reconTiles,
      radius: aerialVehicle.Vehicle.CompVehicleLauncher.ReconDistance, aerialVehicle.DrawPos);
    foreach (PlanetTile neighborTile in reconTiles)
    {
      nodes.Insert(0, new FlightNode(neighborTile));
    }
    circling = true;
  }

  public void ReconCircleAt(PlanetTile tile)
  {
    if (Last.Tile == tile)
    {
      nodes.Pop();
    }
    reconTiles.Clear();
    Ext_World.GetTileNeighbors(tile, reconTiles,
      radius: aerialVehicle.Vehicle.CompVehicleLauncher.ReconDistance, aerialVehicle.DrawPos);
    foreach (PlanetTile rTile in reconTiles)
    {
      nodes.Add(new FlightNode(rTile));
    }
    circling = true;
    aerialVehicle.recon = true;
    nodes.Add(new FlightNode(tile));
    aerialVehicle.GenerateMapForRecon(tile);
  }

  public void ConsumeNode(bool haltCircle = false)
  {
    FlightNode currentNode = nodes.PopAt(0);
    PlanetTile currentTile = currentNode.Tile;
    aerialVehicle.Tile = currentTile;
    currentlyInRecon = reconTiles.Contains(aerialVehicle.Tile);
    if (circling && haltCircle)
    {
      PlanetTile origin = Last.Tile;
      ResetPath();
      AddNode(origin);
    }
    else if (nodes.Count <= 1 && circling)
    {
      if (aerialVehicle.recon)
      {
        ReconCircleAt(First.Tile);
      }
      else
      {
        PushCircleAt(First.Tile);
      }
    }

    if (nodes.NullOrEmpty())
      arrivalAction?.Arrived(currentNode);
  }

  public void ResetPath()
  {
    nodes.Clear();
    reconTiles.Clear();
    circling = false;
    aerialVehicle.recon = false;
    currentlyInRecon = false;
    TotalDistance = 0;
  }

  public void NewPath(FlightPath flightPath)
  {
    ResetPath();
    nodes.AddRange(flightPath.Path);
    arrivalAction = flightPath.arrivalAction;
    RecalculateDistance();
  }

  public void NewPath(List<FlightNode> path, IArrivalAction arrivalAction)
  {
    ResetPath();
    nodes.AddRange(path);
    this.arrivalAction = arrivalAction;
    RecalculateDistance();
  }

  private void RecalculateDistance()
  {
    TotalDistance = 0;
    if (nodes.NullOrEmpty())
      return;

    FlightNode fromNode = nodes[0];
    for (int i = 1; i < nodes.Count; i++)
    {
      FlightNode toNode = nodes[i];
      TotalDistance += Ext_Math.SphericalDistance(
        WorldHelper.GetTilePos(fromNode.Tile),
        WorldHelper.GetTilePos(toNode.Tile));
    }
  }

  public void ExposeData()
  {
    Scribe_Collections.Look(ref nodes, nameof(nodes));
    Scribe_Deep.Look(ref arrivalAction, nameof(arrivalAction));
    Scribe_Collections.Look(ref reconTiles, nameof(reconTiles));
    Scribe_References.Look(ref aerialVehicle, nameof(aerialVehicle));
    Scribe_Values.Look(ref circling, nameof(circling));
    Scribe_Values.Look(ref currentlyInRecon, nameof(currentlyInRecon));

    if (Scribe.mode == LoadSaveMode.LoadingVars)
    {
      RecalculateDistance();
    }
  }

  public static void DrawPath(Vector3 start, Vector3 end, Material material)
  {
    double distance = Ext_Math.SphericalDistance(start, end);
    int steps = Mathf.CeilToInt((float)(distance * 100) / 5);
    start += start.normalized * 0.05f;
    end += end.normalized * 0.05f;
    Vector3 previous = start;

    for (int i = 1; i <= steps; i++)
    {
      float t = (float)i / steps;
      Vector3 midPoint = Vector3.Slerp(start, end, t);

      GenDraw.DrawWorldLineBetween(previous, midPoint, material, 0.5f);
      previous = midPoint;
    }
  }
}