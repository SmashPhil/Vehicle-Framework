using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using Verse.AI;
using HarmonyLib;

namespace Vehicles
{
    public class VehicleReservationManager : MapComponent
    {
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
            return !reservations.ContainsKey(vehicle) || (reservations[vehicle] as T2).CanReserve(pawn, target);
        }

        public int TotalReserving(VehiclePawn vehicle)
        {
            return reservations[vehicle].TotalClaimants;
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

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref reservations, "reservations", LookMode.Reference, LookMode.Reference);
        }

        private Dictionary<VehiclePawn, ReservationBase> reservations = new Dictionary<VehiclePawn, ReservationBase>();

        private const int ReservationVerificationInterval = 50;
    }
}
