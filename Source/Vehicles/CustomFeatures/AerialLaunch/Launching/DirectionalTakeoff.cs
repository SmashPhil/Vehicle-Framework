using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;
using SmashTools;

namespace Vehicles
{
	public class DirectionalTakeoff : LaunchProtocol
	{
		[GraphEditable(Category = AnimationEditorTags.Takeoff)]
		public DirectionalProtocolProperties launchProperties;
		[GraphEditable(Category = AnimationEditorTags.Landing)]
		public DirectionalProtocolProperties landingProperties;

		public DirectionalTakeoff()
		{
		}

		public DirectionalTakeoff(DirectionalTakeoff reference, VehiclePawn vehicle) : base(reference, vehicle)
		{
			launchProperties = reference.launchProperties;
			landingProperties = reference.landingProperties;
		}

		protected override int TotalTicks_Takeoff => LaunchProperties.maxTicks;

		protected override int TotalTicks_Landing => LandingProperties.maxTicks;

		public override LaunchProtocolProperties CurAnimationProperties
		{
			get
			{
				return launchType switch
				{
					LaunchType.Landing => LandingProperties,
					LaunchType.Takeoff => LaunchProperties,
					_ => throw new NotImplementedException("CurAnimationProperties"),
				};
			}
		}

		public override LaunchProtocolProperties LandingProperties => vehicle.Rotation.IsHorizontal ? landingProperties.horizontal : landingProperties.vertical;
		public override LaunchProtocolProperties LaunchProperties => vehicle.Rotation.IsHorizontal ? launchProperties.horizontal : launchProperties.vertical;

		public override bool LaunchRestricted => vehicle.Spawned && LaunchProperties.restriction != null && !LaunchProperties.restriction.CanStartProtocol(vehicle, vehicle.Map, vehicle.Position, vehicle.Rotation);

		public override bool LandingRestricted(Map map, IntVec3 position, Rot4 rotation)
		{
			return LandingProperties.restriction != null && !LandingProperties.restriction.CanStartProtocol(vehicle, map, position, rotation);
		}

		public override bool FinishedAnimation(VehicleSkyfaller skyfaller)
		{
			return TicksPassed >= CurAnimationProperties.maxTicks;
		}

		protected override (Vector3 drawPos, float rotation) AnimateLanding(Vector3 drawPos, float rotation)
		{
			if (!LandingProperties.rotationCurve.NullOrEmpty())
			{
				//Flip rotation if either west or south
				int sign = Ext_Math.Sign(LandingProperties.flipRotation != vehicle.Rotation);
				rotation += LandingProperties.rotationCurve.Evaluate(TimeInAnimation) * sign;
			}
			if (!LandingProperties.xPositionCurve.NullOrEmpty())
			{
				int sign = Ext_Math.Sign(LandingProperties.flipHorizontal != vehicle.Rotation);
				drawPos.x += LandingProperties.xPositionCurve.Evaluate(TimeInAnimation) * sign;
			}
			if (!LandingProperties.zPositionCurve.NullOrEmpty())
			{
				int sign = Ext_Math.Sign(LandingProperties.flipVertical != vehicle.Rotation);
				drawPos.z += LandingProperties.zPositionCurve.Evaluate(TimeInAnimation) * sign;
			}
			if (!LandingProperties.offsetCurve.NullOrEmpty())
			{
				Vector2 offset = LandingProperties.offsetCurve.EvaluateT(TimeInAnimation);
				int signX = Ext_Math.Sign(LandingProperties.flipHorizontal != vehicle.Rotation);
				int signZ = Ext_Math.Sign(LandingProperties.flipVertical != vehicle.Rotation);
				drawPos += new Vector3(offset.x * signX, 0, offset.y * signZ);
			}
			return base.AnimateLanding(drawPos, rotation);
		}

		protected override (Vector3 drawPos, float rotation) AnimateTakeoff(Vector3 drawPos, float rotation)
		{
			if (!LaunchProperties.rotationCurve.NullOrEmpty())
			{
				//Flip rotation if either west or south
				int sign = Ext_Math.Sign(LaunchProperties.flipRotation != vehicle.Rotation);
				rotation += LaunchProperties.rotationCurve.Evaluate(TimeInAnimation) * sign;
			}
			if (!LaunchProperties.xPositionCurve.NullOrEmpty())
			{
				int sign = Ext_Math.Sign(LaunchProperties.flipHorizontal != vehicle.Rotation);
				drawPos.x += LaunchProperties.xPositionCurve.Evaluate(TimeInAnimation) * sign;
			}
			if (!LaunchProperties.zPositionCurve.NullOrEmpty())
			{
				int sign = Ext_Math.Sign(LaunchProperties.flipVertical != vehicle.Rotation);;
				drawPos.z += LaunchProperties.zPositionCurve.Evaluate(TimeInAnimation) * sign;
			}
			if (!LaunchProperties.offsetCurve.NullOrEmpty())
			{
				int signX = Ext_Math.Sign(LaunchProperties.flipHorizontal != vehicle.Rotation);
				int signZ = Ext_Math.Sign(LaunchProperties.flipVertical != vehicle.Rotation);
				Vector2 offset = LaunchProperties.offsetCurve.EvaluateT(TimeInAnimation);
				drawPos += new Vector3(offset.x * signX, 0, offset.y * signZ);
			}
			return base.AnimateTakeoff(drawPos, rotation);
		}

		public override void ResolveProperties(LaunchProtocol reference)
		{
			base.ResolveProperties(reference);
			DirectionalTakeoff directionalReference = reference as DirectionalTakeoff;
			launchProperties = directionalReference.launchProperties;
			landingProperties = directionalReference.landingProperties;
		}
	}
}
