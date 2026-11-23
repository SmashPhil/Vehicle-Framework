namespace Vehicles.Rendering;

public class HeadlightProperties : PointLightProperties
{
  public float angle;
  public float direction;
  public int length;

  public HeadlightProperties()
  {
    glowType = typeof(Headlight);
    intensity = 0.4f;
  }
}
