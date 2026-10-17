using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using SmashTools.Xml;

namespace SmashTools
{
	[StaticConstructorOnStartup]
	public class Dialog_GraphEditor : Dialog_Graph
	{
		/// <summary>
		/// For all animations, t is the time ratio between 0 and max ticks.
		/// Bounds: 0 <= t <= 1
		/// </summary>
		public const float DefaultMinX = 0;

		public const float DefaultMaxX = 1;
		public const float CoordinatesListButtonSize = 24;
		public const float PercentCameraBoxWidth = 0.4f;

		public const float PlaybackBarHeight = 3;
		public const float PlaybackRectHeight = 25;
		public const float PlaybackHandleSize = 12;

		public const float PlayButtonFadeTime = 1.5f;

		public static readonly FloatRange ZoomRange = new FloatRange(2, 16);

		private static readonly string filePath =
			Path.Combine(Application.persistentDataPath, "GraphEditorExport.xml");

		private static readonly Texture2D[] viewerButtonTextures = new Texture2D[3]
		{
			ContentFinder<Texture2D>.Get("SmashTools/VideoPause", false) ??
			ContentFinder<Texture2D>.Get("UI/TimeControls/TimeSpeedButton_Pause", true),
			ContentFinder<Texture2D>.Get("SmashTools/VideoPlay", false) ??
			ContentFinder<Texture2D>.Get("UI/TimeControls/TimeSpeedButton_Normal", true),
			ContentFinder<Texture2D>.Get("SmashTools/VideoSkiptoNext", false) ??
			ContentFinder<Texture2D>.Get("UI/TimeControls/TimeSpeedButton_Fast", true),
		};

		private static readonly Texture2D dragHandleIcon =
			ContentFinder<Texture2D>.Get("SmashTools/ViewHandle", false) ??
			ContentFinder<Texture2D>.Get("UI/Icons/LifeStage/Adult", true);

		private Graph.GraphType graphType;
		private LinearCurve curve;

		private IAnimationTarget animationTarget;
		private List<AnimatorObject> animators;
		private AnimatorObject curAnimator;
		private bool drawCoordLabels = true;

		private Vector2 scrollPos;
		private Listing_SplitColumns lister = new Listing_SplitColumns();
		private List<IAnimationTarget> potentialAnimationTargets = new List<IAnimationTarget>();

		private static bool dragging = false;
		private static float timeFading = 0;

		public Dialog_GraphEditor() : base(null, new FloatRange(DefaultMinX, DefaultMaxX),
			new List<CurvePoint>())
		{
			doCloseX = true;
			forcePause = true;
			GraphType = Graph.GraphType.Linear;
			ReinstantiateCurve();
			ValidateLimits(true);
		}

		public Dialog_GraphEditor(Graph.Function function, FloatRange range,
			bool vectorEvaluation = false) : base(function, range, new List<CurvePoint>(),
			vectorEvaluation)
		{
			doCloseX = true;
			forcePause = true;
			GraphType = Graph.GraphType.Linear;
			ReinstantiateCurve();
			ValidateLimits(true);
		}

		public Dialog_GraphEditor(Graph.Function function, FloatRange range,
			List<CurvePoint> plotPoints, bool vectorEvaluation = false) : base(function, range,
			plotPoints, vectorEvaluation)
		{
			doCloseX = true;
			forcePause = true;
			GraphType = Graph.GraphType.Linear;
			ReinstantiateCurve();
			ValidateLimits(true);
		}

		public Dialog_GraphEditor(IAnimationTarget animationTarget = null,
			bool vectorEvaluation = false) : base(null, new FloatRange(DefaultMinX, DefaultMaxX),
			plotPoints: new List<CurvePoint>(), vectorEvaluation: vectorEvaluation)
		{
			doCloseX = true;
			forcePause = true;
			GraphType = Graph.GraphType.Linear;
			this.animationTarget = animationTarget;
			ReinstantiateCurve();
			ValidateLimits(true);
		}

		public override List<CurvePoint> CurvePoints => curve.points;

		protected override bool Editable => true;

		protected override bool DrawCoordLabels => drawCoordLabels;

		public bool DisableCameraView => animationTarget == null;

		public bool LogReport { get; set; } = false;

		private float StartingAnimationDriverTick { get; set; }

		public override Vector2 InitialSize
		{
			get
			{
				float minSize = Mathf.Min(UI.screenWidth, UI.screenHeight);
				return new Vector2(minSize * 1.5f, minSize);
			}
		}

		public Graph.GraphType GraphType
		{
			get { return graphType; }
			set
			{
				graphType = value;
				ReinstantiateCurve();
				ValidateLimits(true);
			}
		}

