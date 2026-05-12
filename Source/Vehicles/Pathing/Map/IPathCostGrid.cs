using System;
using JetBrains.Annotations;

namespace Vehicles;

[PublicAPI]
public interface IPathCostGrid : IDisposable
{
  int Index { get; set; }

  bool ShouldApplyFor(in PathSettings settings);

  void Update(int index);
}
