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

		public static void PushTargeter(BaseTargeter targeter)
		{
			if (CurrentTargeter == targeter) return;

			CurrentTargeter?.StopTargeting();
			CurrentTargeter = targeter;
		}

		public static void PushTargeter(BaseWorldTargeter targeter)
		{
			if (CurrentWorldTargeter == targeter) return;

			CurrentWorldTargeter?.StopTargeting();
			CurrentWorldTargeter = targeter;
		}

		public static void StopTargeter(BaseTargeter targeter)
		{
			if (CurrentTargeter != targeter) return;

			CurrentTargeter.StopTargeting();
			CurrentTargeter = null;
		}

		public static void StopTargeter(BaseWorldTargeter targeter)
		{
			if (CurrentWorldTargeter != targeter) return;

			CurrentWorldTargeter.StopTargeting();
			CurrentWorldTargeter = null;
		}

		/* ------ Map Targeters ------ */
		internal static void OnGUITargeter()
		{
			if (CurrentTargeter == null) return;

			if (!CurrentTargeter.IsTargeting)
			{
				StopTargeter(CurrentTargeter);
				return;
			}
			CurrentTargeter.TargeterOnGUI();
		}

		internal static void UpdateTargeter()
		{
			if (CurrentTargeter == null) return;

			if (!CurrentTargeter.IsTargeting)
			{
				StopTargeter(CurrentTargeter);
				return;
			}
			CurrentTargeter.TargeterUpdate();
		}

		internal static void ProcessTargeterInputEvent()
		{
			if (CurrentTargeter == null) return;

			if (!CurrentTargeter.IsTargeting)
			{
				StopTargeter(CurrentTargeter);
				return;
			}
			CurrentTargeter.ProcessInputEvents();
		}
		/* --------------------------- */

		/* ----- World Targeters ----- */
		
		internal static void OnGUIWorldTargeter()
		{
			if (CurrentWorldTargeter == null) return;

			if (!CurrentWorldTargeter.IsTargeting)
			{
				StopTargeter(CurrentWorldTargeter);
				return;
			}
			CurrentWorldTargeter.TargeterOnGUI();
		}

		internal static void UpdateWorldTargeter()
		{
			if (CurrentWorldTargeter == null) return;

			if (!CurrentWorldTargeter.IsTargeting)
			{
				StopTargeter(CurrentWorldTargeter);
				return;
			}
			CurrentWorldTargeter.TargeterUpdate();
		}

		internal static void ProcessWorldTargeterInputEvent()
		{
			if (CurrentWorldTargeter == null) return;

			if (!CurrentWorldTargeter.IsTargeting)
			{
				StopTargeter(CurrentWorldTargeter);
				return;
			}
			CurrentWorldTargeter.ProcessInputEvents();
		}
		/* --------------------------- */
	}
}
