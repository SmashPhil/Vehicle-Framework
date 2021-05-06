using System;
using System.Reflection;
using HarmonyLib;
using Verse;
using AchievementsExpanded;

namespace Vehicles.Achievements
{
	public class VehicleBuilderTracker : Tracker<VehiclePawn>
	{
		public VehicleBuildDef def;
		public int count;

		private int triggeredCount;
		public override string Key => "VehicleBuilderTracker";
		protected override string[] DebugText => new string[] { $"Def: {def?.defName ?? "[NoDef]"}", $"Count: {count}", $"Current: {triggeredCount}" };

		public VehicleBuilderTracker()
		{
		}

		public VehicleBuilderTracker(VehicleBuilderTracker reference) : base(reference)
		{
			def = reference.def;
			count = reference.count;
			triggeredCount = 0;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Defs.Look(ref def, "def");
			Scribe_Values.Look(ref count, "count");

			Scribe_Values.Look(ref triggeredCount, "triggeredCount");
		}

		public override bool Trigger(VehiclePawn vehicle)
		{
			base.Trigger(vehicle);
			if (vehicle.VehicleDef.buildDef == def)
			{
				triggeredCount++;
				return triggeredCount >= count;
			}
			return false;
		}
	}
}
