using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using SmashTools;
using Verse;
using Verse.Sound;

namespace Vehicles;

[PublicAPI]
public class DropShip : IExposable
{
	private static readonly FloatRange DropAngleRange = new(170, 190);
	private static readonly FloatRange ExtraAngleVariance = new(-5, 5);

	private Map map;
	private Faction faction;
	private SoundDef flyoverSoundDef;

	private Properties properties;
	private DropZone dropZone;
	private List<IAirDroppable> airDroppables = [];

	private Sustainer sustainer;

	public DropShip(Map map, DropZone dropZone, Properties properties)
	{
		this.map = map;
		this.dropZone = dropZone;
		this.properties = properties;
	}

	private float DropAngle { get; set; }

	public IntVec3 Origin => dropZone.from;

	public Faction Faction
	{
		get => faction;
		init => faction = value;
	}

	public SoundDef FlyoverSoundDef
	{
		get => flyoverSoundDef;
		init => flyoverSoundDef = value;
	}

	public void Add(IAirDroppable airDroppable)
	{
		airDroppables.Add(airDroppable);
	}

	public void OnSpawned()
	{
		if (FlyoverSoundDef != null)
		{
			if (FlyoverSoundDef.sustain)
			{
				sustainer = FlyoverSoundDef.TrySpawnSustainer(SoundInfo.InMap(
					new TargetInfo(Origin, map),
					MaintenanceType.PerTick));
			}
			else
			{
				FlyoverSoundDef.PlayOneShotOnCamera(map);
			}
		}
	}

	private void RecalculateDropAngle()
	{
		DropAngle = DropAngleRange.RandomInRange;
	}

	public void Tick()
	{
		sustainer?.Maintain();
		if (properties.lifetime-- <= 0)
		{
			DropAll();
			Destroy();
		}

		if (properties.delayDropByTicks-- > 0)
			return;

		if (properties.ticksToNextDrop-- <= 0)
		{
			TryDropSingle();
			properties.ticksToNextDrop = properties.ticksBetweenDrops;
		}
	}

	private void TryDropSingle()
	{
		if (airDroppables.Count == 0)
			return;

		int index = dropZone.dropPoints.Count - airDroppables.Count;
		IAirDroppable airDroppable = airDroppables.Pop();
		IntVec3 dropSpot = dropZone.dropPoints[index];
		if (!TryDropNear(airDroppable, map, dropSpot, DropAngle))
		{
			airDroppable.OnFailureToDrop(map, dropSpot);
		}
		else
		{
			airDroppable.OnDropped(map, dropSpot);
		}
	}

	private void DropAll()
	{
		while (airDroppables.Count > 0)
		{
			TryDropSingle();
		}
	}

	private void Destroy()
	{
		map.GetCachedMapComponent<AirdropManager>().Remove(this);
	}

	void IExposable.ExposeData()
	{
		Scribe_References.Look(ref map, nameof(map));
		Scribe_References.Look(ref faction, nameof(faction));
		Scribe_Defs.Look(ref flyoverSoundDef, nameof(flyoverSoundDef));

		Scribe_Deep.Look(ref properties, nameof(properties));
		Scribe_Deep.Look(ref dropZone, nameof(dropZone));
		Scribe_Collections.Look(ref airDroppables, nameof(airDroppables), LookMode.Deep);

		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			RecalculateDropAngle();
		}
	}

	private static bool TryDropNear(IAirDroppable airDroppable, Map map, IntVec3 dropCenter, float angle)
	{
		const int AccuracyRadii = 9; // TODO - Should be dependent on the droppable

		//bool roofPunch = faction != null && faction.HostileTo(Faction.OfPlayer);
		if (!DropCellFinder.TryFindDropSpotNear(dropCenter, map, out IntVec3 position,
				allowFogged: false, canRoofPunch: true,
				allowIndoors: true, maxRadius: AccuracyRadii,
				mustBeReachableFromCenter: false) || !position.IsValid || !position.InBounds(map))
		{
			return false;
		}

		Skyfaller skyfaller = AirdropSkyfallerMaker.MakeAirdrop(airDroppable, angle + ExtraAngleVariance.RandomInRange);
		GenSpawn.Spawn(skyfaller, position, map);
		return true;
	}

	public record Properties : IExposable
	{
		public required int lifetime;
		public int ticksBetweenDrops = int.MaxValue;
		public int delayDropByTicks;

		public int ticksToNextDrop;

		void IExposable.ExposeData()
		{
			Scribe_Values.Look(ref lifetime, nameof(lifetime));
			Scribe_Values.Look(ref ticksBetweenDrops, nameof(ticksBetweenDrops));
			Scribe_Values.Look(ref delayDropByTicks, nameof(delayDropByTicks));
			Scribe_Values.Look(ref ticksToNextDrop, nameof(ticksToNextDrop));
		}
	}
}