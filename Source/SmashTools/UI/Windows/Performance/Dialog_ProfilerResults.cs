using System;
using System.Collections;
using System.Collections.Generic;
using CoreLib;
using UnityEngine;
using Verse;

namespace SmashTools.Performance;

internal sealed class Dialog_ProfilerResults : Window
{
	private const float DefaultWidth = 400;
	private const float DefaultHeight = 600;

	private float maxHeight = DefaultHeight;
	private Vector2 scrollPosition;

	private readonly List<Entry> entries = [];

	public Dialog_ProfilerResults()
	{
		draggable = true;
		resizeable = true;
		focusWhenOpened = false;
		onlyOneOfTypeAllowed = true;
		doCloseX = true;
		absorbInputAroundWindow = false;
		preventCameraMotion = false;
	}

	public override Vector2 InitialSize => new(DefaultWidth, DefaultHeight);

	public override void PostOpen()
	{
		base.PostOpen();
		Profiler.Enable();
		CoroutineManager.StartCoroutine(UpdateResultsRoutine);
	}

	public override void PostClose()
	{
		base.PostClose();
		Profiler.Disable();
	}

	public override void DoWindowContents(Rect inRect)
	{
		const float RowHeight = 22;

		Rect outRect = inRect;
		Rect viewRect = outRect with { width = outRect.width - GenUI.ScrollBarWidth, height = maxHeight };
		using (new ScrollViewScope(outRect, ref scrollPosition, viewRect, showHorizontalScrollbar: false))
		{
			using TextBlock fontBlock = new(GameFont.Small);
			float curY = 0;
			foreach ((string label, Profiler.Summary summary) in entries)
			{
				const float LabelWidthPct = 0.8f;
				Rect rowRect = new(viewRect.x, viewRect.y + curY, viewRect.width, RowHeight);
				rowRect.SplitVerticallyWithMargin(out Rect labelRect, out Rect resultRect, out _,
					leftWidth: rowRect.width * LabelWidthPct);
				Widgets.Label(labelRect, label);
				using TextBlock anchorBlock = new(TextAnchor.UpperRight);
				Widgets.Label(resultRect, Ext_Profiler.ToMeasurementString(summary.Average));
				curY += RowHeight;
			}
			maxHeight = curY;
		}
	}

	private IEnumerator UpdateResultsRoutine()
	{
		const float SecondsPerRecache = 1;

		while (IsOpen)
		{
			yield return new WaitForSeconds(SecondsPerRecache);
			UpdateResults();
		}
	}

	private void UpdateResults()
	{
		entries.Clear();
		using var enumerator = Profiler.GetResults();
		while (enumerator.MoveNext())
		{
			(string label, Profiler.Summary summary) = enumerator.Current;
			entries.Add(new Entry(label, summary));
		}
		entries.Sort();
	}

	private record Entry(string label, Profiler.Summary summary) : IComparable<Entry>
	{
		public readonly string label = label;
		public readonly Profiler.Summary summary = summary;

		int IComparable<Entry>.CompareTo(Entry other)
		{
			if (other?.summary is null)
				return -1;
			// Sort highest to lowest
			return other.summary.Average.CompareTo(summary.Average);
		}
	}
}