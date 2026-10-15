using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SmashTools
{
	public class AnimationDriver
	{
		private string label;
		private Func<int, int> animator;
		private AnimationDrawer drawHandler;
		private int totalAnimationTicks;
		private Action onSelect;

		public delegate (Vector3 drawPos, float rotation) AnimationDrawer(Vector3 drawPos, float rotation);

		public AnimationDriver(string label, Func<int, int> animator, AnimationDrawer drawHandler, int totalAnimationTicks, Action onSelect = null)
		{
			this.label = label;
			this.animator = animator;
			this.drawHandler = drawHandler;
			this.totalAnimationTicks = totalAnimationTicks;
			this.onSelect = onSelect;
		}

		public string Name => label;

		public int AnimationLength => totalAnimationTicks;

		public (Vector3 drawPos, float rotation) Draw(Vector3 drawPos, float rotation) => drawHandler(drawPos, rotation);

		public void Tick(int ticksPassed)
		{
			animator(ticksPassed);
		}

		public void Select()
		{
			onSelect?.Invoke();
		}
	}
}
