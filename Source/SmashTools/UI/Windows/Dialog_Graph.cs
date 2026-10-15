using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace SmashTools
{
	public class Dialog_Graph : Window
	{
		private List<CurvePoint> plotPoints;

		public Dialog_Graph(Graph.Function function, FloatRange range, List<CurvePoint> plotPoints = null, bool vectorEvaluation = false)
		{
			Function = function;
			XRange = range;
			YRange = range;
			VectorEvaluation = vectorEvaluation;
			this.plotPoints = plotPoints;
		}

		public Dialog_Graph(Graph.Function function, FloatRange xRange, FloatRange yRange, List<CurvePoint> plotPoints = null, bool vectorEvaluation = false)
		{
			Function = function;
			XRange = xRange;
			YRange = yRange;
			VectorEvaluation = vectorEvaluation;
			this.plotPoints = plotPoints;
		}

		public FloatRange XRange { get; protected set; }

		public FloatRange YRange { get; protected set; }
		
		public Graph.Function Function { get; protected set; }

		public bool VectorEvaluation { get; protected set; }

		public virtual List<CurvePoint> CurvePoints => plotPoints;

		protected virtual bool Editable => false;

		protected virtual bool DrawCoordLabels => true;

		protected virtual float Progress => 0;

		public override Vector2 InitialSize
		{
			get
			{
				float minSize = Mathf.Min(UI.screenWidth, UI.screenHeight);
				return new Vector2(minSize, minSize);
			}
		}

		public override void DoWindowContents(Rect inRect)
		{
			DrawGraph(inRect);
		}

		protected virtual void DrawGraph(Rect rect)
		{
			Widgets.DrawMenuSection(rect);
			Rect graphRect = rect.ContractedBy(45);
			Graph.DrawGraph(graphRect, Function, XRange, YRange, CurvePoints, progress: Progress, simplified: !VectorEvaluation, editable: Editable, drawCoordLabels: DrawCoordLabels);
		}
	}
}
