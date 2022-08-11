using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Vehicles
{
	public interface IRefundable
	{
		public IEnumerable<(ThingDef thingDef, float count)> Refunds { get; }
	}
}
