using System;
using System.Collections.Generic;
using DevTools.Testing;
using SmashTools.Rendering;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace SmashTools.Testing;

[TestFixture(TestType.MainMenu)]
[TestCategory(TestCategoryNames.Utils, TestCategoryNames.Rendering)]
[TestDescription(
  "LineRenderer generator for baking line vertices into a single mesh for efficient rendering of complex line strips.")]
internal class Test_LineRenderer
{
  [Test]
  private void BakeMesh()
  {
    const int SegmentCount = 5;
    const int VertexCount = SegmentCount + 1;

    Expect.Throws<ArgumentNullException>(() => _ = Ext_Mesh.BakeLineRendererMesh(null));
    Expect.Throws<ArgumentException>(() => _ = Ext_Mesh.BakeLineRendererMesh([]));

    // ReSharper disable AssignNullToNotNullAttribute
    Expect.Throws<ArgumentNullException>(() =>
      Ext_Mesh.RecalculateLineRenderer(null, []));
    Expect.Throws<ArgumentNullException>(() =>
      Ext_Mesh.RecalculateLineRenderer(null, [new LineSegment(Vector3.zero, Vector3.zero)]));
    // ReSharper restore AssignNullToNotNullAttribute

    Mesh mesh = null;
    try
    {
      List<LineSegment> segments = new(SegmentCount);
      Vector3 curPos = Vector3.zero;
      for (int i = 0; i < SegmentCount; i++)
      {
        Vector3 nextPos = curPos + new Vector3(i, 0, i);
        segments.Add(new LineSegment(curPos, nextPos));
        curPos = nextPos;
      }
      mesh = Ext_Mesh.BakeLineRendererMesh(segments);
      Assert.IsNotNull(mesh);
      Vector3[] vertices = mesh.vertices;
      int[] indices = mesh.GetIndices(0);
      Assert.IsFalse(vertices.NullOrEmpty());
      Assert.IsFalse(indices.NullOrEmpty());
      Expect.AreEqual(vertices.Length, VertexCount);
      Expect.AreEqual(indices.Length, VertexCount);
      Expect.AreEqual(mesh.GetTopology(0), MeshTopology.LineStrip);

      Expect.AreEqual(vertices[0], segments[0].from);
      Expect.AreEqual(indices[0], 0);
      for (int i = 0; i < SegmentCount; i++)
      {
        Assert.IsFalse(vertices.OutOfBounds(i));
        int index = i + 1;
        Expect.AreEqual(vertices[index], segments[i].to);
        Expect.AreEqual(indices[index], index);
      }
    }
    finally
    {
      if (mesh)
        Object.Destroy(mesh);
    }
  }
}