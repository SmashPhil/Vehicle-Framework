using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
	/// <summary>
	/// Tween vehicle while still maintaining the non-pawn TrueCenter pos rather than offset position
	/// </summary>
	public class VehicleTweener
	{
		private const float SpringTightness = 0.09f;

		private VehiclePawn vehicle;

		private Vector3 tweenedPos = Vector3.zero;
		private Vector3 lastTickSpringPos;
		private int lastDrawFrame = -1;

		public VehicleTweener(VehiclePawn vehicle)
		{
			this.vehicle = vehicle;
		}

		public Vector3 TweenedPos => tweenedPos;

		public Vector3 LastTickTweenedVelocity => TweenedPos - lastTickSpringPos;

		public void PreDrawPosCalculation()
		{
			if (lastDrawFrame == RealTime.frameCount)
			{
				return;
			}
			if (lastDrawFrame < RealTime.frameCount - 1)
			{
				ResetTweenedPosToRoot();
			}
			else
			{
				lastTickSpringPos = tweenedPos;
				float tickRateMultiplier = Find.TickManager.TickRateMultiplier;
				if (tickRateMultiplier < 5f)
				{
					Vector3 a = TweenedPosRoot() - tweenedPos;
					float num = SpringTightness * (RealTime.deltaTime * 60f * tickRateMultiplier);
					if (RealTime.deltaTime > 0.05f)
					{
						num = Mathf.Min(num, 1f);
					}
					tweenedPos += a * num;
				}
				else
				{
					tweenedPos = TweenedPosRoot();
				}
			}
			lastDrawFrame = RealTime.frameCount;
		}

		public void ResetTweenedPosToRoot()
		{
			tweenedPos = TweenedPosRoot();
			lastTickSpringPos = tweenedPos;
		}

		private Vector3 TweenedPosRoot()
		{
			if (!vehicle.Spawned || vehicle.vehiclePather == null)
			{
				return vehicle.TrueCenter();
			}
			float num = MovedPercent();
			return vehicle.TrueCenter(vehicle.vehiclePather.nextCell) * num + vehicle.TrueCenter() * (1f - num);
		}

		public float MovedPercent()
		{
			if (vehicle.vehiclePather is null)
			{
				return 0f;
			}
			if (!vehicle.vehiclePather.Moving)
			{
				return 0f;
			}
			if (vehicle.vehiclePather.BuildingBlockingNextPathCell() != null)
			{
				return 0f;
			}
			if (vehicle.vehiclePather.NextCellDoorToWaitForOrManuallyOpen() != null)
			{
				return 0f;
			}
			return 1f - vehicle.vehiclePather.nextCellCostLeft / vehicle.vehiclePather.nextCellCostTotal;
		}
	}
}
