using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RimShips
{
    public class WaterRoomGroup
    {
        public List<WaterRoom> Rooms => this.rooms;

        public int ID = -1;

        private List<WaterRoom> rooms = new List<WaterRoom>();

    }
}
