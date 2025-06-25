using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace Vehicles;

[PublicAPI]
public abstract class FloatMenuOptionProvider_Vehicle : FloatMenuOptionProvider
{
  protected override bool Drafted => true;

  protected override bool Undrafted => false;

  protected override bool Multiselect => true;

  protected override bool MechanoidCanDo => false;

  protected override bool IgnoreFogged => false;

  public override bool SelectedPawnValid(Pawn pawn, FloatMenuContext context)
  {
    return pawn is VehiclePawn vehicle && SelectedVehicleValid(vehicle, context);
  }

  protected virtual bool SelectedVehicleValid(VehiclePawn vehicle, FloatMenuContext context)
  {
    return true;
  }
}