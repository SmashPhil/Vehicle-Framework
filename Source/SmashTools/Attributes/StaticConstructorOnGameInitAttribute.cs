using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;

namespace SmashTools
{
	[AttributeUsage(AttributeTargets.Class)]
	[Obsolete("Do not use.", true)]
	public class StaticConstructorOnGameInitAttribute : Attribute
	{
		public static void RunGameInitStaticConstructors()
		{
			foreach (Type type in GenTypes.AllTypesWithAttribute<StaticConstructorOnGameInitAttribute>())
			{
				try
				{
					RuntimeHelpers.RunClassConstructor(type.TypeHandle);
				}
				catch (Exception ex)
				{
					SmashLog.Error($"Exception thrown running constructor of type <type>{type}</type>. Ex=\"{ex}\"");
				}
			}
		}
	}
}
