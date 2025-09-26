using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;

namespace Vehicles
{
	public class ColorStorage : IExposable
	{
		public const int PaletteCount = 20;
		public const int PaletteCountPerRow = 5;

		public List<(Color, Color, Color)> colorPalette = new List<(Color, Color, Color)>();

		private List<Color> paletteColorOnes = new List<Color>();
		private List<Color> paletteColorTwos = new List<Color>();
		private List<Color> paletteColorThrees = new List<Color>();

		public ColorStorage()
		{
		}

		public static int PaletteRowCount => PaletteCount / PaletteCountPerRow;

		public void ResetPalettes()
		{
			colorPalette = new List<ValueTuple<Color, Color, Color>>()
			{
				new ValueTuple<Color, Color, Color>(new Color(0.306f, 0.329f, 0.306f), new Color(0.255f, 0.263f, 0.239f), new Color(0.388f, 0.271f, 0.231f)),
				new ValueTuple<Color, Color, Color>(new Color(0.569f, 0.318f, 0.259f), new Color(0.557f, 0.282f, 0.263f), new Color(0.333f, 0.200f, 0.157f)),
				new ValueTuple<Color, Color, Color>(new Color(0.565f, 0.545f, 0.533f), new Color(0.388f, 0.365f, 0.353f), new Color(0.278f, 0.259f, 0.251f)),
				new ValueTuple<Color, Color, Color>(new Color(0.533f, 0.576f, 0.753f), new Color(0.396f, 0.451f, 0.569f), new Color(0.380f, 0.380f, 0.424f)),
				new ValueTuple<Color, Color, Color>(new Color(0.580f, 0.580f, 0.580f), new Color(0.392f, 0.392f, 0.392f), new Color(0.196f, 0.196f, 0.196f)),

				new ValueTuple<Color, Color, Color>(new Color(0.502f, 0.216f, 0.216f), new Color(0.263f, 0.349f, 0.699f), new Color(0.722f, 0.722f, 0.722f)),
				new ValueTuple<Color, Color, Color>(new Color(0.886f, 0.843f, 0.647f), new Color(0.561f, 0.478f, 0.353f), new Color(0.463f, 0.380f, 0.271f)),
				new ValueTuple<Color, Color, Color>(new Color(0.412f, 0.643f, 0.565f), new Color(0.345f, 0.490f, 0.467f), new Color(0.337f, 0.353f, 0.376f)),
				new ValueTuple<Color, Color, Color>(new Color(0.788f, 0.804f, 0.753f), new Color(0.686f, 0.698f, 0.631f), new Color(0.490f, 0.518f, 0.427f)),
				new ValueTuple<Color, Color, Color>(new Color(0.361f, 0.275f, 0.118f), new Color(0.290f, 0.204f, 0.063f), new Color(0.667f, 0.553f, 0.388f)),

				new ValueTuple<Color, Color, Color>(new Color(0.573f, 0.537f, 0.514f), new Color(0.329f, 0.294f, 0.267f), new Color(0.263f, 0.239f, 0.227f)),
				new ValueTuple<Color, Color, Color>(new Color(0.451f, 0.573f, 0.580f), new Color(0.345f, 0.478f, 0.475f), new Color(0.227f, 0.290f, 0.278f)),
				new ValueTuple<Color, Color, Color>(new Color(0.741f, 0.714f, 0.678f), new Color(0.792f, 0.769f, 0.733f), new Color(0.365f, 0.345f, 0.302f)),
				new ValueTuple<Color, Color, Color>(new Color(0.549f, 0.667f, 0.482f), new Color(0.349f, 0.494f, 0.275f), new Color(0.200f, 0.353f, 0.125f)),
				new ValueTuple<Color, Color, Color>(new Color(0.871f, 0.741f, 0.235f), new Color(0.969f, 0.843f, 0.420f), new Color(0.627f, 0.502f, 0.039f)),

				new ValueTuple<Color, Color, Color>(new Color(0.208f, 0.192f, 0.188f), new Color(0.333f, 0.314f, 0.302f), new Color(0.141f, 0.141f, 0.141f)),
				new ValueTuple<Color, Color, Color>(new Color(0.420f, 0.443f, 0.192f), new Color(0.310f, 0.341f, 0.149f), new Color(0.365f, 0.243f, 0.031f)),
				new ValueTuple<Color, Color, Color>(new Color(0.937f, 0.937f, 0.937f), new Color(0.722f, 0.725f, 0.722f), new Color(0.776f, 0.847f, 0.902f)),
				new ValueTuple<Color, Color, Color>(new Color(0.576f, 0.537f, 0.490f), new Color(0.369f, 0.302f, 0.243f), new Color(0.149f, 0.114f, 0.094f)),
				new ValueTuple<Color, Color, Color>(new Color(0.580f, 0.592f, 0.459f), new Color(0.451f, 0.475f, 0.353f), new Color(0.839f, 0.859f, 0.259f)),
			};
		}

		public void AddPalette(Color colorOne, Color colorTwo, Color colorThree, int index)
		{
			if (index >= PaletteCount || index < 0)
			{
				Log.Error("Attempting to set size of ColorPalette that is larger than predetermined.");
				return;
			}
			colorPalette[index] = new ValueTuple<Color, Color, Color>(colorOne, colorTwo, colorThree);
			VehicleMod.settings.Write();
		}

		public (Color colorOne, Color colorTwo, Color colorThree) GetRandomPalette()
		{
			return colorPalette.RandomElement();
		}

		public void ExposeData()
		{
			if (Scribe.mode == LoadSaveMode.Saving)
			{
				if (colorPalette is null || colorPalette.Count != PaletteCount)
				{
					ResetPalettes();
				}
				paletteColorOnes = colorPalette.Select(c => c.Item1).ToList();
				paletteColorTwos = colorPalette.Select(c => c.Item2).ToList();
				paletteColorThrees = colorPalette.Select(c => c.Item3).ToList();
				if (paletteColorOnes.Count != paletteColorTwos.Count || paletteColorOnes.Count != paletteColorThrees.Count)
				{
					Log.Error("Unequal count of color palettes in unzipped lists. All 3 lists should contain the same amount.");
				}
			}
			Scribe_Collections.Look(ref paletteColorOnes, nameof(paletteColorOnes), LookMode.Value);
			Scribe_Collections.Look(ref paletteColorTwos, nameof(paletteColorTwos), LookMode.Value);
			Scribe_Collections.Look(ref paletteColorThrees, nameof(paletteColorThrees), LookMode.Value);

			if (Scribe.mode == LoadSaveMode.LoadingVars)
			{
				colorPalette = new List<ValueTuple<Color, Color, Color>>();
				for (int i = 0; i < paletteColorOnes.Count; i++)
				{
					colorPalette.Add(new ValueTuple<Color, Color, Color>(paletteColorOnes[i], paletteColorTwos[i], paletteColorThrees[i]));
				}
			}
		}
	}
}
