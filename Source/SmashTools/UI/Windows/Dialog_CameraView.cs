using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;
using Verse.Sound;

namespace SmashTools.Animations
{
	public class Dialog_CameraView : Window
	{
		private const float WindowMargin = 15;
		private const float LeftWindowWidth = 160;
		private const float WindowRatio = (LeftWindowWidth + DefaultViewSize) / DefaultViewSize;

		private const float LeftWindowMinHeight = 250;
		private const float MinWidth = LeftWindowMinHeight * WindowRatio + WindowMargin * 2;

		private const float DefaultViewSize = 300;
		private const float ResizerBtnSize = 24;

		private readonly Func<bool> disabled;
		private readonly Func<Vector3> position;

		private Listing_SplitColumns lister = new Listing_SplitColumns();
		private Vector2 windowPosition;
		private Vector2 windowSize;

		/* ----- Resizing ----- */

		private bool resizing = false;
		private bool needsResizing = false;
		private Rect startingWindowRect;
		private Rect resizeLaterRect;

		/* ------------------------------- */

		public Dialog_CameraView(Func<bool> disabled, Func<Vector3> position, Vector2 bottomRight, float size = 1)
		{
			this.disabled = disabled;
			this.position = position;

			windowSize = new Vector2(LeftWindowWidth + DefaultViewSize * size, DefaultViewSize * size);
			windowPosition = new Vector2(bottomRight.x - windowSize.x, bottomRight.y - windowSize.y);
			SetWindowProperties();
		}

		public override Vector2 InitialSize => windowSize;

		protected override float Margin => 0;

		private void SetWindowProperties()
		{
			this.closeOnClickedOutside = false;
			this.draggable = true;
			this.absorbInputAroundWindow = false;
			this.preventCameraMotion = false;
			this.doCloseX = true;
			this.layer = WindowLayer.Super;
		}

		protected override void SetInitialSizeAndPosition()
		{
			windowRect = new Rect(windowPosition, windowSize).Rounded();
		}

		public override void WindowUpdate()
		{
			base.WindowUpdate();
			if (!disabled())
			{
				if (CameraView.animationSettings.drawCellGrid)
				{
					CameraView.DrawMapGridInView();
				}
				CameraView.Update(position());
			}
		}

		public override void WindowOnGUI()
		{
			if (needsResizing)
			{
				needsResizing = false;
				windowRect = resizeLaterRect;
			}
			base.WindowOnGUI();
		}

		public override void DoWindowContents(Rect inRect)
		{
			Widgets.DrawWindowBackground(inRect);
			DoResizerButton(windowRect);
			Rect rect = inRect.ContractedBy(WindowMargin);

			DrawPreviewWindow(rect, disabled.Invoke());
		}

		private void DrawPreviewWindow(Rect rect, bool disabled)
		{
			string previewWindowString = "Preview Window Disabled";

			bool darkenWindow = false;
			if (!disabled)
			{
				try
				{
					float camRectSize = rect.height;// * PercentCameraBoxWidth;
					Rect cameraRect = new Rect(rect.x + rect.width - camRectSize, rect.y, camRectSize, camRectSize);
					disabled = !CameraView.RenderAt(cameraRect);

					if (!disabled)
					{
						using (new TextBlock(Color.white))
						{
							Rect leftColumnRect = new Rect(rect.x, rect.y, rect.width - camRectSize - 1, camRectSize).ContractedBy(5);
							lister.columnGap = 0;
							lister.Begin(leftColumnRect, 1);
							{
								lister.Header("Settings", anchor: TextAnchor.MiddleCenter);
								lister.CheckboxLabeled("Draw Cell Grid", ref CameraView.animationSettings.drawCellGrid, "Draw lines along edges of the map's cells.", string.Empty, false);
							}
							lister.End();

							if (Mouse.IsOver(cameraRect))
							{
								CameraView.HandleZoom();
							}
						}
					}
				}
				catch (Exception ex)
				{
					disabled = true;
					darkenWindow = true;
					previewWindowString = ex.ToString();
					Log.ErrorOnce($"Exception thrown in CameraView. Exception = {ex}", "CameraView_GraphEditor".GetHashCode());
				}
			}
			if (disabled)
			{
				UIElements.Header(rect, previewWindowString, darkenWindow ? new Color(0, 0, 0, 0.75f) : ListingExtension.BannerColor, anchor: TextAnchor.MiddleCenter);
			}
		}

		private void DoResizerButton(Rect winRect)
		{
			Vector2 mousePosition = Event.current.mousePosition;
			Rect rect = new Rect(winRect.width - ResizerBtnSize, winRect.height - ResizerBtnSize, ResizerBtnSize, ResizerBtnSize);
			if (Event.current.type == EventType.MouseDown && Mouse.IsOver(rect))
			{
				resizing = true;
				startingWindowRect = new Rect(mousePosition.x, mousePosition.y, winRect.width, winRect.height);
			}
			if (resizing)
			{
				float diffX = (mousePosition.x - startingWindowRect.x);
				float diffY = (mousePosition.y - startingWindowRect.y);

				float maxSize = Mathf.Max(diffX, diffY);

				winRect.width = startingWindowRect.width + maxSize;
				winRect.height = startingWindowRect.height + maxSize;

				if (winRect.width < MinWidth || winRect.height < LeftWindowMinHeight)
				{
					winRect.width = MinWidth;
					winRect.height = LeftWindowMinHeight;
				}
				if (winRect.xMax > UI.screenWidth || winRect.yMax > UI.screenHeight)
				{
					return;
				}
				
				if (Event.current.type == EventType.MouseUp)
				{
					resizing = false;
				}
				if (winRect != resizeLaterRect)
				{
					needsResizing = true;
					resizeLaterRect = winRect;
				}
			}
			Widgets.ButtonImage(rect, TexUI.WinExpandWidget);
		}
	}
}
