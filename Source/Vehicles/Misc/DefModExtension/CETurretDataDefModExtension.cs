using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Vehicles
{
	public class CETurretDataDefModExtension : DefModExtension
	{
		public float speed = -1;
		public float sway = -1;
		public float spread = -1;
		public float recoil = -1;
		public float shotHeight = 1;
		public Def _ammoSet = null;
		public string ammoSet = null;
	}
}
