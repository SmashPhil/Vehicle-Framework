using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace Vehicles
{
	public class Designator_AreaRoadClear : Designator_AreaRoad
	{
		public Designator_AreaRoadClear() : base(DesignateMode.Remove)
		{
			defaultLabel = "VF_RoadZoneClear".Translate();
			defaultDesc = "VF_RoadZoneClearDesc".Translate();
			icon = ContentFinder<Texture2D>.Get("UI/Designators/RoadAreaOff");
			soundDragSustain = SoundDefOf.Designate_DragAreaDelete;
			soundDragChanged = null;
			soundSucceeded = SoundDefOf.Designate_ZoneDelete;
		}
	}
}
