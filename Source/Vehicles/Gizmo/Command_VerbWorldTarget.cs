using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace Vehicles
{
	public class Command_VerbWorldTarget : Command
	{
		public Verb verb;
		protected List<Verb> groupedVerbs;
		public bool drawRadius = true;

		public override Color IconDrawColor
		{
			get
			{
				if (verb.EquipmentSource != null)
				{
					return verb.EquipmentSource.DrawColor;
				}
				return base.IconDrawColor;
			}
		}

		public override void GizmoUpdateOnMouseover()
		{
			if (!drawRadius)
			{
				return;
			}
			verb.verbProps.DrawRadiusRing(verb.caster.Position);
			if (!groupedVerbs.NullOrEmpty<Verb>())
			{
				foreach (Verb verb in groupedVerbs)
				{
					verb.verbProps.DrawRadiusRing(verb.caster.Position);
				}
			}
		}

		public override void MergeWith(Gizmo other)
		{
			base.MergeWith(other);
			Command_VerbWorldTarget command_VerbWorldTarget = other as Command_VerbWorldTarget;
			if (command_VerbWorldTarget == null)
			{
				Log.ErrorOnce("Tried to merge Command_VerbTarget with unexpected type", 73406263);
				return;
			}
			if (groupedVerbs == null)
			{
				groupedVerbs = new List<Verb>();
			}
			groupedVerbs.Add(command_VerbWorldTarget.verb);
			if (command_VerbWorldTarget.groupedVerbs != null)
			{
				groupedVerbs.AddRange(command_VerbWorldTarget.groupedVerbs);
			}
		}

		public override void ProcessInput(Event ev)
		{
			base.ProcessInput(ev);
			SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
			//if (verb.CasterIsPawn && targeter.targetingSource != null && targeter.targetingSource.GetVerb.verbProps == verb.verbProps)
			//{
			//	Pawn casterPawn = verb.CasterPawn;
			//	if (!targeter.IsPawnTargeting(casterPawn))
			//	{
			//		targeter.targetingSourceAdditionalPawns.Add(casterPawn);
			//		return;
			//	}
			//}
			//else
			//{
			//	Find.Targeter.BeginTargeting(verb, null);
			//}
		}
	}
}
