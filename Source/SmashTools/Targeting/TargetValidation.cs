namespace SmashTools.Targeting;

public struct TargetValidation
{
  public required bool isValid;

  public string Tooltip { get; init; }

  public static TargetValidation Success => new() { isValid = true };

  public static TargetValidation Failed => new() { isValid = false };

  public static implicit operator bool(TargetValidation result)
  {
    return result.isValid;
  }
}