using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
	/// <summary>
	/// Copy of <see cref="CollectionsMassCalculator"/> with tweaks specific to vehicle mass usage
	/// </summary>
	public static class CaravanInfoHelper
	{
		private static readonly List<ThingCount> tmpThingCounts = new List<ThingCount>();

		public static float Capacity(List<ThingCount> thingCounts, StringBuilder explanation = null)
		{
			float capacity = 0f;
			for (int i = 0; i < thingCounts.Count; i++)
			{
				if (thingCounts[i].Count > 0)
				{
					if (thingCounts[i].Thing is Pawn pawn && !pawn.IsInVehicle() && !CaravanHelper.assignedSeats.ContainsKey(pawn))
					{
						capacity += MassUtility.Capacity(pawn, explanation) * thingCounts[i].Count;
					}
				}
			}
			return Mathf.Max(capacity, 0f);
		}
	}
}
