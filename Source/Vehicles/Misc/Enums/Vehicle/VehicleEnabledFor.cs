using System;
using UnityEngine;
using Verse;

namespace Vehicles
{
	public static class VehicleEnabled
	{
		public enum For
		{
			None,
			Player,
			Raiders,
			Everyone,
		}

		public static For Next(this For current)
		{
			return current switch
			{
				For.None => For.Player,
				For.Player => For.Raiders,
				For.Raiders => For.Everyone,
				For.Everyone => For.None,
				_ => throw new NotImplementedException()
			};
		}

		public static (string text, Color color) GetStatus(For status)
		{
			return status switch
			{
				For.None => ("VF_VehicleDisabled".Translate(), Color.red),
				For.Player => ("VF_VehiclePlayerOnly".Translate(), new Color(0.1f, 0.85f, 0.85f)),
				For.Raiders => ("VF_VehicleRaiderOnly".Translate(), new Color(0.9f, 0.53f, 0.1f)),
				For.Everyone => ("VF_VehicleEnabled".Translate(), Color.green),
				_ => ("[Err] Uncaught Status", Color.red)
			};
		}
	}
}
