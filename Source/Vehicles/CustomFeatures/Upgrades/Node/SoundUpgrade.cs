using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class SoundUpgrade : Upgrade
	{
		public List<VehicleSoundEventEntry<VehicleEventDef>> addOneShots;

		public List<VehicleSoundEventEntry<VehicleEventDef>> removeOneShots;

		public List<VehicleSustainerEventEntry<VehicleEventDef>> addSustainers;

		public List<VehicleSustainerEventEntry<VehicleEventDef>> removeSustainers;

		public override bool UnlockOnLoad => true;

		public override void Unlock(VehiclePawn vehicle, bool unlockingAfterLoad)
		{
			if (!removeOneShots.NullOrEmpty())
			{
				foreach (var soundEventEntry in removeOneShots)
				{
					vehicle.RemoveEvent(soundEventEntry.key, soundEventEntry.removalKey);
				}
			}
			if (!addOneShots.NullOrEmpty())
			{
				foreach (var soundEventEntry in addOneShots)
				{
					vehicle.AddEvent(soundEventEntry.key, () => Ext_Vehicles.PlayOneShot(vehicle, soundEventEntry), soundEventEntry.removalKey);
				}
			}
			if (!removeSustainers.NullOrEmpty())
			{
				foreach (var soundEventEntry in removeSustainers)
				{
					vehicle.sustainers.EndAll(soundEventEntry.value);

					vehicle.RemoveEvent(soundEventEntry.start, soundEventEntry.removalKey);
					vehicle.RemoveEvent(soundEventEntry.stop, soundEventEntry.removalKey);
				}
			}
			if (!addSustainers.NullOrEmpty())
			{
				foreach (var soundEventEntry in addSustainers)
				{
					vehicle.AddEvent(soundEventEntry.start, () => Ext_Vehicles.StartSustainer(vehicle, soundEventEntry), soundEventEntry.removalKey);
					vehicle.AddEvent(soundEventEntry.stop, () => Ext_Vehicles.StopSustainer(vehicle, soundEventEntry), soundEventEntry.removalKey);
				}
			}
		}

		public override void Refund(VehiclePawn vehicle)
		{
			if (!addOneShots.NullOrEmpty())
			{
				foreach (var soundEventEntry in addOneShots)
				{
					vehicle.RemoveEvent(soundEventEntry.key, soundEventEntry.removalKey);
				}
			}
			if (!removeOneShots.NullOrEmpty())
			{
				foreach (var soundEventEntry in removeOneShots)
				{
					vehicle.AddEvent(soundEventEntry.key, () => Ext_Vehicles.PlayOneShot(vehicle, soundEventEntry), soundEventEntry.removalKey);
				}
			}
			if (!addSustainers.NullOrEmpty())
			{
				foreach (var soundEventEntry in addSustainers)
				{
					vehicle.sustainers.EndAll(soundEventEntry.value);

					vehicle.RemoveEvent(soundEventEntry.start, soundEventEntry.removalKey);
					vehicle.RemoveEvent(soundEventEntry.stop, soundEventEntry.removalKey);
				}
			}
			if (!removeSustainers.NullOrEmpty())
			{
				foreach (var soundEventEntry in removeSustainers)
				{
					vehicle.AddEvent(soundEventEntry.start, () => Ext_Vehicles.StartSustainer(vehicle, soundEventEntry), soundEventEntry.removalKey);
					vehicle.AddEvent(soundEventEntry.stop, () => Ext_Vehicles.StopSustainer(vehicle, soundEventEntry), soundEventEntry.removalKey);
				}
			}
		}
	}
}
