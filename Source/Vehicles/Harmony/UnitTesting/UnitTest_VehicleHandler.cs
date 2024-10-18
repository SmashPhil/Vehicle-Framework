using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using SmashTools.Debugging;
using Verse;
using static SmashTools.Debug;

namespace Vehicles.Testing
{
	internal class UnitTest_VehicleHandler : UnitTest_MapTest
	{
		public override string Name => "VehicleHandler";

		public override bool ShouldTest(VehicleDef vehicleDef)
		{
			return vehicleDef.properties.roles.NotNullAndAny(role => role.Slots > 0);
		}

		protected override UTResult TestVehicle(VehiclePawn vehicle, Map map, IntVec3 root)
		{
			Assert(!vehicle.handlers.NullOrEmpty(), "Testing with vehicle which has no roles");

			GenSpawn.Spawn(vehicle, root, map);
			CameraJumper.TryJump(vehicle.Position, map, mode: CameraJumper.MovementMode.Cut);

			UTResult result;

			// Colonists can board
			int total = vehicle.SeatsAvailable;
			for (int i = 0; i < total; i++)
			{
				Pawn colonist = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
				Assert(colonist != null && colonist.Faction == Faction.OfPlayer, "Unable to generate colonist");
				result.Add("ColonistRole", vehicle.TryAddPawn(colonist));
			}
			// Colonist cannot board full vehicle
			Pawn failColonist = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
			Assert(failColonist != null && failColonist.Faction == Faction.OfPlayer, "Unable to generate colonist");
			result.Add("ColonistRole OutOfBounds", !vehicle.TryAddPawn(failColonist));
			failColonist.Destroy();

			vehicle.DestroyPawns();

			Pawn animal = PawnGenerator.GeneratePawn(PawnKindDefOf.Alphabeaver, Faction.OfPlayer);
			Assert(animal != null && animal.Faction == Faction.OfPlayer, "Unable to generate pet");
			result.Add("PetRole", vehicle.TryAddPawn(animal));

			vehicle.DestroyPawns();

			if (ModsConfig.BiotechActive)
			{
				Pawn mechanoid = PawnGenerator.GeneratePawn(PawnKindDefOf.Mech_Warqueen, Faction.OfPlayer);
				Assert(mechanoid != null && mechanoid.Faction == Faction.OfPlayer, "Unable to generate mech");
				result.Add("MechRole", vehicle.TryAddPawn(mechanoid));
			}

			return result;
		}
	}
}
