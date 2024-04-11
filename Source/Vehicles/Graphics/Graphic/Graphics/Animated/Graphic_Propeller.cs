using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Vehicles
{
	public class Graphic_Propeller : Graphic_Rotator
	{
		public const string Key = "Propeller";

		public override string RegistryKey => Key;
	}
}
