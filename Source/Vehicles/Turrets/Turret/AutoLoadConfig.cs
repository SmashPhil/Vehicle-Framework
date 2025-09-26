using System.Collections.Generic;
using Verse;

namespace Vehicles;

public sealed class AutoLoadConfig : IExposable
{
	private VehicleTurret turret;
	private List<Quota> quotas = [];

	private Dictionary<ThingDef, Quota> QuotaLookup { get; } = [];

	public AutoLoadConfig(VehicleTurret turret)
	{
		this.turret = turret;
		RecacheSettings();
	}

	public int Get(ThingDef thingDef)
	{
		return QuotaLookup.TryGetValue(thingDef)?.count ?? 0;
	}

	public void Set(ThingDef thingDef, int count)
	{
		if (!QuotaLookup.TryGetValue(thingDef, out Quota quota))
		{
			Log.Error($"{thingDef} is not registered as a valid ammo type.");
			return;
		}
		quota.count = count;
	}

	public bool IsEnabled(ThingDef thingDef)
	{
		if (!QuotaLookup.TryGetValue(thingDef, out Quota quota))
		{
			Log.Error($"{thingDef} is not registered as a valid ammo type.");
			return false;
		}
		return quota.enabled;
	}

	public void SetEnabled(ThingDef thingDef, bool enabled)
	{
		if (!QuotaLookup.TryGetValue(thingDef, out Quota quota))
		{
			Log.Error($"{thingDef} is not registered as a valid ammo type.");
			return;
		}
		quota.enabled = enabled;
	}

	private void RecacheSettings()
	{
		if (turret.def.ammunition == null)
			return;

		foreach (Quota quota in quotas)
		{
			if (quota.ThingDef != null)
			{
				QuotaLookup[quota.ThingDef] = quota;
			}
		}

		foreach (ThingDef thingDef in turret.def.ammunition.AllowedThingDefs)
		{
			if (!QuotaLookup.TryGetValue(thingDef, out Quota quota))
			{
				quota = new Quota(thingDef);
				quotas.Add(quota);
				QuotaLookup[quota.ThingDef] = quota;
			}
		}
	}

	void IExposable.ExposeData()
	{
		Scribe_References.Look(ref turret, nameof(turret));
		Scribe_Values.Look(ref quotas, nameof(quotas));

		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			RecacheSettings();
		}
	}

	private record Quota : IExposable
	{
		private string defName;

		public bool enabled;
		public int count;

		public Quota(ThingDef thingDef)
		{
			defName = thingDef.defName;
			ThingDef = thingDef;
		}

		public ThingDef ThingDef { get; private set; }

		void IExposable.ExposeData()
		{
			Scribe_Values.Look(ref defName, nameof(defName));
			Scribe_Values.Look(ref enabled, nameof(enabled));
			Scribe_Values.Look(ref count, nameof(count));

			if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
			{
				ThingDef = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
			}
		}
	}
}