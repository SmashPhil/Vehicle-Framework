using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Verse;

namespace Vehicles;

/// <summary>
/// Denotes a Lord that has vehicle tasks as part of its state graph.
/// </summary>
[PublicAPI]
public interface IVehicleTaskLord
{
  /// <summary>
  /// Task this lord will work at some point in its state graph.
  /// </summary>
  IVehicleTask Task { get; }

  /// <summary>
  /// Where pawns should meet before starting the task.
  /// </summary>
  IntVec3 MeetingSpot { get; }

  /// <summary>
  /// Where the vehicle should go to carry out the task.
  /// </summary>
  IntVec3 Destination { get; }
}
