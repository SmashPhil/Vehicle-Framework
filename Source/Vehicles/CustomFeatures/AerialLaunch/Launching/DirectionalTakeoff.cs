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

		public override LaunchProtocolProperties GetProperties(LaunchType launchType, Rot4 rot)
		{
			return launchType switch
			{
				LaunchType.Landing => rot.IsHorizontal ? landingProperties.horizontal : landingProperties.vertical,
				LaunchType.Takeoff => rot.IsHorizontal ? launchProperties.horizontal : launchProperties.vertical,
				_ => throw new NotImplementedException(),
			};
		}

		public override bool LandingRestricted(Map map, IntVec3 position, Rot4 rotation)
		{
			return LandingProperties.restriction != null && !LandingProperties.restriction.CanStartProtocol(vehicle, map, position, rotation);
		}

		public override bool FinishedAnimation(VehicleSkyfaller skyfaller)
		{
			return TicksPassed >= CurAnimationProperties.maxTicks;
		}

		protected override (Vector3 drawPos, float rotation, ShadowData shadowData) AnimateLanding(Vector3 drawPos, float rotation, ShadowData shadowData)
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

			if (LandingProperties.renderShadow)
			{
				if (!LandingProperties.shadowSizeXCurve.NullOrEmpty())
				{
					shadowData.width = LandingProperties.shadowSizeXCurve.Evaluate(TimeInAnimation);
				}
				if (!LandingProperties.shadowSizeZCurve.NullOrEmpty())
				{
					shadowData.height = LandingProperties.shadowSizeZCurve.Evaluate(TimeInAnimation);
				}
				if (!LandingProperties.shadowAlphaCurve.NullOrEmpty())
				{
					shadowData.alpha = LandingProperties.shadowAlphaCurve.Evaluate(TimeInAnimation);
				}
			}

			return base.AnimateLanding(drawPos, rotation, shadowData);
		}

		protected override (Vector3 drawPos, float rotation, ShadowData shadowData) AnimateTakeoff(Vector3 drawPos, float rotation, ShadowData shadowData)
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

			if (LaunchProperties.renderShadow)
			{
				if (!LaunchProperties.shadowSizeXCurve.NullOrEmpty())
				{
					shadowData.width = LaunchProperties.shadowSizeXCurve.Evaluate(TimeInAnimation);
				}
				if (!LaunchProperties.shadowSizeZCurve.NullOrEmpty())
				{
					shadowData.height = LaunchProperties.shadowSizeZCurve.Evaluate(TimeInAnimation);
				}
				if (!LaunchProperties.shadowAlphaCurve.NullOrEmpty())
				{
					shadowData.alpha = LaunchProperties.shadowAlphaCurve.Evaluate(TimeInAnimation);
				}
			}

			return base.AnimateTakeoff(drawPos, rotation, shadowData);
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
