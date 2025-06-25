using UnityEngine;
using Verse;

namespace Vehicles;

public class AnimationProperties
{
  /* Customizable but not required */
  public int cycles = 1;
  public FloatRange exactRotation = new(0, 0);
  public float rotationRate;
  public float scale = 1;
  public FloatRange growthRate = new(0, 0);
  public Vector3 offset = Vector3.zero;
  public Color color = Color.white;

  /* MoteThrown exclusive */
  public FloatRange speedThrown = new(0, 0);
  public FloatRange deceleration = new(0, 0);
  public float fixedAcceleration = 0;
  public FloatRange angleThrown = new(0, 0);

  /* Required */
  public ThingDef moteDef;
  public AnimationWrapperType animationType;
}