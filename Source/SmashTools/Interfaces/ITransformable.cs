using JetBrains.Annotations;

namespace SmashTools.Rendering;

[PublicAPI]
public interface ITransformable
{
  Transform Transform { get; }
}