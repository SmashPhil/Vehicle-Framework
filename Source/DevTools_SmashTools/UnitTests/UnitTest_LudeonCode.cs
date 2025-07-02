using System;
using DevTools.UnitTesting;
using Verse;

// ReSharper disable all
namespace SmashTools.UnitTesting;

[UnitTest(TestType.MainMenu)]
[Disabled]
public sealed class UnitTest_LudeonCode
{
  [Test]
  private void DivideByZero()
  {
    Expect.Throws<DivideByZeroException>(delegate
    {
      int n = 1;
      int d = 0;
      int ex = n / d;
    });

    float infinity = 1f / 0f;
    Expect.IsTrue(float.IsPositiveInfinity(infinity));
    double alsoInfinity = 1d / 0d;
    Expect.IsTrue(double.IsPositiveInfinity(alsoInfinity));

    Expect.Throws<DivideByZeroException>(delegate
    {
      decimal n = 1;
      decimal d = 0;
      decimal ex = n / d;
    });
  }

  [Test]
  private void Overflow()
  {
    int n = int.MaxValue;
    n++;
    Expect.AreEqual(n, int.MinValue);

    unchecked
    {
      int un = int.MaxValue;
      un++;
      Expect.AreEqual(un, int.MinValue);
    }

    ulong ul = ulong.MaxValue;
    ul++;
    Expect.AreEqual(ul, 0u);

    unchecked
    {
      ulong un = ulong.MaxValue;
      long cl = (long)un;
      Expect.IsTrue(cl < 0);
    }
  }

  [Test]
  public void ParseHelperAction()
  {
    // Format is Type::Name
    Expect.Throws<NullReferenceException>(() => ParseHelper.ParseAction("ParseHelperAction"));
    // This next line throws because if no namespace is included, the type fails to resolve. Additionally you can ONLY
    // include 1 namespace name, any extended namespace will throw.
    Expect.Throws<NullReferenceException>(() =>
      ParseHelper.ParseAction("UnitTest_LudeonCode.ParseHelperAction"));
    Expect.Throws<NullReferenceException>(() =>
      ParseHelper.ParseAction("SmashTools.UnitTest_LudeonCode.ParseHelperAction"));
    Expect.Throws<NullReferenceException>(() =>
      ParseHelper.ParseAction("SmashTools.UnitTesting.UnitTest_LudeonCode.ParseHelperAction"));
    Expect.Throws<NullReferenceException>(() =>
      ParseHelper.ParseAction("12+1?l8`.+InvalidMethodName"));
  }
}