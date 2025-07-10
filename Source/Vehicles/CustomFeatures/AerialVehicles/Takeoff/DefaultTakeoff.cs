using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine.Assertions;
using Vehicles.World;

namespace Vehicles;

public class DefaultTakeoff : LaunchProtocol
{
  /* -- Xml Input -- */

  [GraphEditable(Category = AnimationEditorTags.Takeoff)]
  public LaunchProtocolProperties launchProperties;

  [GraphEditable(Category = AnimationEditorTags.Landing)]
  public LaunchProtocolProperties landingProperties;

  /* ---------------- */

  public DefaultTakeoff()
  {
  }

  public DefaultTakeoff(DefaultTakeoff reference, VehiclePawn vehicle) : base(reference, vehicle)
  {
    landingProperties = reference.landingProperties;
    launchProperties = reference.launchProperties;
  }

  protected override int TotalTicks_Takeoff => launchProperties.maxTicks;

  protected override int TotalTicks_Landing => landingProperties.maxTicks;

  public override LaunchProtocolProperties CurAnimationProperties =>
    launchType == LaunchType.Landing ? landingProperties : launchProperties;

  public override LaunchProtocolProperties LandingProperties => landingProperties;

  public override LaunchProtocolProperties LaunchProperties => launchProperties;

  public override LaunchProtocolProperties GetProperties(LaunchType launchType, Rot4 rot)
  {
    return launchType switch
    {
      LaunchType.Landing => LandingProperties,
      LaunchType.Takeoff => LaunchProperties,
      _                  => throw new NotImplementedException(),
    };
  }

  public override bool FinishedAnimation(VehicleSkyfaller skyfaller)
  {
    return ticksPassed >= CurAnimationProperties.maxTicks;
  }

  /// <summary>
  /// Tick method for <see cref="AnimationSimulator"/> with total ticks passed since start.
  /// </summary>
  /// <param name="ticksPassed"></param>
  protected override int AnimationEditorTick_Landing(int ticksPassed)
  {
    this.ticksPassed = ticksPassed.Take(landingProperties.maxTicks, out int remaining);
    TickMotes();
    return remaining;
  }

  protected override int AnimationEditorTick_Takeoff(int ticksPassed)
  {
    this.ticksPassed = ticksPassed;
    TickMotes();
    return 0;
  }

  protected override (Vector3 drawPos, float rotation, DynamicShadowData shadowData) AnimateLanding(
    Vector3 drawPos, float rotation, DynamicShadowData shadowData)
  {
    if (!LandingProperties.rotationCurve.NullOrEmpty())
    {
      //Flip rotation if either west or south
      int sign = Ext_Math.Sign(LandingProperties.flipRotation != vehicle.Rotation);
      rotation += LandingProperties.rotationCurve.Evaluate(TimeInAnimation) * sign;
    }
    if (!LandingProperties.xPositionCurve.NullOrEmpty())
    {
      //Flip rotation if either west or south
      int sign = Ext_Math.Sign(LandingProperties.flipHorizontal != vehicle.Rotation);
      drawPos.x += LandingProperties.xPositionCurve.Evaluate(TimeInAnimation) * sign;
    }
    if (!LandingProperties.zPositionCurve.NullOrEmpty())
    {
      //Flip rotation if either west or south
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

  protected override (Vector3 drawPos, float rotation, DynamicShadowData shadowData) AnimateTakeoff(
    Vector3 drawPos, float rotation, DynamicShadowData shadowData)
  {
    if (!LaunchProperties.rotationCurve.NullOrEmpty())
    {
      //Flip rotation if either west or south
      int sign = Ext_Math.Sign(LaunchProperties.flipRotation != vehicle.Rotation);
      rotation += LaunchProperties.rotationCurve.Evaluate(TimeInAnimation) * sign;
    }
    if (!LaunchProperties.xPositionCurve.NullOrEmpty())
    {
      //Flip rotation if either west or south
      int sign = Ext_Math.Sign(LaunchProperties.flipHorizontal != vehicle.Rotation);
      drawPos.x += LaunchProperties.xPositionCurve.Evaluate(TimeInAnimation) * sign;
    }
    if (!LaunchProperties.zPositionCurve.NullOrEmpty())
    {
      //Flip rotation if either west or south
      int sign = Ext_Math.Sign(LaunchProperties.flipVertical != vehicle.Rotation);
      drawPos.z += LaunchProperties.zPositionCurve.Evaluate(TimeInAnimation) * sign;
    }
    if (!LaunchProperties.offsetCurve.NullOrEmpty())
    {
      Vector2 offset = LaunchProperties.offsetCurve.EvaluateT(TimeInAnimation);
      int signX = Ext_Math.Sign(LaunchProperties.flipHorizontal != vehicle.Rotation);
      int signZ = Ext_Math.Sign(LaunchProperties.flipVertical != vehicle.Rotation);
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
    DefaultTakeoff defaultReference = reference as DefaultTakeoff;
    Assert.IsNotNull(defaultReference);
    launchProperties = defaultReference.launchProperties;
    landingProperties = defaultReference.landingProperties;
  }
}