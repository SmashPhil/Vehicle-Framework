using System.Collections.Generic;
using DevTools.Testing;
using UnityEngine.Assertions;

namespace CoreLib.Testing;

[TestFixture(TestType.MainMenu)]
[TestCategory(TestCategoryNames.Utils)]
[TestDescription("Rolling back values with disposable pattern.")]
internal class Test_ScopedRollback
{
  [Test]
  [TestDescription("Unmanaged value is rolled back.")]
  private void UnmanagedValue()
  {
    int x = 1;
    using (new ScopedRollback<int>(ref x))
    {
      x = 5;
      Assert.AreEqual(x, 5);
    }
    Expect.AreEqual(x, 1);
  }

  [Test]
  [TestDescription("Unmanaged value is rolled back.")]
  private void UnmanagedValuePreassign()
  {
    int x = 1;
    using (new ScopedRollback<int>(ref x, 5))
    {
      Assert.AreEqual(x, 5);
    }
    Expect.AreEqual(x, 1);
  }

  [Test]
  [TestDescription("Managed reference is rolled back.")]
  private void ManagedReference()
  {
    object a = new();
    object b = new();
    object obj = a;
    Expect.ReferencesAreEqual(obj, a);
    using (new ScopedRollback<object>(ref obj))
    {
      obj = b;
      Expect.ReferencesAreEqual(obj, b);
    }
    Expect.ReferencesAreEqual(obj, a);
  }

  [Test]
  [TestDescription("Managed reference is rolled back.")]
  private void ManagedReferencePreassign()
  {
    object a = new();
    object b = new();
    object obj = a;
    Expect.ReferencesAreEqual(obj, a);
    using (new ScopedRollback<object>(ref obj, b))
    {
      Expect.ReferencesAreEqual(obj, b);
    }
    Expect.ReferencesAreEqual(obj, a);
  }

  [Test]
  [TestDescription("Empty list is cleared when rolled back.")]
  private void EmptyList()
  {
    List<int> intList = [];
    using (new ClearOnDispose<int>(intList))
    {
      Assert.AreEqual(intList.Count, 0);
      for (int i = 0; i < 5; i++)
        intList.Add(i);
      Assert.AreEqual(intList.Count, 5);
    }
    Expect.AreEqual(intList.Count, 0);
  }

  [Test]
  [TestDescription("Empty list is cleared when rolled back.")]
  private void EmptyHashSet()
  {
    HashSet<int> intSet = [];
    using (new ClearOnDispose<int>(intSet))
    {
      Assert.AreEqual(intSet.Count, 0);
      for (int i = 0; i < 5; i++)
        intSet.Add(i);
      Assert.AreEqual(intSet.Count, 5);
    }
    Expect.AreEqual(intSet.Count, 0);
  }
}