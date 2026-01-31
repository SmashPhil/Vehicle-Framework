using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld.Planet;
using SmashTools.Targeting;
using UnityEngine;
using Vehicles.World;

namespace Vehicles;

/// <summary>
/// Sends a payload to the world map
/// </summary>
[PublicAPI]
public interface ILauncher
{
  /// <summary>
  /// Destination to launch to.
  /// </summary>
  PlanetTile Tile { get; }

  /// <summary>
  /// The location on the world map the object is originating from.
  /// </summary>
  Vector3 Origin { get; }

  /// <summary>
  /// Launch the object on the world map.
  /// </summary>
  void Launch(TargetData<GlobalTargetInfo> targetData, IArrivalAction arrivalAction);

  /// <summary>
  /// Arrival actions at designated target.
  /// </summary>
  IEnumerable<ArrivalOption> OptionsAt(GlobalTargetInfo target);
}