using DevTools.Benchmarking;

// ReSharper disable all

namespace SmashTools.Performance;

[BenchmarkClass("Recursion Linear")]
internal class Benchmark_RecursionLinear
{
  [Benchmark(Label = "Recursion")]
  private static void Recursion_TailCall(ref RecursionContext context)
  {
    ProcessNode(context.root);
  }

  [Benchmark(Label = "Iterative")]
  private static void Recursion_Iterative(ref RecursionContext context)
  {
    Node node = context.root;
    while (node != null)
    {
      node.processed = true;
      node = node.child;
    }
  }

  private static void ProcessNode(Node node)
  {
    node.processed = true;
    if (node.child != null)
      ProcessNode(node.child);
  }

  private readonly struct RecursionContext
  {
    public readonly Node root;

    public RecursionContext()
    {
      const int Depth = 15;

      root = new Node(Depth);
    }
  }

  private class Node
  {
    public Node child;
    public bool processed;

    public Node(int depth)
    {
      if (depth > 0)
        child = new Node(--depth);
    }
  }
}