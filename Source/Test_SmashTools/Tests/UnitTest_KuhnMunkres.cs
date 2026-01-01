using DevTools.Testing;
using SmashTools.Algorithms;
using UnityEngine.Assertions;

namespace SmashTools.UnitTesting;

[UnitTest(TestType.MainMenu)]
[TestDescription("Hungarian algorithm implementation.")]
internal class UnitTest_KuhnMunkres
{
  private const int MatrixSize = 3;

  private readonly KuhnMunkres kuhnMunkres = new(MatrixSize);

  [Test]
  private void UniqueMin()
  {
    float[,] costMatrix = new float[,]
    {
      { 10, 19, 8 },
      { 10, 18, 7 },
      { 13, 16, 9 }
    };
    int[] expected = [0, 2, 1];
    int[] actual = kuhnMunkres.Compute(costMatrix);
    ValidateResults(costMatrix, expected, actual);
  }

  [Test]
  private void UniqueMin2()
  {
    float[,] costMatrix = new float[,]
    {
      { 9, 11, 14 },
      { 6, 15, 13 },
      { 12, 13, 6 }
    };
    int[] expected = [1, 0, 2];
    int[] actual = kuhnMunkres.Compute(costMatrix);
    ValidateResults(costMatrix, expected, actual);
  }

  [Test]
  private void Symmetric()
  {
    // Actual indices do not matter, all that needs validation is that
    // the total cost adds up to the only possible solution.
    float[,] costMatrix = new float[,]
    {
      { 5, 5, 5 },
      { 5, 5, 5 },
      { 5, 5, 5 }
    };
    int[] expected = [0, 1, 2];
    int[] actual = kuhnMunkres.Compute(costMatrix);
    ValidateResults(costMatrix, expected, actual);
  }

  [Test]
  private void DiagonalDown()
  {
    float[,] costMatrix = new float[,]
    {
      { 1, 11, 14 },
      { 6, 1, 13 },
      { 12, 13, 1 }
    };
    int[] expected = [0, 1, 2];
    int[] actual = kuhnMunkres.Compute(costMatrix);
    ValidateResults(costMatrix, expected, actual);
  }

  [Test]
  private void DiagonalUp()
  {
    float[,] costMatrix = new float[,]
    {
      { 12, 11, 1 },
      { 6, 1, 13 },
      { 1, 13, 14 }
    };
    int[] expected = [2, 1, 0];
    int[] actual = kuhnMunkres.Compute(costMatrix);
    ValidateResults(costMatrix, expected, actual);
  }

  [Test]
  private void Ordered()
  {
    // All possible solutions are equally valid, we just need to verify that having
    // multiple correct solutions still chooses one correctly.
    float[,] costMatrix = new float[,]
    {
      { 1, 2, 3 },
      { 4, 5, 6 },
      { 7, 8, 9 }
    };
    int[] expected = [0, 1, 2];
    int[] actual = kuhnMunkres.Compute(costMatrix);
    ValidateResults(costMatrix, expected, actual);
  }

  [Test]
  private void Reverse()
  {
    // All possible solutions are equally valid, we need to verify that having
    // multiple correct solutions still chooses one correctly.
    float[,] costMatrix = new float[,]
    {
      { 3, 2, 1 },
      { 6, 5, 4 },
      { 9, 8, 7 }
    };
    int[] expected = [0, 1, 2];
    int[] actual = kuhnMunkres.Compute(costMatrix);
    ValidateResults(costMatrix, expected, actual);
  }

  private static void ValidateResults(float[,] costMatrix, int[] expected, int[] actual)
  {
    Assert.AreEqual(expected.Length, actual.Length);

    float expectedCost = 0;
    for (int i = 0; i < expected.Length; i++)
      expectedCost += costMatrix[i, expected[i]];

    float cost = 0;
    for (int i = 0; i < actual.Length; i++)
      cost += costMatrix[i, actual[i]];
    Expect.AreApproximatelyEqual(cost, expectedCost);
  }
}