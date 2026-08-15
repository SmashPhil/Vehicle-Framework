using System;
using CoreLib;
using DevTools.Testing;
using UnityEngine;
using static CoreLib.ShuntingYard<SmashTools.Testing.Test_ShuntingYard.OperatorType, int>;

namespace SmashTools.Testing;

[Disabled]
[TestFixture(TestType.MainMenu)]
[TestDescription("ShuntingYard algorithm for string input and evaluation with generic operators.")]
internal class Test_ShuntingYard
{
  
  [TestCase("1++", ExpectedResult = 2)]
  [TestCase("++1", ExpectedResult = 2)]
  [TestCase("++5 * 2", ExpectedResult = 12)]
  [TestCase("5++ * 2", ExpectedResult = 11)]
  private int UnaryMath(string expression)
  {
    ShuntingYard<OperatorType, int> shuntingYard = new(int.Parse, evalUnary: EvaluateUnary);
    shuntingYard.AddUnaryOperator("!", OperatorType.Not, Unary.Prefix);
    shuntingYard.AddUnaryOperator("++", OperatorType.Or, Unary.Prefix | Unary.Postfix);
    shuntingYard.AddUnaryOperator("--", OperatorType.Or, Unary.Prefix | Unary.Postfix);
    shuntingYard.AddBinaryOperator("*", OperatorType.Multiply);
    return shuntingYard.Evaluate(expression);
  }

  [TestCase("!true", ExpectedResult = 0)]
  [TestCase("!false", ExpectedResult = 1)]
  [TestCase("!!true", ExpectedResult = 1)]
  private int UnaryPrefix(string expression)
  {
    ShuntingYard<OperatorType, int> shuntingYard = new(ParseBooleanToInt, evalUnary: EvaluateUnary);
    shuntingYard.AddUnaryOperator("!", OperatorType.Not, Unary.Prefix);
    return shuntingYard.Evaluate(expression);
  }

  [TestCase("true?", ExpectedResult = 0)]
  [TestCase("false?", ExpectedResult = 1)]
  [TestCase("true??", ExpectedResult = 1)]
  private int UnaryPostfix(string expression)
  {
    ShuntingYard<OperatorType, int> shuntingYard = new(ParseBooleanToInt, evalUnary: EvaluateUnary);
    shuntingYard.AddUnaryOperator("?", OperatorType.Not, Unary.Postfix);
    return shuntingYard.Evaluate(expression);
  }

  [TestCase("false || false", ExpectedResult = 0)]
  [TestCase("true || false", ExpectedResult = 1)]
  [TestCase("true && false", ExpectedResult = 0)]
  [TestCase("true && true", ExpectedResult = 1)]
  private int Boolean(string expression)
  {
    ShuntingYard<OperatorType, int> shuntingYard = new(ParseBooleanToInt, evalBinary: EvaluateBinary);
    shuntingYard.AddBinaryOperator("&&", OperatorType.And);
    shuntingYard.AddBinaryOperator("||", OperatorType.Or);
    return shuntingYard.Evaluate(expression);
  }

  [TestCase("7 + 5", ExpectedResult = 12)]
  [TestCase("1 + 2", ExpectedResult = 3)]
  [TestCase("1 + 2 + 3", ExpectedResult = 6)]
  private int Addition(string expression)
  {
    ShuntingYard<OperatorType, int> shuntingYard = new(int.Parse, evalBinary: EvaluateBinary);
    shuntingYard.AddBinaryOperator("+", OperatorType.Plus);
    return shuntingYard.Evaluate(expression);
  }

  [TestCase("+7 + -5", ExpectedResult = 2)]
  [TestCase("-5 + +7", ExpectedResult = 2)]
  [TestCase("-1 + -3", ExpectedResult = -4)]
  [TestCase("-0 + +2", ExpectedResult = 2)]
  private int AdditionWithPrefixes(string expression)
  {
    ShuntingYard<OperatorType, int> shuntingYard = new(int.Parse, evalBinary: EvaluateBinary);
    shuntingYard.AddBinaryOperator("+", OperatorType.Plus);
    return shuntingYard.Evaluate(expression);
  }

  [TestCase("5 - 7", ExpectedResult = -2)]
  [TestCase("2 - 1", ExpectedResult = 1)]
  [TestCase("2 - 0", ExpectedResult = 2)]
  private int Subtraction(string expression)
  {
    ShuntingYard<OperatorType, int> shuntingYard = new(int.Parse, evalBinary: EvaluateBinary);
    shuntingYard.AddBinaryOperator("-", OperatorType.Minus);
    return shuntingYard.Evaluate(expression);
  }

  [TestCase("-1 - +3", ExpectedResult = -4)]
  [TestCase("+1 - -3", ExpectedResult = 4)]
  [TestCase("+1 - +3", ExpectedResult = -2)]
  [TestCase("+0 - -3", ExpectedResult = 3)]
  private int SubtractionWithPrefixes(string expression)
  {
    ShuntingYard<OperatorType, int> shuntingYard = new(int.Parse, evalBinary: EvaluateBinary);
    shuntingYard.AddBinaryOperator("-", OperatorType.Minus);
    return shuntingYard.Evaluate(expression);
  }

  [TestCase("1 * 2", ExpectedResult = 2)]
  [TestCase("-1 * 3", ExpectedResult = -3)]
  [TestCase("1 * -3", ExpectedResult = -3)]
  [TestCase("-2 * -3", ExpectedResult = 6)]
  private int Multiplication(string expression)
  {
    ShuntingYard<OperatorType, int> shuntingYard = new(int.Parse, evalBinary: EvaluateBinary);
    shuntingYard.AddBinaryOperator("*", OperatorType.Multiply);
    return shuntingYard.Evaluate(expression);
  }

