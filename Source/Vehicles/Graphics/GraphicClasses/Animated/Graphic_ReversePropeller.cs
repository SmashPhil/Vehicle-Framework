namespace Vehicles;

public class Graphic_ReversePropeller : Graphic_Rotator
{
  public const string Key = "ReversePropeller";

  public override int RegistryKey { get; } = Key.GetHashCode();

  public override float ModifyIncomingRotation(float rotation)
  {
    return -rotation;
  }
}