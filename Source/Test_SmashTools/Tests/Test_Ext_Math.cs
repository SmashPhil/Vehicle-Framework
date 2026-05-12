using System;
using DevTools.Testing;
using UnityEngine.Assertions;

namespace SmashTools.Testing;

[TestFixture(TestType.MainMenu)]
[TestCategory(TestCategoryNames.Utils)]
[TestDescription("Math utility methods")]
internal class Test_Ext_Math
{
  [TestCase(5, 0, 1f)]
  [TestCase(5, 5, 1f)]
  [TestCase(5, 2, 10)]
  [TestCase(6, 3, 20)]
  private void Binomial(int n, int i, float result)
  {
    Expect.AreApproximatelyEqual(Ext_Math.Binomial(n, i), result);
  }

  [Test]
  private void Bernstein([Parameters(0, 0.25f, 0.5f, 0.75f, 1f)] float t)
  {
    const int N = 3;
    float sum = 0f;
    for (int i = 0; i <= N; i++)
    {
      sum += Ext_Math.Bernstein(N, i, t);
    }
    // Sums to 1
    Assert.AreApproximatelyEqual(1f, sum, 1e-5f);
  }

  [TestCase(true, ExpectedResult = 1)]
  [TestCase(false, ExpectedResult = -1)]
  private int Sign_Bool(bool boolean)
  {
    return Ext_Math.Sign(boolean);
  }

  [Test]
  private void IsOddOrEven()
  {
    Expect.IsTrue(3.IsOdd());
    Expect.IsFalse(3.IsEven());
    Expect.IsFalse(4.IsOdd());
    Expect.IsTrue(4.IsEven());
    Expect.IsTrue(0.IsEven());
  }

  [TestCase(7f, 5f, 15f, 0.2f)] // (7 - 5) / (15 - 5) = 0.2
  [TestCase(3f, -1f, 15f, 0.25f)] // descending interval yields negative t
  private void ReverseInterpolate(float value, float a, float b, float result)
  {
    const float Tolerance = 1e-6f;
    Assert.AreApproximatelyEqual(result, Ext_Math.ReverseInterpolate(value, a, b), Tolerance);
  }

  [TestCase(2, 6, 8, 14)] // 6, 8
  [TestCase(3, 6, 8, 21)] // 6, 7, 8
  [TestCase(3, 30, 10, 60)] // 30, 20, 10
  [TestCase(4, 10, 20, 60)] // decimal ... 10 + 13 + 17 + 20
  [TestCase(0, 123, 456, 0)] // blank
  private void ArithmeticSeries(int n, int a, int k, int result)
  {
    Expect.AreEqual(result, Ext_Math.ArithmeticSeries(n, a, k));
  }

  [Test]
  private void ArithmeticSeriesInvalid()
  {
    Expect.Throws<InvalidOperationException>(() => Ext_Math.ArithmeticSeries(-1, 123, 456));
  }

  [TestCase(3.14159f, 0.01f, 3.14f)]
  [TestCase(3.14159f, 1, 3)]
  [TestCase(-3.14159f, 0.01f, -3.14f)]
  private void RoundToFloat(float x, float roundTo, float result)
  {
    Expect.AreApproximatelyEqual(x.RoundTo(roundTo), result);
    
  }

  [Test]
  private void RoundToFloatZero()
  {
    Expect.Throws<InvalidOperationException>(() => 3.14159f.RoundTo(0));
  }

  [TestCase(13, 5, 15)]
  [TestCase(15, 5, 15)]
  [TestCase(-3, 5, -5)]
  private void RoundToInt(int x, int roundTo, int result)
  {
    Expect.AreEqual(x.RoundTo(roundTo), result);
  }

  [Test]
  private void RoundToIntZero()
  {
    Expect.Throws<InvalidOperationException>(() => 12.RoundTo(0));
  }

  [Test]
  private void PowTwo()
  {
    Expect.AreEqual(Ext_Math.PowTwo(0), 1);
    Expect.AreEqual(Ext_Math.PowTwo(3), 8);
    Expect.AreEqual(Ext_Math.PowTwo(4), 16);
  }

  [TestCase(2, 3, 8)]
  [TestCase(5, 4, 625)]
  [TestCase(2, 0, 1)]
  private void Pow(int x, int y, long result)
  {
    Expect.AreEqual(x.Pow(y), result);
  }

  [TestCase(10, 3, 3, 7)]   // take less than
  [TestCase(10, 10, 10, 0)] // take equal amount
  [TestCase(10, 12, 10, 0)] // take greater than
  private void Take(int value, int take, int remainder, int result)
  {
    Expect.AreEqual(value.Take(take, out int remaining), remainder);
    Expect.AreEqual(result, remaining);
  }
}