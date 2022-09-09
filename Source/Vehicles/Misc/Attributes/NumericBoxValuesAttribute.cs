using System;

namespace Vehicles
{
	[AttributeUsage(AttributeTargets.Field, Inherited = true)]
	public class NumericBoxValuesAttribute : Attribute
	{
		public float MinValue { get; set; } = float.MinValue;
		public float MaxValue { get; set; } = float.MaxValue;
	}
}
