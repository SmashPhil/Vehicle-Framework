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
    public class Vehicle_DrawTracker
    {
		public Vector3 DrawPos
		{
			get
			{
				tweener.PreDrawPosCalculation();
				Vector3 vector = tweener.TweenedPos;
				vector.y = vehicle.def.Altitude;

				if (rTracker.Recoil > 0f)
				{
					vector = SPTrig.PointFromAngle(vector, rTracker.Recoil, rTracker.Angle);
				}
				return vector;
			}
		}

		public Vehicle_DrawTracker(VehiclePawn vehicle)
		{
			this.vehicle = vehicle;
			tweener = new PawnTweener(vehicle);
			renderer = new VehicleRenderer(vehicle);
			ui = new PawnUIOverlay(vehicle);
			footprintMaker = new PawnFootprintMaker(vehicle);
			rTracker = new Vehicle_RecoilTracker();
		}

		public void VehicleDrawerTick()
		{
			if (!vehicle.Spawned)
			{
				return;
			}
			if (Current.ProgramState == ProgramState.Playing && !Find.CameraDriver.CurrentViewRect.ExpandedBy(3).Contains(vehicle.Position))
			{
				return;
			}
			footprintMaker.FootprintMakerTick();
			renderer.RendererTick();
			rTracker.RecoilTick();
		}

		public void DrawAt(Vector3 loc)
		{
			renderer.RenderPawnAt(loc);
		}

		public void Notify_Spawned()
		{
			tweener.ResetTweenedPosToRoot();
		}

		public void Notify_DamageApplied(DamageInfo dinfo)
		{
			if (vehicle.Destroyed)
			{
				return;
			}
			//renderer.Notify_DamageApplied(dinfo);
		}

		public void Notify_DamageDeflected(DamageInfo dinfo)
		{
			if (vehicle.Destroyed)
			{
				return;
			}
		}

		private VehiclePawn vehicle;

		public PawnTweener tweener;
		public VehicleRenderer renderer;
		public PawnUIOverlay ui;
		public PawnFootprintMaker footprintMaker;
		public Vehicle_RecoilTracker rTracker;
    }
}
