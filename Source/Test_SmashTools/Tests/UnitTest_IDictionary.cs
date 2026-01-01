using System;
using System.Collections.Generic;
using DevTools.Testing;
using UnityEngine.Assertions;

// ReSharper disable AccessToModifiedClosure
namespace SmashTools.UnitTesting;

[UnitTest(TestType.MainMenu)]
[TestCategory(TestCategoryNames.Utils)]
[TestDescription("IDictionary extension utils.")]
internal class UnitTest_IDictionary
{
  private const int AddOrAppendKey = 1;

  [Test]
  private void AddOrAppendList()
  {
    const int OtherKey = 2;
    const string TestInput1 = "Hello";
    const string TestInput2 = "World";

    // Args
    Dictionary<string, List<int>> invalidDict = null;
    Expect.Throws<ArgumentNullException>(() => invalidDict.AddOrAppend("NullDict", 1));
    invalidDict = [];
    Expect.Throws<ArgumentNullException>(() => invalidDict.AddOrAppend(null, 1));


    Dictionary<int, List<string>> dict = [];
    // Create new list
    dict.AddOrAppend(AddOrAppendKey, TestInput1);
    Assert.IsTrue(dict.ContainsKey(1));
    Assert.IsNotNull(dict[AddOrAppendKey]);
    Expect.AreEqual(dict[AddOrAppendKey].Count, 1);
    Expect.AreEqual(dict[AddOrAppendKey][0], TestInput1);

    // Append to existing list6
    dict.AddOrAppend(AddOrAppendKey, TestInput2);
    Expect.AreEqual(dict[AddOrAppendKey].Count, 2);
    Expect.AreEqual(dict[AddOrAppendKey][0], TestInput1);
    Expect.AreEqual(dict[AddOrAppendKey][1], TestInput2);

    const string OtherValue = "Test";

    // Separate key
    dict.AddOrAppend(OtherKey, OtherValue);
    Assert.IsTrue(dict.ContainsKey(OtherKey));
    Expect.AreEqual(dict.Count, 2);
    Expect.AreEqual(dict[OtherKey].Count, 1);
    Expect.AreEqual(dict[OtherKey][0], OtherValue);
  }

  [Test]
  private void AddOrAppendHash()
  {
    const string HashInput1 = "Test";
    const string HashInput2 = "Duplicate";

    Dictionary<int, HashSet<string>> dictHash = [];
    dictHash.AddOrAppend(AddOrAppendKey, HashInput1);
    dictHash.AddOrAppend(AddOrAppendKey, HashInput1);
    dictHash.AddOrAppend(AddOrAppendKey, HashInput2);
    Assert.IsTrue(dictHash.ContainsKey(AddOrAppendKey));
    Assert.IsNotNull(dictHash[AddOrAppendKey]);
    Expect.IsTrue(dictHash[AddOrAppendKey].Contains(HashInput1));
    Expect.IsTrue(dictHash[AddOrAppendKey].Contains(HashInput2));
    Expect.AreEqual(dictHash.Count, 1);
  }

  [Test]
  private void AddOrAppendLinkedList()
  {
    const string HashInput1 = "Link";
    const string HashInput2 = "List";

    Dictionary<int, LinkedList<string>> dictLinkList = [];
    dictLinkList.AddOrAppend(AddOrAppendKey, HashInput1);
    dictLinkList.AddOrAppend(AddOrAppendKey, HashInput2);
    Assert.IsTrue(dictLinkList.ContainsKey(AddOrAppendKey));
    Assert.IsNotNull(dictLinkList[AddOrAppendKey]);
    Expect.IsTrue(dictLinkList[AddOrAppendKey].Contains(HashInput1));
    Expect.IsTrue(dictLinkList[AddOrAppendKey].Contains(HashInput2));
    Expect.AreEqual(dictLinkList.Count, 1);
    Expect.AreEqual(dictLinkList[AddOrAppendKey].First.Value, HashInput1);
    Assert.IsNotNull(dictLinkList[AddOrAppendKey].First.Next);
    Expect.AreEqual(dictLinkList[AddOrAppendKey].First.Next.Value, HashInput2);
  }
}