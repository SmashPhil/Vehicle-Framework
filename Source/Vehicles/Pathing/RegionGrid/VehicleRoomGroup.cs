using System.Collections.Generic;

namespace Vehicles
{
	public class VehicleRoomGroup
	{
		public List<VehicleRoom> Rooms => this.rooms;

		public int ID = -1;

		private List<VehicleRoom> rooms = new List<VehicleRoom>();

	}
}
