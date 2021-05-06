using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vehicles
{
	public struct OffsetMote
	{
		public bool windAffected;
		public float moteThrownSpeed;
		public float? predeterminedAngleVector;
		public float xOffset;
		public float zOffset;
		public int numTimesSpawned;

		public int NumTimesSpawned
		{
			get
			{
				return numTimesSpawned == 0 ? 1 : numTimesSpawned;
			}
		}
	}
}
