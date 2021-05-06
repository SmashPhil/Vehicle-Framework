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

		public List<Tuple<Color, Color, Color>> colorPalette = new List<Tuple<Color, Color, Color>>();

		private List<Color> paletteColorOnes = new List<Color>();
		private List<Color> paletteColorTwos = new List<Color>();
		private List<Color> paletteColorThrees = new List<Color>();

		public ColorStorage()
		{
		}

		public static int PaletteRowCount => PaletteCount / PaletteCountPerRow;

		public void ResetPalettes()
		{
			colorPalette = new List<Tuple<Color, Color, Color>>()
			{
				new Tuple<Color, Color, Color>(new Color32(25, 25, 25, 255), new Color32(210, 210, 210, 255), new Color32(160, 160, 160, 255)),
				new Tuple<Color, Color, Color>(new Color32(150, 10, 0, 255), new Color32(30, 50, 80, 255), new Color32(200, 200, 200, 255)),
				new Tuple<Color, Color, Color>(new Color32(200, 140, 85, 255), new Color32(130, 85, 45, 255), new Color32(80, 50, 10, 255)),
				new Tuple<Color, Color, Color>(new Color32(170, 120, 0, 255), new Color32(35, 25, 20, 255), new Color32(135, 135, 135, 255)),
				new Tuple<Color, Color, Color>(new Color32(5, 40, 10, 255), new Color32(0, 70, 5, 255), new Color32(6, 30, 15, 255)),

				new Tuple<Color, Color, Color>(new Color32(0, 50, 100, 255), new Color32(30, 20, 60, 255), new Color32(70, 95, 160, 255)),
				new Tuple<Color, Color, Color>(new Color32(0, 70, 35, 255), new Color32(20, 85, 65, 255), new Color32(10, 30, 20, 255)),
				new Tuple<Color, Color, Color>(new Color32(175, 170, 40, 255), new Color32(85, 80, 0, 255), new Color32(30, 25, 20, 255)),
				new Tuple<Color, Color, Color>(new Color32(55, 35, 60, 255), new Color32(35, 20, 45, 255), new Color32(45, 40, 70, 255)),
				new Tuple<Color, Color, Color>(new Color32(78, 78, 78, 255), new Color32(60, 70, 75, 255), new Color32(55, 65, 60, 255)),

				new Tuple<Color, Color, Color>(new Color32(5, 40, 70, 255), new Color32(0, 55, 75, 255), new Color32(65, 80, 100, 255)),
				new Tuple<Color, Color, Color>(new Color32(85, 30, 0, 255), new Color32(110, 65, 0, 255), new Color32(35, 20, 10, 255)),
				new Tuple<Color, Color, Color>(new Color32(60, 30, 30, 255), new Color32(56, 56, 56, 255), new Color32(65, 40, 40, 255)),
				new Tuple<Color, Color, Color>(new Color32(80, 130, 0, 255), new Color32(42, 42, 42, 255), new Color32(145, 185, 110, 255)),
				new Tuple<Color, Color, Color>(new Color32(25, 30, 60, 255), new Color32(56, 56, 56, 255), new Color32(30, 45, 60, 255)),

				new Tuple<Color, Color, Color>(new Color32(140, 120, 0, 255), new Color32(78, 78, 78, 255), new Color32(210, 210, 210, 255)),
				new Tuple<Color, Color, Color>(new Color32(135, 135, 135, 255), new Color32(0, 75, 85, 255), new Color32(210, 210, 210, 255)),
				new Tuple<Color, Color, Color>(new Color32(120, 135, 140, 255), new Color32(191, 191, 191, 255), new Color32(210, 210, 210, 255)),
				new Tuple<Color, Color, Color>(new Color32(120, 135, 140, 255), new Color32(191, 191, 191, 255), new Color32(210, 210, 210, 255)),
				new Tuple<Color, Color, Color>(new Color32(120, 135, 140, 255), new Color32(191, 191, 191, 255), new Color32(210, 210, 210, 255)),
			};
		}

		public void AddPalette(Color c1, Color c2, Color c3, int index)
		{
			if(index >= PaletteCount || index < 0)
			{
				Log.Error("Attempting to set size of ColorPalette that is larger than predetermined.");
				return;
			}
			colorPalette[index] = new Tuple<Color, Color, Color>(c1, c2, c3);
			VehicleMod.settings.Write();
		}

		public void ExposeData()
		{
			if(Scribe.mode == LoadSaveMode.Saving)
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
			Scribe_Collections.Look(ref paletteColorOnes, "paletteColorOnes", LookMode.Value);
			Scribe_Collections.Look(ref paletteColorTwos, "paletteColorTwos", LookMode.Value);
			Scribe_Collections.Look(ref paletteColorThrees, "paletteColorThrees", LookMode.Value);
			if (Scribe.mode == LoadSaveMode.LoadingVars)
			{
				colorPalette = new List<Tuple<Color, Color, Color>>();
				for (int i = 0; i < paletteColorOnes.Count; i++)
				{
					colorPalette.Add(new Tuple<Color, Color, Color>(paletteColorOnes[i], paletteColorTwos[i], paletteColorThrees[i]));
				}
			}
		}
	}
}
