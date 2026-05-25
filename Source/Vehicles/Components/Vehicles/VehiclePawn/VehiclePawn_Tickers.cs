using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using SmashTools.Performance;
using Verse;

namespace Vehicles;

public partial class VehiclePawn
{
	public const int HoursIdleToAlert = 2;
	public const int HoursIdleToDismount = 5;
	public const int TicksTillAlert = GenDate.TicksPerHour * HoursIdleToAlert;
	public const int TicksTillDismount = GenDate.TicksPerHour * HoursIdleToDismount;

	public const int MaxTickInterval = GenTicks.TickRareInterval;

	[Unsaved]
	public VehicleSustainers sustainers;

	private int ticksIdle;
	private List<TimedExplosion> explosives = [];

	public bool IdlePawnsInVehicle => ticksIdle >= TicksTillAlert && 
		(AllPawnsAboard.Count > 0 || AllInventoryPawns.Count > 0);

	// Vehicles should never be suspended since there is no logic for handling passengers in a
	// suspended vehicle. Suspending the vehicle would also suspend all passengers by proxy.
	public override bool Suspended => false;

	public int AttachedExplosives => explosives.Count;

	// Pawn has null held things
	bool IThingHolderTickable.ShouldTickContents => false;

	protected override int MaxTickIntervalRate => MaxTickInterval;

	public override int UpdateRateTicks
	{
		get
		{
			if (AllPawnsAboard.Count == 0 && compTickers.Count == 0)
				return MaxTickInterval;
			return base.UpdateRateTicks;
		}
	}

	public void AddTimedExplosion(TimedExplosion exploder)
	{
		explosives.Add(exploder);
		DrawTracker.AddRenderer(exploder);
	}

	public TimedExplosion AddTimedExplosion(TimedExplosion.Data explosionData,
		DrawOffsets drawOffsets = null)
	{
		TimedExplosion exploder = new(this, explosionData, drawOffsets: drawOffsets);
		AddTimedExplosion(exploder);
		return exploder;
	}

	[Profile]
	protected override void Tick()
	{
		BaseTickOptimized();
		TickAllComps();
		if (Faction != Faction.OfPlayer)
		{
			vehicleAI?.AITick();
		}
	}

	public bool RequestTickStart<T>(T comp) where T : ThingComp
	{
		if (!compTickers.Contains(comp))
		{
			compTickers.Add(comp);
			return true;
		}
		return false;
	}

	public bool RequestTickStop<T>(T comp) where T : ThingComp
	{
		return compTickers.Remove(comp);
	}

	private void TickExplosives()
	{
		for (int i = explosives.Count - 1; i >= 0; i--)
		{
			TimedExplosion timedExplosion = explosives[i];
			if (!timedExplosion.Tick())
			{
				explosives.Remove(timedExplosion);
				DrawTracker.RemoveRenderer(timedExplosion);
			}
		}
	}

	protected virtual void TickAllComps()
	{
		for (int i = compTickers.Count - 1; i >= 0; i--)
		{
			// Must run back to front in case CompTick methods trigger their own removal
			compTickers[i].CompTick();
		}
	}

	public override void TickRare()
	{
		base.TickRare();
		EventRegistry[VehicleEventDefOf.ScanRare].ExecuteEvents();
	}

	private void TickShort()
	{
		EventRegistry[VehicleEventDefOf.ScanShort].ExecuteEvents();
	}

	[Profile]
	protected override void TickInterval(int delta)
	{
		if (cachedComps != null)
		{
			for (int i = 0; i < cachedComps.Count; i++)
			{
				cachedComps[i].CompTickInterval(delta);
			}
		}

		ageTracker.AgeTickInterval(delta);
		records.RecordsTickInterval(delta);
		if (!this.IsWorldPawn())
		{
			jobs.JobTrackerTickInterval(delta);
		}

		if (Spawned && !vehiclePather.Moving && ticksIdle < TicksTillDismount)
		{
			ticksIdle += delta;
		}
		if (ticksIdle >= TicksTillDismount)
		{
			DisembarkAll();
			DisembarkAllFromInventory();
			ResetIdleTicks();
		}

		// TODO VF-301,302,303: Enable gas, toxic, and vacuum to affect pawns in vehicles.
	}

	[Profile]
	private void TickHandlers()
	{
		// Only need to tick VehicleHandlers with pawns inside them
		foreach (VehicleRoleHandler handler in OccupiedHandlers)
		{
			handler.DoTick();
		}
	}

	[Profile]
	protected virtual void BaseTickOptimized()
	{
		const int ScanShortTicks = 60;
		const int ScanRareTicks = GenTicks.TickRareInterval;

		if (this.IsHashIntervalTick(ScanShortTicks))
			TickShort();
		if (this.IsHashIntervalTick(ScanRareTicks))
			TickRare();

		sustainers.Tick();
		TickHandlers();

		if (Spawned)
		{
			//animator?.AnimationTick();
			vehiclePather.PatherTick();
			stances.StanceTrackerTick();
			if (Drafted || fishingTracker is { IsFishing: true } || CompVehicleTurrets is { Deploying: true })
			{
				jobs.JobTrackerTick();
			}
			TickExplosives();
		}

		//abilities?.AbilitiesTick();
		inventory.innerContainer.DoTick();
		inventory.InventoryTrackerTick();
	}
}