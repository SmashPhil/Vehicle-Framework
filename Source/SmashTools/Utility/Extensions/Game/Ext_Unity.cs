using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace SmashTools
{
	public static class Ext_Unity
	{
		/// <summary>
		/// Convert <paramref name="vector"/> to <see cref="Pair{T1, T2}"/>
		/// </summary>
		/// <param name="vector"></param>
		public static Pair<float, float> ToPair(this Vector2 vector)
		{
			return new Pair<float, float>(vector.x, vector.y);
		}

		/// <summary>
		/// Convert <paramref name="vector"/> to <see cref="Pair{T1, T2}"/>
		/// </summary>
		/// <param name="vector"></param>
		public static Pair<float, float> ToPair(this Vector3 vector)
		{
			return new Pair<float, float>(vector.x, vector.z);
		}

		/// <summary>
		/// Mirror <paramref name="vector"/> around (0, 0, 0)
		/// </summary>
		/// <param name="vector"></param>
		public static Vector3 MirrorHorizontal(this Vector3 vector)
		{
			return new Vector3(-vector.x, vector.y, vector.z);
		}

		/// <summary>
		/// Mirror <paramref name="vector"/> around (0, 0, 0)
		/// </summary>
		/// <param name="vector"></param>
		public static Vector3 MirrorVertical(this Vector3 vector)
		{
			return new Vector3(vector.x, vector.y, -vector.z);
		}
	}
}
