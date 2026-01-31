using CoreLib.Collections;
using RimWorld.Planet;
using SmashTools.Targeting;
using UnityEngine;
using Verse;

namespace Vehicles.World;

public sealed class FuelTargetUpdater : FlightPathTargetUpdater
{
  public FuelTargetUpdater(VehiclePawn vehicle, ILauncher launcher) : base(vehicle, launcher)
  {
  }

  private float TotalFuelCost { get; set; }

  private bool NoReturnTrip => TotalFuelCost > vehicle.CompFueledTravel.Fuel - TotalFuelCost;

  private bool NotEnoughFuel => TotalFuelCost > vehicle.CompFueledTravel.Fuel;

  public override void TargeterOnGUI()
  {
    base.TargeterOnGUI();
    Vector2 mousePosition = Event.current.mousePosition;
    if (vehicle.CompFueledTravel != null && TotalFuelCost > 0)
    {
      using TextBlock textFont = new(GameFont.Small);

      string fuelCostLabel = "VF_VehicleFuelCost".Translate(TotalFuelCost);
      Vector2 textSize = Text.CalcSize(fuelCostLabel);
      Rect labelPosition = new(mousePosition.x, mousePosition.y + textSize.y + 20f, textSize.x,
        textSize.y);
      GUI.DrawTexture(new Rect(labelPosition.x - textSize.x * 0.1f, labelPosition.y, textSize.x * 1.2f, textSize.y),
        TexUI.GrayTextBG);
      Widgets.Label(labelPosition, fuelCostLabel);
    }
  }

  public override void TargeterUpdate(ref readonly TargetData<GlobalTargetInfo> targetData)
  {
    base.TargeterUpdate(in targetData);
    TotalFuelCost = vehicle.CompVehicleLauncher.FuelNeededToLaunchAtDist(TotalDistance);
  }

  public override TargetValidation ValidateTargets(ReadOnlyList<GlobalTargetInfo> targets)
  {
    TargetValidation baseResult = base.ValidateTargets(targets);
    if (!baseResult)
      return baseResult;

    if (vehicle.CompFueledTravel != null)
    {
      if (NotEnoughFuel)
      {
        return TargetValidation.Failed with
        {
          Tooltip = "VF_NotEnoughFuel".Translate().Colorize(TexData.RedReadable)
        };
      }
      if (NoReturnTrip)
      {
        return TargetValidation.Success with
        {
          Tooltip = "VF_NoFuelReturnTrip".Translate().Colorize(TexData.YellowReadable)
        };
      }
    }
    return TargetValidation.Success;
  }

  protected override ShuttleLaunchStatus LaunchStatus()
  {
    ShuttleLaunchStatus status = base.LaunchStatus();
    if (status is ShuttleLaunchStatus.Invalid)
      return status;

    if (vehicle.CompFueledTravel != null)
    {
      if (!CurrentTargetUnderMouse().IsValid || TotalFuelCost > vehicle.CompFueledTravel.Fuel)
        return ShuttleLaunchStatus.Invalid;
      if (TotalFuelCost > vehicle.CompFueledTravel.Fuel - TotalFuelCost)
        return ShuttleLaunchStatus.NoReturnTrip;
    }
    return status;
  }
}