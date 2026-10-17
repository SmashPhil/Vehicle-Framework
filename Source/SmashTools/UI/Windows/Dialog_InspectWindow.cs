using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace SmashTools
{
	[Obsolete]
	public class Dialog_InspectWindow : Window
	{
		private IInspectable inspectable;
		private Vector2 size = new Vector2(950, 760);

		public Dialog_InspectWindow(IInspectable inspectable)
		{
			this.inspectable = inspectable;

			forcePause = true;
			doCloseButton = true;
			doCloseX = true;
			absorbInputAroundWindow = true;
			closeOnClickedOutside = true;
			soundAppear = SoundDefOf.InfoCard_Open;
			soundClose = SoundDefOf.InfoCard_Close;
		}

		public Dialog_InspectWindow(IInspectable inspectable, Vector2 size) : this(inspectable)
		{
			this.size = size;
			SetInitialSizeAndPosition();
		}

		public override Vector2 InitialSize => size;

		public override void PreOpen()
		{
			base.PreOpen();
			//inspectable.InspectOpen();
		}

		public override void PostClose()
		{
			base.PostClose();
			//inspectable.InspectClose();
		}

		public override void DoWindowContents(Rect inRect)
		{
			//inspectable.DrawInspectDialog(inRect);
		}
	}
}
