using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace CoreLib;

/// <summary>
/// Converts infix expressions into reverse Polish notation using the shunting yard algorithm.
/// </summary>
/// <typeparam name="TOperator">The operator token type used by the expression parser.</typeparam>
/// <typeparam name="TOperand">The operand token type used by the expression parser.</typeparam>
/// <remarks>
/// All operations are left-associative. If an operator should be right-associative, it must be grouped
/// with parenthesis for the operations to evaluate in the correct sequence.
/// </remarks>
[PublicAPI]
public sealed class ShuntingYard<TOperator, TOperand>
{
  private readonly Parser parser;
  private readonly UnaryEval evalUnary;
  private readonly BinaryEval evalBinary;
  private readonly Dictionary<string, Operator> operators = [];
  private readonly HashSet<char> operatorOpeners = [];

  private char startGroup;
  private char endGroup;

  public delegate TOperand Parser(string token);

  public delegate TOperand UnaryEval(TOperand operand, TOperator op);
  public delegate TOperand BinaryEval(TOperand lhs, TOperand rhs, TOperator op);

  public ShuntingYard(Parser parser, UnaryEval evalUnary = null, BinaryEval evalBinary = null)
  {
    this.parser = parser;
    this.evalUnary = evalUnary;
    this.evalBinary = evalBinary;
    SetGroupSymbols('(', ')');
  }

  public void SetGroupSymbols(char start, char end)
  {
    startGroup = start;
    endGroup = end;
  }

  public void AddBinaryOperator(string symbol, TOperator value, uint precedence = 0)
  {
    if (string.IsNullOrEmpty(symbol))
      throw new ArgumentException("symbol must be valid");

    operators.Add(symbol, new Operator
    {
      value = value,
      precedence = precedence,
      type = Operator.Type.Binary
    });
    operatorOpeners.Add(symbol[0]);
  }

  public void AddUnaryOperator(string symbol, TOperator value, Unary unary, uint precedence = 0)
  {
    if (string.IsNullOrEmpty(symbol))
      throw new ArgumentException("symbol must be valid");

    operators.Add(symbol, new Operator
    {
      value = value,
      precedence = precedence,
      type = Operator.Type.Unary
    });
    operatorOpeners.Add(symbol[0]);
  }

  public TOperand Evaluate(string expression)
  {
    if (string.IsNullOrWhiteSpace(expression))
      return default;

    var tokens = Tokenize(expression);
    State state = new(this);
    foreach (Token token in tokens)
    {
      state.ProcessToken(token);
    }
    state.Flush();
    return state.Result;
  }

  private List<Token> Tokenize(string expression)
  {
    List<Token> tokens = [];

    string buffer = "";
    for (int i = 0; i < expression.Length; i++)
    {
      char c = expression[i];
      if (char.IsWhiteSpace(c))
      {
        FlushBuffer(Token.Type.Operand);
        continue;
      }
      if (c == startGroup || c == endGroup)
      {
        FlushBuffer(Token.Type.Operand);
        tokens.Add(new Token { type = Token.Type.Operator, value = c.ToString() });
        continue;
      }

      if (operatorOpeners.Contains(c) || IsNumericPrefix(c))
      {
        Unary alignment = buffer != "" ? Unary.Postfix : Unary.Prefix;
        Operator.Type type = Operator.Type.Binary;
        FlushBuffer(Token.Type.Operand);
        string lookahead = "";
        if (IsNumericPrefix(c))
        {
          buffer = c.ToString();
          type = Operator.Type.Unary;
        }
        List<string> opsToMatch = operators.Keys.ToList();
        int charIndex = 0;
        for (int j = i; j < expression.Length; j++, charIndex++)
        {
          lookahead += expression[j];
          if (operators.TryGetValue(lookahead, out var op))
          {
            buffer = lookahead;
            type = op.type;
            opsToMatch.Remove(lookahead);
          }

          for (int k = opsToMatch.Count - 1; k >= 0; k--)
          {
            string match = opsToMatch[k];
            if (charIndex >= match.Length)
            {
              opsToMatch.RemoveAt(k);
            }
          }
          if (opsToMatch.Count == 0)
            break;
        }
        FlushBuffer(Token.Type.Operator, type == Operator.Type.Unary ? alignment : null);
      }
      else
      {
        buffer += c;
      }
    }
    if (buffer != string.Empty)
    {
      FlushBuffer(Token.Type.Operand);
    }
    return tokens;

    void FlushBuffer(Token.Type type, Unary? alignment = null)
    {
      if (buffer == string.Empty)
        return;

      switch (type)
      {
        case Token.Type.Operand:
          tokens.Add(new Token { type = Token.Type.Operand, value = buffer });
          break;
        case Token.Type.Operator:
          tokens.Add(alignment != null ?
            new Token { type = Token.Type.Operator, value = buffer, unary = alignment.Value } :
            new Token { type = Token.Type.Operator, value = buffer });
          break;
      }
      buffer = "";
    }
  }

  //private List<Token> TokenizeOld(string expression)
  //{
  //  var nextToken = Token.Type.Operand;
  //  List<Token> tokens = [];

  //  string buffer = "";
  //  foreach (char c in expression)
  //  {
  //    if (char.IsWhiteSpace(c))
  //    {
  //      FlushBuffer();
  //      continue;
  //    }

  //    if (c == startGroup || c == endGroup)
  //    {
  //      FlushBuffer();
  //      tokens.Add(Token.Separator(c.ToString()));
  //      continue;
  //    }

