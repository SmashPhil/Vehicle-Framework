using System.Collections.Generic;

namespace Vehicles
{
    public class WaterRoomGroup
    {
        public List<WaterRoom> Rooms => this.rooms;

        public int ID = -1;

        private List<WaterRoom> rooms = new List<WaterRoom>();

    }
}
