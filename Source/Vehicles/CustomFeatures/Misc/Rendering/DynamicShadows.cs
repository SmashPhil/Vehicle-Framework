using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Vehicles
{
	public static class DynamicShadows
	{
		private const float AlphaIsoThreshold = 0.5f;

		private static readonly Dictionary<int, Mesh> shadowMeshDict = new Dictionary<int, Mesh>();

		public static Mesh GetShadowMesh(Texture2D texture, ShadowData shadowData)
		{
			return GetShadowMesh(texture, shadowData.BaseX, shadowData.BaseZ, shadowData.BaseY);
		}

		public static Mesh GetShadowMesh(Texture2D texture, float baseWidth, float baseHeight, float tallness)
		{
			int key = HashOf(texture, baseWidth, baseHeight, tallness);
			if (!shadowMeshDict.TryGetValue(key, out Mesh mesh))
			{
				//CreateMeshFromTexture(texture, baseWidth, baseHeight, tallness);
				mesh = MeshMakerShadows.NewShadowMesh(baseWidth, baseHeight, tallness);
				shadowMeshDict.Add(key, mesh);
			}
			return mesh;
		}

		/// <summary>
		/// Generates mesh based on <paramref name="texture"/>
		/// </summary>
		/// <remarks>See https://en.wikipedia.org/wiki/Marching_squares for algorithm</remarks>
		public static Mesh CreateMeshFromTexture(Texture2D texture, float baseWidth, float baseHeight, float tallness)
		{
			int width = texture.width - 1;
			int height = texture.height - 1;

			int[,] contouringGrid = new int[width, height];
			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					int value = 0;
					//Get 4 corners of contour cell for bit value
					for (int i = 0; i < 2; i++)
					{
						for (int j = 0; j < 2; j++)
						{
							Color pixelColor = texture.GetPixel(x + i, y + j);
							bool validPixel = pixelColor.a >= AlphaIsoThreshold;
							value |= (validPixel ? 1 : 0) & (1 << (i + j));
						}
					}
					contouringGrid[x, y] = value;
					Log.Message($"Setting {value} at ({x}, {y})");
				}
			}
			return null;
		}

		private static int HashOf(Texture2D texture, float baseWidth, float baseheight, float tallness)
		{
			int num = (int)(baseWidth * 1000f);
			int num2 = (int)(baseheight * 1000f);
			int num3 = (int)(tallness * 1000f);
			return Gen.HashCombineInt(num * 391 ^ 261231 ^ num2 * 612331 ^ num3 * 456123, texture.GetHashCode());
		}
	}
}
