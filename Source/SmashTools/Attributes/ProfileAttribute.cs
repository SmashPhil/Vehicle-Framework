using System;

namespace SmashTools.Performance;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property)]
public class ProfileAttribute : Attribute
{
}