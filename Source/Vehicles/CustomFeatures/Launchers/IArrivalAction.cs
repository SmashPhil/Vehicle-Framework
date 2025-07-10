using JetBrains.Annotations;
using RimWorld.Planet;
using Verse;

namespace Vehicles.World;

[PublicAPI]
public interface IArrivalAction : IExposable
{
  void Arrived(GlobalTargetInfo target);
}