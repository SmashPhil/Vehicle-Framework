using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
    public class VehicleGraphicSet
    {
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

		public List<Material> MatsBodyBaseAt(Rot4 facing, RotDrawMode bodyCondition = RotDrawMode.Fresh)
		{
			if(facing.IsHorizontal && vehicle.Angle != vehicle.CachedAngle)
            {
                cachedMatsBodyBase.Clear();
                cachedMatsBodyBaseHash = -1;
                vehicle.CachedAngle = vehicle.Angle;
            }
			int num = facing.AsInt + 1000 * (int)bodyCondition;
			if (num != cachedMatsBodyBaseHash)
			{
				cachedMatsBodyBase.Clear();
				cachedMatsBodyBaseHash = num;
				cachedMatsBodyBase.Add(vehicle.VehicleGraphic.MatAt(facing, vehicle));
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

			//Future content?
			if (vehicle.RaceProps.packAnimal)
			{
				packGraphic = GraphicDatabase.Get<Graphic_Vehicle>(vehicle.VehicleGraphic.path + "Pack", ShaderDatabase.Cutout, vehicle.VehicleGraphic.drawSize, Color.white);
			}
			rottingGraphic = vehicle.VehicleGraphic.GetColoredVersion(ShaderDatabase.CutoutSkin, PawnGraphicSet.RottingColor, PawnGraphicSet.RottingColor);
		}

		public void SetAllGraphicsDirty()
		{
			if (AllResolved)
			{
				ResolveAllGraphics();
			}
		}

		public VehiclePawn vehicle;

		public Graphic rottingGraphic;

		public Graphic dessicatedGraphic;

		public Graphic packGraphic;

		private List<Material> cachedMatsBodyBase = new List<Material>();

		private int cachedMatsBodyBaseHash = -1;
    }
}
