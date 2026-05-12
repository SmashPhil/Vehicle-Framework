using System.Collections.Generic;
using CoreLib.Collections;
using DevTools.Testing;
using UnityEngine.Assertions;

namespace SmashTools.Testing;

[TestFixture(TestType.MainMenu)]
[TestDescription("IEnumerable extensions that build off linq and other similar utility methods.")]
internal class Test_ExtEnumerable
{
  [Test]
  private void SelectManyAndFlatten()
  {
    // items
    // ├─ item1
    // │  ├─ item1Child1
    // │  │  ├─ item1Child1Child1
    // │  │  └─ item1Child1Child2
    // │  └─ item1Child2
    // │     └─ item1Child2Child1
    // ├─ item2
    // │  └─ item2Child1
    // └─ item3
    MockItem item1 = new();
    MockItem item1Child1 = new();
    MockItem item1Child1Child1 = new();
    MockItem item1Child1Child2 = new();
    MockItem item1Child2 = new();
    MockItem item1Child2Child1 = new();

    item1Child1.Children.Add(item1Child1Child1);
    item1Child1.Children.Add(item1Child1Child2);
    item1Child2.Children.Add(item1Child2Child1);
    item1.Children.Add(item1Child1);
    item1.Children.Add(item1Child2);

    MockItem item2 = new();
    MockItem item2Child1 = new();
    item2.Children.Add(item2Child1);

    MockItem item3 = new();

    List<MockItem> items = [item1, item2, item3];
    List<MockItem> flattened = [.. items.SelectManyAndFlatten(item => item.Children)];

    Assert.AreEqual(9, flattened.Count);

    Expect.ReferencesAreEqual(item1, flattened[0]);
    Expect.ReferencesAreEqual(item1Child1, flattened[1]);
    Expect.ReferencesAreEqual(item1Child1Child1, flattened[2]);
    Expect.ReferencesAreEqual(item1Child1Child2, flattened[3]);
    Expect.ReferencesAreEqual(item1Child2, flattened[4]);
    Expect.ReferencesAreEqual(item1Child2Child1, flattened[5]);
    Expect.ReferencesAreEqual(item2, flattened[6]);
    Expect.ReferencesAreEqual(item2Child1, flattened[7]);
    Expect.ReferencesAreEqual(item3, flattened[8]);
  }

  private class MockItem
  {
    public List<MockItem> Children { get; } = [];
  }
}