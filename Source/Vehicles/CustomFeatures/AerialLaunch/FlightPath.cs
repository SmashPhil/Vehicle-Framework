using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public class FlightPath : IExposable
	{
		private List<FlightNode> nodes = new List<FlightNode>();
		private List<int> reconTiles = new List<int>();
		private AerialVehicleInFlight aerialVehicle;
		private bool circling = false;
		private bool currentlyInRecon = false;

		public FlightPath(AerialVehicleInFlight aerialVehicle)
		{
			this.aerialVehicle = aerialVehicle;
		}

		public List<FlightNode> Path => nodes;

		public FlightNode First => nodes.FirstOrDefault();

		public FlightNode Last => nodes.LastOrDefault();

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

		public int AltitudeDirection
		{
			get
			{
				if (aerialVehicle.recon)
				{
					return 1;
				}
				int ticksLeft = 0;
				Vector3 start = aerialVehicle.DrawPos;
				float transitionPctLeft = 1 - aerialVehicle.transition;
				if (circling || nodes.Count <= 1)
				{
					Vector3 nextTile = Find.WorldGrid.GetTileCenter(Last.tile);
					float distance = Ext_Math.SphericalDistance(start, nextTile);
					float speedPctPerTick = (AerialVehicleInFlight.PctPerTick / distance) * aerialVehicle.vehicle.CompVehicleLauncher.FlightSpeed;
					ticksLeft += Mathf.RoundToInt(transitionPctLeft / speedPctPerTick);
				}
				else
				{
					foreach (FlightNode node in nodes)
					{
						Vector3 nextTile = Find.WorldGrid.GetTileCenter(node.tile);
						float distance = Ext_Math.SphericalDistance(start, nextTile);
						start = nextTile;

						float speedPctPerTick = (AerialVehicleInFlight.PctPerTick / distance) * aerialVehicle.vehicle.CompVehicleLauncher.FlightSpeed;
						ticksLeft += Mathf.RoundToInt(transitionPctLeft / speedPctPerTick);
						transitionPctLeft = 1; //Only first node being traveled to has any progression
					}
				}
				int direction = ticksLeft <= aerialVehicle.TicksTillLandingElevation ? -1 : 1;
				return direction;
			}
		}

		public void VerifyFlightPath()
		{
			First.RecalculateCenter();
		}

		public void AddNode(int tile, AerialVehicleArrivalAction arrivalAction = null)
		{
			nodes.Add(new FlightNode(tile, arrivalAction));
		}

		public void PushCircleAt(int tile)
		{
			reconTiles = Ext_World.GetTileNeighbors(tile, aerialVehicle.vehicle.CompVehicleLauncher.ReconDistance, aerialVehicle.DrawPos);
			foreach (int neighborTile in reconTiles)
			{
				nodes.Insert(0, new FlightNode(neighborTile));
			}
			circling = true;
		}

		public void ReconCircleAt(int tile)
		{
			if (Last.tile == tile)
			{
				nodes.Pop();
			}
			reconTiles = Ext_World.GetTileNeighbors(tile, aerialVehicle.vehicle.CompVehicleLauncher.ReconDistance, aerialVehicle.DrawPos);
			foreach (int rTile in reconTiles)
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
			int currentTile = currentNode.tile;
			aerialVehicle.Tile = currentTile;
			currentlyInRecon = reconTiles.Contains(aerialVehicle.Tile);
			currentNode.arrivalAction?.Arrived(aerialVehicle.Tile);
			if (circling && haltCircle)
			{
				int origin = Last.tile;
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

	public struct FlightNode : IExposable
	{
		public int tile;
		public Vector3 center;
		public AerialVehicleArrivalAction arrivalAction;

		public bool spaceObject;
		public WorldObject WorldObject { get; private set; }

		public FlightNode(int tile)
		{
			this.tile = tile;
			arrivalAction = null;

			WorldObject = WorldHelper.WorldObjectAt(tile);
			center = WorldHelper.GetTilePos(tile, WorldObject, out spaceObject);
		}

		public FlightNode(int tile, AerialVehicleArrivalAction arrivalAction)
		{
			this.tile = tile;
			this.arrivalAction = arrivalAction;

			WorldObject = WorldHelper.WorldObjectAt(tile);
			center = WorldHelper.GetTilePos(tile, WorldObject, out spaceObject);
		}

		public void RecalculateCenter()
		{
			if (spaceObject)
			{
				center = WorldHelper.GetTilePos(tile, WorldObject, out _);
			}
		}

		public void ExposeData()
		{
			Scribe_Values.Look(ref tile, "tile");
			Scribe_Deep.Look(ref arrivalAction, "arrivalAction");
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				WorldObject = WorldHelper.WorldObjectAt(tile);
				center = WorldHelper.GetTilePos(tile, WorldObject, out spaceObject);
			}
		}
	}
}
