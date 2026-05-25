using JetBrains.Annotations;
using Verse;

namespace Vehicles;

[PublicAPI]
public interface IVehicleAssigner
{
  AssignedSeat GetAssignedSeat(Pawn pawn);
}
