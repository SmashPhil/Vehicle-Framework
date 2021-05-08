using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld.Planet;

namespace Vehicles
{
	[StaticConstructorOnStartup]
	public static class TexData
	{
		public const int CloseRange = 5;
		public const int MidRange = 15;
		public const int FarRange = 25;

		/// <summary>
		/// Solid Color Textures
		/// </summary>
		public static readonly Texture2D FillableBarBackgroundTex = SolidColorMaterials.NewSolidColorTexture(Color.black);
		public static readonly Texture2D FillableBarInnerTex = SolidColorMaterials.NewSolidColorTexture(new ColorInt(19, 22, 27).ToColor);

		public static readonly Texture2D YellowTex = SolidColorMaterials.NewSolidColorTexture(new ColorInt(255, 210, 45).ToColor);
		public static readonly Texture2D YellowOrangeTex = SolidColorMaterials.NewSolidColorTexture(new ColorInt(255, 175, 45).ToColor);
		public static readonly Texture2D OrangeTex = SolidColorMaterials.NewSolidColorTexture(new ColorInt(255, 110, 15).ToColor);
		public static readonly Texture2D OrangeRedTex = SolidColorMaterials.NewSolidColorTexture(new ColorInt(255, 75, 15).ToColor);
		public static readonly Texture2D RedTex = SolidColorMaterials.NewSolidColorTexture(new ColorInt(155, 30, 30).ToColor);
		public static readonly Texture2D MaroonTex = SolidColorMaterials.NewSolidColorTexture(new ColorInt(60, 30, 30).ToColor);
		public static readonly Texture2D BlueTex = SolidColorMaterials.NewSolidColorTexture(new ColorInt(35, 50, 185).ToColor);
		public static readonly Texture2D GreenTex = SolidColorMaterials.NewSolidColorTexture(new ColorInt(0, 115, 40).ToColor);

		public static readonly Texture2D BlueAddedStatBarTexture = SolidColorMaterials.NewSolidColorTexture(new ColorInt(35, 50, 185, 120).ToColor);
		public static readonly Texture2D GreenAddedStatBarTexture = SolidColorMaterials.NewSolidColorTexture(new ColorInt(0, 115, 40, 120).ToColor);
		public static readonly Texture2D RedAddedStatBarTexture = SolidColorMaterials.NewSolidColorTexture(new ColorInt(155, 30, 30, 120).ToColor);
		public static readonly Texture2D OrangeAddedStatBarTexture = SolidColorMaterials.NewSolidColorTexture(new ColorInt(185, 110, 15, 120).ToColor);
		public static readonly Texture2D RedBrownAddedStatBarTexture = SolidColorMaterials.NewSolidColorTexture(new ColorInt(60, 30, 30, 120).ToColor);

		public static readonly Texture2D FillableBarTexture = SolidColorMaterials.NewSolidColorTexture(0.5f, 0.5f, 0.5f, 0.5f);
		public static readonly Texture2D ClearBarTexture = BaseContent.ClearTex;
		
		/// <summary>
		/// World Materials for colored lines
		/// </summary>
		public static readonly Material WorldLineMatWhite = MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.WorldOverlayTransparent, Color.white, WorldMaterials.WorldLineRenderQueue);
		public static readonly Material WorldLineMatYellow = MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.WorldOverlayTransparent, Color.yellow, WorldMaterials.WorldLineRenderQueue);
		public static readonly Material WorldLineMatRed = MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.WorldOverlayTransparent, Color.red, WorldMaterials.WorldLineRenderQueue);

		/// <summary>
		/// Preset UI colors
		/// </summary>
		public static readonly Color IconColor = new Color(0.84f, 0.84f, 0.84f); 
		public static readonly Color RedReadable = new Color(1f, 0.2f, 0.2f);
		public static readonly Color YellowReadable = new Color(1f, 1f, 0.2f);
		public static readonly Color MenuBGColor = new ColorInt(135, 135, 135).ToColor;


		public static Material RangeMat(int radius)
		{
			if(radius <= CloseRange)
			{
				return VehicleTex.RangeCircle_Close;
			}
			else if(radius <= MidRange)
			{
				return VehicleTex.RangeCircle_Mid;
			}
			else if(radius <= FarRange)
			{
				return VehicleTex.RangeCircle_Wide;
			}
			else
			{
				return VehicleTex.RangeCircle_ExtraWide;
			}
		}

		public static Texture2D HeatColorPercent(float percent)
		{
			if (percent <= 0.25)
			{
				return YellowTex;
			}
			else if (percent <= 0.5f)
			{
				return YellowOrangeTex;
			}
			else if (percent <= 0.75f)
			{
				return OrangeTex;
			}
			else if (percent < 1f)
			{
				return OrangeRedTex;
			}
			return RedTex;
		}
	}
}
