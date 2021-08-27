using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
				if (innerLookup.TryGetValue(key.ToUpperInvariant(), out float value))
				{
					return value;
				}
				return 0;
			}
			set
			{
				innerLookup[key.ToUpperInvariant()] = value;
			}
		}
	}
}
