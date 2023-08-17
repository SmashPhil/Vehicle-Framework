using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;
using SmashTools;

namespace Vehicles
{
	public class VehicleGraphicSet
	{
		public VehiclePawn vehicle;

		public Graphic rottingGraphic;

		public Graphic dessicatedGraphic;

		public Graphic packGraphic;

		private List<Material> cachedMatsBodyBase = new List<Material>();

		private int cachedMatsBodyBaseHash = -1;

		public VehicleGraphicSet(VehiclePawn vehicle)
		{
			this.vehicle = vehicle;
		}

		public bool AllResolved
		{
			get
			{
				return vehicle.VehicleGraphic != null;
			}
		}

		public List<Material> MatsBodyBaseAt(Rot8 facing)
		{
			if (facing.IsHorizontal && vehicle.Angle != vehicle.CachedAngle)
			{
				cachedMatsBodyBase.Clear();
				cachedMatsBodyBaseHash = -1;
				vehicle.CachedAngle = vehicle.Angle;
			}
			int num = facing.AsInt;
			if (num != cachedMatsBodyBaseHash)
			{
				cachedMatsBodyBase.Clear();
				cachedMatsBodyBaseHash = num;
				cachedMatsBodyBase.Add(vehicle.VehicleGraphic.MatAtFull(facing));
			}
			return cachedMatsBodyBase;
		}

		public void ClearCache()
		{
			cachedMatsBodyBaseHash = -1;
		}


		public void ResolveAllGraphics()
		{
			ClearCache();
		}

		public void SetAllGraphicsDirty()
		{
			if (AllResolved)
			{
				ResolveAllGraphics();
			}
		}
	}
}
