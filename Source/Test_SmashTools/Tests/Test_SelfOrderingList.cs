using System;
using DevTools.Testing;
using UnityEngine.Assertions;

namespace SmashTools.Testing;

[TestFixture(TestType.MainMenu)]
[TestCategory(TestCategoryNames.Utils)]
[TestDescription(
  "Self ordering list for bumping items to the front for less frequent worst case reads.")]
internal class Test_SelfOrderingList
{
  [Test]
  private void NullEnumerableConstructor()
  {
    Expect.Throws<ArgumentNullException>(() => _ = new SelfOrderingList<int>(null));
  }

  [Test]
  private void AddOrderPreserved()
  {
    SelfOrderingList<string> list = [];
    list.Add("first");
    list.Add("second");
    list.Add("third");

    Expect.AreEqual(list.Count, 3);
    Expect.AreEqual(list[0], "first");
    Expect.AreEqual(list[1], "second");
    Expect.AreEqual(list[2], "third");
  }

  [Test]
  private void IndexOutOfRange()
  {
    SelfOrderingList<int> list = [1, 2, 3];
    Expect.Throws<IndexOutOfRangeException>(() => _ = list[-1]);
    Expect.Throws<IndexOutOfRangeException>(() => _ = list[3]);
  }

  [Test]
  private void TryGrabNotFound()
  {
    SelfOrderingList<int> list = [1, 2, 3];
    bool found = list.TryGrab(4, out int result);
    Expect.IsFalse(found);
    Expect.AreEqual(result, 0);
  }

  [Test]
  private void GrabBumps()
  {
    SelfOrderingList<string> list = ["a", "b", "c"];

    // b once, c twice
    Assert.AreEqual(list.Grab("b"), "b");
    Assert.AreEqual(list.Grab("c"), "c");
    Assert.AreEqual(list.Grab("c"), "c");

    // Expected order: c(2), b(1), a(0)
    Expect.AreEqual(list[0], "c");
    Expect.AreEqual(list[1], "b");
    Expect.AreEqual(list[2], "a");
  }

  [Test]
  private void TouchBumps()
  {
    SelfOrderingList<string> list = ["a", "b", "c"];

    // c sets counter = 1, bump up 1 slot
    list.Touch(2);

    Expect.AreEqual(list[0], "a");
    Expect.AreEqual(list[1], "c");
    Expect.AreEqual(list[2], "b");

    // c increments again, bump 1 more time
    list.Touch(1);

    Expect.AreEqual(list[0], "c");
    Expect.AreEqual(list[1], "a");
    Expect.AreEqual(list[2], "b");
  }

  [Test]
  private void InsertRange()
  {
    SelfOrderingList<int> list = [1, 4];
    list.InsertRange(1, [2, 3]);

    // Expected = 1,2,3,4
    Assert.AreEqual(list.Count, 4);
    for (int i = 0; i < 4; i++)
      Expect.AreEqual(list[i], i + 1);
  }

  [Test]
  private void InsertRangeInvalid()
  {
    SelfOrderingList<int> list = [];
    Expect.Throws<ArgumentNullException>(() => list.InsertRange(0, null));
    Expect.Throws<IndexOutOfRangeException>(() => list.InsertRange(-1, [1]));
    Expect.Throws<IndexOutOfRangeException>(() => list.InsertRange(1, [1]));
  }

  [Test]
  private void RemoveAt()
  {
    SelfOrderingList<string> list = ["a", "b", "c"];
    list.RemoveAt(1);

    Expect.AreEqual(list.Count, 2);
    Expect.AreEqual(list[0], "a");
    Expect.AreEqual(list[1], "c");
  }

  [Test]
  private void Clear()
  {
    SelfOrderingList<int> list = [1, 2, 3];
    list.Clear();
    Assert.AreEqual(list.Count, 0);

    list.Add(4);
    Assert.AreEqual(list.Count, 1);
    Expect.AreEqual(list[0], 4);
  }
}