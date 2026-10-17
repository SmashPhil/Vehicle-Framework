using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace UpdateLogTool
{
	public abstract class AnimatedSegment : ImageSegment
	{
		protected abstract int DefaultFramesPerSecond { get; }

		protected virtual int DefaultDelayOnReset { get; } = 0;

		protected virtual int CurrentFrame(int maxFrames, int fps, int delay)
		{
			return Mathf.Clamp((Mathf.FloorToInt(Time.time * fps) % (maxFrames + delay)) - delay, 0, maxFrames);
		}
	}
}
