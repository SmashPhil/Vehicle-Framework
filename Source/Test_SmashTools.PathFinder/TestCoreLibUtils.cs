using CoreLib.PathFinding;
using NUnit.Framework;
using SmashTools.PathFinder.Collections;
using Unity.Collections;
using Unity.Mathematics;

namespace SmashTools.PathFinder.Tests
{
  [TestFixture]
  internal class TestCoreLibUtils
  {
    private const int MapWidth = 10;

    [Test]
    public void ToInt3()
    {
      Path.Node node = new(3, 7);
      int3 pos = node.ToInt3();

      Assert.AreEqual(expected: 3, actual: pos.x);
      Assert.AreEqual(expected: 0, actual: pos.y);
      Assert.AreEqual(expected: 7, actual: pos.z);
    }

    [Test]
    public void ToPathNode()
    {
      int3 pos = new(4, 99, 6);
      Path.Node node = pos.ToPathNode();

      Assert.AreEqual(expected: new Path.Node(4, 6), actual: node);
    }

    [Test]
    public void Populate()
    {
      using NativeFixedList<int> list = new(length: 3, Allocator.Temp);
      list.Add(MapWidth * 2 + 2);
      list.Add(MapWidth + 1);
      list.Add(0);

      Path path = new();
      path.Populate(in list, MapWidth);

      Assert.IsTrue(path.IsValid);
      Assert.AreEqual(expected: 3, actual: path.Nodes.Count);
      Assert.AreEqual(expected: new Path.Node(0, 0), actual: path.FirstNode);
      Assert.AreEqual(expected: new Path.Node(2, 2), actual: path.LastNode);
    }

    [Test]
    public void PopulateClearsExistingPath()
    {
      using NativeFixedList<int> listA = new(length: 3, Allocator.Temp);
      using NativeFixedList<int> listB = new(length: 2, Allocator.Temp);

      listA.Add(MapWidth * 2 + 2);
      listA.Add(MapWidth + 1);
      listA.Add(0);

      listB.Add(6);
      listB.Add(5);

      Path path = new();
      path.Populate(in listA, MapWidth);
      Assert.AreEqual(expected: 3, actual: path.Nodes.Count);

      path.Populate(in listB, MapWidth);

      Assert.IsTrue(path.IsValid);
      Assert.AreEqual(expected: 2, actual: path.Nodes.Count);
      Assert.AreEqual(expected: new Path.Node(5, 0), actual: path.FirstNode);
      Assert.AreEqual(expected: new Path.Node(6, 0), actual: path.LastNode);
    }

    [Test]
    public void ValidateInt3()
    {
      using NativeFixedList<int> list = new(length: 3, Allocator.Temp);
      list.Add(MapWidth * 2 + 2);
      list.Add(MapWidth + 1);
      list.Add(0);
      
      Path path = new();
      path.Populate(in list, MapWidth);

      path.Validate(path.FirstNode.ToInt3(), path.LastNode.ToInt3());
      Assert.IsTrue(path.IsValid);

      path.Validate(new int3(9, 0, 9), path.LastNode.ToInt3());
      Assert.IsFalse(path.IsValid);
    }

    [Test]
    public void ValidatePathNode()
    {
      using NativeFixedList<int> list = new(length: 3, Allocator.Temp);
      list.Add(MapWidth * 2 + 2);
      list.Add(MapWidth + 1);
      list.Add(0);

      Path path = new();
      path.Populate(in list, MapWidth);
      path.Validate(path.FirstNode, path.LastNode);
      Assert.IsTrue(path.IsValid);

      path.Validate(new Path.Node(8, 8), path.LastNode);
      Assert.IsFalse(path.IsValid);
    }
  }
}