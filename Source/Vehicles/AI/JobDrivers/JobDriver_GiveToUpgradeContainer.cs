using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Vehicles;

public class JobDriver_GiveToUpgradeContainer : JobDriver_LoadVehicle
{
  protected override string ListerTag => ReservationType.LoadUpgradeMaterials;

  protected override bool FailJob()
  {
    if (!Vehicle.CompUpgradeTree.Upgrading || !Vehicle.CompUpgradeTree.NodeUnlocking.AvailableSpace(Vehicle, Item))
      return true;
    return base.FailJob();
  }

  protected override Toil GiveAsMuchToVehicleAsPossible()
  {
    return new Toil
    {
      initAction = delegate
      {
        if (Item is null || Item.stackCount <= 0)
        {
          pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
        }
        else
        {
          ThingDefCountClass materialRequired = Vehicle.CompUpgradeTree.NodeUnlocking.MaterialsRequired(Vehicle)
           .FirstOrDefault(x => x.thingDef == Item.def);

          if (materialRequired is null || materialRequired.count <= 0)
          {
            pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
          }
          else
          {
            int count = Mathf.Min(materialRequired.count, Item.stackCount); //Check back here
            Vehicle.CompUpgradeTree.AddToContainer(pawn.carryTracker.innerContainer, Item, count);
          }
        }
      }
    };
  }
}