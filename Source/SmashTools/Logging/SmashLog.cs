using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace SmashTools;

[StaticConstructorOnStartup]
public static class SmashLog
{
  private static string RichTextRegex = @"(<i>)|(<\/i>)|(<b>)|(<\/b>)|(<color.*?>|<\/color>)";

  private static string RichTextRegexStartingBrackets = "";
  private static string RichTextRegexEndingBrackets = "";

  private static readonly List<(string, Color)> bracketColor = [];

  static SmashLog()
  {
    RegisterRichTextBracket("text", Color.white);
    RegisterRichTextBracket("field", new Color(0.5f, 0.35f, 0.95f));
    RegisterRichTextBracket("property", new Color(0.05f, 0.5f, 1));
    RegisterRichTextBracket("method", new Color(1, 0.65f, 0));
    RegisterRichTextBracket("struct", new Color(0, 0.75f, 0.4f));
    RegisterRichTextBracket("class", new Color(0, 0.65f, 0.5f));
    RegisterRichTextBracket("type", new Color(0, 0.65f, 0.5f));
    RegisterRichTextBracket("success", new Color(0, 0.5f, 0));
    RegisterRichTextBracket("error", ColorLibrary.LogError);
    RegisterRichTextBracket("warning", Color.yellow);
    RegisterRichTextBracket("mod", new Color(0, 0.5f, 0.5f));
    RegisterRichTextBracket("attribute", new Color(1, 0.4f, 0.4f));
    RegisterRichTextBracket("xml", new Color(0.25f, 0.75f, 0.95f));
  }

  [Obsolete("Do not use this method outside of development.")]
  public static void QuickMessage(string text)
  {
    Log.Clear();
    Log.Message(text);
  }

  public static void Message(string text)
  {
    Log.Message(ColorizeBrackets(text));
  }

  public static void ErrorLabel(string label, string text)
  {
    Error($"{ColorizeBrackets($"<error>{label}</error>")} {ColorizeBrackets(text)}");
  }

  public static void Error(string text)
  {
    try
    {
      if (DebugSettings.pauseOnError && Current.ProgramState == ProgramState.Playing)
      {
        Find.TickManager.Pause();
      }
      Log.Message(ColorizeBrackets(text));
      if (!PlayDataLoader.Loaded || Prefs.DevMode)
      {
        Log.TryOpenLogWindow();
      }
    }
    catch (Exception ex)
    {
      Log.Error($"An error occurred while logging an error with a label: {ex}");
    }
  }

  public static void ErrorOnce(string text, int key)
  {
    Log.ErrorOnce(ColorizeBrackets(text), key);
  }

  public static string ColorizeBrackets(this string text)
  {
    foreach ((string key, Color color) in bracketColor)
    {
      text = Regex.Replace(text, $@"\<{key}.*?\>",
        $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>", RegexOptions.Singleline);
      text = Regex.Replace(text, $@"\<\/{key}\>", "</color>", RegexOptions.Singleline);
    }
    return text;
  }

  private static void RegisterRichTextBracket(string textLabel, Color color)
  {
    RichTextRegex += @$"|(<{textLabel}.*?\>|\<\/{textLabel}\>)";
    if (!RichTextRegexStartingBrackets.NullOrEmpty())
    {
      RichTextRegexStartingBrackets += "|";
    }
    if (!RichTextRegexEndingBrackets.NullOrEmpty())
    {
      RichTextRegexEndingBrackets += "|";
    }
    RichTextRegexStartingBrackets += @$"(<{textLabel}.*?\>)";
    RichTextRegexEndingBrackets += $@"(\<\/{textLabel}\>)";
    bracketColor.Add((textLabel, color));
  }

  internal static void TestAllBrackets()
  {
    string buildText = "Testing all brackets: ";
    foreach ((string key, _) in bracketColor)
    {
      buildText += $"<{key}>{key}</{key}> ";
    }
    Message(buildText);
  }

  public static IEnumerable<CodeInstruction> RemoveRichTextFromDebugLogTranspiler(
    IEnumerable<CodeInstruction> instructions)
  {
    MethodInfo debugLogMethod = AccessTools.Method(typeof(Debug),
      nameof(Debug.Log), parameters: [typeof(object)]);
    return RemoveBracketsForMethodCall(instructions, debugLogMethod);
  }

  public static IEnumerable<CodeInstruction> RemoveRichTextFromDebugLogWarningTranspiler(
    IEnumerable<CodeInstruction> instructions)
  {
    MethodInfo debugLogMethod = AccessTools.Method(typeof(Debug),
      nameof(Debug.LogWarning), parameters: [typeof(object)]);
    return RemoveBracketsForMethodCall(instructions, debugLogMethod);
  }

  public static IEnumerable<CodeInstruction> RemoveRichTextFromDebugLogErrorTranspiler(
    IEnumerable<CodeInstruction> instructions)
  {
    MethodInfo debugLogMethod = AccessTools.Method(typeof(Debug),
      nameof(Debug.LogError), parameters: [typeof(object)]);
    return RemoveBracketsForMethodCall(instructions, debugLogMethod);
  }

  public static IEnumerable<CodeInstruction> RemoveRichTextMessageDetailsTranspiler(
    IEnumerable<CodeInstruction> instructions)
  {
    List<CodeInstruction> instructionList = instructions.ToList();

    for (int i = 0; i < instructionList.Count; i++)
    {
      CodeInstruction instruction = instructionList[i];

      if (instruction.LoadsField(AccessTools.Field(typeof(LogMessage), nameof(LogMessage.text))))
      {
        yield return instruction;
        instruction = instructionList[++i];

        yield return new CodeInstruction(opcode: OpCodes.Call, operand:
          AccessTools.Method(typeof(SmashLog), nameof(LogTextWithoutRichText)));
      }

      yield return instruction;
    }
  }

  private static IEnumerable<CodeInstruction> RemoveBracketsForMethodCall(
    IEnumerable<CodeInstruction> instructions,
    MethodInfo method)
  {
    List<CodeInstruction> instructionList = instructions.ToList();
    for (int i = 0; i < instructionList.Count; i++)
    {
      CodeInstruction instruction = instructionList[i];

      if (instruction.Calls(method))
      {
        yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(
          typeof(SmashLog),
          nameof(LogTextWithoutRichText)));
      }

      yield return instruction;
    }
  }

  private static string LogTextWithoutRichText(string text)
  {
    const int MSTimeout = 50;

    try
    {
      string textNoBrackets = Regex.Replace(text, RichTextRegex, "", RegexOptions.Singleline,
        TimeSpan.FromMilliseconds(MSTimeout));
      return textNoBrackets;
    }
    catch (RegexMatchTimeoutException)
    {
    }
    return text;
  }
}