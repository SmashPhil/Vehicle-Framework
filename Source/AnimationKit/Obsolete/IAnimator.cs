using SmashTools.Animations;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;

namespace SmashTools.Animations
{
	public interface IAnimator : IAnimationObject
	{
		AnimationManager Manager { get; }

		ModContentPack ModContentPack { get; }

		Vector3 DrawPos { get; }
	}
}
