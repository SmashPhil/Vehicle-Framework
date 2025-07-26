using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using Verse;
using Verse.AI;

namespace Vehicles;

[PublicAPI]
public abstract class JobDriver_WorkVehicle : VehicleJobDriver
{
  protected abstract float TotalWork { get; }

  protected virtual float Work { get; set; }

  protected abstract StatDef Stat { get; }

  protected virtual SkillDef Skill => null;

  protected virtual EffecterDef EffecterDef => Vehicle.def.repairEffect;

  protected virtual float SkillAmount => 0.08f;

  protected virtual ToilCompleteMode ToilCompleteMode => ToilCompleteMode.Delay;

  protected float GetProgressPct()
  {
    return Work / TotalWork;
  }

  protected override IEnumerable<Toil> MakeNewToils()
  {
    this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
    Toil gotoCellToil = Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);
    gotoCellToil.FailOnMoving(TargetIndex.A);
    yield return gotoCellToil;

    Toil workToil = new();
    workToil.FailOnMoving(TargetIndex.A);
    workToil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
    workToil.initAction = ResetWork;
    workToil.tickAction = WorkAction;
    if (EffecterDef != null)
    {
      workToil.WithEffect(EffecterDef, TargetIndex.A);
    }
    else
    {
      workToil.WithProgressBar(TargetIndex.A, GetProgressPct);
    }

    workToil.defaultCompleteMode = ToilCompleteMode;
    workToil.defaultDuration = 2000;
    if (Skill != null)
    {
      workToil.activeSkill = () => Skill;
    }

    yield return workToil;
    yield break;

    void WorkAction()
    {
      Pawn actor = workToil.actor;
      if (Skill != null)
        actor.skills?.Learn(Skill, SkillAmount);

      float statValue = actor.GetStatValue(Stat);
      Work -= statValue;
      if (Work <= 0f)
      {
        WorkComplete(actor);
      }
    }
  }

  protected virtual void ResetWork()
  {
    Work = TotalWork;
  }

  protected abstract void WorkComplete(Pawn actor);
}