  [TestCase("1 / 2", ExpectedResult = 0.5f)]
  [TestCase("-1 / 3", ExpectedResult = -0.333333f)]
  [TestCase("1 / -3", ExpectedResult = -0.333333f)]
  [TestCase("-2 / -3", ExpectedResult = 0.666667f)]
  private float Division(string expression)
  {
    ShuntingYard<OperatorType, float> shuntingYard = new(float.Parse, evalBinary: EvaluateBinaryF);
    shuntingYard.AddBinaryOperator("/", OperatorType.Divide);
    return shuntingYard.Evaluate(expression);
  }

  [Test]
  private void DivisionByZero()
  {
    ShuntingYard<OperatorType, int> shuntingYard = new(int.Parse, evalBinary: EvaluateBinary);
    shuntingYard.AddBinaryOperator("/", OperatorType.Divide);
    Expect.Throws<DivideByZeroException>(() => shuntingYard.Evaluate("1 / 0"));
  }

  [TestCase("1 + 2 * 3", ExpectedResult = 7)]
  [TestCase("1 * 2 + 3", ExpectedResult = 5)]
  [TestCase("1 + 2 * 3 - 2", ExpectedResult = 5)]
  [TestCase("5 + 2 * 3 / 2 - 2", ExpectedResult = 6)]
  [TestCase("4 / 2 * 3", ExpectedResult = 6)]
  [TestCase("4 * 2 ^ 3 + 4 - 2", ExpectedResult = 34)]
  private int Precedence(string expression)
  {
    ShuntingYard<OperatorType, int> shuntingYard = new(int.Parse, evalBinary: EvaluateBinary);
    shuntingYard.AddBinaryOperator("+", OperatorType.Plus, precedence: 1);
    shuntingYard.AddBinaryOperator("-", OperatorType.Minus, precedence: 1);
    shuntingYard.AddBinaryOperator("*", OperatorType.Multiply, precedence: 2);
    shuntingYard.AddBinaryOperator("/", OperatorType.Divide, precedence: 2);
    shuntingYard.AddBinaryOperator("^", OperatorType.Exponent, precedence: 3);
    return shuntingYard.Evaluate(expression);
  }

  [TestCase("(1 + 2) * 3", ExpectedResult = 9)]
  [TestCase("(1 * 2 + 3) * 2", ExpectedResult = 10)]
  [TestCase("(1 + 2) - 2", ExpectedResult = 1)]
  [TestCase("2 ^ (2 ^ 3)", ExpectedResult = 256)]
  [TestCase("2 ^ 2 ^ 3", ExpectedResult = 64)]
  [TestCase("((1 + 2) * 2) + 1 * 3", ExpectedResult = 9)]
  [TestCase("((3 * 2) - (1 + 1)) / 2", ExpectedResult = 2)]
  private int Parenthesis(string expression)
  {
    ShuntingYard<OperatorType, int> shuntingYard = new(int.Parse, evalBinary: EvaluateBinary);
    shuntingYard.AddBinaryOperator("+", OperatorType.Plus, precedence: 1);
    shuntingYard.AddBinaryOperator("-", OperatorType.Minus, precedence: 1);
    shuntingYard.AddBinaryOperator("*", OperatorType.Multiply, precedence: 2);
    shuntingYard.AddBinaryOperator("/", OperatorType.Divide, precedence: 2);
    shuntingYard.AddBinaryOperator("^", OperatorType.Exponent, precedence: 3);
    return shuntingYard.Evaluate(expression);
  }

  private static int EvaluateUnary(int value, OperatorType type)
  {
    return type switch
    {
      OperatorType.Not => value != 0 ? 0 : 1,
      OperatorType.Increment => ++value,
      OperatorType.Decrement => --value,
      _ => throw new NotSupportedException(type.ToString())
    };
  }

  private static int EvaluateBinary(int lhs, int rhs, OperatorType type)
  {
    return type switch
    {
      OperatorType.Plus => lhs + rhs,
      OperatorType.Minus => lhs - rhs,
      OperatorType.Multiply => lhs * rhs,
      OperatorType.Divide => lhs / rhs,
      OperatorType.Exponent => (int)Math.Pow(lhs, rhs),
      OperatorType.And => lhs != 0 && rhs != 0 ? 1 : 0,
      OperatorType.Or => lhs != 0 || rhs != 0 ? 1 : 0,
      _ => throw new NotSupportedException(type.ToString())
    };
  }

  private static float EvaluateBinaryF(float lhs, float rhs, OperatorType type)
  {
    return type switch
    {
      OperatorType.Plus => lhs + rhs,
      OperatorType.Minus => lhs - rhs,
      OperatorType.Multiply => lhs * rhs,
      OperatorType.Divide => lhs / rhs,
      OperatorType.Exponent => Mathf.Pow(lhs, rhs),
      OperatorType.And => lhs != 0 && rhs != 0 ? 1 : 0,
      OperatorType.Or => lhs != 0 || rhs != 0 ? 1 : 0,
      _ => throw new NotSupportedException(type.ToString())
    };
  }

  private static int ParseBooleanToInt(string token)
  {
    return token switch
    {
      "1" or "true" => 1,
      "0" or "false" => 0,
      _ => throw new InvalidOperationException($"Unable to parse {token}")
    };
  }

  internal enum OperatorType
  {
    Plus,
    Minus,
    Multiply,
    Divide,
    Exponent,
    And,
    Or,
    Not,
    Increment,
    Decrement
  }
}