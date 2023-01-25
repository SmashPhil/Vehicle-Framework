using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;

namespace Vehicles
{
	public class WindDirectional : MapComponent
	{
		private const int WindDirectionChangeTimer = 2500;

		private float windDirectionByAngle;

		private float newWindDirectionAngle;

		public WindDirectional(Map map) : base(map)
		{
		}

		public float WindDirection => windDirectionByAngle;

		public override void MapComponentTick()
		{
			base.MapComponentTick();
			if (Find.TickManager.TicksGame % WindDirectionChangeTimer == 0)
			{
				newWindDirectionAngle = Rand.Range(-30f, 30f);
			}

			if (windDirectionByAngle != newWindDirectionAngle)
			{
				if(windDirectionByAngle > newWindDirectionAngle)
				{
					windDirectionByAngle -= 0.15f;
				}
				else
				{
					windDirectionByAngle += 0.15f;
				}
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref windDirectionByAngle, "windDirectionByAngle");
		}
	}
}
