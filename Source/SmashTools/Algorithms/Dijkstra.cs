using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Verse;

namespace SmashTools.Algorithms;

[PublicAPI]
public class Dijkstra<T>
{
  private readonly PriorityQueue<T, int> openQueue = new();
  private readonly Dictionary<T, Node> nodes = [];

  private readonly Func<T, bool> canEnter;
  private readonly Func<T, IEnumerable<T>> neighbors;
  private readonly Func<T, T, int> cost;

  public Dijkstra(Func<T, T, int> cost, Func<T, List<T>> neighbors, Func<T, bool> canEnter = null)
  {
    this.cost = cost;
    this.neighbors = neighbors;
    this.canEnter = canEnter;
  }

  public bool IsRunning { get; private set; }

  public void Run(T start, T destination, List<T> path)
  {
    IsRunning = true;
    try
    {
      openQueue.Clear();
      openQueue.Enqueue(start, 0);
      while (openQueue.Count > 0)
      {
        if (!openQueue.TryDequeue(out T current, out _))
          break;

        if (current.Equals(destination))
        {
          SolvePath(start, destination, path);
          return;
        }

        foreach (T neighbor in neighbors(current))
        {
          if (canEnter != null && !canEnter(neighbor))
            continue;

          if (!CreateNode(current, neighbor, out Node node))
            continue;

          nodes[neighbor] = node;
          // 0 cost heuristic for Dijkstra, but AStar<> uses this under the hood
          openQueue.Enqueue(neighbor, node.cost + node.heuristicCost);
        }
      }
      Log.Error($"Unable to find path from {start} to {destination}.");
    }
    finally
    {
      IsRunning = false;
    }
  }

  protected virtual bool CreateNode(T current, T neighbor, out Node node)
  {
    if (nodes.TryGetValue(neighbor, out node) && node.closed)
      return false;
    node = new Node
    {
      parent = current,
      cost = cost(current, neighbor),
      heuristicCost = 0,
      closed = true
    };
    return true;
  }

  protected void SolvePath(T start, T destination, List<T> path)
  {
    T current = destination;
    Node node = nodes[current];
    while (!start.Equals(current))
    {
      path.Add(current);
      current = node.parent;
      node = nodes[current];
    }
    path.Add(start);

    if (!path[path.Count - 1].Equals(start))
      SmashLog.Error($"BFS was unable to solve path from {start} to {destination}.");
  }

  protected struct Node
  {
    public T parent;
    public int cost;
    public bool closed;

    // strictly for A* implementation, is not considered by Dijkstra
    public int heuristicCost;
  }
}