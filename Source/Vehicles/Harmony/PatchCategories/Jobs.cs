using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;
using RimWorld.Planet;
using OpCodes = System.Reflection.Emit.OpCodes;
using UnityEngine;
using Vehicles.Lords;

namespace Vehicles
{
	internal class Jobs : IPatchCategory
	{
		public void PatchMethods()
		{
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(JobUtility), nameof(JobUtility.TryStartErrorRecoverJob)),
				prefix: new HarmonyMethod(typeof(Jobs),
				nameof(VehicleErrorRecoverJob)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(JobGiver_Wander), "TryGiveJob"),
				prefix: new HarmonyMethod(typeof(Jobs),
				nameof(VehiclesDontWander)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.CheckForJobOverride)), prefix: null, postfix: null,
				transpiler: new HarmonyMethod(typeof(Jobs),
				nameof(NoOverrideDamageTakenTranspiler)));
			VehicleHarmony.Patch(original: AccessTools.Property(typeof(JobDriver_PrepareCaravan_GatherItems), "Transferables").GetGetMethod(nonPublic: true),
				prefix: new HarmonyMethod(typeof(Jobs),
				nameof(TransferablesVehicle)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(ThingRequest), nameof(ThingRequest.Accepts)), prefix: null,
				postfix: new HarmonyMethod(typeof(Jobs),
				nameof(AcceptsVehicleRefuelable)));
		}

		//REDO
		/// <summary>
		/// Intercept Error Recover handler of no job, and assign idling for vehicle
		/// </summary>
		/// <param name="pawn"></param>
		/// <param name="message"></param>
		/// <param name="exception"></param>
		/// <param name="concreteDriver"></param>
		/// <returns></returns>
		public static bool VehicleErrorRecoverJob(Pawn pawn, string message, Exception exception = null, JobDriver concreteDriver = null)
		{
			if(pawn is VehiclePawn)
			{
				if (pawn.jobs != null)
				{
					if (pawn.jobs.curJob != null)
					{
						pawn.jobs.EndCurrentJob(JobCondition.Errored, false);
					}
					try
					{
						if (pawn.jobs.jobQueue.Count > 0)
						{
							Job job = pawn.jobs.jobQueue.Dequeue().job;
							pawn.jobs.StartJob(job, JobCondition.Succeeded);
						}
						else
						{
							pawn.jobs.StartJob(new Job(JobDefOf_Vehicles.IdleVehicle, 150, false), JobCondition.None, null, false, true, null, null, false);
						}  
					}
					catch
					{
						Log.Error("An error occurred when trying to recover the job for ship " + pawn.def + ". Please contact Mod Author.");
					}
				}
				return false;
			}
			return true;
		}

		public static bool VehiclesDontWander(Pawn pawn, ref Job __result)
		{
			if(pawn is VehiclePawn)
			{
				__result = new Job(JobDefOf_Vehicles.IdleVehicle);
				return false;
			}
			return true;
		}

		public static IEnumerable<CodeInstruction> NoOverrideDamageTakenTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
		{
			List<CodeInstruction> instructionList = instructions.ToList();

			for (int i = 0; i < instructionList.Count; i++)
			{
				CodeInstruction instruction = instructionList[i];

				if(instruction.opcode == OpCodes.Stloc_1)
				{
					yield return instruction; //STLOC.1
					instruction = instructionList[++i];
					Label label = ilg.DefineLabel();
					Label retlabel = ilg.DefineLabel();

					yield return new CodeInstruction(opcode: OpCodes.Ldloc_0);
					yield return new CodeInstruction(opcode: OpCodes.Brfalse, retlabel);

					yield return new CodeInstruction(opcode: OpCodes.Ldloca_S, operand: 1);
					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Property(typeof(ThinkResult), nameof(ThinkResult.IsValid)).GetGetMethod());
					yield return new CodeInstruction(opcode: OpCodes.Brtrue, label);

					yield return new CodeInstruction(opcode: OpCodes.Ret) { labels = new List<Label> { retlabel } };

					instruction.labels.Add(label);
				}

				yield return instruction;
			}
		}

		public static bool TransferablesVehicle(JobDriver_PrepareCaravan_GatherItems __instance, ref List<TransferableOneWay> __result)
		{
			if (__instance.job.lord.LordJob is LordJob_FormAndSendVehicles)
			{
				__result = ((LordJob_FormAndSendVehicles)__instance.job.lord.LordJob).transferables;
				return false;
			}
			return true;
		}

		public static void AcceptsVehicleRefuelable(Thing t, ref bool __result, ThingRequest __instance)
		{
			if(t is VehiclePawn vehicle && __instance.group == ThingRequestGroup.Refuelable)
			{
				__result = vehicle.CompFueledTravel != null;
			}
		}
	}
}
