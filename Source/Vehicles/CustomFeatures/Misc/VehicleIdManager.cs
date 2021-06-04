using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace Vehicles
{
	public class VehicleIdManager : GameComponent
	{
		private int nextUpgradeId;

		private int nextCannonId;

		private int nextVehicleHandlerId;

		private int nextReservationId;

		private int nextRequestCollectionId;

		private int nextAirDefenseId;

		public VehicleIdManager(Game game)
		{
			Instance = this;
		}

		public static VehicleIdManager Instance { get; private set; }

		public int GetNextRequestCollectionId()
		{
			return GetNextId(ref nextRequestCollectionId);
		}

		public int GetNextReservationId()
		{
			return GetNextId(ref nextReservationId);
		}

		public int GetNextUpgradeId()
		{
			return GetNextId(ref nextUpgradeId);
		}

		public int GetNextCannonId()
		{
			return GetNextId(ref nextCannonId);
		}

		public int GetNextHandlerId()
		{
			return GetNextId(ref nextVehicleHandlerId);
		}

		public int GetNextAirDefenseId()
		{
			return GetNextId(ref nextAirDefenseId);
		}

		private int GetNextId(ref int id)
		{
			id++;
			return id;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref nextUpgradeId, "nextUpgradeId", 0);
			Scribe_Values.Look(ref nextCannonId, "nextCannonId", 0);
			Scribe_Values.Look(ref nextVehicleHandlerId, "nextVehicleHandlerId", 0);
			Scribe_Values.Look(ref nextReservationId, "nextReservationId", 0);
			Scribe_Values.Look(ref nextRequestCollectionId, "nextRequestCollectionId", 0);
			Scribe_Values.Look(ref nextAirDefenseId, "nextAirDefenseId", 0);
		}
	}
}
