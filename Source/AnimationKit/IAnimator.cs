using JetBrains.Annotations;

namespace AnimationKit;

[PublicAPI]
public interface IAnimator
{
  int EntityId { get; }
}
