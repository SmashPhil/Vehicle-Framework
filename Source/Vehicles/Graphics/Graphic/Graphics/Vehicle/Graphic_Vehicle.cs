using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class Graphic_Vehicle : Graphic_RGB
	{
		public override Material MatNorth => maskMatPatterns[PatternDefOf.Default].Second[0];
		public override Material MatEast => maskMatPatterns[PatternDefOf.Default].Second[1];
		public override Material MatSouth => maskMatPatterns[PatternDefOf.Default].Second[2];
		public override Material MatWest => maskMatPatterns[PatternDefOf.Default].Second[3];


		public Material MatAt(Rot8 rot, PatternDef pattern, VehiclePawn vehicle = null)
		{
			if (pattern is null)
			{
				return MatAt(rot, vehicle);
			}
			if (maskMatPatterns.TryGetValue(pattern, out var values))
			{
				return values.Second[rot.AsInt];
			}
			Log.Error($"[{VehicleHarmony.LogLabel}] Key {pattern.defName} not found in {GetType()}.");
			string folders = string.Empty;
			foreach(var item in maskMatPatterns)
			{
				folders += $"Item: {item.Key} Destination: {item.Value.First}\n";
			}
			Debug.Warning($"{VehicleHarmony.LogLabel} Additional Information:\n" +
				$"MatCount: {maskMatPatterns.Count}\n" +
				$"{folders}");
			return BaseContent.BadMat;
		}

		public override Material MatAt(Rot4 rot, Thing thing = null)
		{
			if (thing is null || !(thing is VehiclePawn vehicle))
			{
				return base.MatAt(rot, thing);
			}
			if (maskMatPatterns.TryGetValue(vehicle.Pattern, out var values))
			{
				return values.Second[vehicle.FullRotation.AsInt];
			}
			else
			{
				Log.Error($"[{VehicleHarmony.LogLabel}] Key {vehicle.Pattern.defName} not found in {GetType()} for {vehicle}. Make sure there is an individual folder for each additional mask.");
				if(Prefs.DevMode)
				{
					string folders = string.Empty;
					foreach(var item in maskMatPatterns)
					{
						folders += $"Item: {item.Key} Destination: {item.Value.First}\n";
					}
					Log.Warning($"{VehicleHarmony.LogLabel} Additional Information:\n" +
						$"MatCount: {maskMatPatterns.Count}\n" +
						$"{folders}");
				}
			}
			return BaseContent.BadMat;
		}

		public override void Init(GraphicRequestRGB req, bool cacheResults = true)
		{
			base.Init(req);
			textureArray = new Texture2D[MatCount];
			textureArray[0] = ContentFinder<Texture2D>.Get(req.path + "_north", false);
			textureArray[0] ??= ContentFinder<Texture2D>.Get(req.path, false);
			textureArray[1] = ContentFinder<Texture2D>.Get(req.path + "_east", false);
			textureArray[2] = ContentFinder<Texture2D>.Get(req.path + "_south", false);
			textureArray[3] = ContentFinder<Texture2D>.Get(req.path + "_west", false);
			textureArray[4] = ContentFinder<Texture2D>.Get(req.path + "_northEast", false);
			textureArray[5] = ContentFinder<Texture2D>.Get(req.path + "_southEast", false);
			textureArray[6] = ContentFinder<Texture2D>.Get(req.path + "_southWest", false);
			textureArray[7] = ContentFinder<Texture2D>.Get(req.path + "_northWest", false);
			
			if (textureArray[0] is null)
			{
				if (textureArray[2] != null)
				{
					textureArray[0] = textureArray[2];
					drawRotatedExtraAngleOffset = 180f;
				}
				else if (textureArray[1] != null)
				{
					textureArray[0] = textureArray[1];
					drawRotatedExtraAngleOffset = -90f;
				}
				else if (textureArray[3] != null)
				{
					textureArray[0] = textureArray[3];
					drawRotatedExtraAngleOffset = 90f;
				}
			}
			if (textureArray[0] is null)
			{
				Log.Error("Failed to find any textures at " + req.path + " while constructing " + this.ToStringSafe());
				return;
			}
			if (textureArray[2] is null)
			{
				textureArray[2] = textureArray[0];
			}
			if (textureArray[1] is null)
			{
				if (textureArray[3] != null)
				{
					textureArray[1] = textureArray[3];
					eastFlipped = DataAllowsFlip;
				}
				else
				{
					textureArray[1] = textureArray[0];
				}
			}
			if (textureArray[3] is null)
			{
				if (textureArray[1] != null)
				{
					textureArray[3] = textureArray[1];
					westFlipped = DataAllowsFlip;
				}
				else
				{
					textureArray[3] = textureArray[0];
				}
			}

			if (textureArray[4] is null)
			{
				if (textureArray[7] != null)
				{
					textureArray[4] = textureArray[7];
				}
				else
				{
					textureArray[4] = textureArray[0];
				}
				eastDiagonalRotated = DataAllowsFlip;
			}
			if (textureArray[5] is null)
			{
				if (textureArray[6] != null)
				{
					textureArray[5] = textureArray[6];
				}
				else
				{
					textureArray[5] = textureArray[2];
				}
				eastDiagonalRotated = DataAllowsFlip;
			}
			if (textureArray[6] is null)
			{
				if (textureArray[5] != null)
				{
					textureArray[6] = textureArray[5];
				}
				else
				{
					textureArray[6] = textureArray[2];
				}
				westDiagonalRotated = DataAllowsFlip;
			}
			if (textureArray[7] is null)
			{
				if (textureArray[4] != null)
				{
					textureArray[7] = textureArray[4];
				}
				else
				{
					textureArray[7] = textureArray[0];
				}
				westDiagonalRotated = DataAllowsFlip;
			}
			
			foreach (PatternDef pattern in DefDatabase<PatternDef>.AllDefs)
			{
				maskMatPatterns.Add(pattern, new Pair<string, Material[]>(req.path, GenerateMasks(req, pattern)));
			}
		}

		public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
		{
			return GraphicDatabase.Get<Graphic_Vehicle>(path, newShader, drawSize, newColor, newColorTwo, DataRGB);
		}

		public override Graphic_RGB GetColoredVersion(Shader shader, Color colorOne, Color colorTwo, Color colorThree, float tiles = 1, float displacementX = 0, float displacementY = 0)
		{
			return GraphicDatabaseRGB.Get<Graphic_Vehicle>(path, shader, drawSize, colorOne, colorTwo, colorThree, tiles, displacementX, displacementY, DataRGB);
		}
	}
}
