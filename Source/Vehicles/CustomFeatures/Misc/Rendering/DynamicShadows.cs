using SmashTools;
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

		//TopLeft = 4th bit, TopRight = 3rd bit, BottomLeft = 1st bit, BottomRight = 2nd bit
		//Result: able to loop CW from top left, but bit place starts bottom left and goes CCW for contour values
		private static int[] bitArray = { 4, 3, 1, 2 }; 

		public static Mesh GetShadowMesh(Texture2D texture, ShadowData shadowData)
		{
			return GetShadowMesh(texture, shadowData.BaseX, shadowData.BaseZ, shadowData.BaseY);
		}

		public static Mesh GetShadowMesh(Texture2D texture, float baseWidth, float baseHeight, float tallness)
		{
			int key = HashOf(texture, baseWidth, baseHeight, tallness);
			if (!shadowMeshDict.TryGetValue(key, out Mesh mesh))
			{
				CreateMeshFromTexture(texture, baseWidth, baseHeight, tallness);
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
			Texture2D readableTex = Ext_Texture.CreateReadableTexture(texture);
			try
			{
				Color[] texData = readableTex.GetPixels();

				int width = texture.width - 1;
				int height = texture.height - 1;
				int[,] contourGrid = new int[width, height];
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
								Color pixelColor = texData[(x + i) + (y + j)];
								if (pixelColor.a >= AlphaIsoThreshold)
								{
									int contourIndex = bitArray[i + j];
									value |= 1 << contourIndex; //Sets 2nd to 5th bit, or 1 -> 2 -> 4 -> 8 CCW starting from bottom left corner
								}
							}
						}
						//Final value between 0 and 15, depending on bit values of pixels. See link for mapping bit values to contour lines
						contourGrid[x, y] = value;
					}
				}
			}
			finally
			{
				UnityEngine.Object.Destroy(readableTex);
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
