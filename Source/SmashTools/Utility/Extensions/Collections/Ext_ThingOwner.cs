using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace SmashTools
{
	public static class Ext_ThingOwner
	{
		public static void Swap<T>(this ThingOwner<T> thingOwner1, ThingOwner<T> thingOwner2, T thing1, T thing2) where T : Thing
		{
			if (thingOwner1.Contains(thing2) && thingOwner2.Contains(thing2))
			{
				//Swap things for correct replacement
				T tmpThing = thing1;
				thing1 = thing2;
				thing2 = tmpThing;
			}

			thingOwner1.Remove(thing1);
			thingOwner2.Remove(thing2);

			thingOwner1.TryAdd(thing2);
			thingOwner2.TryAdd(thing1);
		}
	}
}
