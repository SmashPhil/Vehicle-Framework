using System.Collections.Generic;
using DevTools.Benchmarking;

// ReSharper disable all

namespace SmashTools.Performance;

[BenchmarkClass("Recursion Tree")]
internal class Benchmark_RecursionTree
{
  [Benchmark(Label = "Recursion")]
  private static void Recursion_TailCall(ref RecursionContext context)
  {
    ProcessNode(context.root);
  }

  [Benchmark(Label = "Stack Loop")]
  private static void Recursion_StackLoop(ref RecursionContext context)
  {
    Stack<Node> stack = context.stack;
    stack.Push(context.root);
    while (context.stack.Count > 0)
    {
      Node current = stack.Pop();
      current.processed = true;

      foreach (Node child in current.children)
      {
        stack.Push(child);
      }
    }
  }

  private static void ProcessNode(Node node)
  {
    node.processed = true;
    foreach (Node childNode in node.children)
      ProcessNode(childNode);
  }

  private static List<Node> GetChildren(Node node)
  {
    return node.children;
  }

  private readonly struct RecursionContext
  {
    public readonly Stack<Node> stack = new();
    public readonly Node root;

    public RecursionContext()
    {
      const int Depth = 10;

      root = new Node(Depth);
    }
  }

  private class Node
  {
    public List<Node> children = [];
    public bool processed;

    public Node(int depth)
    {
      if (depth > 0)
      {
        children.Add(new Node(--depth));
        children.Add(new Node(--depth));
      }
    }
  }
}