using JetBrains.Annotations;
using RimWorld;
using SmashTools;
using Vehicles.Compatibility;
using Verse;
using Verse.Sound;

namespace Vehicles;

[PublicAPI]
public sealed class FishingTracker(VehiclePawn vehicle) : IExposable
{
  private bool fishingNow;

  private Command_Toggle fishToggle;

  public bool IsFishing => fishingNow;

  public bool CanFish
  {
    get
    {
      return FishingCompatibility.CanFishAt(vehicle, vehicle.Position);
    }
  }

  public bool HasWorkers
  {
    get
    {
      if ((vehicle.MovementPermissions & VehiclePermissions.Autonomous) != 0)
        return true;

      foreach (VehicleRoleHandler handler in vehicle.handlers)
      {
        foreach (Pawn pawn in handler.thingOwner)
        {
          if (pawn.skills?.GetSkill(SkillDefOf.Animals) is { TotallyDisabled: false })
            return true;
        }
      }
      return false;
    }
  }

  public void StartFishing()
  {
    fishingNow = true;
  }

  public void StopFishing()
  {
    fishingNow = false;
  }

  private void ToggleFishing()
  {
    fishingNow = !fishingNow;
    (fishingNow ? SoundDefOf.Checkbox_TurnedOn : SoundDefOf.Checkbox_TurnedOff).PlayOneShotOnCamera();
  }

  internal void RegisterEvents()
  {
    vehicle.AddEvent(VehicleEventDefOf.MoveStart, StopFishing);
  }

  internal Command_Toggle GetFishingGizmo()
  {
    fishToggle ??= new Command_Toggle
    {
      defaultLabel = "VF_StartFishing".Translate(),
      defaultDesc = "VF_StartFishingDesc".Translate(),
      icon = VehicleTex.FishingIcon,
      isActive = () => fishingNow,
      toggleAction = ToggleFishing
    };

    fishToggle.Disabled = false;
    fishToggle.disabledReason = null;

    if (!HasWorkers)
    {
      fishingNow = false;
      fishToggle.Disable("VF_NoFishermenTooltip".Translate());
    }
    if (!CanFish)
    {
      fishingNow = false;
      fishToggle.Disable("VF_NoFishAtSpot".Translate());
    }
    return fishToggle;
  }

  void IExposable.ExposeData()
  {
    Scribe_Values.Look(ref fishingNow, nameof(fishingNow));
  }
}