		public Type CurveType
		{
			get
			{
				return GraphType switch
				{
					Graph.GraphType.Linear    => typeof(LinearCurve),
					Graph.GraphType.Bezier    => typeof(BezierCurve),
					Graph.GraphType.Lagrange  => typeof(LagrangeCurve),
					Graph.GraphType.Staircase => typeof(StaircaseCurve),
					_                         => null
				};
			}
		}

		protected override float Progress
		{
			get
			{
				return -1;
				//if (AnimationManager.CurrentDriver != null)
				//{
				//	return Mathf.Clamp01((AnimationManager.TicksPassed - StartingAnimationDriverTick) / AnimationManager.CurrentDriver.AnimationLength);
				//}
				//return 0;
			}
		}

		public override void PostOpen()
		{
			base.PostOpen();
			if (!Find.Maps.NullOrEmpty())
			{
				potentialAnimationTargets = Find.Maps
				 .SelectMany(map => map.spawnedThings.Where(thing => thing is IAnimationTarget))
				 .Cast<IAnimationTarget>().ToList();
			}
			if (!DisableCameraView && AnimationSimulator.Reserve(animationTarget, AnimationTick))
			{
				TryStartCamera(animationTarget);
			}
		}

		public override void PostClose()
		{
			base.PostClose();
			CameraView.Close();
			AnimationSimulator.Release();
		}

		private void TryStartCamera(IAnimationTarget animationTarget)
		{
			if (CameraView.InUse)
			{
				CameraView.Close();
			}
			this.animationTarget = animationTarget;
			if (this.animationTarget != null)
			{
				RecacheAnimationTargetCurves();
				CameraJumper.TryJump(animationTarget.Thing, mode: CameraJumper.MovementMode.Cut);
				CameraView.Start(orthographicSize: CameraView.animationSettings.orthographicSize);
				Find.Selector.ClearSelection();
				AnimationSimulator.Reset();
				AnimationSimulator.SetDriver(null);
				RecacheAnimationDriverStartingTick();
			}
		}

		public void RecacheAnimationTargetCurves()
		{
			StringBuilder stringBuilder = LogReport ? new StringBuilder() : null;
			animators = animationTarget?.GetAnimators(stringBuilder)
			 .OrderBy(animObject => animObject.DisplayName).ToList();
			if (stringBuilder != null) Log.Message($"----- Report: -----\n{stringBuilder}");
		}

		public void ValidateLimits(bool hardSet = false)
		{
			if (curve != null && !curve.points.NullOrEmpty())
			{
				float xMinCurve = curve.points.Min(cp => cp.x);
				float xMaxCurve = curve.points.Max(cp => cp.x);
				float xMin =
					hardSet ?
						xMinCurve :
						Mathf.Min(XRange.min,
							xMinCurve); //Take original value into account if not fully resetting
				float xMax = hardSet ? xMaxCurve : Mathf.Max(XRange.max, xMaxCurve);
				XRange = new FloatRange(xMin, xMax);

				float yMinCurve = curve.points.Min(cp => cp.y);
				float yMaxCurve = curve.points.Max(cp => cp.y);
				float yMin =
					hardSet ?
						yMinCurve :
						Mathf.Min(YRange.min,
							yMinCurve); //Take original value into account if not fully resetting
				float yMax = hardSet ? yMaxCurve : Mathf.Max(YRange.max, yMaxCurve);
				YRange = new FloatRange(yMin, yMax);
			}
			else
			{
				XRange = new FloatRange(0, 1);
				YRange = new FloatRange(0, 1);
			}
		}

		private void SelectAnimator(AnimatorObject animatorObject)
		{
			curAnimator = animatorObject;
			if (animatorObject != null)
			{
				curve = animatorObject.Curve;
				GraphType = CurveToGraphType(curve.GetType());
				Function = curve.Function;
			}
			else
			{
				GraphType = Graph.GraphType.Linear;
				curve = null;
				XRange = new FloatRange(0, 1);
				YRange = new FloatRange(0, 1);
				ReinstantiateCurve();
			}
			ValidateLimits(true);
		}

		private void SelectAnimationDriver(AnimationDriver animationDriver)
		{
			if (AnimationSimulator.CurrentDriver != null &&
				AnimationSimulator.CurrentDriver != animationDriver)
			{
				AnimationSimulator.Reset();
			}
			AnimationSimulator.SetDriver(animationDriver);
			RecacheAnimationDriverStartingTick();
			SelectAnimator(null);
		}

