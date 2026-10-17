using System;
using System.Diagnostics;

namespace JetBrains.Annotations;

[PublicAPI, MeansImplicitUse(ImplicitUseTargetFlags.Itself)]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Struct |
                AttributeTargets.Interface | AttributeTargets.Method | AttributeTargets.Constructor)]
[Conditional("JETBRAINS_ANNOTATIONS")]
public class UsedWithReflectionAttribute : Attribute
{
}