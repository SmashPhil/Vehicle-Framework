using SmashTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Vehicles
{
	public class GridOwners
	{
		private int[] piggyToOwner;
		private List<VehicleDef> owners;
		private List<VehicleDef> piggies;

		private PathConfig[] configs;

		public List<VehicleDef> Owners => owners;

		public List<VehicleDef> Piggies => piggies;

		public void Init()
		{
			piggyToOwner = new int[DefDatabase<VehicleDef>.DefCount].Populate(-1);
			owners = new List<VehicleDef>();
			piggies = new List<VehicleDef>();

			GenerateConfigs();
			SeparateIntoGroups();
		}

		private void GenerateConfigs()
		{
			configs = new PathConfig[DefDatabase<VehicleDef>.DefCount];
			foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefsListForReading)
			{
				configs[vehicleDef.DefIndex] = new PathConfig(vehicleDef);
			}
		}

		private void SeparateIntoGroups(bool compress = true)
		{
			foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefsListForReading)
			{
				if (VehicleHarmony.gridOwners.TryGetOwner(vehicleDef, out int ownerId) && compress)
				{
					//Piggy back off vehicles with similar width + impassability
					VehicleHarmony.gridOwners.SetPiggy(vehicleDef, ownerId);
				}
				else
				{
					VehicleHarmony.gridOwners.SetOwner(vehicleDef);
				}
			}
		}

		public bool IsOwner(VehicleDef vehicleDef)
		{
			return piggyToOwner[vehicleDef.DefIndex] == vehicleDef.DefIndex;
		}

		public VehicleDef GetOwner(VehicleDef vehicleDef)
		{
			int id = piggyToOwner[vehicleDef.DefIndex];
			return GetOwner(id);
		}

		public VehicleDef GetOwner(int ownerId)
		{
			return owners.FirstOrDefault(vehicleDef => vehicleDef.DefIndex == ownerId);
		}

		public void SetOwner(VehicleDef vehicleDef)
		{
			piggyToOwner[vehicleDef.DefIndex] = vehicleDef.DefIndex;
			owners.Add(vehicleDef);
		}

		public void SetPiggy(VehicleDef vehicleDef, int ownerId)
		{
			piggyToOwner[vehicleDef.DefIndex] = ownerId;
			piggies.Add(vehicleDef);
		}

		private bool TryGetOwner(VehicleDef vehicleDef, out int ownerId)
		{
			PathConfig config = configs[vehicleDef.DefIndex];
			foreach (VehicleDef checkingOwner in VehicleHarmony.AllVehicleOwners)
			{
				ownerId = checkingOwner.DefIndex;
				if (config.MatchesReachability(configs[ownerId]))
				{
					return true;
				}
			}
			ownerId = -1;
			return false;
		}

		private class PathConfig
		{
			private readonly VehicleDef vehicleDef;

			private readonly HashSet<ThingDef> impassableThingDefs;
			private readonly HashSet<TerrainDef> impassableTerrain;
			private readonly int size;
			private readonly bool defaultTerrainImpassable;

			internal PathConfig(VehicleDef vehicleDef)
			{
				this.vehicleDef = vehicleDef;

				size = Mathf.Min(vehicleDef.Size.x, vehicleDef.Size.z);
				defaultTerrainImpassable = vehicleDef.properties.defaultTerrainImpassable;
				impassableThingDefs = vehicleDef.properties.customThingCosts.Where(kvp => kvp.Value >= VehiclePathGrid.ImpassableCost).Select(kvp => kvp.Key).ToHashSet();
				impassableTerrain = vehicleDef.properties.customTerrainCosts.Where(kvp => kvp.Value >= VehiclePathGrid.ImpassableCost).Select(kvp => kvp.Key).ToHashSet();
			}

			public bool UsesRegions => vehicleDef.vehicleMovementPermissions > VehiclePermissions.NotAllowed;

			internal bool MatchesReachability(PathConfig other)
			{
				return UsesRegions == other.UsesRegions && size == other.size && defaultTerrainImpassable == other.defaultTerrainImpassable &&
					impassableThingDefs.SetEquals(other.impassableThingDefs) && impassableTerrain.SetEquals(other.impassableTerrain);
			}
		}
	}
}