		private void RecacheAnimationDriverStartingTick()
		{
			StartingAnimationDriverTick = 0;
			if (AnimationSimulator.CurrentDriver != null)
			{
				int totalLength = 0;
				foreach (AnimationDriver animationDriver in animationTarget.Animations)
				{
					if (animationDriver == AnimationSimulator.CurrentDriver)
					{
						StartingAnimationDriverTick = totalLength;
						return;
					}
					totalLength += animationDriver.AnimationLength;
				}
			}
		}

		public Graph.GraphType CurveToGraphType(Type type)
		{
			if (type == typeof(LinearCurve))
			{
				return Graph.GraphType.Linear;
			}
			if (type == typeof(BezierCurve))
			{
				return Graph.GraphType.Bezier;
			}
			if (type == typeof(LagrangeCurve))
			{
				return Graph.GraphType.Lagrange;
			}
			if (type == typeof(StaircaseCurve))
			{
				return Graph.GraphType.Staircase;
			}
			throw new NotImplementedException("GraphType");
		}

		public void ReinstantiateCurve()
		{
			curve = (LinearCurve)Activator.CreateInstance(CurveType,
				curve?.points ?? new List<CurvePoint>());
			Function = curve.Function;
		}

		public override void WindowUpdate()
		{
			base.WindowUpdate();
			if (!DisableCameraView)
			{
				if (CameraView.animationSettings.drawCellGrid)
				{
					CameraView.DrawMapGridInView();
				}
				(Vector3 drawPos, float rotation) = animationTarget.DrawData;
				if (AnimationSimulator.CurrentDriver != null)
				{
					(drawPos, _) = AnimationSimulator.CurrentDriver.Draw(drawPos, rotation);
				}
				CameraView.Update(drawPos);
			}
			AnimationSimulator.Update();
		}

		public void AnimationTick()
		{
			if (AnimationSimulator.CurrentDriver != null)
			{
				if (AnimationSimulator.TicksPassed >= AnimationSimulator.CurrentDriver.AnimationLength)
				{
					AnimationSimulator.Reset();
					AnimationSimulator.Paused = !CameraView.animationSettings.loop;
				}
				else
				{
					AnimationSimulator.CurrentDriver.Tick(AnimationSimulator.TicksPassed);
				}
			}
		}

