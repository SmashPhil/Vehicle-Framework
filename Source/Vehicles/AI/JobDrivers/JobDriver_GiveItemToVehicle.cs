using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;
using HarmonyLib;
using SmashTools;

namespace Vehicles
{
	public class JobDriver_GiveItemToVehicle : JobDriver_LoadVehicle
	{
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return base.TryMakePreToilReservations(errorOnFailed) && pawn.Reserve(Vehicle, job, errorOnFailed: errorOnFailed);
		}
	}
}
