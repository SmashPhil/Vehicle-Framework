using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;

namespace Vehicles.UI
{
    public class ColorStorage : GameComponent
    {
        public List<Pair<Color, Color>> colorPalette;

        public ColorStorage(Game game)
        {

        }

        public override void LoadedGame()
        {
            base.LoadedGame();

            if(colorPalette is null || colorPalette.Count != PaletteCount)
            {
                ResetPalettes();
            }
        }

        public void ResetPalettes()
        {
            colorPalette = new List<Pair<Color, Color>>()
            {
                new Pair<Color, Color>(new Color32(25, 25, 25, 255), new Color32(210, 210, 210, 255)),
                new Pair<Color, Color>(new Color32(150, 10, 0, 255), new Color32(30, 50, 80, 255)),
                new Pair<Color, Color>(new Color32(200, 140, 85, 255), new Color32(130, 85, 45, 255)),
                new Pair<Color, Color>(new Color32(80, 55, 0, 255), new Color32(50, 45, 30, 255)),
                new Pair<Color, Color>(new Color32(5, 40, 10, 255), new Color32(0, 70, 5, 255)),
                new Pair<Color, Color>(new Color32(0, 50, 100, 255), new Color32(30, 20, 60, 255)),
                new Pair<Color, Color>(new Color32(0, 70, 35, 255), new Color32(20, 85, 65, 255)),
                new Pair<Color, Color>(new Color32(175, 170, 40, 255), new Color32(85, 80, 0, 255)),
                new Pair<Color, Color>(new Color32(55, 35, 60, 255), new Color32(35, 20, 45, 255)),
                new Pair<Color, Color>(new Color32(78, 78, 78, 255), new Color32(60, 70, 75, 255)),
                new Pair<Color, Color>(new Color32(5, 40, 70, 255), new Color32(0, 55, 75, 255)),
                new Pair<Color, Color>(new Color32(85, 30, 0, 255), new Color32(110, 65, 0, 255)),
                new Pair<Color, Color>(new Color32(60, 30, 30, 255), new Color32(56, 56, 56, 255)),
                new Pair<Color, Color>(new Color32(80, 130, 0, 255), new Color32(42, 42, 42, 255)),
                new Pair<Color, Color>(new Color32(25, 30, 60, 255), new Color32(56, 56, 56, 255)),
                new Pair<Color, Color>(new Color32(140, 120, 0, 255), new Color32(78, 78, 78, 255)),
                new Pair<Color, Color>(new Color32(135, 135, 135, 255), new Color32(0, 75, 85, 255)),
                new Pair<Color, Color>(new Color32(120, 135, 140, 255), new Color32(191, 191, 191, 255))
            };
        }

        public void AddPalette(Color c1, Color c2, int index)
        {
            if(index >= PaletteCount || index < 0)
            {
                Log.Error("Attempting to set size of ColorPalette that is larger than predetermined.");
                return;
            }
            colorPalette[index] = new Pair<Color, Color>(c1, c2);
        }

        private List<Color> paletteColorOnes;
        private List<Color> paletteColorTwos;
        public override void ExposeData()
        {
            if(Scribe.mode == LoadSaveMode.Saving)
            {
                paletteColorOnes = colorPalette.Select(c => c.First).ToList();
                paletteColorTwos = colorPalette.Select(c => c.Second).ToList();
            }
            Scribe_Collections.Look(ref paletteColorOnes, "paletteColorOnes", LookMode.Value);
            Scribe_Collections.Look(ref paletteColorTwos, "paletteColorTwos", LookMode.Value);
            if(Scribe.mode == LoadSaveMode.LoadingVars)
            {
                colorPalette = paletteColorOnes.Zip(paletteColorTwos, (c1, c2) => new Pair<Color, Color>(c1, c2)).ToList();
            }
        }

        public const int PaletteCount = 18;
        public const int PaletteCountPerRow = 6;

        public static int PaletteDivisor => PaletteCount / PaletteCountPerRow;
    }
}
