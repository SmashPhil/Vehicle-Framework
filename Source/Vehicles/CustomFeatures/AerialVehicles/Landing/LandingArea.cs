using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Vehicles
{
	public class LandingArea
	{
		public List<IntVec3> cells = new List<IntVec3>();

		public IntVec3 this[int index]
		{
			get
			{
				if (index >= cells.Count)
				{
					return IntVec3.Invalid;
				}
				return cells[index];
			}
		}
	}
}
