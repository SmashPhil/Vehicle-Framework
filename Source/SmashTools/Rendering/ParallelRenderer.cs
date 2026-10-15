namespace SmashTools.Rendering;

public static class ParallelRenderer
{
  public static void SetDirty(this IParallelRenderer renderer)
  {
    renderer.IsDirty = true;
  }
}