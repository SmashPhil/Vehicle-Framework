using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using SmashTools;

namespace Vehicles
{
	public class VehicleReservationManager : MapComponent
	{
		private const int ReservationVerificationInterval = 50;

		private Dictionary<VehiclePawn, ReservationBase> reservations = new Dictionary<VehiclePawn, ReservationBase>();
		private Dictionary<VehiclePawn, VehicleRequestCollection> vehicleListers = new Dictionary<VehiclePawn, VehicleRequestCollection>();

		private List<VehiclePawn> vehicleReservations = new List<VehiclePawn>();
		private List<ReservationBase> reservationSets = new List<ReservationBase>();
		private List<VehiclePawn> vehicleListerPawns = new List<VehiclePawn>();
		private List<VehicleRequestCollection> vehicleListerRequests = new List<VehicleRequestCollection>();

		public VehicleReservationManager(Map map) : base(map)
		{
		}

		public bool Reserve<T1, T2>(VehiclePawn vehicle, Pawn pawn, Job job, T1 target, int maxClaimantsIfNew = 1) where T2 : Reservation<T1>
		{
			if(vehicle is null || pawn is null || job is null)
			{
				return false;
			}
			try
			{
				ReleaseAllClaimedBy(pawn);
				
				if(reservations.ContainsKey(vehicle) && reservations[vehicle].GetType() != typeof(T2))
				{
					ClearReservedFor(vehicle);
				}
				
				if (reservations.ContainsKey(vehicle))
				{
					if(CanReserve<T1, T2>(vehicle, pawn, target))
					{
						(reservations[vehicle] as T2).AddClaimant(pawn, target);
					}
					else
					{
						return false;
					}
				}
				else
				{
					reservations.Add(vehicle, (ReservationBase)Activator.CreateInstance(typeof(T2), new object[] { vehicle, job, maxClaimantsIfNew }));
					(reservations[vehicle] as T2).AddClaimant(pawn, target);
				}
			}
			catch(Exception ex)
			{
				Log.Error($"Exception thrown while attempting to reserve Vehicle based job. {ex}");
				return false;
			}
			return true;
		}

		public T GetReservation<T>(VehiclePawn vehicle) where T : ReservationBase
		{
			if (!reservations.ContainsKey(vehicle))
				return default;
			return (T)reservations[vehicle];
		}
		
		public void ReleaseAllClaims()
		{
			foreach (VehiclePawn vehicle in reservations.Keys.ToArray())
			{
				ClearReservedFor(vehicle);
			}
		}

		public void ReleaseAllClaimedBy(Pawn pawn)
		{
			foreach(ReservationBase reservation in reservations.Values)
			{
				reservation.ReleaseReservationBy(pawn);
			}
		}

		public void ClearReservedFor(VehiclePawn vehicle)
		{
			if (reservations.ContainsKey(vehicle))
			{
				reservations[vehicle].ReleaseAllReservations();
				reservations.Remove(vehicle);
			}
		}

		public bool CanReserve<T1, T2>(VehiclePawn vehicle, Pawn pawn, T1 target) where T2 : Reservation<T1>
		{
			if (reservations.TryGetValue(vehicle, out var reservation))
			{
				return reservation is T2 type && type.CanReserve(pawn, target);
			}
			return true;
		}

		public int TotalReserving(VehiclePawn vehicle)
		{
			if (reservations.TryGetValue(vehicle, out ReservationBase claim))
			{
				return claim.TotalClaimants;
			}
			return 0;
		}

		public override void MapComponentTick()
		{
			if(Find.TickManager.TicksGame % ReservationVerificationInterval == 0)
			{
				for(int i = reservations.Count - 1; i >= 0; i--)
				{
					KeyValuePair<VehiclePawn, ReservationBase> reservation = reservations.ElementAt(i);
					reservation.Value.VerifyAndValidateClaimants();
					if(reservation.Value.RemoveNow)
					{
						reservations.Remove(reservation.Key);
					}
				}
			}
		}

