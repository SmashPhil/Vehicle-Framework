using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Vehicles
{
	public class FloatMenuTargeter : FloatMenu
	{
		public FloatMenuTargeter(List<FloatMenuOption> options) : base(options)
		{
		}

		public FloatMenuTargeter(List<FloatMenuOption> options, string title, bool needSelection = false) : base(options)
		{
		}

		public override void PreOptionChosen(FloatMenuOption opt)
		{
			base.PreOptionChosen(opt);
			LaunchTargeter.Instance.StopTargeting();
		}
	}
}
