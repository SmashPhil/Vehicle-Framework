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
		public int nextUpgradeId;

		public int nextCannonId;

		public int nextVehicleHandlerId;

		public int nextReservationId;

		public int nextRequestCollectionId;

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
		}
	}
}