		public override void FinalizeInit()
		{
			base.FinalizeInit();

			VerifyCollection(ref reservations);
			VerifyCollection(ref vehicleListers);
			VerifyCollection(ref vehicleReservations);
			VerifyCollection(ref reservationSets);
			VerifyCollection(ref vehicleListerPawns);
			VerifyCollection(ref vehicleListerRequests);

			if (vehicleListers.Keys.Any(l => l is null))
			{
				vehicleListers.RemoveAll(v => v.Key is null || v.Value.requests is null);
			}
		}

		private static void VerifyCollection<T>(ref List<T> list)
		{
			if (list is null)
			{
				list = new List<T>();
			} 
			for(int i = list.Count - 1; i >= 0; i++)
			{
				var item = list[i];
				if (item is null)
				{
					list.Remove(item);
				}
			}
		}

		private static void VerifyCollection<T>(ref Dictionary<VehiclePawn, T> dict)
		{
			if (dict is null)
			{
				dict = new Dictionary<VehiclePawn, T>();
			}
			foreach(var item in dict.ToArray())
			{
				if (item.Key is null || item.Value is null)
				{
					dict.Remove(item.Key);
				}
			}
		}

		public IEnumerable<VehiclePawn> VehicleListers(string req)
		{
			return vehicleListers.Where(v => v.Value.requests.Contains(req)).Select(v => v.Key);
		}

		public bool RegisterLister(VehiclePawn vehicle, string req)
		{
			if (vehicleListers.TryGetValue(vehicle, out var request))
			{
				if (!request.requests.NotNullAndAny())
				{
					vehicleListers[vehicle] = new VehicleRequestCollection(req);
					return true;
				}
				return request.requests.Add(req);
			}
			vehicleListers.Add(vehicle, new VehicleRequestCollection(req));
			return true;
		}

		public bool RemoveLister(VehiclePawn vehicle, string req)
		{
			if (vehicleListers.TryGetValue(vehicle, out var requests))  
			{
				if(requests.requests.EnumerableNullOrEmpty())
				{
					return vehicleListers.Remove(vehicle);
				}
				return requests.requests.Remove(req);
			}
			return false;
		}

		public static VehiclePawn VehicleInhabitingCells(IEnumerable<IntVec3> cells, Map map)
		{
			foreach (VehiclePawn vehicle in map.mapPawns.AllPawnsSpawned.Where(p => p is VehiclePawn))
			{
				if (vehicle.OccupiedRect().Any(c => cells.Contains(c)))
				{
					return vehicle;
				}
			}
			return null;
		}

		public static bool AnyVehicleInhabitingCells(IEnumerable<IntVec3> cells, Map map)
		{
			return VehicleInhabitingCells(cells, map) != null;
		}

		internal void ClearAllListers()
		{
			reservations.Clear();
			vehicleListers.Clear();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref reservations, "reservations", LookMode.Reference, LookMode.Deep, ref vehicleReservations, ref reservationSets);
			Scribe_Collections.Look(ref vehicleListers, "vehicleListers", LookMode.Reference, LookMode.Deep, ref vehicleListerPawns, ref vehicleListerRequests);
		}

		public class VehicleRequestCollection : IExposable
		{
			public HashSet<string> requests = new HashSet<string>();

			public VehicleRequestCollection()
			{
				requests = new HashSet<string>();
				//uniqueId = Current.Game.GetCachedGameComponent<VehicleIdManager>().GetNextRequestCollectionId();
			}

			public VehicleRequestCollection(string req)
			{
				requests = new HashSet<string>() { req };
				//uniqueId = Current.Game.GetCachedGameComponent<VehicleIdManager>().GetNextRequestCollectionId();
			}

			public void ExposeData()
			{
				Scribe_Collections.Look(ref requests, "requests", LookMode.Value);
			}
		}
	}
}
