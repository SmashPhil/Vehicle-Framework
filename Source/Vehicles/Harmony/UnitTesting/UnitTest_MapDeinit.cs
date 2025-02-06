using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using SmashTools.Debugging;
using SmashTools.Performance;
using Verse;
using Verse.Profile;

namespace Vehicles.Testing
{
	internal class UnitTest_MapDeinit : UnitTest
	{
		public override TestType ExecuteOn => TestType.GameLoaded;

		public override ExecutionPriority Priority => ExecutionPriority.Last;

		public override string Name => "Map Deinit";

		public override IEnumerable<UTResult> Execute()
		{
			Assert.IsNotNull(Current.Game);
			Assert.IsNotNull(Find.CurrentMap);

			VehicleMapping mapping = Find.CurrentMap.GetCachedMapComponent<VehicleMapping>();
			Assert.IsNotNull(mapping);

			// Testing that all threads can be terminated and the caller will wait until they finish
			ThreadManager.ReleaseThreadsAndClearCache();
			yield return UTResult.For("Threads Terminated", ThreadManager.AllThreadsTerminated);
		}
	}
}
