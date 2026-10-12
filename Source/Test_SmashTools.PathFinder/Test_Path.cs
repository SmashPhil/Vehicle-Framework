using System;
using System.Collections.Generic;
using CoreLib.Collections;
using CoreLib.PathFinding;
using DevTools.Testing;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace SmashTools.PathFinder.Tests;

[TestFixture(TestType.MainMenu)]
internal class Test_Path
{
  private const int MapWidth = 10;

  [Test]
  public void Constructor()
  {
    Path path = new();
    Assert.IsFalse(path.IsValid);
    Assert.AreEqual(-1, path.Current);
    Assert.AreEqual(0, path.NodesLeft);
    Assert.IsTrue(path.IsFinished);
  }

  [Test]
  public void Populate()
  {
    // (2, 2) -> (1, 1) -> (0, 0), path is finalized in reverse order instead of back tracking.
    using NativeFixedList<int> list = new(length: 3, Allocator.Temp);
    list.Add(0);
    list.Add(MapWidth + 1);
    list.Add(MapWidth * 2 + 2);

    Path path = new();
    path.Populate(in list, MapWidth);

    Assert.AreEqual(expected: 3, path.Nodes.Count);
    Assert.AreEqual(expected: 0, path.NodesConsumedCount);
    Assert.AreEqual(new int3(0, 0, 0), path.LastNode.ToInt3());
    Assert.AreEqual(new int3(1, 0, 1), path.Nodes[1].ToInt3());
    Assert.AreEqual(new int3(2, 0, 2), path.FirstNode.ToInt3());
  }

  [Test]
  public void PopulateList()
  {
    // (2, 2) -> (1, 1) -> (0, 0), path is populated sequentially.
    List<Path.Node> list = new()
      {
        new Path.Node(0, 0),
        new Path.Node(1, 1),
        new Path.Node(2, 2)
      };

    Path path = new();
    path.Populate(list);
    Assert.IsTrue(path.IsValid);
    Assert.AreEqual(expected: 3, path.Nodes.Count);
    Assert.AreEqual(expected: 0, path.NodesConsumedCount);
    Assert.AreEqual(new int3(0, 0, 0), path.LastNode.ToInt3());
    Assert.AreEqual(new int3(1, 0, 1), path.Nodes[1].ToInt3());
    Assert.AreEqual(new int3(2, 0, 2), path.FirstNode.ToInt3());
  }

  [Test]
  public void PopulateEnumerable()
  {
    // (2, 2) -> (1, 1) -> (0, 0), path is populated sequentially.
    List<Vector3Int> list = new()
      {
        new Vector3Int(0, 0, 0),
        new Vector3Int(1, 0, 1),
        new Vector3Int(2, 0, 2)
      };

    Path path = new();
    path.Populate(ToNode(list));
    Assert.IsTrue(path.IsValid);
    Assert.AreEqual(expected: 3, path.Nodes.Count);
    Assert.AreEqual(expected: 0, path.NodesConsumedCount);
    Assert.AreEqual(new int3(0, 0, 0), path.LastNode.ToInt3());
    Assert.AreEqual(new int3(1, 0, 1), path.Nodes[1].ToInt3());
    Assert.AreEqual(new int3(2, 0, 2), path.FirstNode.ToInt3());
    return;

    static IEnumerable<Path.Node> ToNode(List<Vector3Int> list)
    {
      foreach (Vector3Int v3 in list)
      {
        yield return new Path.Node(v3.x, v3.z);
      }
    }
  }

  [Test]
  public void Reset()
  {
    using NativeFixedList<int> list = new(length: 3, Allocator.Temp);
    // (3, 2) -> (2, 1) -> (2, 0)
    list.Add(2);
    list.Add(2 + MapWidth);
    list.Add(3 + MapWidth * 2);
    Path path = new();
    path.Populate(in list, MapWidth);
    path.Validate(path.FirstNode, path.LastNode);

    Assert.IsTrue(path.IsValid);
    Assert.AreEqual(path.Nodes.Count - 1, path.Current);
    Assert.AreEqual(expected: 3, path.NodesLeft);
    Assert.AreEqual(expected: 0, path.NodesConsumedCount);
    Assert.IsFalse(path.IsFinished);
  }

