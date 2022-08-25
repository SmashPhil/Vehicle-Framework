using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace Vehicles
{
	[Obsolete]
	public abstract class WorldComponentTemp
	{
		public WorldComponentTemp(World world)
		{
		}

		public abstract void WorldComponentUpdate();

		public abstract void WorldComponentTick();

		public abstract void FinalizeInit();

		public virtual void ExposeData()
		{
		}
	}
}
