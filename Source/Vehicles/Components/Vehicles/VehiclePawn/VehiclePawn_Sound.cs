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
using SmashTools.Animations;

namespace Vehicles
{
	/// <summary>
	/// Rendering & Graphics
	/// </summary>
	public partial class VehiclePawn
	{
		public ISustainerTarget SustainerTarget { get; private set; }

		public void SetSustainerTarget(ISustainerTarget sustainerTarget)
		{
			SustainerTarget = sustainerTarget;
		}

		public void ReleaseSustainerTarget()
		{
			sustainers.EndAll();
			SustainerTarget = null;
		}

		public virtual void SoundCleanup()
		{
			if (sustainers != null)
			{
				sustainers.EndAll();
			}
		}

		[AnimationEvent]
		private void PlaySound(SoundDef soundDef)
		{
			if (Spawned)
			{
				soundDef.PlayOneShot(this);
			}
		}

		[AnimationEvent]
		private void PlaySustainer(SoundDef soundDef)
		{
			if (Spawned)
			{
				sustainers.Spawn(this, soundDef);
			}
		}

		[AnimationEvent]
		private void EndSustainer(SoundDef soundDef)
		{
			sustainers.EndAll(soundDef);
		}

		[AnimationEvent]
		private void EndAllSustainers()
		{
			sustainers.EndAll();
		}
	}
}