  [Test]
  public void Clear()
  {
    using NativeFixedList<int> list = new(length: 3, Allocator.Temp);
    // (3, 2) -> (2, 1) -> (2, 0)
    list.Add(2);
    list.Add(2 + MapWidth);
    list.Add(3 + MapWidth * 2);
    Path path = new();
    path.Populate(in list, MapWidth);
    path.Validate(path.FirstNode, path.LastNode);

    Assert.IsTrue(path.IsValid);
    Assert.AreEqual(path.Nodes.Count - 1, path.Current);
    Assert.AreEqual(expected: 3, path.NodesLeft);
    Assert.IsFalse(path.IsFinished);

    path.Clear();

    Assert.IsFalse(path.IsValid);
    Assert.AreEqual(expected: -1, path.Current);
    Assert.AreEqual(expected: 0, path.NodesLeft);
    Assert.IsTrue(path.IsFinished);
  }

  [Test]
  public void Peek()
  {
    using NativeFixedList<int> list = new(length: 3, Allocator.Temp);
    // (3, 2) -> (2, 1) -> (2, 0)
    list.Add(2);
    list.Add(2 + MapWidth);
    list.Add(3 + MapWidth * 2);
    Path path = new();
    path.Populate(in list, MapWidth);
    path.Validate(path.FirstNode, path.LastNode);

    Assert.AreEqual(expected: new int3(3, 0, 2), actual: path.Peek(0).ToInt3());
    Assert.AreEqual(expected: new int3(2, 0, 1), actual: path.Peek(1).ToInt3());
    Assert.AreEqual(expected: new int3(2, 0, 0), actual: path.Peek(2).ToInt3());
    Expect.Throws<ArgumentOutOfRangeException>(() => path.Peek(3));
  }

  [Test]
  public void ConsumeNextNode()
  {
    using NativeFixedList<int> list = new(length: 3, Allocator.Temp);
    // (3, 2) -> (2, 1) -> (2, 0)
    list.Add(2);
    list.Add(2 + MapWidth);
    list.Add(3 + MapWidth * 2);
    Path path = new();
    path.Populate(in list, MapWidth);
    path.Validate(path.FirstNode, path.LastNode);

    Assert.AreEqual(expected: 3, path.NodesLeft);
    Assert.AreEqual(expected: 0, path.NodesConsumedCount);
    Assert.IsFalse(path.IsFinished);
    Assert.AreEqual(expected: new int3(2, 0, 1), actual: path.ConsumeNextNode().ToInt3());
    Assert.AreEqual(expected: 2, path.NodesLeft);
    Assert.AreEqual(expected: 1, path.NodesConsumedCount);
    Assert.IsFalse(path.IsFinished);
    Assert.AreEqual(expected: new int3(2, 0, 0), actual: path.ConsumeNextNode().ToInt3());
    Assert.AreEqual(expected: 1, path.NodesLeft);
    Assert.AreEqual(expected: 2, path.NodesConsumedCount);
    Assert.IsTrue(path.IsFinished);
    Expect.Throws<ArgumentOutOfRangeException>(() => path.ConsumeNextNode());
  }

  [Test]
  public void AppendInvalidPath()
  {
    using NativeFixedList<int> aList = new(length: 2, Allocator.Temp);
    // (3, 0) -> (2, 0) appends Empty
    aList.Add(2);
    aList.Add(3);

    Path pathA = new();
    pathA.Populate(in aList, MapWidth);
    pathA.Validate(pathA.FirstNode, pathA.LastNode);

    Path pathB = new();
    Assert.IsFalse(pathB.IsValid);
    Expect.Throws<ArgumentException>(() => pathA.Combine(pathB));
  }

  [Test]
  public void AppendDisconnectedPaths()
  {
    using NativeFixedList<int> aList = new(length: 2, Allocator.Temp);
    using NativeFixedList<int> bList = new(length: 2, Allocator.Temp);
    // (3, 0) -> (2, 0) appends (1, 0) -> (0, 0)
    aList.Add(2);
    aList.Add(3);
    bList.Add(0);
    bList.Add(1);

    Path pathA = new();
    Path pathB = new();
    pathA.Populate(in aList, MapWidth);
    pathB.Populate(in bList, MapWidth);
    pathA.Validate(pathA.FirstNode, pathA.LastNode);
    pathB.Validate(pathB.FirstNode, pathB.LastNode);

    Expect.Throws<InvalidOperationException>(() => pathA.Combine(pathB));
  }

