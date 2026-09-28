using CoreLib;
using SmashTools.Animations;
using ParamType = SmashTools.Animations.AnimationParameter.ParamType;

namespace SmashTools
{
  internal static class AnimationEvents
  {
    [AnimationEvent]
    private static void SetFloat(IAnimator __animator, AnimationParameterDef paramDef, float value)
    {
      Trace.IsTrue(paramDef.type == ParamType.Trigger, $@"Mismatched AnimationParameterDef type. 
Must call method with matching type {paramDef.type}");

      __animator.Manager.SetFloat(paramDef, value);
    }

    [AnimationEvent]
    private static void SetInt(IAnimator __animator, AnimationParameterDef paramDef, int value)
    {
      Trace.IsTrue(paramDef.type == ParamType.Trigger, $@"Mismatched AnimationParameterDef type. 
Must call method with matching type {paramDef.type}");
      __animator.Manager.SetInt(paramDef, value);
    }

    [AnimationEvent]
    private static void SetBool(IAnimator __animator, AnimationParameterDef paramDef, bool value)
    {
      Trace.IsTrue(paramDef.type == ParamType.Trigger, $@"Mismatched AnimationParameterDef type. 
Must call method with matching type {paramDef.type}");
      __animator.Manager.SetBool(paramDef, value);
    }

    [AnimationEvent]
    private static void SetTrigger(IAnimator __animator, AnimationParameterDef paramDef, bool value)
    {
      Trace.IsTrue(paramDef.type == ParamType.Trigger, $@"Mismatched AnimationParameterDef type. 
Must call method with matching type {paramDef.type}");
      __animator.Manager.SetTrigger(paramDef, value);
    }
  }
}
