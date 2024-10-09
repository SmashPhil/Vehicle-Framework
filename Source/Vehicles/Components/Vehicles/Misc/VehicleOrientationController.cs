using RimWorld.Planet;
using RimWorld;
using SmashTools;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Vehicles
{
	public class VehicleOrientationController : BaseTargeter
	{
		private const float DragThreshold = 1f;
		private const float HoldTimeThreshold = 0.5f;

		private IntVec3 cell;
		private IntVec3 clickCell;
		private Vector3 clickPos;

		private float timeHeldDown = 0;

		public Rot8 Rotation
		{
			get
			{
				IntVec3 mouseCell = UI.MouseCell();
				float angle = cell.AngleToCell(mouseCell);
				Rot8 rot = Rot8.FromAngle(angle); 
				return rot;
			}
		}

		private bool IsDragging { get; set; }

		public override bool IsTargeting => vehicle != null && vehicle.Spawned;

		public static VehicleOrientationController Instance { get; private set; }

		private void Init(VehiclePawn vehicle, IntVec3 cell, IntVec3 clickCell)
		{
			this.vehicle = vehicle;
			this.cell = cell;
			this.clickCell = clickCell;
			this.clickPos = UI.MouseMapPosition();
			OnStart();
		}

		public static void StartOrienting(VehiclePawn dragging, IntVec3 cell)
		{
			StartOrienting(dragging, cell, cell);
		}

		public static void StartOrienting(VehiclePawn dragging, IntVec3 cell, IntVec3 clickCell)
		{
			Instance.StopTargeting();
			Instance.Init(dragging, cell, clickCell);
		}

		public void ConfirmOrientation()
		{
			Job job = new Job(JobDefOf.Goto, cell);

			bool isOnEdge = CellRect.WholeMap(vehicle.Map).IsOnEdge(clickCell, 3);

			bool exitCell = vehicle.Map.exitMapGrid.IsExitCell(clickCell);

			bool vehicleCellsOverlapExit = vehicle.InhabitedCellsProjected(clickCell, Rot8.Invalid).NotNullAndAny(cell => cell.InBounds(vehicle.Map) && 
				vehicle.Map.exitMapGrid.IsExitCell(cell));

			if (exitCell || vehicleCellsOverlapExit)
			{
				job.exitMapOnArrival = true;
			}
			else if (!vehicle.Map.IsPlayerHome && !vehicle.Map.exitMapGrid.MapUsesExitGrid && isOnEdge &&
				vehicle.Map.Parent.GetComponent<FormCaravanComp>() is FormCaravanComp formCaravanComp &&
				MessagesRepeatAvoider.MessageShowAllowed($"MessagePlayerTriedToLeaveMapViaExitGrid-{vehicle.Map.uniqueID}", 60f))
			{
				if (formCaravanComp.CanFormOrReformCaravanNow)
				{
					Messages.Message("MessagePlayerTriedToLeaveMapViaExitGrid_CanReform".Translate(), vehicle.Map.Parent, MessageTypeDefOf.RejectInput, false);
				}
				else
				{
					Messages.Message("MessagePlayerTriedToLeaveMapViaExitGrid_CantReform".Translate(), vehicle.Map.Parent, MessageTypeDefOf.RejectInput, false);
				}
			}
			if (vehicle.jobs.TryTakeOrderedJob(job, JobTag.Misc))
			{
				Rot8 endRot = IsDragging ? Rotation : Rot8.Invalid;
				vehicle.vehiclePather.SetEndRotation(endRot);

				FleckMaker.Static(cell, vehicle.Map, FleckDefOf.FeedbackGoto, 1f);
			}
			StopTargeting();
		}

		public override void ProcessInputEvents()
		{
			if (!Input.GetMouseButton(1))
			{
				ConfirmOrientation();
			}
		}

		public override void TargeterUpdate()
		{
			timeHeldDown += Time.deltaTime;
			float dragDistance = Vector3.Distance(clickPos, UI.MouseMapPosition());
			if (timeHeldDown >= HoldTimeThreshold || dragDistance >= DragThreshold || dragDistance <= -DragThreshold)
			{
				IsDragging = true;
			}
			
			if (IsDragging)
			{
				Vector3 drawPos = cell.ToVector3();
				drawPos.y = AltitudeLayer.MoteOverheadLow.AltitudeFor();
				VehicleGhostUtility.DrawGhostVehicleDef(cell, Rotation, vehicle.VehicleDef, VehicleGhostUtility.whiteGhostColor, AltitudeLayer.MoteOverhead, vehicle);
			}
		}

		public override void StopTargeting()
		{
			vehicle = null;
			cell = IntVec3.Invalid;
			clickCell = IntVec3.Invalid;
			clickPos = Vector3.zero;
			IsDragging = false;
			timeHeldDown = 0;
		}

		public override void TargeterOnGUI()
		{
		}

		public override void PostInit()
		{
			Instance = this;
		}
	}
}
