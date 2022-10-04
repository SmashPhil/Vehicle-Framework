using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;

namespace Vehicles
{
	/// <summary>
	/// Tween vehicle while still maintaining TrueCenter pos rather than root position
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
			if (!vehicle.Spawned)
			{
				return vehicle.Position.ToVector3Shifted();
			}
			float num = MovedPercent();
			return vehicle.pather.nextCell.ToVector3Shifted() * num + vehicle.Position.ToVector3Shifted() * (1f - num);
		}

		private float MovedPercent()
		{
			if (!vehicle.pather.Moving)
			{
				return 0f;
			}
			if (vehicle.stances.FullBodyBusy)
			{
				return 0f;
			}
			if (vehicle.pather.BuildingBlockingNextPathCell() != null)
			{
				return 0f;
			}
			if (vehicle.pather.NextCellDoorToWaitForOrManuallyOpen() != null)
			{
				return 0f;
			}
			if (vehicle.pather.WillCollideWithPawnOnNextPathCell())
			{
				return 0f;
			}
			return 1f - vehicle.pather.nextCellCostLeft / vehicle.pather.nextCellCostTotal;
		}
	}
}
