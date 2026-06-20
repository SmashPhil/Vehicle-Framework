using UnityEngine;
using Verse;

namespace AnimationKit;

[StaticConstructorOnStartup]
internal static class TestMain
{
  static TestMain()
  {
    Transform transform = new();
    if (!transform.Disposed)
    {
      var p = transform.Position;
      transform.Position = new Vector3(1, 2, 3);
      p = transform.Position;
    }
  }
}
