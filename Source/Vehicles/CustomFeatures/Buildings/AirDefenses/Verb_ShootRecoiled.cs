using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class Verb_ShootRecoiled : Verb_ShootRealistic
	{
		protected override bool TryCastShot()
		{
			if (base.TryCastShot())
			{
				if (caster is Building_RecoiledTurret turret)
				{
					turret.Notify_Recoiled();
				}
				else
				{
					SmashLog.Error($"Unable to produce recoil to {caster.Label} of type <type>{caster.GetType()}</type>. Type should be <type>Building_RecoiledTurret</type>");
				}
				return true;
			}
			return false;
		}
	}
}
