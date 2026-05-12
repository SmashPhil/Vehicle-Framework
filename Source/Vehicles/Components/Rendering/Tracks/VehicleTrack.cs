using UnityEngine;

namespace Vehicles;

public abstract class VehicleTrack
{
  public abstract void TryPlaceTrack(VehiclePawn vehicle, ref Vector3 lastTrackPlacePos);
}