using System;
using Verse;

namespace SmashTools
{
  public static class Ext_Types
  {
    public static void Deconstruct<Item1, Item2>(this Pair<Item1, Item2> pair, out Item1 item1,
      out Item2 item2)
    {
      item1 = pair.First;
      item2 = pair.Second;
    }
  }
}