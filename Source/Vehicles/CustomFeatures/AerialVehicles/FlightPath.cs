using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld.Planet;
using SmashTools;
using UnityEngine;
using Verse;

namespace Vehicles.World;

[PublicAPI]
public class FlightPath : IExposable
{
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

  public FlightNode First => nodes.FirstOrDefault();

  public FlightNode Last => nodes.LastOrDefault();

  public bool Empty => nodes.NullOrEmpty();

  public FlightNode this[int index] => nodes[index];

  public bool Circling => circling;

  public bool InRecon => currentlyInRecon;

  public float DistanceLeft
  {
    get
    {
      float distance = 0;
      Vector3 start = aerialVehicle.DrawPos;
      foreach (FlightNode node in nodes)
      {
        Vector3 nextTile = WorldHelper.GetTilePos(node.tile);
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

  public void AddNode(PlanetTile tile, AerialVehicleArrivalAction arrivalAction = null)
  {
    nodes.Add(new FlightNode(tile, arrivalAction));
  }

  public void PushCircleAt(PlanetTile tile)
  {
    reconTiles.Clear();
    Ext_World.GetTileNeighbors(tile, reconTiles,
      radius: aerialVehicle.vehicle.CompVehicleLauncher.ReconDistance, aerialVehicle.DrawPos);
    foreach (PlanetTile neighborTile in reconTiles)
    {
      nodes.Insert(0, new FlightNode(neighborTile));
    }
    circling = true;
  }

  public void ReconCircleAt(PlanetTile tile)
  {
    if (Last.tile == tile)
    {
      nodes.Pop();
    }
    reconTiles.Clear();
    Ext_World.GetTileNeighbors(tile, reconTiles,
      radius: aerialVehicle.vehicle.CompVehicleLauncher.ReconDistance, aerialVehicle.DrawPos);
    foreach (PlanetTile rTile in reconTiles)
    {
      nodes.Add(new FlightNode(rTile));
    }
    circling = true;
    aerialVehicle.recon = true;
    nodes.Add(new FlightNode(tile));
    aerialVehicle.GenerateMapForRecon(tile);
  }

  public void NodeReached(bool haltCircle = false)
  {
    FlightNode currentNode = nodes.PopAt(0);
    PlanetTile currentTile = currentNode.tile;
    aerialVehicle.Tile = currentTile;
    currentlyInRecon = reconTiles.Contains(aerialVehicle.Tile);
    currentNode.arrivalAction?.Arrived(aerialVehicle, aerialVehicle.Tile);
    if (circling && haltCircle)
    {
      PlanetTile origin = Last.tile;
      ResetPath();
      AddNode(origin);
    }
    else if (nodes.Count <= 1 && circling)
    {
      if (aerialVehicle.recon)
      {
        ReconCircleAt(First.tile);
      }
      else
      {
        PushCircleAt(First.tile);
      }
    }
  }

  public void ResetPath()
  {
    nodes.Clear();
    reconTiles.Clear();
    circling = false;
    aerialVehicle.recon = false;
    currentlyInRecon = false;
  }

  public void NewPath(FlightPath flightPath)
  {
    ResetPath();
    nodes.AddRange(flightPath.Path);
  }

  public void NewPath(List<FlightNode> path)
  {
    ResetPath();
    nodes.AddRange(path);
  }

  public void ExposeData()
  {
    Scribe_Collections.Look(ref nodes, "nodes");
    Scribe_Collections.Look(ref reconTiles, "reconTiles");
    Scribe_References.Look(ref aerialVehicle, "aerialVehicle");
    Scribe_Values.Look(ref circling, "circling");
    Scribe_Values.Look(ref currentlyInRecon, "currentlyInRecon");
  }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public struct FlightNode : IExposable
{
  public PlanetTile tile;
  public Vector3 origin;
  public AerialVehicleArrivalAction arrivalAction;

  public bool spaceObject;

  public WorldObject WorldObject { get; private set; }

  public FlightNode(PlanetTile tile)
  {
    this.tile = tile;
    arrivalAction = null;

    WorldObject = WorldHelper.WorldObjectAt(tile);
    origin = WorldHelper.GetTilePos(tile, WorldObject, out spaceObject);
  }

  public FlightNode(PlanetTile tile, AerialVehicleArrivalAction arrivalAction)
  {
    this.tile = tile;
    this.arrivalAction = arrivalAction;

    WorldObject = WorldHelper.WorldObjectAt(tile);
    origin = WorldHelper.GetTilePos(tile, WorldObject, out spaceObject);
  }

  public Vector3 GetCenter(AerialVehicleInFlight aerialVehicle)
  {
    if (WorldObject != null && WorldObject != aerialVehicle)
    {
      return WorldObject.DrawPos;
    }
    return origin;
  }

  public void RecalculateCenter()
  {
    if (spaceObject)
    {
      origin = WorldHelper.GetTilePos(tile, WorldObject, out _);
    }
  }

  public void ExposeData()
  {
    Scribe_Values.Look(ref tile, nameof(tile));
    Scribe_Deep.Look(ref arrivalAction, nameof(arrivalAction));
    if (Scribe.mode == LoadSaveMode.LoadingVars)
    {
      WorldObject = WorldHelper.WorldObjectAt(tile);
      origin = WorldHelper.GetTilePos(tile, WorldObject, out spaceObject);
    }
  }
}