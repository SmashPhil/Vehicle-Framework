using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.Sound;

namespace SmashTools
{
	public static class AnimationSimulator
	{
		internal static readonly float[] playbackSpeeds = { 0.25f, 0.5f, 0.75f, 1, 2, 3, 4 };
		private static float realTimeToTick;
		private static int ticksThisFrame;
		private static int totalTicksPassed = 0;

		private static IAnimationTarget animationTarget;
		private static AnimationDriver animationDriver;
		private static Ticker ticker;

		private static List<AnimatedButton> animatedButtons = new List<AnimatedButton>();

		public delegate void Ticker();

		public static bool Paused { get; set; }

		public static bool EditingTicks { get; set; }

		public static bool InUse => animationTarget != null;

		public static IAnimationTarget AnimationTarget => animationTarget;

		public static AnimationDriver CurrentDriver => animationDriver;

		public static float PlaybackSpeed { get; set; } = 1;

		public static bool PausedNoEdit
		{
			get
			{
				return Paused && !EditingTicks;
			}
		}

		public static int TicksPassed
		{
			get
			{
				return totalTicksPassed;
			}
			set
			{
				totalTicksPassed = value.Clamp(0, animationDriver?.AnimationLength ?? int.MaxValue);
			}
		}

		public static float TimePerTick
		{
			get
			{
				if (PausedNoEdit)
				{
					return 0;
				}
				return 1 / (60f * PlaybackSpeed);
			}
		}

		public static bool Reserve(IAnimationTarget animationTarget, Ticker ticker)
		{
			if (AnimationSimulator.animationTarget != null && AnimationSimulator.animationTarget != animationTarget)
			{
				Log.Error($"Attempting to reserve AnimationManager while it's already in use.  It should only be used by 1 updater at a time to avoid duplicating tick calls.");
				return false;
			}
			AnimationSimulator.animationTarget = animationTarget;
			AnimationSimulator.ticker = ticker;
			Paused = true;
			return true;
		}

		public static void Release()
		{
			Reset();
			animationTarget = null;
			animatedButtons.Clear();
		}

		public static void Reset()
		{
			TicksPassed = 0;
			realTimeToTick = 0;
			ticksThisFrame = 0;
			Paused = true;
		}

		public static void SetDriver(AnimationDriver animationDriver)
		{
			AnimationSimulator.animationDriver = animationDriver;
			AnimationSimulator.animationDriver?.Select();
		}

		public static void TogglePause(bool validate = false)
		{
			Paused = !Paused;
			if (validate && !Paused)
			{
				Paused = animationDriver is null;
				if (animationDriver is null)
				{
					Messages.Message("Must select animation driver in order to play animation.", MessageTypeDefOf.RejectInput);
				}
			}
		}

		public static void OnGUI()
		{
			UpdateAnimatedButtons_OnGUI();
		}

		public static void Update()
		{
			UpdateAnimatedButtons_Update();
			if (InUse)
			{
				ticksThisFrame = 0;
				if (!PausedNoEdit)
				{
					float timePerTick = TimePerTick;
					if (Mathf.Abs(Time.deltaTime - timePerTick) < timePerTick * 0.1f)
					{
						realTimeToTick += timePerTick;
					}
					else
					{
						realTimeToTick += Time.deltaTime;
					}

					while (realTimeToTick > 0f && ticksThisFrame < PlaybackSpeed * 2f)
					{
						DoSingleTick();
						realTimeToTick -= timePerTick;
						ticksThisFrame++;
						if (PausedNoEdit)
						{
							break;
						}
					}
					if (realTimeToTick > 0f)
					{
						realTimeToTick = 0f;
					}
				}
			}
		}

		private static void DoSingleTick()
		{
			TicksPassed++;
			ticker();
		}

		public static bool ButtonUpdated(Rect rect, Action updateHandler, Action onGUI, Func<bool> exitCondition, bool doMouseoverSound = true)
		{
			bool clicked = Widgets.ButtonInvisible(rect, doMouseoverSound);
			if (clicked)
			{
				animatedButtons.Add(new AnimatedButton(updateHandler, onGUI, exitCondition));
			}
			return clicked;
		}

		private static void UpdateAnimatedButtons_Update()
		{
			for (int i = animatedButtons.Count - 1; i >= 0; i--)
			{
				AnimatedButton animatedButton = animatedButtons[i];

				if (!animatedButton.Update())
				{
					animatedButtons.RemoveAt(i);
				}
			}
		}

		private static void UpdateAnimatedButtons_OnGUI()
		{
			foreach (AnimatedButton animatedButton in animatedButtons)
			{
				animatedButton.OnGUI();
			}
		}

		public class AnimatedButton
		{
			private Action updateHandler;
			private Action onGUI;
			private Func<bool> exitCondition;
			
			public AnimatedButton(Action updateHandler, Action onGUI, Func<bool> exitCondition)
			{
				this.updateHandler = updateHandler;
				this.onGUI = onGUI;
				this.exitCondition = exitCondition;
			}

			public void OnGUI()
			{
				onGUI?.Invoke();
			}

			public bool Update()
			{
				updateHandler?.Invoke();
				if (exitCondition())
				{
					return false;
				}
				return true;
			}
		}
	}
}
