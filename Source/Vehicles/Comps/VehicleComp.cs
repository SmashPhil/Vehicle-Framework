using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class VehicleComp : ThingComp
	{
		public VehiclePawn Vehicle => parent as VehiclePawn;

		/// <summary>
		/// If true, must request to start / stop ticking caller
		/// </summary>
		public virtual bool TickByRequest => false;

		public virtual IEnumerable<AnimationDriver> Animations { get; }

		public virtual void PostLoad()
		{
		}
		
		public virtual void PostGenerationSetup()
		{
		}

		public virtual void SpawnedInGodMode()
		{
		}

		public virtual bool CanStartEngine(out string failReason)
		{
			failReason = string.Empty;
			return true;
		}

		public void StartTicking()
		{
			if (TickByRequest)
			{
				Vehicle.RequestTickStart(this);
			}
		}

		public void StopTicking()
		{
			if (TickByRequest)
			{
				Vehicle.RequestTickStop(this);
			}
		}
	}
}
