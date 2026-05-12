using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreLib.PathFinding;
using CoreLib.Performance;
using JetBrains.Annotations;
using SmashTools;
using UnityEngine;
using Verse;
using Path = CoreLib.PathFinding.Path;

namespace Vehicles;

#pragma warning disable CS0612 // Type or member is obsolete

public class VehiclePathReceipt : IPathPromise
{
  private const int MaxCancelWaitTime = 1000;

  private readonly CancellationTokenSource cts = new();
  private readonly AsyncPathFindAction action;

  internal VehiclePathReceipt(AsyncPathFindAction action)
  {
    this.action = action;
  }

  internal Task Task { get; set; }

  internal Path Path { get; set; }

  bool IPathPromise.IsCompleted => Task is { IsCompleted: true };

  public CancellationToken Token => cts.Token;

  void IPathPromise.Cancel()
  {
    cts.Cancel();
    if (Task != null)
    {
      Task.WaitAll([Task], MaxCancelWaitTime);
    }
  }

  void IDisposable.Dispose()
  {
    cts.Dispose();
    action.ReturnToPool();
  }

  Path IPathPromise.GetPath()
  {
    return Path;
  }
}

// TODO 1.7 - Remove and use CoreLib.Path fully
[PublicAPI]
public class VehiclePath : Path, IDisposable
{
  private List<IntVec3> copyOfNodes;

  [Obsolete("Use IsValid instead.")]
  public bool Found => IsValid;

  [Obsolete]
  public bool UsedHeuristics { get; private set; }

  public new IntVec3 LastNode => base.LastNode.ToIntVec3();

  public new IReadOnlyList<IntVec3> Nodes
  {
    get
    {
      copyOfNodes ??= base.Nodes.Select(node => node.ToIntVec3()).ToList();
      return copyOfNodes;
    }
  }

  public static VehiclePath NotFound => new();

  public void Init(bool usedHeuristics)
  {
    UsedHeuristics = usedHeuristics;
    IsValid = true;
    ResetPathToStart();
  }

  public void AddNode(IntVec3 cell)
  {
    Add(cell.ToPathNode());
  }

  public new IntVec3 ConsumeNextNode()
  {
    Node node = base.ConsumeNextNode();
    return node.ToIntVec3();
  }

  public new IntVec3 Peek(int nodesAhead)
  {
    return base.Peek(nodesAhead).ToIntVec3();
  }

  public void DrawPath(VehiclePawn vehicle)
  {
    if (!IsValid || IsFinished)
      return;

    float drawOffset = AltitudeLayer.Item.AltitudeFor();

    for (int i = 0; i < NodesLeft - 1; i++)
    {
      Vector3 from = Peek(i).ToVector3Shifted();
      from.y = drawOffset;
      Vector3 to = Peek(i + 1).ToVector3Shifted();
      to.y = drawOffset;
      GenDraw.DrawLineBetween(from, to);
    }
    if (vehicle is not null)
    {
      Vector3 curFrom = vehicle.DrawPos;
      curFrom.y = drawOffset;
      Vector3 curTo = Peek(0).ToVector3Shifted();
      curTo.y = drawOffset;
      if ((curFrom - curTo).sqrMagnitude > 0.01f)
      {
        GenDraw.DrawLineBetween(curFrom, curTo);
      }
    }
  }

  public void Dispose()
  {
    UsedHeuristics = false;
    Clear();
    AsyncPool<VehiclePath>.Return(this);
  }

  public static VehiclePath FromCoreLibPath(Path path)
  {
    if (path is VehiclePath existingPath)
      return existingPath;

    VehiclePath vehiclePath = new();
    foreach (Node node in path.Nodes)
    {
      vehiclePath.Add(node);
    }
    vehiclePath.IsValid = path.IsValid;
    vehiclePath.ResetPathToStart();
    return vehiclePath;
  }
}