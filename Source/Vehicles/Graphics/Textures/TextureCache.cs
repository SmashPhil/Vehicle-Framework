using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	public static class TextureCache
	{
		private static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

		public static Texture2D Get(string path, bool reportFailure = true)
		{
			if (!cache.TryGetValue(path, out Texture2D texture))
			{
				texture = ContentFinder<Texture2D>.Get(path, reportFailure);
				cache.Add(path, texture);
			}
			return texture;
		}
	}
}
