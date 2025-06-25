namespace Vehicles;

public abstract class Graphic_Rotator : Graphic_Rgb
{
  public abstract int RegistryKey { get; }

  public virtual float ModifyIncomingRotation(float rotation)
  {
    return rotation;
  }
}