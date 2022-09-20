using UnityEngine;
using Verse;

namespace Vehicles
{
	/// <summary>
	/// Non-ticking non-serializable component for additional effects and actions for VehicleComponent events
	/// </summary>
	/// <remarks>Only triggers events for spawned vehicles</remarks>
	public abstract class Reactor
	{
		public IndicatorDef indicator;
		public Color highlightColor = new Color(1, 0.5f, 0);

		public abstract void Hit(VehiclePawn vehicle, VehicleComponent component, ref DamageInfo dinfo, bool penetrated);

		public virtual void Repaired(VehiclePawn vehicle, VehicleComponent component, float amount)
		{
		}
	}
}
