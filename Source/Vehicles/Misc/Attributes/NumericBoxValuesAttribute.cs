using System;

namespace Vehicles
{
	[AttributeUsage(AttributeTargets.Field, Inherited = true)]
	public class NumericBoxValuesAttribute : Attribute
	{
		public float MinValue { get; set; }
		public float MaxValue { get; set; }
	}
}
