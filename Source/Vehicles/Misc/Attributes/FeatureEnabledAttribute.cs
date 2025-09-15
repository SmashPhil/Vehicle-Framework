using System;

namespace Vehicles;

[AttributeUsage(AttributeTargets.Field)]
public class FeatureEnabledAttribute : Attribute
{
	public FeatureEnabledAttribute(string featureName)
	{
		FeatureName = featureName;
	}

	public string FeatureName { get; }
}