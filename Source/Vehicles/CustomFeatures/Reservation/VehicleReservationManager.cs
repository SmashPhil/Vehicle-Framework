using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using SmashTools;
using System.Collections;

namespace Vehicles
{
	public class VehicleReservationManager : MapComponent
	{
		private const int ReservationVerificationInterval = 120;

		private Dictionary<VehiclePawn, VehicleReservationCollection> reservations = new Dictionary<VehiclePawn, VehicleReservationCollection>();
		private Dictionary<VehiclePawn, VehicleRequestCollection> vehicleListers = new Dictionary<VehiclePawn, VehicleRequestCollection>();

		private List<VehiclePawn> vehiclesReserving_tmp = new List<VehiclePawn>();
		private List<VehicleReservationCollection> vehicleReservations_tmp = new List<VehicleReservationCollection>();

		private List<VehiclePawn> vehicleListerPawns_tmp = new List<VehiclePawn>();
		private List<VehicleRequestCollection> vehicleListerRequests_tmp = new List<VehicleRequestCollection>();

		public VehicleReservationManager(Map map) : base(map)
		{
		}

		public bool Reserve<T1, T2>(VehiclePawn vehicle, Pawn pawn, Job job, T1 target) where T2 : Reservation<T1>
		{
			if (vehicle is null || pawn is null || job is null)
			{
				return false;
			}

			try
			{
				ReleaseAllClaimedBy(pawn);
				
				if (GetReservation<T2>(vehicle) is T2 reversionSubType)
				{
					if (reversionSubType.CanReserve(pawn, target))
					{
						return reversionSubType.AddClaimant(pawn, target);
					}
					return false;
				}
				int maxClaimaints = vehicle.TotalAllowedFor(job.def);

				if (!reservations.TryGetValue(vehicle, out VehicleReservationCollection reservationCollection))
				{
					reservationCollection = new VehicleReservationCollection();
					reservations[vehicle] = reservationCollection;
				}
				reservationCollection.Add((ReservationBase)Activator.CreateInstance(typeof(T2), new object[] { vehicle, job, maxClaimaints }));

				reversionSubType = GetReservation<T2>(vehicle);
				if (reversionSubType == null)
				{
					Log.Error($"Unable to retrieve reservation for {pawn} performing Job={job} from new reservation.");
					return false;
				}
				reversionSubType.AddClaimant(pawn, target);
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
			if (!reservations.TryGetValue(vehicle, out VehicleReservationCollection vehicleReservations))
			{
				return null;
			}
			foreach (ReservationBase reservation in vehicleReservations.List)
			{
				if (reservation is T matchingReservation)
				{
					return matchingReservation;
				}
			}
			return null;
		}
		
		public void ReleaseAllClaims()
		{
			foreach (VehiclePawn vehicle in reservations.Keys.ToList()) //Repack in list to avoid modifying enumerator
			{
				ClearReservedFor(vehicle);
			}
		}

		public void ReleaseAllClaimedBy(Pawn pawn)
		{
			//Only 1 vehicle will have reservations by this pawn, but no way to know which without checking all
			foreach (VehicleReservationCollection vehicleReservations in reservations.Values)
			{
				foreach (ReservationBase reservation in vehicleReservations.List)
				{
					reservation.ReleaseReservationBy(pawn);
				}
			}
		}

		public void ClearReservedFor(VehiclePawn vehicle)
		{
			if (reservations.ContainsKey(vehicle))
			{
				reservations[vehicle].List.ForEach(reservation => reservation.ReleaseAllReservations());
				reservations.Remove(vehicle);
			}
		}

		public bool CanReserve(VehiclePawn vehicle, Pawn pawn, JobDef jobDef, StringBuilder stringBuilder = null)
		{
			stringBuilder?.AppendLine($"Starting Reservation check.");
			if (reservations.TryGetValue(vehicle, out VehicleReservationCollection vehicleReservations))
			{
				foreach (ReservationBase reservation in vehicleReservations.List)
				{
					if (reservation.JobDef == jobDef)
					{
						stringBuilder?.AppendLine($"Reservation cached. Claimants = {vehicle.TotalAllowedFor(jobDef)}/{reservation.TotalClaimants}.");
						return vehicle.TotalAllowedFor(jobDef) > reservation.TotalClaimants;
					}
				}
			}
			stringBuilder?.AppendLine($"Reservation not cached. Can automatically reserve");
			return true;
		}

		public bool CanReserve<T1, T2>(VehiclePawn vehicle, Pawn pawn, T1 target, StringBuilder stringBuilder = null) where T2 : Reservation<T1>
		{
			stringBuilder?.AppendLine($"Starting Reservation check.");
			if (reservations.TryGetValue(vehicle, out VehicleReservationCollection vehicleReservations))
			{
				foreach (ReservationBase reservation in vehicleReservations.List)
				{
					if (reservation is T2 reversionSubType)
					{
						stringBuilder?.AppendLine($"Reservation cached. Type={typeof(T2)} CanReserve={reversionSubType.CanReserve(pawn, target)}");
						return reversionSubType.CanReserve(pawn, target);
					}
				}
			}
			stringBuilder?.AppendLine($"Reservation not cached. Can automatically reserve");
			return true;
		}

		public int TotalReserving(VehiclePawn vehicle)
		{
			if (reservations.TryGetValue(vehicle, out VehicleReservationCollection vehicleReservations))
			{
				int total = 0;
				foreach (ReservationBase reservation in vehicleReservations.List)
				{
					total += reservation.TotalClaimants;
				}
				return total;
			}
			return 0;
		}

		public override void MapComponentTick()
		{
			if (Find.TickManager.TicksGame % ReservationVerificationInterval == 0)
			{
				List<KeyValuePair<VehiclePawn, VehicleReservationCollection>> vehicleRegistry = reservations.ToList();

				for (int i = vehicleRegistry.Count - 1; i >= 0; i--)
				{
					(VehiclePawn vehicle, VehicleReservationCollection vehicleReservations) = vehicleRegistry[i];
					for (int j = vehicleReservations.Count - 1; j >= 0; j--)
					{
						ReservationBase reservation = vehicleReservations[j];
						reservation.VerifyAndValidateClaimants();
						if (reservation.RemoveNow)
						{
							reservations[vehicle].Remove(reservation);
						}
					}
					if (reservations[vehicle].NullOrEmpty())
					{
						reservations.Remove(vehicle);
					}
				}
			}
		}

		public override void FinalizeInit()
		{
			base.FinalizeInit();

			VerifyCollection(ref reservations);
			VerifyCollection(ref vehicleListers);
			VerifyCollection(ref vehiclesReserving_tmp);
			VerifyCollection(ref vehicleReservations_tmp);
			VerifyCollection(ref vehicleListerPawns_tmp);
			VerifyCollection(ref vehicleListerRequests_tmp);

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
			Scribe_Collections.Look(ref reservations, nameof(reservations), LookMode.Reference, LookMode.Deep, ref vehiclesReserving_tmp, ref vehicleReservations_tmp);
			Scribe_Collections.Look(ref vehicleListers, nameof(vehicleListers), LookMode.Reference, LookMode.Deep, ref vehicleListerPawns_tmp, ref vehicleListerRequests_tmp);
		}

		/// <summary>
		/// Serves as a wrapper class for reservation list.  Easier to save than a nested collection
		/// </summary>
		public class VehicleReservationCollection : IExposable
		{
			public List<ReservationBase> reservations = new List<ReservationBase>();

			public bool NullOrEmpty() => reservations.NullOrEmpty();

			public void Add<T>(T reservation) where T : ReservationBase
			{
				reservations.Add(reservation);
			}

			public bool Remove<T>(T reservation) where T : ReservationBase
			{
				return reservations.Remove(reservation);
			}

			public List<ReservationBase> List => reservations; //avoid enumerator and just pass back list, no need for extra garbage creation

			public int Count => reservations.Count;

			public ReservationBase this[int index] => reservations[index];

			public void ExposeData()
			{
				Scribe_Collections.Look(ref reservations, nameof(reservations), LookMode.Deep);
			}
		}

		/// <summary>
		/// Serves as a wrapper class for vehicle requests.  Easier to save than a nested collection
		/// </summary>
		public class VehicleRequestCollection : IExposable
		{
			public HashSet<string> requests = new HashSet<string>();

			public VehicleRequestCollection()
			{
				requests = new HashSet<string>();
			}

			public VehicleRequestCollection(string req)
			{
				requests = new HashSet<string>() { req };
			}

			public void ExposeData()
			{
				Scribe_Collections.Look(ref requests, nameof(requests), LookMode.Value);
			}
		}
	}
}
