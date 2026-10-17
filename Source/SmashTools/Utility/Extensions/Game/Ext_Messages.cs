using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;

namespace SmashTools
{
  public static class Ext_Messages
  {
    // Message.DefaultMessageLifespan
    private const float DefaultMessageLifespan = 13;

    private static readonly FieldInfo messageStartingTime;

    
    static Ext_Messages()
    {
      messageStartingTime = AccessTools.Field(typeof(Message), "startingTime");
    }

    public static void Message(string text, MessageTypeDef messageTypeDef, float time = DefaultMessageLifespan, 
      bool historical = true)
    {
      Message message = new(text.CapitalizeFirst(), messageTypeDef);
      messageStartingTime.SetValue(message, RealTime.LastRealTime - (DefaultMessageLifespan - time));
      Messages.Message(message, historical: historical);
    }
  }
}
