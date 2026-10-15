using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace SmashTools;

public static class Ext_String
{
  public static string ConvertRichText(this string text)
  {
    return text.ColorizeBrackets();
  }
}