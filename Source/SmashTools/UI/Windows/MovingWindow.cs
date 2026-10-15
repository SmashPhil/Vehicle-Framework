using System;
using UnityEngine;
using Verse;

namespace SmashTools
{
	/// <summary>
	/// Window abstract for moving dialogs with max drift
	/// </summary>
	public abstract class MovingWindow : Window
	{
		protected bool driftOut = false;
		private bool driftingOut = false;

		protected Action clickAction;

		protected int ticksActive;
		private Vector2 drift;

		public override Vector2 InitialSize => new Vector2(250, 75);
		protected override float Margin => 10f;
		protected virtual Vector2 MaxDrift => Vector2.zero;
		protected virtual Vector2 FloatSpeed => Vector2.zero;
		protected virtual int TicksTillRemoval => -1;
		protected virtual Vector2 WindowPosition => new Vector2(windowRect.x, windowRect.y);

		public override void PreOpen()
		{
			base.PreOpen();
			drift = Vector2.zero;
			ticksActive = 0;
			windowRect.x = WindowPosition.x;
			windowRect.y = WindowPosition.y;
		}

		public override void DoWindowContents(Rect inRect)
		{
			Drift();
			TicksActive();

			if (Widgets.ButtonInvisible(inRect))
			{
				if (clickAction != null)
					clickAction();
				else
					Close(false);
			}
		}

		protected virtual void Drift()
		{
			if (driftingOut)
			{
				windowRect.x -= FloatSpeed.x;
				drift.x -= Mathf.Abs(FloatSpeed.x);
				
				windowRect.y -= FloatSpeed.y;
				drift.y -= Mathf.Abs(FloatSpeed.y);

				if(ticksActive >= (TicksTillRemoval * 2) || (windowRect.position == WindowPosition))
				{
					Close(false);
				}
			}
			else
			{
				if (drift.x < MaxDrift.x)
				{
					windowRect.x += FloatSpeed.x;
					drift.x += Mathf.Abs(FloatSpeed.x);
				}
				if (drift.y < MaxDrift.y)
				{
					windowRect.y += FloatSpeed.y;
					drift.y += Mathf.Abs(FloatSpeed.y);
				} 
			}
		}

		protected virtual void TicksActive()
		{
			ticksActive++;
			if(TicksTillRemoval > 0 && ticksActive > TicksTillRemoval)
			{
				if(driftOut)
				{
					driftingOut = true;
				}
				else
				{
					Close(false);
				}
			}
		}
	}
}
