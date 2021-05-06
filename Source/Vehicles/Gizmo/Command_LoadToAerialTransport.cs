using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
	[StaticConstructorOnStartup]
	public class Command_LoadToAerialTransport : Command
	{
		public VehiclePawn transport;

		private List<VehiclePawn> transports;

		public override void ProcessInput(Event ev)
		{
			base.ProcessInput(ev);
			//if (transports is null)
			//{
			//    transports = new List<VehiclePawn>();
			//}
			//if (!transports.Contains(transport))
			//{
			//    transports.Add(transport);
			//}
			//CompVehicleLauncher launchable = transport.GetCachedComp<CompAerialTransport>().Launchable;
			//if (launchable != null)
			//{
			//    Map map = transport.Map;
			//    //map.floodFiller.FloodFill(fuelingPortSource.Position, (IntVec3 x) => FuelingPortUtility.AnyFuelingPortGiverAt(x, map), delegate (IntVec3 x)
			//    //{
			//    //    tmpFuelingPortGivers.Add(FuelingPortUtility.FuelingPortGiverAt(x, map));
			//    //}, int.MaxValue, false, null);
			//}
			//for (int j = 0; j < transports.Count; j++)
			//{
			//    if (transports[j] != transport && !transport.Map.reachability.CanReach(transport.Position, transports[j], PathEndMode.Touch, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)))
			//    {
			//        Messages.Message("MessageTransporterUnreachable".Translate(), transports[j], MessageTypeDefOf.RejectInput, false);
			//        return;
			//    }
			//}
			//Dialog_LoadAerialTransports dialog_LoadTransporters = new Dialog_LoadAerialTransports(transport.Map, transports);
			//Find.WindowStack.Add(dialog_LoadTransporters);
		}

		public override bool InheritInteractionsFrom(Gizmo other)
		{
			Command_LoadToAerialTransport command_LoadToTransporter = (Command_LoadToAerialTransport)other;
			if (command_LoadToTransporter.transport.def != transport.def)
			{
				return false;
			}
			if (transports is null)
			{
				transports = new List<VehiclePawn>();
			}
			transports.Add(command_LoadToTransporter.transport);
			return false;
		}
	}
}
