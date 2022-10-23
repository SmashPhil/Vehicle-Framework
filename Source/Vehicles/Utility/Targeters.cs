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
			throw new NotImplementedException();

			//Stop existing targeter
			//Assign to active targeter
			//Update method hooks to update only active targeters
		}

		public static void StartWorldTargeter(BaseWorldTargeter baseTargeter)
		{
			throw new NotImplementedException();

			//Stop existing targeter
			//Assign to active targeter
			//Update method hooks to update only active targeters
		}

		/* ------ Map Targeters ------ */
		public static void StopAllTargeters()
		{
			foreach (BaseTargeter targeter in targeters)
			{
				if (targeter.IsTargeting)
				{
					targeter.StopTargeting();
				}
			}
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
		/* --------------------------- */

		/* ----- World Targeters ----- */
		public static void StopAllWorldTargeters()
		{
			foreach (BaseWorldTargeter targeter in worldTargeters)
			{
				if (targeter.IsTargeting)
				{
					targeter.StopTargeting();
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
		/* --------------------------- */
	}
}
