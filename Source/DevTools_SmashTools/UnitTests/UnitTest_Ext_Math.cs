using System;
using DevTools.Testing;
using UnityEngine.Assertions;

namespace SmashTools.UnitTesting;

[UnitTest(TestType.MainMenu)]
[TestCategory(TestCategoryNames.Utils)]
[TestDescription("Math utility methods")]
internal class UnitTest_Ext_Math
{
	[Test]
	private void Binomial()
	{
		Expect.AreEqual(Ext_Math.Binomial(5, 0), 1f);
		Expect.AreEqual(Ext_Math.Binomial(5, 5), 1f);
		Expect.AreEqual(Ext_Math.Binomial(5, 2), 10f);
		Expect.AreEqual(Ext_Math.Binomial(6, 3), 20f);
	}

	[Test]
	private void Bernstein()
	{
		// Sum to 1
		const int N = 3;
		float[] ts = [0f, 0.25f, 0.5f, 0.75f, 1f];
		foreach (float t in ts)
		{
			float sum = 0f;
			for (int i = 0; i <= N; i++)
				sum += Ext_Math.Bernstein(N, i, t);
			Assert.AreApproximatelyEqual(1f, sum, 1e-5f);
		}
	}

	[Test]
	private void Sign_Bool()
	{
		Expect.AreEqual(Ext_Math.Sign(true), 1);
		Expect.AreEqual(Ext_Math.Sign(false), -1);
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

	[Test]
	private void ReverseInterpolate()
	{
		// (7 - 5) / (15 - 5) = 0.2
		Assert.AreApproximatelyEqual(0.2f, Ext_Math.ReverseInterpolate(7f, 5f, 15f), 1e-6f);
		// descending interval yields negative t
		Assert.AreApproximatelyEqual(0.25f, Ext_Math.ReverseInterpolate(3f, -1f, 15f), 1e-6f);
	}

	[Test]
	private void ArithmeticSeries()
	{
		// 6, 8
		Expect.AreEqual(Ext_Math.ArithmeticSeries(2, 6, 8), 14);
		// 6, 7, 8
		Expect.AreEqual(Ext_Math.ArithmeticSeries(3, 6, 8), 21);
		// 30, 20, 10
		Expect.AreEqual(Ext_Math.ArithmeticSeries(3, 30, 10), 60);
		// decimal ... 10 + 13 + 17 + 20
		Expect.AreEqual(Ext_Math.ArithmeticSeries(4, 10, 20), 60);
		// blank
		Expect.AreEqual(Ext_Math.ArithmeticSeries(0, 123, 456), 0);
		// invalid
		Expect.Throws<InvalidOperationException>(() => Ext_Math.ArithmeticSeries(-1, 123, 456));
	}

	[Test]
	private void RoundTo_Float()
	{
		Expect.AreApproximatelyEqual(3.14159f.RoundTo(0.01f), 3.14f);
		Expect.AreApproximatelyEqual(3.14159f.RoundTo(1), 3);
		Expect.AreApproximatelyEqual(-3.14159f.RoundTo(0.01f), -3.14f);
		Expect.Throws<InvalidOperationException>(() => 3.14159f.RoundTo(0));
	}

	[Test]
	private void RoundTo_Int()
	{
		Expect.AreEqual(13.RoundTo(5), 15);
		Expect.AreEqual(15.RoundTo(5), 15);
		Expect.AreEqual(-3.RoundTo(5), -5);
		Expect.Throws<InvalidOperationException>(() => 12.RoundTo(0));
	}

	[Test]
	private void PowTwo()
	{
		Expect.AreEqual(Ext_Math.PowTwo(0), 1);
		Expect.AreEqual(Ext_Math.PowTwo(3), 8);
		Expect.AreEqual(Ext_Math.PowTwo(4), 16);
	}

	[Test]
	private void Pow()
	{
		Expect.AreEqual(5.Pow(4), 625L);
	}

	private void Take()
	{
		Expect.AreEqual(10.Take(3, out int remaining), 3);
		Expect.AreEqual(remaining, 7);

		// take equal to value
		Expect.AreEqual(10.Take(10, out remaining), 10);
		Expect.AreEqual(remaining, 0);

		// take greater than value
		Expect.AreEqual(10.Take(12, out remaining), 10);
		Expect.AreEqual(remaining, 0);
	}
}