using JetBrains.Annotations;

namespace SmashTools.Targeting;

[PublicAPI]
public interface ITargetOption
{
  public string Label { get; }
}