		public override void DoWindowContents(Rect inRect)
		{
			AnimationSimulator.OnGUI();

			Rect inputRect = new Rect(inRect)
			{
				width = InitialSize.x - InitialSize.y - 10
			};


			using var textBlock = new TextBlock(GameFont.Small);

			Widgets.DrawMenuSection(inputRect);
			inputRect = inputRect.ContractedBy(10);
			TopLevelButtons(inputRect,
				(graphType.ToString(), SelectCurveType),
				(VectorEvaluation ? "Vector" : "Simplified", () => VectorEvaluation = !VectorEvaluation),
				("Save", () => SaveEdits()),
				("Export Xml", ExportAnimationXml));

			string formula = graphType switch
			{
				Graph.GraphType.Linear   => "y = mx + b",
				Graph.GraphType.Bezier   => "Σ{n : i=0} nC_i ((1-t) ^ (n-i)) * (t^i) * (P_i)",
				Graph.GraphType.Lagrange => "Σ{n-1 : i=0} y * ∏{n-1 : j=0, j≠1} (x - xj) / (xi - xj)",
				_                        => string.Empty
			};
			Vector2 textSize = Text.CalcSize(formula);
			Rect formulaRect = new Rect(inputRect.x + inputRect.width - textSize.x, inputRect.y,
				textSize.x, textSize.y);
			//Widgets.Label(formulaRect, formula);

			Rect graphSizeRect = new Rect(inputRect.x, formulaRect.yMax + 10, inputRect.width / 3, 30);
			using (new TextBlock(GameFont.Medium))
			{
				Widgets.Label(graphSizeRect, "Axis Limits");
			}

			float xMin = XRange.min;
			float xMax = XRange.max;
			float yMin = YRange.min;
			float yMax = YRange.max;

			float axisInputWidth = inputRect.width / 6;
			Rect xRangeRect = new Rect(inputRect.x, graphSizeRect.yMax + 3, axisInputWidth, 30);
			UIElements.NumericBox(xRangeRect, ref xMin, "xMin", string.Empty, string.Empty,
				float.MinValue, float.MaxValue, labelProportion: 0.4f);
			xRangeRect.x += xRangeRect.width + 5;
			UIElements.NumericBox(xRangeRect, ref xMax, "xMax", string.Empty, string.Empty,
				float.MinValue, float.MaxValue, labelProportion: 0.4f);

			Rect yRangeRect = new Rect(inputRect.x, xRangeRect.yMax + 3, axisInputWidth, 30);
			UIElements.NumericBox(yRangeRect, ref yMin, "yMin", string.Empty, string.Empty,
				float.MinValue, float.MaxValue, labelProportion: 0.4f);
			yRangeRect.x += yRangeRect.width + 5;
			UIElements.NumericBox(yRangeRect, ref yMax, "yMax", string.Empty, string.Empty,
				float.MinValue, float.MaxValue, labelProportion: 0.4f);

			string animationTargetLabel = animationTarget?.Thing.Label ?? "No Animator";
			Rect animationTargetRect = new Rect(graphSizeRect.xMax, graphSizeRect.y,
				inputRect.width - graphSizeRect.width, graphSizeRect.height);
			if (UIElements.ClickableLabel(animationTargetRect, animationTargetLabel, GenUI.MouseoverColor,
				Color.white, fontSize: GameFont.Medium, anchor: TextAnchor.MiddleRight))
			{
				if (!potentialAnimationTargets.NullOrEmpty())
				{
					List<FloatMenuOption> options = new List<FloatMenuOption>();
					options.Add(new FloatMenuOption("None", () => TryStartCamera(null)));
					foreach (IAnimationTarget animationTarget in potentialAnimationTargets)
					{
						options.Add(new FloatMenuOption(animationTarget.Thing.Label,
							() => TryStartCamera(animationTarget)));
					}
					Find.WindowStack.Add(new FloatMenu(options));
				}
				else
				{
					Messages.Message("Map must be loaded with at least 1 IAnimationTarget spawned.",
						MessageTypeDefOf.RejectInput);
				}
			}

			Rect drawLabelsRect = new Rect(inputRect.xMax - 250, xRangeRect.y, 250, 30);
			if (animationTarget != null && !animators.NullOrEmpty())
			{
				if (Widgets.ButtonText(drawLabelsRect,
					AnimationSimulator.CurrentDriver?.Name ?? "Select Animation Driver"))
				{
					List<FloatMenuOption> options = new List<FloatMenuOption>();
					options.Add(new FloatMenuOption("None", () => SelectAnimationDriver(null)));
					foreach (AnimationDriver animationDriver in animationTarget.Animations)
					{
						options.Add(new FloatMenuOption($"{animationDriver.Name}",
							() => SelectAnimationDriver(animationDriver)));
					}
					Find.WindowStack.Add(new FloatMenu(options));
				}
				drawLabelsRect.y = yRangeRect.y;
				if (Widgets.ButtonText(drawLabelsRect, curAnimator?.DisplayName ?? "None"))
				{
					List<FloatMenuOption> options = new List<FloatMenuOption>();
					options.Add(new FloatMenuOption("None", () => SelectAnimator(null)));
					if (AnimationSimulator.CurrentDriver != null)
					{
						foreach (AnimatorObject animatorObject in animators.Where(anim =>
							!anim.category.NullOrEmpty() && anim.category.Split('.')[0] ==
							AnimationSimulator.CurrentDriver.Name))
						{
							options.Add(new FloatMenuOption($"{animatorObject.DisplayName}",
								() => SelectAnimator(animatorObject)));
						}
					}
					Find.WindowStack.Add(new FloatMenu(options));
				}
			}

			if (xMin - xMax > -0.01f)
			{
				xMin = xMax - 0.01f;
			}
			if (xMax - xMin < 0.01f)
			{
				xMax = xMin + 0.01f;
			}
			if (yMin - yMax > -0.01f)
			{
				yMin = yMax - 0.01f;
			}
			if (yMax - yMin < 0.01f)
			{
				yMax = yMin + 0.01f;
			}

			ValidateLimits();

			XRange = new FloatRange(xMin, xMax);
			YRange = new FloatRange(yMin, yMax);

			float textHeight = Text.CalcHeight("Coordinates", inputRect.width);
			Rect coordinatesLabelRect =
				new Rect(inputRect.x, yRangeRect.yMax + 10, inputRect.width, textHeight);
			Widgets.Label(coordinatesLabelRect, "Coordinates");

			Rect curveFieldRect =
				new Rect(inputRect.x + inputRect.width - 250, coordinatesLabelRect.y, 250, 30);
			UIElements.CheckboxLabeled(curveFieldRect, "Draw Coordinate Labels", ref drawCoordLabels);

			Rect coordinateOutRect = new Rect(inputRect.x, coordinatesLabelRect.yMax + 5, inputRect.width,
				inputRect.height / 2);
			float curvePointsMenuHeight = (curve.PointsCount + 1) * CoordinatesListButtonSize * 2 - 10;
			//16 for width of scrollbar
			Rect coordinateViewRect = new Rect(coordinateOutRect.x, coordinateOutRect.y,
				coordinateOutRect.width - 16, curvePointsMenuHeight);
			Widgets.DrawMenuSection(coordinateOutRect);
			Widgets.BeginScrollView(coordinateOutRect, ref scrollPos, coordinateViewRect);
			{
				Rect coordinatesListRect = new Rect(coordinateViewRect.x, coordinateViewRect.y,
					coordinateViewRect.width, CoordinatesListButtonSize * 2).ContractedBy(5);
				if (!curve.points.NullOrEmpty())
				{
					int indexToRemove = -1;
					for (int i = 0; i < curve.PointsCount; i++)
					{
						UIHighlighter.HighlightOpportunity(coordinatesListRect, $"GraphEditor_CurvePoint_{i}");
						Rect buttonArrowRect = new Rect(coordinatesListRect)
						{
							width = CoordinatesListButtonSize,
							height = coordinatesListRect.height / 2
						};

						if (i == 0) GUIState.Disable();
						if (Widgets.ButtonImage(buttonArrowRect, TexButton.ReorderUp,
								GUI.enabled ? Color.white : UIElements.InactiveColor,
								GUI.enabled ? GenUI.MouseoverColor : UIElements.InactiveColor,
								doMouseoverSound: GUI.enabled) && GUI.enabled)
						{
							curve.points.Swap(i, i - 1);
						}
						GUIState.Enable();

						buttonArrowRect.y += buttonArrowRect.height;

						if (i == curve.PointsCount - 1) GUIState.Disable();
						if (Widgets.ButtonImage(buttonArrowRect, TexButton.ReorderDown,
								GUI.enabled ? Color.white : UIElements.InactiveColor,
								GUI.enabled ? GenUI.MouseoverColor : UIElements.InactiveColor,
								doMouseoverSound: GUI.enabled) && GUI.enabled)
						{
							curve.points.Swap(i, i + 1);
						}
						GUIState.Enable();

						CurvePoint coord = curve.points[i];
						Rect vectorRect = new Rect(coordinatesListRect)
						{
							x = coordinatesListRect.x + buttonArrowRect.width + 10,
							width = (coordinatesListRect.width - CoordinatesListButtonSize) / 7
						};
						float x = UIElements.NumericBox(vectorRect, coord.x, "X", string.Empty, string.Empty,
							labelProportion: 0.25f);
						vectorRect.x += vectorRect.width + 5;
						float y = UIElements.NumericBox(vectorRect, coord.y, "Y", string.Empty, string.Empty,
							labelProportion: 0.25f);
						curve.points[i] = new CurvePoint(x.RoundTo(0.01f), y.RoundTo(0.01f));

						Rect deleteButtonRect = new Rect(coordinatesListRect.width - CoordinatesListButtonSize,
							coordinatesListRect.y + (coordinatesListRect.height - CoordinatesListButtonSize) / 2,
							CoordinatesListButtonSize, CoordinatesListButtonSize);
						if (Widgets.ButtonImage(deleteButtonRect, TexButton.Minus))
						{
							indexToRemove = i;
						}

						coordinatesListRect.y += coordinatesListRect.height;
					}
					if (indexToRemove >= 0)
					{
						curve.points.RemoveAt(indexToRemove);
					}
				}

				Rect addCoordinateRect = new Rect(coordinatesListRect.x,
					coordinatesListRect.y + (coordinatesListRect.height - CoordinatesListButtonSize) / 2,
					CoordinatesListButtonSize, CoordinatesListButtonSize);
				if (Widgets.ButtonImage(addCoordinateRect, TexButton.Plus))
				{
					CurvePoint curvePoint = curve.points.NullOrEmpty() ?
						new CurvePoint(0, 0) :
						new CurvePoint(XRange.Average, YRange.Average);
					curve.Add(curvePoint);
				}
			}
			Widgets.EndScrollView();

			float cameraViewerHeight = inputRect.height - coordinateOutRect.yMax;
			Rect cameraViewerRect = new Rect(coordinateOutRect.x, coordinateOutRect.yMax + 5,
				coordinateOutRect.width, cameraViewerHeight - 5);
			DrawPreviewWindow(cameraViewerRect);

			Rect graphRect = new Rect(inRect.width - inRect.height, 0, inRect.height, inRect.height);
			DrawGraph(graphRect);
		}

