using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Vehicles
{
	public class PatternProperties
	{
		//All = Apply to all vehicles
		public Dictionary<string, float> tiles = new Dictionary<string, float>(); //REDO - disable Dialog_ColorPicker zoom slider for this condition
		public bool equalize = true;
		public bool dynamicTiling = true;

		public Color? colorOne;
		public Color? colorTwo;
		public Color? colorThree;

		public bool IsDefault { get; internal set; }

		public override int GetHashCode()
		{
			return Gen.HashCombine(Gen.HashCombine(Gen.HashCombine(Gen.HashCombine(Gen.HashCombine(0, tiles), equalize), colorOne), colorTwo), colorThree);
		}

		public override string ToString()
		{
			return $"Tiles:{tiles} Equalize:{equalize} ColorOne:{colorOne.ToStringSafe() ?? "Null"} ColorTwo:{colorTwo.ToStringSafe() ?? "Null"} ColorThree:{colorThree.ToStringSafe() ?? "Null"}";
		}
	}
}
