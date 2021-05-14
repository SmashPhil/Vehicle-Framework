using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Verse;

namespace Vehicles
{
	[StaticConstructorOnStartup]
	public static class Targeters
	{
		private static readonly List<BaseTargeter> targeters = new List<BaseTargeter>();
		private static readonly List<BaseWorldTargeter> worldTargeters = new List<BaseWorldTargeter>();

		static Targeters()
		{
			foreach (Type type in typeof(BaseTargeter).InstantiableDescendantsAndSelf())
			{
				BaseTargeter targeter = (BaseTargeter)Activator.CreateInstance(type, null);
				targeters.Add(targeter);
				targeter.PostInit();
			}
			foreach (Type type in typeof(BaseWorldTargeter).InstantiableDescendantsAndSelf())
			{
				BaseWorldTargeter targeter = (BaseWorldTargeter)Activator.CreateInstance(type, null);
				worldTargeters.Add(targeter);
				targeter.PostInit();
			}
			Debug.Message($"{targeters.Count} Targeters initialized.");
			Debug.Message($"{worldTargeters.Count} WorldTargeters initialized.");
		}

		public static void OnGUITargeters()
		{
			foreach (BaseTargeter targeter in targeters)
			{
				if (targeter.IsTargeting)
				{
					targeter.TargeterOnGUI();
				}
			}
		}

		public static void UpdateTargeters()
		{
			foreach (BaseTargeter targeter in targeters)
			{
				if (targeter.IsTargeting)
				{
					targeter.TargeterUpdate();
				}
			}
		}

		public static void ProcessTargeterInputEvents()
		{
			foreach (BaseTargeter targeter in targeters)
			{
				if (targeter.IsTargeting)
				{
					targeter.ProcessInputEvents();
				}
			}
		}

		public static void OnGUIWorldTargeters()
		{
			foreach (BaseWorldTargeter targeter in worldTargeters)
			{
				if (targeter.IsTargeting)
				{
					targeter.TargeterOnGUI();
				}
			}
		}

		public static void UpdateWorldTargeters()
		{
			foreach (BaseWorldTargeter targeter in worldTargeters)
			{
				if (targeter.IsTargeting)
				{
					targeter.TargeterUpdate();
				}
			}
		}

		public static void ProcessWorldTargeterInputEvents()
		{
			foreach (BaseWorldTargeter targeter in worldTargeters)
			{
				if (targeter.IsTargeting)
				{
					targeter.ProcessInputEvents();
				}
			}
		}
	}
}
