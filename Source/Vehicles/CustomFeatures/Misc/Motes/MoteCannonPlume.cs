using Verse;
using SmashTools;

namespace Vehicles
{
	public class MoteCannonPlume : Mote
	{
		protected int ticksActive = 0;
		protected int frame = 0;
		protected bool reverse = false;

		public int cyclesLeft;
		public float angle;
		public AnimationWrapperType animationType;

		public new Graphic_Animate Graphic => base.Graphic as Graphic_Animate;
		public virtual bool AnimationFinished => frame < 0 || frame > Graphic.AnimationCount;

		public override void Draw()
		{
			exactPosition.y = def.altitudeLayer.AltitudeFor();
			Graphic.DrawWorkerAnimated(this, frame, angle.ClampAndWrap(0, 360));
		}

		public override void Tick()
		{
			base.Tick();
			switch (animationType)
			{
				case AnimationWrapperType.Reset:
					TickReset();
					break;
				case AnimationWrapperType.Oscillate:
					TickOscillate();
					break;
				case AnimationWrapperType.Off:
					Destroy();
					break;
			}
			ticksActive++;
		}

		public virtual void TickOscillate()
		{
			if (ticksActive >= Graphic.AnimationCount - 1)
			{
				if (reverse)
				{
					cyclesLeft--;
					if (cyclesLeft <= 0)
					{
						Destroy();
					}
				}
				ticksActive = 0;
				reverse = true;
			}
			if (reverse)
			{
				frame--;
			}
			else
			{
				frame++;
			}
		}

		public virtual void TickReset()
		{
			if (ticksActive >= Graphic.AnimationCount - 1)
			{
				cyclesLeft--;
				if (cyclesLeft <= 0)
				{
					Destroy();
				}
				ticksActive = 0;
				frame = 0;
			}
			frame++;
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			if (!(base.Graphic is Graphic_Animate))
			{
				Log.Error($"Cannot spawn Mote_Animated without using Graphic_Animate class.");
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref ticksActive, "ticksActive");
			Scribe_Values.Look(ref frame, "frame");
			Scribe_Values.Look(ref reverse, "reverse");

			Scribe_Values.Look(ref cyclesLeft, "cyclesLeft");
			Scribe_Values.Look(ref angle, "angle");
			Scribe_Values.Look(ref animationType, "animationType");
		}
	}
}