		private void TopLevelButtons(Rect rect, params (string label, Action onClick)[] buttons)
		{
			float buttonWidth = rect.width / buttons.Length; // - (5f * (buttons.Length - 1));
			Rect graphTypeRect = new Rect(rect.x, rect.y, buttonWidth, 30);
			foreach ((string label, Action onClick) in buttons)
			{
				if (Widgets.ButtonText(graphTypeRect, label))
				{
					onClick();
				}
				graphTypeRect.x += buttonWidth; // + 5;
			}
		}

		private void DrawPreviewWindow(Rect rect)
		{
			Widgets.DrawMenuSection(rect);
			rect = rect.ContractedBy(1);
			string previewWindowString = "Preview Window Disabled";
			bool disabledView = animationTarget is null;
			bool darkenWindow = false;
			if (!disabledView)
			{
				try
				{
					float camRectSize = rect.height; // * PercentCameraBoxWidth;
					Rect cameraRect = new Rect(rect.x + rect.width - camRectSize, rect.y, camRectSize,
						camRectSize);

					disabledView = !CameraView.RenderAt(cameraRect);

					if (!disabledView)
					{
						UIElements.DrawLineVertical(cameraRect.x - 1, cameraRect.y, camRectSize,
							UIElements.MenuSectionBgBorderColor);
						//UIElements.DrawLineHorizontal(cameraRect.x - 1, cameraRect.y + camRectSize + 1, camRectSize, UIElements.MenuSectionBGBorderColor);

						Rect leftColumnRect =
							new Rect(rect.x, rect.y, rect.width - camRectSize - 1, camRectSize).ContractedBy(5);
						lister.columnGap = 0;
						lister.Begin(leftColumnRect, 1);
						{
							lister.Header("Settings", anchor: TextAnchor.MiddleCenter);

							CameraView.OrthographicSize = lister.SliderLabeled("Orthographic Size",
								CameraView.OrthographicSize, "Camera Zoom", string.Empty, string.Empty,
								ZoomRange.min, ZoomRange.max, decimalPlaces: 1);
							if (lister.Button($"Playback Speed: {AnimationSimulator.PlaybackSpeed}",
								highlightTag: "Speed multiplier on animation window"))
							{
								List<FloatMenuOption> options = new List<FloatMenuOption>();

								foreach (float speed in AnimationSimulator.playbackSpeeds)
								{
									options.Add(new FloatMenuOption($"{speed}",
										() => AnimationSimulator.PlaybackSpeed = speed));
								}

								Find.WindowStack.Add(new FloatMenu(options));
							}

							lister.Gap(4);

							//lister.CheckboxLabeled("Pause Transition", ref animationSettings.pauseOnTransition, "Pause the viewer when transitioning between animation curves.", string.Empty, false);
							lister.CheckboxLabeled("Loop", ref CameraView.animationSettings.loop,
								"Loop the viewer when reaching the end of the animation.", string.Empty, false);
							lister.CheckboxLabeled("Display Ticks", ref CameraView.animationSettings.displayTicks,
								"Display the time remaining in ticks, rather than seconds.", string.Empty, false);
							lister.CheckboxLabeled("Draw Cell Grid",
								ref CameraView.animationSettings.drawCellGrid,
								"Draw lines along edges of the map's cells.", string.Empty, false);
							//lister.Button("Curve", )
						}
						lister.End();

						if (Mouse.IsOver(cameraRect))
						{
							Widgets.DrawTextureFitted(cameraRect, UIData.TransparentBlackBG, 1);

							float buttonSpacing = PlaybackRectHeight / 2;
							Rect playerBarRect = new Rect(cameraRect.x + buttonSpacing,
									cameraRect.yMax - PlaybackRectHeight - 5, PlaybackRectHeight, PlaybackRectHeight)
							 .ContractedBy(2);
							Texture2D pauseTex = AnimationSimulator.Paused ?
								viewerButtonTextures[1] :
								viewerButtonTextures[0];
							if (Widgets.ButtonImage(playerBarRect, pauseTex))
							{
								if (AnimationSimulator.CurrentDriver != null && AnimationSimulator.TicksPassed >=
									AnimationSimulator.CurrentDriver.AnimationLength)
								{
									AnimationSimulator.Reset();
								}
								AnimationSimulator.TogglePause(true);
								SoundDefOf.Click.PlayOneShotOnCamera();
							}
							Rect invisibleClickableWindowRect = new Rect(cameraRect.x, cameraRect.y,
								cameraRect.width, cameraRect.height - playerBarRect.height * 2.5f);
							float playButtonAnimatedSize = cameraRect.width / 5;
							Rect playButtonAnimated = new Rect(cameraRect.x, cameraRect.y, playButtonAnimatedSize,
								playButtonAnimatedSize);
							if (AnimationSimulator.ButtonUpdated(invisibleClickableWindowRect,
								() => timeFading += Time.deltaTime, () => ButtonFadeHandler(playButtonAnimated),
								() => timeFading <= 0, doMouseoverSound: false))
							{
								timeFading = 0;
								AnimationSimulator.TogglePause(true);
								SoundDefOf.Click.PlayOneShotOnCamera();
							}

							string timeCount = CameraView.animationSettings.displayTicks ? "0 / 0" : "0:00/0:00";
							if (AnimationSimulator.CurrentDriver != null)
							{
								if (CameraView.animationSettings.displayTicks)
								{
									timeCount =
										$"{AnimationSimulator.TicksPassed} / {AnimationSimulator.CurrentDriver.AnimationLength}";
								}
								else
								{
									TimeSpan timePassed = new TimeSpan(0, 0,
										Mathf.CeilToInt(AnimationSimulator.TicksPassed.TicksToSeconds()));
									TimeSpan timeMax = new TimeSpan(0, 0,
										Mathf.CeilToInt(
											AnimationSimulator.CurrentDriver.AnimationLength.TicksToSeconds()));
									timeCount = $"{timePassed:m\\:ss} / {timeMax:m\\:ss}";
								}
							}
							using (new TextBlock(GameFont.Small, TextAnchor.MiddleRight))
							{
								Rect timeLeftRect = new Rect(cameraRect.xMax - cameraRect.width / 2 - 10,
									playerBarRect.y,
									cameraRect.width / 2, playerBarRect.height);
								Widgets.Label(timeLeftRect, timeCount);
							}

							Rect progressBarRect = new Rect(cameraRect.x + 8,
								playerBarRect.y - PlaybackBarHeight - PlaybackRectHeight / 3, cameraRect.width - 16,
								PlaybackBarHeight);
							float handleSize = PlaybackHandleSize;
							if (Mouse.IsOver(progressBarRect.ExpandedBy(2)))
							{
								progressBarRect.height *= 1.5f;
								handleSize *= 1.5f;
								progressBarRect.y -= (progressBarRect.height - PlaybackBarHeight) / 2;
							}
							float viewerPercent = 0;
							if (AnimationSimulator.CurrentDriver != null)
							{
								viewerPercent = (float)AnimationSimulator.TicksPassed /
									AnimationSimulator.CurrentDriver.AnimationLength;
							}
							UIElements.FillableBar(progressBarRect, viewerPercent, UIData.FillableBarProgressBar,
								UIData.FillableBarProgressBarBG);

							Rect progressBarHandleRect = new Rect(
								progressBarRect.x + (progressBarRect.width * viewerPercent) - handleSize / 2,
								progressBarRect.y + progressBarRect.height / 2 - handleSize / 2, handleSize,
								handleSize);
							using (new TextBlock(UIData.ProgressBarRed))
							{
								Widgets.DrawTextureFitted(progressBarHandleRect, dragHandleIcon, 1);
							}

							Widgets.DraggableResult result = Widgets.ButtonInvisibleDraggable(progressBarRect);
							if (result == Widgets.DraggableResult.Dragged &&
								AnimationSimulator.CurrentDriver != null)
							{
								dragging = true;
								AnimationSimulator.EditingTicks = true;
							}
							if (!Input.GetMouseButton(0))
							{
								dragging = false;
								AnimationSimulator.EditingTicks = false;
							}
							if ((dragging || result == Widgets.DraggableResult.Pressed) &&
								AnimationSimulator.CurrentDriver != null)
							{
								float percentDrag =
									Mathf.Clamp01((Event.current.mousePosition.x - progressBarRect.x) /
										progressBarRect.width);
								AnimationSimulator.TicksPassed =
									Mathf.RoundToInt(AnimationSimulator.CurrentDriver.AnimationLength * percentDrag);
							}
						}
					}
				}
				catch (Exception ex)
				{
					disabledView = true;
					darkenWindow = true;
					previewWindowString = ex.ToString();
					Log.ErrorOnce($"Exception thrown in CameraView. Exception = {ex}",
						"CameraView_GraphEditor".GetHashCode());
				}
			}
			if (disabledView)
			{
				UIElements.Header(rect, previewWindowString,
					darkenWindow ? new Color(0, 0, 0, 0.75f) : ListingExtension.BannerColor,
					anchor: TextAnchor.MiddleCenter);
			}
		}

