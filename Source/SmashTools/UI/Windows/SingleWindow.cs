using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace SmashTools
{
	[Obsolete]
	public abstract class SingleWindow : Window
	{
		public static SingleWindow CurrentlyOpenedWindow { get; set; }

		public bool closeOnAnyClickOutside;

		public override void PreClose()
		{
			base.PreClose();
			CurrentlyOpenedWindow = null;
		}

		public override void PreOpen()
		{
			base.PreOpen();
			CurrentlyOpenedWindow = this;
			List<Window> windows = Find.WindowStack.Windows.ToList();
			foreach (Window window in windows)
			{
				if (window is SingleWindow single && single != CurrentlyOpenedWindow)
				{
					Find.WindowStack.TryRemove(window, false);
				}
			}
		}
	}
}
