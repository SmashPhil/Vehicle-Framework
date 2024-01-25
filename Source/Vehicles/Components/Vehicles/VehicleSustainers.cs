using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;
using SmashTools;

namespace Vehicles
{
	public class VehicleSustainers
	{
		private VehiclePawn vehicle;
		private List<Sustainer> activeSustainers = new List<Sustainer>();

		public VehicleSustainers(VehiclePawn vehicle)
		{
			this.vehicle = vehicle;
		}

		public void Spawn(ISustainerTarget sustainerTarget, SoundDef soundDef)
		{
			SpawnSustainer(sustainerTarget.Target, soundDef, sustainerTarget.MaintenanceType);
		}

		public void Spawn(VehiclePawn vehicle, SoundDef soundDef)
		{
			SpawnSustainer(vehicle, soundDef);
		}

		private void SpawnSustainer(TargetInfo target, SoundDef soundDef, MaintenanceType maintenanceType = MaintenanceType.PerTick)
		{
			SoundInfo soundInfo = SoundInfo.InMap(target, maintenanceType);
			Sustainer sustainer = soundDef.TrySpawnSustainer(soundInfo);
			activeSustainers.Add(sustainer);
		}

		/// <summary>
		/// End all active sustainers
		/// </summary>
		public void EndAll()
		{
			for (int i = activeSustainers.Count - 1; i >= 0; i--)
			{
				activeSustainers[i]?.End();
			}
		}

		/// <summary>
		/// End all active sustainers of <paramref name="soundDef"/> type
		/// </summary>
		public void EndAll(SoundDef soundDef)
		{
			List<Sustainer> sustainers = activeSustainers.Where(sustainer => sustainer?.def == soundDef).ToList();
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
	}
}
