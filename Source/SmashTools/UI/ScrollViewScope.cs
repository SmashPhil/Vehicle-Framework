using System;
using UnityEngine;

namespace SmashTools;

public readonly struct ScrollViewScope : IDisposable
{
	public ScrollViewScope(Rect outRect, ref Vector2 scrollPosition, Rect viewRect)
	{
		UIElements.BeginScrollView(outRect, ref scrollPosition, viewRect);
	}

	public ScrollViewScope(Rect outRect, ref Vector2 scrollPosition, Rect viewRect, bool showHorizontalScrollbar)
	{
		UIElements.BeginScrollView(outRect, ref scrollPosition, viewRect, showHorizontalScrollbar);
	}

	public ScrollViewScope(Rect outRect, ref Vector2 scrollPosition, Rect viewRect, bool showHorizontalScrollbar,
		bool showVerticalScrollBar)
	{
		UIElements.BeginScrollView(outRect, ref scrollPosition, viewRect, showHorizontalScrollbar, showVerticalScrollBar);
	}

	public void Dispose()
	{
		UIElements.EndScrollView();
	}
}