using System.Collections.Generic;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class PatternDef : Def
	{
		public string path;
		public PatternProperties properties;

		public List<VehicleDef> exclusiveFor;

		private Texture2D[] patterns;

		public bool IsDefault => this == PatternDefOf.Default;

		public Texture2D this[Rot8 rot]
		{
			get
			{
				if (patterns is null)
				{
					RecacheTextures();
				}
				return patterns[rot.AsInt];
			}
		}

		public bool ValidFor(VehicleDef def)
		{
			return exclusiveFor.NullOrEmpty() || exclusiveFor.Contains(def);
		}

		public void RecacheTextures()
		{
			patterns = new Texture2D[Graphic_RGB.MatCount];
			patterns[0] = ContentFinder<Texture2D>.Get(path, false);
			patterns[0] ??= ContentFinder<Texture2D>.Get(path + "_north", false);
			if (patterns[0] is null)
			{
				SmashLog.Error($"Unable to find Texture2D for <field>path</field> at {path}.");
				return;
			}
			if (IsDefault)
			{
				patterns[1] = patterns[0];
				patterns[2] = patterns[0];
				patterns[3] = patterns[0];
				patterns[4] = patterns[0];
				patterns[5] = patterns[0];
				patterns[6] = patterns[0];
				patterns[7] = patterns[0];
				return;
			}
			patterns[1] = ContentFinder<Texture2D>.Get(path + "_east", false);
			patterns[2] = ContentFinder<Texture2D>.Get(path + "_south", false);
			patterns[3] = ContentFinder<Texture2D>.Get(path + "_west", false);
			patterns[4] = ContentFinder<Texture2D>.Get(path + "_northEast", false);
			patterns[5] = ContentFinder<Texture2D>.Get(path + "_southEast", false);
			patterns[6] = ContentFinder<Texture2D>.Get(path + "_southWest", false);
			patterns[7] = ContentFinder<Texture2D>.Get(path + "_northWest", false);

			if (patterns[1] is null)
			{
				if (patterns[3] != null)
				{
					patterns[1] = patterns[3].Rotate(180);
				}
				else
				{
					patterns[1] = patterns[0].Rotate(90);
				}
			}
			if (patterns[2] is null)
			{
				patterns[2] = patterns[0].Rotate(90);
			}

			if (patterns[3] is null)
			{
				patterns[3] = patterns[0].Rotate(90);
			}

			if (patterns[5] is null)
			{
				if (patterns[4] != null)
				{
					patterns[5] = patterns[4];
				}
				else
				{
					patterns[5] = patterns[1];
				}
			}
			if(patterns[6] is null)
			{
				if(patterns[7] != null)
				{
					patterns[6] = patterns[7];
				}
				else
				{
					patterns[6] = patterns[3];
				}
			}
			if(patterns[4] is null)
			{
				if(patterns[5] != null)
				{
					patterns[4] = patterns[5];
				}
				else
				{
					patterns[4] = patterns[1];
				}
			}
			if(patterns[7] is null)
			{
				if(patterns[6] != null)
				{
					patterns[7] = patterns[6];
				}
				else
				{
					patterns[7] = patterns[3];
				}
			}
		}

		public override IEnumerable<string> ConfigErrors()
		{
			foreach (string error in base.ConfigErrors())
			{
				yield return error;
			}

			if (path.NullOrEmpty())
			{
				yield return $"<field>path</field> must be a valid texture path.".ConvertRichText();
			}
			if (properties?.tiles != null)
			{
				foreach (KeyValuePair<string, float> tileData in properties.tiles)
				{
					if (tileData.Value == 0)
					{
						yield return $"key <field>{tileData.Key}</field> in <field>tiles</field> should not be set to 0. This will result in odd coloring of the pattern.".ConvertRichText();
					}
				}
			}
		}

		public override void ResolveReferences()
		{
			if (properties is null)
			{
				properties = new PatternProperties();
			}
			if (properties.tiles is null)
			{
				properties.tiles = new Dictionary<string, float>();
			}
			if (IsDefault)
			{
				properties.IsDefault = true;
			}
		}
	}
}
