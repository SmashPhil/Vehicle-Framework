using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using SmashTools;
using SmashTools.Performance;

namespace Vehicles
{
	public class AsyncRegionRegisterAction : AsyncAction
	{
		private VehicleMapping mapping;
		private Thing thing;
		private bool register;

		public override bool IsValid => mapping?.map?.Index > -1;

		public void Set(VehicleMapping mapping, Thing thing, bool register)
		{
			this.mapping = mapping;
			this.thing = thing;
			this.register = register;
		}

		public override void Invoke()
		{
			if (register)
			{
				VehiclePathing.RegisterInRegions(thing, mapping);
			}
			else
			{
				VehiclePathing.DeregisterInRegions(thing, mapping);
			}
		}

		public override void ReturnToPool()
		{
			mapping = null;
			thing = null;
			AsyncPool<AsyncRegionRegisterAction>.Return(this);
		}
	}
}
