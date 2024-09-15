using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Vehicles
{
	public class VehicleStrategyDef : Def
	{
		public Type strategyWorker;

		private StrategyWorker worker;

		public StrategyWorker Worker
		{
			get
			{
				worker ??= (StrategyWorker)Activator.CreateInstance(strategyWorker);
				return worker;
			}
		}
	}
}
