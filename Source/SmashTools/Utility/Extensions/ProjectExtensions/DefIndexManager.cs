using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace SmashTools
{
	[StaticConstructorOnStartup]
	internal static class DefIndexManager
	{
		static DefIndexManager()
		{
			foreach (Type type in typeof(Def).AllSubclassesNonAbstract())
			{
				if (type.HasInterface(typeof(IDefIndex<>)))
				{
					GenGeneric.InvokeStaticMethodOnGenericType(typeof(Indexer<>), type, "Init");
				}
			}
		}

		public static class Indexer<T> where T : Def
		{
			private static int nextIndex = 0;
			private static bool initialized = false;

			public static void Init()
			{
				if (initialized)
				{
					Log.Error($"Attempting to reinit DefIndexManager.");
					return;
				}

				foreach (T def in DefDatabase<T>.AllDefsListForReading)
				{
					if (def is IDefIndex<T> indexer)
					{
						indexer.DefIndex = nextIndex++;
					}
				}

				initialized = true;
			}
		}
	}
}
