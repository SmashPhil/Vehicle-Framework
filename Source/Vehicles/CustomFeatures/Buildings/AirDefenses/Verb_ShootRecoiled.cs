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
          Log.Error($"Unable to produce recoil to {caster.Label} of type {caster.GetType()}." +
                    $" Type should be Building_RecoiledTurret");
        }
        return true;
      }
      return false;
    }
  }
}
