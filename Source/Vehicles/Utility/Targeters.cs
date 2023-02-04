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

		private static readonly List<BaseTargeter> activeTargeters = new List<BaseTargeter>();
		private static readonly List<BaseWorldTargeter> activeWorldTargeters = new List<BaseWorldTargeter>();

		public static BaseTargeter CurrentTargeter { get; private set; }
		public static BaseWorldTargeter CurrentWorldTargeter { get; private set; }

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
		}

		public static void StartTargeter(BaseTargeter baseTargeter)
		{
			activeTargeters.Add(baseTargeter);
		}

		public static void StartWorldTargeter(BaseWorldTargeter baseTargeter)
		{
			activeWorldTargeters.Add(baseTargeter);
		}

		/* ------ Map Targeters ------ */
		internal static void StopAllTargeters()
		{
			foreach (BaseTargeter targeter in targeters)
			{
				if (targeter.IsTargeting)
				{
					targeter.StopTargeting();
				}
			}
		}

		internal static void OnGUITargeters()
		{
			foreach (BaseTargeter targeter in targeters)
			{
				if (targeter.IsTargeting)
				{
					targeter.TargeterOnGUI();
				}
			}
		}

		internal static void UpdateTargeters()
		{
			foreach (BaseTargeter targeter in targeters)
			{
				if (targeter.IsTargeting)
				{
					targeter.TargeterUpdate();
				}
			}
		}

		internal static void ProcessTargeterInputEvents()
		{
			foreach (BaseTargeter targeter in targeters)
			{
				if (targeter.IsTargeting)
				{
					targeter.ProcessInputEvents();
				}
			}
		}
		/* --------------------------- */

		/* ----- World Targeters ----- */
		internal static void StopAllWorldTargeters()
		{
			foreach (BaseWorldTargeter targeter in worldTargeters)
			{
				if (targeter.IsTargeting)
				{
					targeter.StopTargeting();
				}
			}
		}

		internal static void OnGUIWorldTargeters()
		{
			foreach (BaseWorldTargeter targeter in worldTargeters)
			{
				if (targeter.IsTargeting)
				{
					targeter.TargeterOnGUI();
				}
			}
		}

		internal static void UpdateWorldTargeters()
		{
			foreach (BaseWorldTargeter targeter in worldTargeters)
			{
				if (targeter.IsTargeting)
				{
					targeter.TargeterUpdate();
				}
			}
		}

		internal static void ProcessWorldTargeterInputEvents()
		{
			foreach (BaseWorldTargeter targeter in worldTargeters)
			{
				if (targeter.IsTargeting)
				{
					targeter.ProcessInputEvents();
				}
			}
		}
		/* --------------------------- */
	}
}
