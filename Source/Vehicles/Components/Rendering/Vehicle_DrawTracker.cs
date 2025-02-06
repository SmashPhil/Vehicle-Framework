using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;
using SmashTools.Animations;

namespace Vehicles
{
	public class Vehicle_DrawTracker
	{
		private VehiclePawn vehicle;

		public VehicleTweener tweener;
		public VehicleRenderer renderer;
		public PawnUIOverlay ui; //reimplement for better control over vehicle overlays (names should show despite animal Prefs set to none, traders inside should transfer question mark, etc.)
		public VehicleTrackMaker trackMaker; //reimplement for vehicle specific "footprints"
		public Vehicle_RecoilTracker rTracker;

		public Vehicle_DrawTracker(VehiclePawn vehicle)
		{
			this.vehicle = vehicle;
			tweener = new VehicleTweener(vehicle);
			renderer = new VehicleRenderer(vehicle);
			ui = new PawnUIOverlay(vehicle);
			trackMaker = new VehicleTrackMaker(vehicle);
			rTracker = new Vehicle_RecoilTracker();
		}

		public Vector3 DrawPos
		{
			get
			{
				tweener.PreDrawPosCalculation();
				Vector3 vector = tweener.TweenedPos;
				vector.y = vehicle.def.Altitude;

				if (rTracker.Recoil > 0f)
				{
					vector = Ext_Math.PointFromAngle(vector, rTracker.Recoil, rTracker.Angle);
				}
				return vector;
			}
		}

		public void ProcessPostTickVisuals(int ticksPassed)
		{
			if (!vehicle.Spawned)
			{
				return;
			}
			renderer.ProcessPostTickVisuals(ticksPassed);
			trackMaker.ProcessPostTickVisuals(ticksPassed);
			rTracker.ProcessPostTickVisuals(ticksPassed);
		}

		public void DrawAt(Vector3 loc, float extraRotation)
		{
			vehicle.CalculateAngle(out bool northSouthRotation);
			renderer.RenderPawnAt(loc, extraRotation, northSouthRotation);
		}

		public void Notify_Spawned()
		{
			tweener.ResetTweenedPosToRoot();
		}
	}
}
