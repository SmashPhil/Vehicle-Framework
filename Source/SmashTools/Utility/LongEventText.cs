using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace SmashTools;

public readonly struct LongEventText : IDisposable
{
  private readonly string text;

  public LongEventText()
  {
    text = Ext_LongEventHandler.GetLongEventText();
  }

  void IDisposable.Dispose()
  {
    LongEventHandler.SetCurrentEventText(text);
  }
}