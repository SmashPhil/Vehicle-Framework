using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Vehicles
{
	public class AerialVehicleArrivalModeDef : Def
	{
		public Type workerClass = typeof(AerialVehicleArrivalModeWorker);
		public SimpleCurve selectionWeightCurve;
		public SimpleCurve pointsFactorCurve;
		public TechLevel minTechLevel;
		public bool forQuickMilitaryAid;
		public bool walkIn;

		[MustTranslate]
		public string textEnemy;
		[MustTranslate]
		public string textFriendly;
		[MustTranslate]
		public string textWillArrive;

		[Unsaved(false)]
		private AerialVehicleArrivalModeWorker workerInt;

		public AerialVehicleArrivalModeWorker Worker
		{
			get
			{
				if (workerInt == null)
				{
					workerInt = (AerialVehicleArrivalModeWorker)Activator.CreateInstance(workerClass);
					workerInt.def = this;
				}
				return workerInt;
			}
		}
	}
}
