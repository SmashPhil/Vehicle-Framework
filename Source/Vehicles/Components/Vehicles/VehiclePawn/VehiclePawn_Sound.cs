using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using UnityEngine;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.Sound;
using Verse.AI;
using Verse.AI.Group;
using SmashTools;

namespace Vehicles
{
	/// <summary>
	/// Rendering & Graphics
	/// </summary>
	public partial class VehiclePawn
	{
		protected Sustainer sustainerAmbient;

		public virtual void SoundCleanup()
		{
			if (sustainerAmbient != null)
			{
				sustainerAmbient.End();
				sustainerAmbient = null;
			}
			if (sustainers != null)
			{
				sustainers.EndAll();
			}
		}
	}
}