		private void SelectCurveType()
		{
			List<FloatMenuOption> floatMenuOptions = new List<FloatMenuOption>
			{
				new FloatMenuOption(Graph.GraphType.Linear.ToString(),
					() => GraphType = Graph.GraphType.Linear),
				new FloatMenuOption(Graph.GraphType.Bezier.ToString(),
					() => GraphType = Graph.GraphType.Bezier),
				new FloatMenuOption(Graph.GraphType.Lagrange.ToString(),
					() => GraphType = Graph.GraphType.Lagrange),
				new FloatMenuOption(Graph.GraphType.Staircase.ToString(),
					() => GraphType = Graph.GraphType.Staircase)
			};
			//floatMenuOptions.Add(new FloatMenuOption(Graph.GraphType.Freeform.ToString(), () => graphType = Graph.GraphType.Freeform));
			Find.WindowStack.Add(new FloatMenu(floatMenuOptions));
		}

		private void SaveEdits(bool reportFail = true)
		{
			if (curAnimator != null)
			{
				curAnimator.SetCurve(curve);
				Messages.Message($"Animation data saved to {curAnimator.DisplayName}",
					MessageTypeDefOf.NeutralEvent);
			}
			else if (reportFail)
			{
				Messages.Message($"Unable to save animation data. No animator loaded.",
					MessageTypeDefOf.RejectInput);
			}
		}

