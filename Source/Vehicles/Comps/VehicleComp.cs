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
		
		/// <summary>
		/// Called when newly generated, unlike PostSpawnSetup called every time it is spawned in-map
		/// </summary>
		public virtual void PostGeneration()
		{
		}

		public virtual void EventRegistration()
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

		public virtual void StartTicking()
		{
			if (TickByRequest)
			{
				Vehicle.RequestTickStart(this);
			}
		}

		public virtual void StopTicking()
		{
			if (TickByRequest)
			{
				Vehicle.RequestTickStop(this);
			}
		}
	}
}