  [Test]
  public void AppendInsertsNodes()
  {
    using NativeFixedList<int> aList = new(length: 2, Allocator.Temp);
    using NativeFixedList<int> bList = new(length: 2, Allocator.Temp);
    // (3, 0) -> (2, 0) appends (2, 0) -> (1, 0)
    aList.Add(2);
    aList.Add(3);
    bList.Add(1);
    bList.Add(2);

    Path pathA = new();
    Path pathB = new();
    pathA.Populate(in aList, MapWidth);
    pathB.Populate(in bList, MapWidth);
    pathA.Validate(pathA.FirstNode, pathA.LastNode);
    pathB.Validate(pathB.FirstNode, pathB.LastNode);

    pathA.Combine(pathB);
    Assert.AreEqual(expected: 3, pathA.NodesLeft);
    Assert.AreEqual(expected: new int3(3, 0, 0), actual: pathA.Peek(0).ToInt3());
    Assert.AreEqual(expected: new int3(2, 0, 0), actual: pathA.Peek(1).ToInt3());
    Assert.AreEqual(expected: new int3(1, 0, 0), actual: pathA.Peek(2).ToInt3());
  }

  [Test]
  public void AppendAdjustsState()
  {
    using NativeFixedList<int> aList = new(length: 2, Allocator.Temp);
    using NativeFixedList<int> bList = new(length: 2, Allocator.Temp);
    // (3, 0) -> (2, 0) appends (2, 0) -> (1, 0)
    aList.Add(2);
    aList.Add(3);
    bList.Add(1);
    bList.Add(2);

    Path pathA = new();
    Path pathB = new();
    pathA.Populate(in aList, MapWidth);
    pathB.Populate(in bList, MapWidth);
    pathA.Validate(pathA.FirstNode, pathA.LastNode);
    pathB.Validate(pathB.FirstNode, pathB.LastNode);

    Assert.AreEqual(expected: 1, pathA.Current);
    pathA.Combine(pathB);
    Assert.IsTrue(pathA.IsValid);
    Assert.AreEqual(expected: 2, pathA.Current);
  }

  [Test]
  public void AppendMidTraversal()
  {
    using NativeFixedList<int> aList = new(length: 3, Allocator.Temp);
    using NativeFixedList<int> bList = new(length: 3, Allocator.Temp);
    // (3, 0) -> (2, 0) -> (1, 0) appends (1, 0) -> (1, 1) -> (1, 2)
    aList.Add(1);
    aList.Add(2);
    aList.Add(3);
    bList.Add(1 + MapWidth * 2);
    bList.Add(1 + MapWidth);
    bList.Add(1);

    Path pathA = new();
    Path pathB = new();
    pathA.Populate(in aList, MapWidth);
    pathB.Populate(in bList, MapWidth);
    pathA.Validate(pathA.FirstNode, pathA.LastNode);
    pathB.Validate(pathB.FirstNode, pathB.LastNode);

    Assert.AreEqual(expected: new int3(2, 0, 0), actual: pathA.ConsumeNextNode().ToInt3());
    Assert.AreEqual(expected: 2, pathA.NodesLeft);
    Assert.AreEqual(expected: 1, pathA.Current);

    pathA.Combine(pathB);
    Assert.AreEqual(expected: 4, pathA.NodesLeft);
    Assert.AreEqual(expected: 3, pathA.Current);
    Assert.AreEqual(expected: new int3(2, 0, 0), actual: pathA.Peek(0).ToInt3());
    Assert.AreEqual(expected: new int3(1, 0, 0), actual: pathA.Peek(1).ToInt3());
    Assert.AreEqual(expected: new int3(1, 0, 1), actual: pathA.Peek(2).ToInt3());
    Assert.AreEqual(expected: new int3(1, 0, 2), actual: pathA.Peek(3).ToInt3());
  }
}
