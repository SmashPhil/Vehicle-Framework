﻿using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;

namespace Vehicles
{
	public class FloatMenuMulti : FloatMenu
	{
		public const int RevalidateEveryFrame = 3;

		private Vector3 clickPos;

		private List<Pawn> selPawns;

		private Pawn clickedPawn;

		public FloatMenuMulti(List<FloatMenuOption> options, List<Pawn> selPawns, Pawn clickedPawn, string title, Vector3 clickPos) : base(options, title, false)
		{
			this.clickPos = clickPos;
			this.selPawns = selPawns;
			this.clickedPawn = clickedPawn;
		}

		public override void DoWindowContents(Rect rect)
		{
			if (selPawns is null || selPawns.Count < 1)
			{
				Find.WindowStack.TryRemove(this, true);
				return;
			}
			if (Time.frameCount % RevalidateEveryFrame == 0)
			{
				for (int i = 0; i < options.Count; i++)
				{
					if(!options[i].Disabled && !StillValid(options[i], selPawns, clickedPawn))
					{
						options[i].Disabled = true;
					}
				}
			}
			base.DoWindowContents(rect);
		}

		private static bool StillValid(FloatMenuOption opt, List<Pawn> pawns, Pawn ship)
		{
			if(!ship.Spawned || ship.Dead || ship.Downed || (ship as VehiclePawn).vehiclePather.Moving)
			{
				return false;
			}

			if(!pawns.All(x => !x.Dead && !x.Downed && !x.InMentalState))
			{
				return false;
			}
			return true;
		}
	}
}
