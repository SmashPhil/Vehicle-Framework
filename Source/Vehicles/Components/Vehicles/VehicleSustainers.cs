using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;

namespace Vehicles
{
	public class VehicleSustainers
	{
		private VehiclePawn vehicle;

		public List<Sustainer> activeSustainers;

		public VehicleSustainers(VehiclePawn vehicle)
		{
			this.vehicle = vehicle;
			activeSustainers = new List<Sustainer>();
		}

		public void Spawn(SoundDef soundDef, MaintenanceType maintenanceType)
		{
			SoundInfo soundInfo = SoundInfo.InMap(vehicle, maintenanceType);
			Sustainer sustainer = soundDef.TrySpawnSustainer(soundInfo);
			activeSustainers.Add(sustainer);
		}

		public void EndAll(SoundDef soundDef)
		{
			List<Sustainer> sustainers = activeSustainers.Where(sustainers => sustainers.def == soundDef).ToList();
			for (int i = sustainers.Count - 1; i >= 0; i--)
			{
				sustainers[i].End();
			}
		}

		public void Tick()
		{
			for (int i = activeSustainers.Count - 1; i >= 0; i--)
			{
				Sustainer sustainer = activeSustainers[i];

				if (sustainer == null || sustainer.Ended)
				{
					activeSustainers.Remove(sustainer);
				}
				else
				{
					sustainer.Maintain();
				}
			}
		}

		public void Cleanup()
		{
			for (int i = activeSustainers.Count - 1; i >= 0; i--)
			{
				activeSustainers[i].End();
			}
		}
	}
}
