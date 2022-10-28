using System;
using System.Collections.Generic;
using System.Linq;

namespace Vehicles
{
	public class TurretCooldownProperties
	{
		public float heatPerShot = 5;
		public float dissipationRate = 0.25f; //Per Tick
		public float dissipationCapMultiplier = 0.5f;
	}
}
