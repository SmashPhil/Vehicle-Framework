using DevTools.Testing;
using Verse;

namespace SmashTools.Testing;

[TestFixture(TestType.MainMenu)]
[TestCategory(TestCategoryNames.Utils)]
[TestDescription("Math extension utils.")]
internal class Test_Numerics
{
  [Test]
  private void ClampAngle()
  {
    // min boundary (inclusive)
    Expect.AreApproximatelyEqual(0f.ClampAngle(), 0);
    Expect.AreApproximatelyEqual(0.0001f.ClampAngle(), 0.0001f);

    // below min
    Expect.AreApproximatelyEqual((-360f).ClampAngle(), 0);
    Expect.AreApproximatelyEqual((-45f).ClampAngle(), 315);
    Expect.AreApproximatelyEqual((-0.0001f).ClampAngle(), 359.9999f);

    // max boundary (exclusive)
    Expect.AreApproximatelyEqual(360f.ClampAngle(), 0);
    Expect.AreApproximatelyEqual(359.9999f.ClampAngle(), 359.9999f);

    // above max
    Expect.AreApproximatelyEqual(450f.ClampAngle(), 90);
    Expect.AreApproximatelyEqual(360.0001f.ClampAngle(), 0.0001f);

    // above max, double
    Expect.AreApproximatelyEqual(720f.ClampAngle(), 0);
  }

  [Test]
  private void InRangeFloat()
  {
    FloatRange range = new(0f, 10f);

    // Boundaries (inclusive)
    Expect.IsTrue(range.InRange(0f));
    Expect.IsTrue(range.InRange(10f));

    // In range
    Expect.IsTrue(range.InRange(2f));
    Expect.IsTrue(range.InRange(5f));
    Expect.IsTrue(range.InRange(7f));
    Expect.IsTrue(range.InRange(2.1235f));

    // Out of bounds
    Expect.IsFalse(range.InRange(15));
    Expect.IsFalse(range.InRange(1658435));
    Expect.IsFalse(range.InRange(-1658435));
    Expect.IsFalse(range.InRange(-15));

    // Just out of bounds
    Expect.IsFalse(range.InRange(-0.0001f));
    Expect.IsFalse(range.InRange(10.0001f));

    // Edging
    Expect.IsTrue(range.InRange(0.000001f));
    Expect.IsTrue(range.InRange(9.999999f));
  }

  [Test]
  private void InRangeInt()
  {
    IntRange range = new(0, 10);

    // Boundaries (inclusive)
    Expect.IsTrue(range.InRange(0));
    Expect.IsTrue(range.InRange(10));

    // In range
    Expect.IsTrue(range.InRange(2));
    Expect.IsTrue(range.InRange(5));
    Expect.IsTrue(range.InRange(7));

    // Out of bounds
    Expect.IsFalse(range.InRange(15));
    Expect.IsFalse(range.InRange(1658435));
    Expect.IsFalse(range.InRange(-1658435));
    Expect.IsFalse(range.InRange(-15));

    // Just out of bounds
    Expect.IsFalse(range.InRange(-1));
    Expect.IsFalse(range.InRange(11));

    // Edging
    Expect.IsTrue(range.InRange(1));
    Expect.IsTrue(range.InRange(9));
  }
}