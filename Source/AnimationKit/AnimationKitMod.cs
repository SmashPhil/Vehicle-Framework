using SmashTools;
using Verse;

namespace AnimationKit;

#if ANIMATOR
internal class AnimationKitMod : Mod
{
  public AnimationKitMod(ModContentPack content) : base(content)
  {
    if (!DllInjector.LoadNative(content, "animation_kit", typeof(AnimationKitMod).Assembly))
    {
      Log.Error("Unable to load animation kit.");
    }
  }
}
#endif