		private void ExportAnimationXml()
		{
			if (animationTarget == null)
			{
				Messages.Message($"Unable to export animation data. No animation target loaded.",
					MessageTypeDefOf.RejectInput);
				return;
			}
			else if (animators.NullOrEmpty())
			{
				Messages.Message($"Unable to export animation data. No animations to export.",
					MessageTypeDefOf.RejectInput);
				return;
			}
			bool exported = true;
			try
			{
				SaveEdits(false);
				XmlExporter.StartDocument(filePath);
				{
					XmlExporter.OpenNode("Animations");
					{
						IOrderedEnumerable<AnimatorObject> animatorsOrdered =
							animators.OrderBy(anim => anim.category);
						string category = animatorsOrdered.FirstOrDefault().category;
						string prefix = animatorsOrdered.FirstOrDefault().prefix;
						XmlExporter.OpenNode(category);
						{
							foreach (AnimatorObject animatorObject in animatorsOrdered)
							{
								if (category != animatorObject.category)
								{
									XmlExporter.CloseNode(); //Close previous node
									category = animatorObject.category;
									XmlExporter.OpenNode(category); //Open new prefix
								}
								if (!animatorObject.prefix.NullOrEmpty() && prefix != animatorObject.prefix)
								{
									XmlExporter.CloseNode(); //Close previous node
									prefix = animatorObject.prefix;
									XmlExporter.OpenNode(prefix); //Open new prefix
								}
								Type curveType = animatorObject.Curve.GetType();
								(string name, string value) attribute =
									curveType.IsSubclassOf(typeof(LinearCurve)) ?
										("Class", $"{curveType.Namespace}.{curveType.Name}") :
										(string.Empty, string.Empty);

								XmlExporter.OpenNode(animatorObject.fieldInfo.Name, attribute);
								{
									if (animatorObject.Curve.PointsCount > 0)
									{
										XmlExporter.OpenNode(nameof(LinearCurve.points));
										{
											foreach (CurvePoint curvePoint in animatorObject.Curve)
											{
												XmlExporter.OpenNode("li");
												{
													//Not using ToString overload, since it outputs 2 digit places regardless of decimal places in the floats eg. 1.00 instead of 1
													XmlExporter.WriteString($"({curvePoint.x:0.##}, {curvePoint.y:0.##})");
												}
												XmlExporter.CloseNode();
											}
										}
										XmlExporter.CloseNode();
									}
								}
								XmlExporter.CloseNode();
							}
						}
						XmlExporter.CloseNode();
					}
					XmlExporter.CloseNode();
				}
				XmlExporter.Export();
			}
			catch (Exception ex)
			{
				exported = false;
				Log.Error($"Unable to export animation data. Exception = {ex}");
			}
			finally
			{
				XmlExporter.Close();
			}

			if (exported)
			{
				SoundDefOf.Click.PlayOneShotOnCamera();
			}
			else
			{
				SoundDefOf.ClickReject.PlayOneShotOnCamera();
			}
		}

		private static void ButtonFadeHandler(Rect rect)
		{
			float size = rect.width;
			float percentFaded = timeFading / PlayButtonFadeTime;
			float expanded = Mathf.Lerp(size, size * 2, percentFaded);
			rect.size = new Vector2(expanded, expanded);
			float alpha = Mathf.Lerp(0.5f, 0, percentFaded);
			using (new TextBlock(new Color(0, 0, 0, alpha)))
			{
				Widgets.DrawTextureFitted(rect, dragHandleIcon, 1);
			}
			using (new TextBlock(new Color(1, 1, 1, alpha)))
			{
				Widgets.DrawTextureFitted(rect, viewerButtonTextures[0], 1);
			}
		}

		public class AnimationSettings
		{
			public float orthographicSize = 4;
			public bool pauseOnTransition = false;
			public bool loop = false;
			public bool displayTicks = true;
			public bool drawCellGrid = false;
		}
	}
}