using System.Collections.Generic;
using DevTools.UnitTesting;
using UnityEngine.Assertions;

namespace SmashTools.UnitTesting;

[UnitTest(TestType.MainMenu)]
[TestCategory(TestGroup.Utils)]
[TestDescription("Rolling back values with disposable pattern.")]
internal class UnitTest_ScopedRollback
{
  [Test]
  [TestDescription("Unmanaged value is rolled back.")]
  private void UnmanagedValue()
  {
    int x = 1;
    using (new ScopedValueRollback<int>(ref x))
    {
      x = 5;
      Assert.AreEqual(x, 5);
    }
    Expect.AreEqual(x, 1);
  }

  [Test]
  [TestDescription("Empty list is cleared when rolled back.")]
  private void EmptyList()
  {
    List<int> intList = [];
    using (new ScopedListRollback<int>(intList))
    {
      Assert.AreEqual(intList.Count, 0);
      for (int i = 0; i < 5; i++)
        intList.Add(i);
      Assert.AreEqual(intList.Count, 5);
    }
    Expect.AreEqual(intList.Count, 0);
  }

  [Test]
  [TestDescription("Order of items is preserved when list is rolled back.")]
  private void PopulatedListReordered()
  {
    List<int> intList = [1, 2, 3, 4, 5];
    using (new ScopedListRollback<int>(intList))
    {
      Assert.AreEqual(intList.Count, 5);
      Assert.IsTrue(intList[0] == 1 && intList[^1] == 5);
      intList.Reverse();
      Assert.IsTrue(intList[0] == 5 && intList[^1] == 1);
    }
    Expect.IsTrue(intList[0] == 1 && intList[^1] == 5);
  }

  [Test]
  [TestDescription("Non-empty list is repopulated when rolled back.")]
  private void PopulatedListCleared()
  {
    List<int> intList2 = [1, 2, 3, 4, 5];
    using (new ScopedListRollback<int>(intList2))
    {
      Assert.AreEqual(intList2.Count, 5);
      intList2.Clear();
      Assert.AreEqual(intList2.Count, 0);
    }
    Expect.AreEqual(intList2.Count, 5);
  }

  [Test]
  [TestDescription(
    "Shallow copy of list contents is made. When list is rolled back, shallow copy of objects are readded to the list.")]
  private void PopulatedListReferences()
  {
    TestObject obj = new();
    int id = obj.id;
    List<TestObject> objList = [obj];
    using (new ScopedListRollback<TestObject>(objList))
    {
      Assert.AreEqual(objList.Count, 1);
      Assert.IsTrue(ReferenceEquals(objList[0], obj));
      objList.Clear();
      objList.Add(new TestObject());
      Expect.IsFalse(ReferenceEquals(objList[0], obj));
    }
    Assert.AreEqual(objList.Count, 1);
    Expect.IsTrue(ReferenceEquals(objList[0], obj));
    Expect.AreEqual(obj.id, id);
  }

  private class TestObject
  {
    private static int nextId;

    public readonly int id;

    public TestObject()
    {
      id = nextId++;
    }
  }
}