  //    switch (nextToken)
  //    {
  //      case Token.Type.Operand:
  //        if (operatorOpeners.Contains(c))
  //        {
  //          if (IsNumericPrefix(c) && (tokens.Count == 0 || operators.ContainsKey(tokens[^1].value)))
  //          {
  //            // Numerical prefixes i.e. plus and minus signs can only be added when an operator
  //            // was specified last token, this prevents grouping of otherwise valid operations.
  //            // e.g. "(1 + 2) - 3" should be 1 plus 2 minus 3, not 1 plus 2 -3
  //            buffer += c;
  //          }
  //          else
  //          {
  //            FlushBuffer();
  //            buffer = c.ToString();
  //          }
  //        }
  //        else
  //        {
  //          buffer += c;
  //        }
  //        break;
  //      case Token.Type.Operator:
  //        if (operators.ContainsKey(buffer) && !operators.ContainsKey(buffer + c))
  //        {
  //          if (buffer == "")
  //            throw new ArgumentException("Unable to tokenize expression. Mismatched operators to operands.");

  //          FlushBuffer();
  //          buffer = c.ToString();
  //        }
  //        else
  //        {
  //          buffer += c;
  //        }
  //        break;
  //    }
  //  }
  //  if (buffer != "")
  //  {
  //    FlushBuffer();
  //  }
  //  return tokens;

  //  void FlushBuffer(Unary? alignment = null)
  //  {
  //    if (buffer != "")
  //    {
  //      switch (nextToken)
  //      {
  //        case Token.Type.Operand:
  //          tokens.Add(Token.Operand(buffer));
  //          nextToken = Token.Type.Operator;
  //          break;
  //        case Token.Type.Operator:
  //          tokens.Add(alignment != null ?
  //            Token.UnaryOperator(buffer, unary: alignment.Value) :
  //            Token.BinaryOperator(buffer));
  //          nextToken = Token.Type.Operand;
  //          break;
  //      }
  //      buffer = "";
  //    }
  //  }
  //}

  private static bool IsNumericPrefix(string str)
  {
    return str.Length == 1 && IsNumericPrefix(str[0]);
  }

  private static bool IsNumericPrefix(char c)
  {
    return typeof(TOperand).IsNumericType() && c is '-' or '+';
  }

  [Flags]
  public enum Unary { Prefix = 1, Postfix = 2 }

  [DebuggerDisplay("Token = {value}")]
  private class Token
  {
    public Type type;
    public string value;
    public Unary unary;

    //public Operator op;

    internal enum Type { Operator, Operand, OpenGroup, CloseGroup }
  }

  private struct Operator
  {
    public TOperator value;
    public Type type;
    public uint precedence;

    //public Unary unary;

    internal enum Type { Unary, Binary }
  }

  private sealed class State(ShuntingYard<TOperator, TOperand> shuntingYard)
  {
    private Token.Type type = Token.Type.Operand;

    private readonly Stack<TOperand> output = [];
    private readonly Stack<Operator> holding = [];

    // Hoist delegates and operator cache so we don't have to access them through the parent every time.
    private readonly Parser parser = shuntingYard.parser;
    private readonly UnaryEval evalUnary = shuntingYard.evalUnary;
    private readonly BinaryEval evalBinary = shuntingYard.evalBinary;

    public TOperand Result { get; private set; }

    public void Flush()
    {
      // Flush the rest of the holding stack, it should already be in order of precedence.
      while (holding.TryPop(out Operator top))
      {
        Evaluate(top);
      }

      if (output.Count != 1)
      {
        Logger.Error("Unable to evaluate expression, order of operations did not evaluate the entire expression.");
        Result = default!;
      }
      else
      {
        Result = output.Pop();
      }
    }

    public void ProcessToken(Token token)
    {
      switch (token.type)
      {
        case Token.Type.Operator:
          if (type == Token.Type.Operand && IsNumericPrefix(token.value))
          {
            token.unary = Unary.Prefix;
          }
          //InsertOperator(token.op);
          token.type = Token.Type.Operand;
          break;
        case Token.Type.Operand:
          output.Push(parser(token.value));
          token.type = Token.Type.Operator;
          break;
        case Token.Type.OpenGroup:
          //holding.Push(token.op);
          token.type = Token.Type.Operand;
          break;
        case Token.Type.CloseGroup:
        {
          // Evaluate the entire expression from last open parenthesis
          //while (holding.TryPop(out Operator top) && top.type is not Group.Open)
          //{
          //  Evaluate(top);
          //}
          token.type = Token.Type.Operand;
          break;
        }
      }
    }

    private void InsertOperator(in Operator op)
    {
      if (output.Count >= 2)
      {
        // If precedence is not larger than the operator at the top of the stack, flush the holding
        // stack one by one until next operator is equal to or greater than the top of the stack.
        while (holding.TryPeek(out Operator top) && op.precedence <= top.precedence)
        {
          top = holding.Pop();
          Evaluate(top);
        }
      }
      holding.Push(op);
    }

    private void Evaluate(in Operator op)
    {
      TOperator value = op.value;
      TOperand result;
      switch (op.type)
      {
        case Operator.Type.Unary:
        {
          result = evalUnary(output.Pop(), value);
          break;
        }
        case Operator.Type.Binary:
        {
          TOperand rhs = output.Pop();
          TOperand lhs = output.Pop();
          result = evalBinary(lhs, rhs, value);
          break;
        }
        default:
          throw new NotSupportedException(op.type.ToString());
      }
      output.Push(result);
    }
  }
}
