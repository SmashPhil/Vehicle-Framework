using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
    internal struct VehicleStatusEffecters
    {
		public VehicleStatusEffecters(VehiclePawn vehicle)
		{
			this.vehicle = vehicle;
			pairs = new List<LiveEffecter>();
		}

		public void EffectersTick()
		{
			List<Hediff> hediffs = vehicle.health.hediffSet.hediffs;
			for (int i = 0; i < hediffs.Count; i++)
			{
				HediffComp_Effecter hediffComp_Effecter = hediffs[i].TryGetComp<HediffComp_Effecter>();
				if (hediffComp_Effecter != null)
				{
					EffecterDef effecterDef = hediffComp_Effecter.CurrentStateEffecter();
					if (effecterDef != null)
					{
						AddOrMaintain(effecterDef);
					}
				}
			}
			if (vehicle.mindState.mentalStateHandler.CurState != null)
			{
				EffecterDef effecterDef2 = vehicle.mindState.mentalStateHandler.CurState.CurrentStateEffecter();
				if (effecterDef2 != null)
				{
					AddOrMaintain(effecterDef2);
				}
			}
			for (int j = pairs.Count - 1; j >= 0; j--)
			{
				if (pairs[j].Expired)
				{
					pairs[j].Cleanup();
					pairs.RemoveAt(j);
				}
				else
				{
					pairs[j].Tick(vehicle);
				}
			}
		}

		private void AddOrMaintain(EffecterDef def)
		{
			for (int i = 0; i < pairs.Count; i++)
			{
				if (pairs[i].def == def)
				{
					pairs[i].Maintain();
					return;
				}
			}
			LiveEffecter liveEffecter = FullPool<LiveEffecter>.Get();
			liveEffecter.def = def;
			liveEffecter.Maintain();
			pairs.Add(liveEffecter);
		}

		public Pawn vehicle;

		private List<LiveEffecter> pairs;

		private class LiveEffecter : IFullPoolable
		{
			public bool Expired
			{
				get
				{
					return Find.TickManager.TicksGame > lastMaintainTick;
				}
			}

			public void Cleanup()
			{
				if (effecter != null)
				{
					effecter.Cleanup();
				}
				FullPool<LiveEffecter>.Return(this);
			}

			public void Reset()
			{
				def = null;
				effecter = null;
				lastMaintainTick = -1;
			}

			public void Maintain()
			{
				lastMaintainTick = Find.TickManager.TicksGame;
			}

			public void Tick(Pawn vehicle)
			{
				if (effecter == null)
				{
					effecter = def.Spawn();
				}
				effecter.EffectTick(vehicle, null);
			}

			public EffecterDef def;
			public Effecter effecter;
			public int lastMaintainTick;
		}
	
    }
}
