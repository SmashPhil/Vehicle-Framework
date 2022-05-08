using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Vehicles
{
	/// <summary>
	/// Case-insensitive wrapper class for safe dictionary lookups and registration for rotations specific to <seealso cref="Graphic_Rotator"/> graphics
	/// </summary>
	public class ExtraRotationRegistry
	{
		private readonly Dictionary<string, float> innerLookup = new Dictionary<string, float>();

		public float this[string key]
		{
			get
			{
				return innerLookup.TryGetValue(key.ToUpperInvariant(), 0);
			}
			set
			{
				innerLookup[key.ToUpperInvariant()] = value;
			}
		}
	}
}
