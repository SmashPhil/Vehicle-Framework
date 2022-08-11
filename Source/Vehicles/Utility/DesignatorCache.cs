using System;
using System.Collections.Generic;
using Verse;

namespace Vehicles
{
	public static class DesignatorCache
	{
		private static Dictionary<Type, Designator> designators = new Dictionary<Type, Designator>();

		public static T Get<T>() where T : Designator
		{
			if (!designators.TryGetValue(typeof(T), out Designator designator))
			{
				designator = (T)Activator.CreateInstance(typeof(T));
				designators[typeof(T)] = designator;
			}
			return (T)designator;
		}

		public static Designator Get(Type type)
		{
			if (!type.IsSubclassOf(typeof(Designator)))
			{
				Log.Error($"Attempting to retrieve {type} from DesignatorCache. Type must be of Designator type.");
				return null;
			}
			if (!designators.TryGetValue(type, out Designator designator))
			{
				designator = (Designator)Activator.CreateInstance(type);
				designators[type] = designator;
			}
			return designator;
		}
	}
}
