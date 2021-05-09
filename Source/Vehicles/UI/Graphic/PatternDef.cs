using System.Collections.Generic;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class PatternDef : Def
	{
		public string path;

		public Color? colorOne;
		public Color? colorTwo;
		public Color? colorThree;

		public bool replaceTex = false;

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
			if (IsDefault)
			{
				return;
			}

			patterns[0] = ContentFinder<Texture2D>.Get(path, false);
			if (patterns[0] is null)
			{
				patterns[0] = ContentFinder<Texture2D>.Get(path + "_north", false);
			}
			patterns[1] = ContentFinder<Texture2D>.Get(path + "_east", false);
			patterns[2] = ContentFinder<Texture2D>.Get(path + "_south", false);
			patterns[3] = ContentFinder<Texture2D>.Get(path + "_west", false);
			patterns[4] = ContentFinder<Texture2D>.Get(path + "_northEast", false);
			patterns[5] = ContentFinder<Texture2D>.Get(path + "_southEast", false);
			patterns[6] = ContentFinder<Texture2D>.Get(path + "_southWest", false);
			patterns[7] = ContentFinder<Texture2D>.Get(path + "_northWest", false);

			if (patterns[0] is null)
			{
				SmashLog.Error($"Unable to find Texture2D for <field>path</field> at {path}.");
				return;
			}
			if (patterns[1] is null)
			{
				if (patterns[3] != null)
				{
					patterns[1] = patterns[3];
				}
				else
				{
					patterns[1] = patterns[0];
				}
			}
			if (patterns[2] is null)
			{
				patterns[2] = patterns[0];
			}

			if (patterns[3] is null)
			{
				if (patterns[1] != null)
				{
					patterns[3] = patterns[1];
				}
				else
				{
					patterns[3] = patterns[0];
				}
			}

			if(patterns[5] is null)
			{
				if(patterns[4] != null)
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

			if (path.NullOrEmpty() && !IsDefault)
			{
				yield return $"<field>path</field> must be included.".ConvertRichText();
			}
		}
	}
}
