using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Verse;

namespace SmashTools.Animations
{
	public static class AnimationGraph
	{
		public const float DragHandleSize = 12;

		// f(x) = y, can be any function that matches the input and output types
		public delegate float Function(float frame);

		/// <returns>If any KeyFrame handle is currently being dragged</returns>
		public static void DrawAnimationCurve(Rect rect, Rect visibleRect, AnimationCurve curve, Color color, float spacing)
		{
			if (curve != null && !curve.points.NullOrEmpty())
			{
				DrawCurve(rect, spacing, curve, color);
			}
		}

		private static void DrawCurve(Rect rect, float spacing, AnimationCurve curve, Color color)
		{
			float step = 1;// spacing / AxisStepFactor;
			if (step <= 0)
			{
				return;
			}
			FloatRange xRange = curve.RangeX;
			float x = curve.RangeX.min;
			float y = curve.Function(x);
			Vector2 coordLeft = GraphCoordToScreenPos(rect, new Vector2(x, y), xRange, spacing);
			for (x = xRange.min + step; x <= xRange.max; x += step) //start 1 step in
			{
				y = curve.Function(x);
				if (float.IsNaN(y) || float.IsNaN(x))
				{
					continue;
				}
				Vector2 coordRight = GraphCoordToScreenPos(rect, new Vector2(x, y), xRange, spacing);
				// Todo - Cull lines outside of visibleRect
				Widgets.DrawLine(coordLeft, coordRight, color, 1);
				coordLeft = coordRight;
			}
		}

		public static Vector2 GraphCoordToScreenPos(Rect rect, Vector2 coord, FloatRange xRange, float spacing)
		{
			float xStep = (coord.x - xRange.min) / (xRange.max - xRange.min);
			float yOffset = coord.y * spacing;
			// yOffset inverted, UI rendered top to bottom
			return new Vector2(rect.x + rect.width * xStep, rect.y + rect.height / 2 - yOffset);
		}

		public static (int frame, float value) ScreenPosToGraphCoord(Rect rect, Rect visibleRect, Vector2 mousePos, 
			FloatRange xRange, float scrollY, float spacing)
		{
			float mouseX = mousePos.x + visibleRect.x;
			float mouseY = mousePos.y + visibleRect.y - 16 + scrollY;
			float xStep = (mouseX - rect.x) / rect.width;
			// mouseY inverted, UI rendered top to bottom
			float yOffset = rect.y + rect.height / 2 - mouseY;
			return (Mathf.RoundToInt(xStep * (xRange.max - xRange.min) + xRange.min), (yOffset / spacing).RoundTo(0.001f));
		}
	}
}
