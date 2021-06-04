using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public class Flak : AntiAircraft
	{
		protected const float Spread = 0.1f;
		private const float MinSpeed = 0.01f;

		public override void Initialize(WorldObject firedFrom, AerialVehicleInFlight target, Vector3 source)
		{
			this.target = target;
			Vector3 misfire = new Vector3(Rand.Range(-Spread, Spread), Rand.Range(-Spread, Spread), Rand.Range(-Spread, Spread));
			destination = this.target.DrawPosAhead(50) - misfire;
			this.source = source;
			this.firedFrom = firedFrom;
			speedPctPerTick = Mathf.Max((AerialVehicleInFlight.PctPerTick / Ext_Math.SphericalDistance(this.source, destination)) * Rand.Range(40, 70), MinSpeed);
			InitializeFacing();

			explosionFrame = -1;
		}
	}
}
