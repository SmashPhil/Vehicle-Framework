using System;
using System.Diagnostics;

namespace JetBrains.Annotations;

[PublicAPI, MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class)]
[Conditional("JETBRAINS_ANNOTATIONS")]
public class AssignedFromXmlAttribute : Attribute
{
}