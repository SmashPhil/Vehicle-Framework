using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Vehicles
{
	[AttributeUsage(AttributeTargets.Field, Inherited = true)]
	public class SliderValuesAttribute : Attribute
	{
		public float MinValue { get; set; }
		public float MaxValue { get; set; }
		public float EndValue { get; set; }
		public string EndSymbol { get; set; }
		public int RoundDecimalPlaces { get; set; }
		public float Increment { get; set; }
		public string MinValueDisplay { get; set; }
		public string MaxValueDisplay { get; set; }
	}
}
