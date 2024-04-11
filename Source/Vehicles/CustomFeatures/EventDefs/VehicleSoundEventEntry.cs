using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using RimWorld;
using Verse;

namespace Vehicles
{
	public class VehicleSoundEventEntry<T>
	{
		public T key;
		public SoundDef value;
		public string removalKey;
	}

	public class VehicleSustainerEventEntry<T>
	{
		public T start;
		public T stop;
		public SoundDef value;
		public string removalKey;
	}
}
