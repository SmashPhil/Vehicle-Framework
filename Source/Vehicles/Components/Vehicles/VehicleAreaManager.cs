using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using SmashTools;
using Vehicles.AI;

namespace Vehicles
{
	public class VehicleAreaManager : MapComponent
	{

		public int pathCost = 0;

		private Dictionary<Area, AreaPathConfig> vehicleConfigs = new Dictionary<Area, AreaPathConfig>();

		private List<Area> tmpAreas;
		private List<AreaPathConfig> tmpAreaPathConfigs;

		public VehicleAreaManager(Map map) : base(map)
		{
		}

		public void UpdateArea(VehiclePawn vehicle, Area area, int cost)
		{
			if (!vehicleConfigs.TryGetValue(area, out AreaPathConfig areaPathConfig))
			{
				areaPathConfig = new AreaPathConfig();
				vehicleConfigs.Add(area, areaPathConfig);
			}
			areaPathConfig.SetCost(vehicle, cost);
		}

		public void UpdateArea(VehicleDef vehicleDef, Area area, int cost)
		{
			if (!vehicleConfigs.TryGetValue(area, out AreaPathConfig areaPathConfig))
			{
				areaPathConfig = new AreaPathConfig();
				vehicleConfigs.Add(area, areaPathConfig);
			}
			areaPathConfig.SetCost(vehicleDef, cost);
		}

		public int AdditionalTerrainCost(VehiclePawn vehicle, Area area)
		{
			int cost = 0;
			if (vehicleConfigs.TryGetValue(area, out AreaPathConfig areaPathConfig))
			{
				cost = areaPathConfig.CostFor(vehicle);
			}
			return cost;
		}

		public int AdditionalTerrainCost(VehicleDef vehicleDef, Area area)
		{
			int cost = 0;
			if (vehicleConfigs.TryGetValue(area, out AreaPathConfig areaPathConfig))
			{
				cost = areaPathConfig.CostFor(vehicleDef);
			}
			return cost;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref vehicleConfigs, "vehicleConfigs", LookMode.Reference, LookMode.Deep, ref tmpAreas, ref tmpAreaPathConfigs);
		}

		public class AreaPathConfig : IExposable
		{
			public const int MinPathCost = -50;
			public const int MaxPathCost = 50;

			private Dictionary<VehiclePawn, int> vehicles = new Dictionary<VehiclePawn, int>();
			private Dictionary<VehicleDef, int> vehicleDefs = new Dictionary<VehicleDef, int>();

			private List<VehiclePawn> tmpVehicleList;
			private List<int> tmpVehicleCosts;
			private List<VehicleDef> tmpVehicleDefs;
			private List<int> tmpVehicleDefCosts;

			public void SetCost(VehiclePawn vehicle, int cost)
			{
				if (cost == 0)
				{
					vehicles.Remove(vehicle);
				}
				else if (cost > MaxPathCost)
				{
					vehicles[vehicle] = VehiclePathGrid.ImpassableCost;
				}
				else
				{
					vehicles[vehicle] = cost.Clamp(MinPathCost, MaxPathCost);
				}
			}

			public void SetCost(VehicleDef vehicleDef, int cost)
			{
				if (cost == 0)
				{
					vehicleDefs.Remove(vehicleDef);
				}
				else if (cost > MaxPathCost)
				{
					vehicleDefs[vehicleDef] = VehiclePathGrid.ImpassableCost;
				}
				else
				{
					vehicleDefs[vehicleDef] = cost.Clamp(MinPathCost, MaxPathCost);
				}
			}

			public int CostFor(VehiclePawn vehicle)
			{
				if (vehicles.TryGetValue(vehicle, out int vehicleCost))
				{
					return vehicleCost;
				}
				return CostFor(vehicle.VehicleDef);
			}

			public int CostFor(VehicleDef vehicleDef)
			{
				return vehicleDefs.TryGetValue(vehicleDef, 0);
			}

			public void ExposeData()
			{
				Scribe_Collections.Look(ref vehicles, "vehicles", LookMode.Reference, LookMode.Value, ref tmpVehicleList, ref tmpVehicleCosts);
				Scribe_Collections.Look(ref vehicleDefs, "vehicleDefs", LookMode.Def, LookMode.Value, ref tmpVehicleDefs, ref tmpVehicleDefCosts);

				if (Scribe.mode == LoadSaveMode.PostLoadInit)
				{
					vehicles ??= new Dictionary<VehiclePawn, int>();
					vehicleDefs ??= new Dictionary<VehicleDef, int>();
				}
			}
		}
	}
}
