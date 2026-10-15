using UnityEngine;

namespace SmashTools
{
	public static class Ext_Color
	{
		/// <summary>
		/// Add to RGB color values without affecting alpha channel
		/// </summary>
		/// <param name="color"></param>
		/// <param name="r"></param>
		/// <param name="g"></param>
		/// <param name="b"></param>
		public static Color AddNoAlpha(this Color color, float r, float g, float b)
		{
			return new Color(color.r + r, color.g + g, color.b + b);
		}

		/// <summary>
		/// Subtract RGB color values without affecting alpha channel
		/// </summary>
		/// <param name="color"></param>
		/// <param name="r"></param>
		/// <param name="g"></param>
		/// <param name="b"></param>
		public static Color SubtractNoAlpha(this Color color, float r, float g, float b)
		{
			return new Color(color.r - r, color.g - g, color.b - b);
		}

		/// <summary>
		/// <see cref="AddNoAlpha(Color, float, float, float)"/> in 32 bit format
		/// </summary>
		/// <param name="color"></param>
		/// <param name="r"></param>
		/// <param name="g"></param>
		/// <param name="b"></param>
		public static Color Add255NoAlpha(this Color color, int r, int g, int b)
		{
			float r32 = r / 255f;
			float g32 = g / 255f;
			float b32 = b / 255f;
			return color.AddNoAlpha(r32, g32, b32);
		}

		/// <summary>
		/// <see cref="SubtractNoAlpha(Color, float, float, float)"/> in 32 bit format
		/// </summary>
		/// <param name="color"></param>
		/// <param name="r"></param>
		/// <param name="g"></param>
		/// <param name="b"></param>
		public static Color Subtract255NoAlpha(this Color color, int r, int g, int b)
		{
			float r32 = r / 255f;
			float g32 = g / 255f;
			float b32 = b / 255f;
			return color.SubtractNoAlpha(r32, g32, b32);
		}
	}
}
