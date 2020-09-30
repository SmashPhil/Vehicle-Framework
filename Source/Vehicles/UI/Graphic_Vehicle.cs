using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
    public class Graphic_Vehicle : Graphic
    {
		//public override Material MatSingle => maskMatPatterns.FirstOrDefault().Value.Second[0];

		public override Material MatSingle
        {
            get
            {
				var test = maskMatPatterns.FirstOrDefault().Value.Second[0];
				return test;
            }
        }

		public override Material MatNorth => maskMatPatterns.FirstOrDefault().Value.Second[0];
        public override Material MatEast => maskMatPatterns.FirstOrDefault().Value.Second[1];
		public override Material MatSouth => maskMatPatterns.FirstOrDefault().Value.Second[2];
        public override Material MatWest => maskMatPatterns.FirstOrDefault().Value.Second[3];


        public override bool WestFlipped
		{
			get
			{
				return westFlipped;
			}
		}

		public override bool EastFlipped
		{
			get
			{
				return eastFlipped;
			}
		}

		public override bool ShouldDrawRotated
		{
			get
			{
				return (data == null || data.drawRotated) && (MatEast == MatNorth || MatWest == MatNorth);
			}
		}

		public override float DrawRotatedExtraAngleOffset
		{
			get
			{
				return drawRotatedExtraAngleOffset;
			}
		}

        public override Material MatAt(Rot4 rot, Thing thing = null)
        {
			if (thing is null || !(thing is VehiclePawn vehicle))
			{
				return base.MatAt(rot, thing);
			}
            float angle = vehicle.Angle;

			if(maskMatPatterns.TryGetValue(vehicle.selectedMask, out var values))
            {
				return values.Second[vehicle.VehicleRot8()];
            }
            else
            {
				Log.Error($"{VehicleHarmony.LogLabel} Key {vehicle.selectedMask} not found in {GetType()} for {vehicle}. Make sure there is an individual folder for each additional mask.");
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

        public override void Init(GraphicRequest req)
		{
			maskTexPatterns = new Dictionary<string, Pair<string, Texture2D[]>>();
			maskMatPatterns = new Dictionary<string, Pair<string, Material[]>>();

			data = req.graphicData;
			path = req.path;
			color = req.color;
			colorTwo = req.colorTwo;
			drawSize = req.drawSize;

			textureArray = new Texture2D[MatCount];
			textureArray[0] = ContentFinder<Texture2D>.Get(req.path + "_north", false);
			textureArray[1] = ContentFinder<Texture2D>.Get(req.path + "_east", false);
			textureArray[2] = ContentFinder<Texture2D>.Get(req.path + "_south", false);
			textureArray[3] = ContentFinder<Texture2D>.Get(req.path + "_west", false);
            textureArray[4] = ContentFinder<Texture2D>.Get(req.path + "_northEast", false);
            textureArray[5] = ContentFinder<Texture2D>.Get(req.path + "_southEast", false);
            textureArray[6] = ContentFinder<Texture2D>.Get(req.path + "_southWest", false);
            textureArray[7] = ContentFinder<Texture2D>.Get(req.path + "_northWest", false);
            
			if (textureArray[0] == null)
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
				else
				{
					textureArray[0] = ContentFinder<Texture2D>.Get(req.path, false);
				}
			}
			if (textureArray[0] == null)
			{
				Log.Error("Failed to find any textures at " + req.path + " while constructing " + this.ToStringSafe<Graphic_Vehicle>(), false);
				return;
			}
			if (textureArray[2] == null)
			{
				textureArray[2] = textureArray[0];
			}
			if (textureArray[1] == null)
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
			if (textureArray[3] == null)
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

            if(textureArray[5] == null)
            {
                if(textureArray[4] != null)
                {
                    textureArray[5] = textureArray[4];
                    eastDiagonalFlipped = DataAllowsFlip;
                }
                else
                {
                    textureArray[5] = textureArray[1];
                }
            }
            if(textureArray[6] == null)
            {
                if(textureArray[7] != null)
                {
                    textureArray[6] = textureArray[7];
                    westDiagonalFlipped = DataAllowsFlip;
                }
                else
                {
                    textureArray[6] = textureArray[3];
                }
            }
            if(textureArray[4] == null)
            {
                if(textureArray[5] != null)
                {
                    textureArray[4] = textureArray[5];
                    eastDiagonalFlipped = DataAllowsFlip;
                }
                else
                {
                    textureArray[4] = textureArray[1];
                }
            }
            if(textureArray[7] == null)
            {
                if(textureArray[6] != null)
                {
                    textureArray[7] = textureArray[6];
                    westDiagonalFlipped = DataAllowsFlip;
                }
                else
                {
                    textureArray[7] = textureArray[3];
                }
            }
            
			int sIndex = req.path.LastIndexOf("/");
			var colorPath = req.path;
			if (sIndex > 0)
				colorPath = string.Concat(colorPath.Substring(0, sIndex), "/Color");
			List<string> additionalMaskLocations = HelperMethods.GetAllFolderNamesInFolder(colorPath);

			if (req.shader.SupportsMaskTex())
			{
				GenerateMasks(req, "Default");
				var textureName = req.path.Substring(req.path.LastIndexOf('/') + 1);
				foreach(string extraMask in additionalMaskLocations)
				{
					int index = extraMask.LastIndexOf("/");
					string folderName = extraMask.Substring(index + 1);
					string path = string.Concat(colorPath, '/', folderName, '/', textureName);
					GraphicRequest gReq = new GraphicRequest(req.graphicClass, path, ShaderDatabase.CutoutComplex, req.drawSize, req.color, req.colorTwo, req.graphicData, req.renderQueue, req.shaderParameters);
					GenerateMasks(gReq, folderName);
				}
			}
		}

		protected virtual void GenerateMasks(GraphicRequest req, string maskName)
        {
			var tmpMaskArray = new Texture2D[MatCount];
			tmpMaskArray[0] = ContentFinder<Texture2D>.Get(req.path + "_northm", true);
			tmpMaskArray[1] = ContentFinder<Texture2D>.Get(req.path + "_eastm", false);
			tmpMaskArray[2] = ContentFinder<Texture2D>.Get(req.path + "_southm", false);
			tmpMaskArray[3] = ContentFinder<Texture2D>.Get(req.path + "_westm", false);
            tmpMaskArray[4] = ContentFinder<Texture2D>.Get(req.path + "_northEastm", false);
            tmpMaskArray[5] = ContentFinder<Texture2D>.Get(req.path + "_southEastm", false);
            tmpMaskArray[6] = ContentFinder<Texture2D>.Get(req.path + "_southWestm", false);
            tmpMaskArray[7] = ContentFinder<Texture2D>.Get(req.path + "_northWestm", false);
			if (tmpMaskArray[0] == null)
			{
				if (tmpMaskArray[2] != null)
				{
					tmpMaskArray[0] = tmpMaskArray[2];
				}
				else if (tmpMaskArray[1] != null)
				{
					tmpMaskArray[0] = tmpMaskArray[1];
				}
				else if (tmpMaskArray[3] != null)
				{
					tmpMaskArray[0] = tmpMaskArray[3];
				}
			}
			if (tmpMaskArray[2] == null)
			{
				tmpMaskArray[2] = tmpMaskArray[0];
			}
			if (tmpMaskArray[1] == null)
			{
				if (tmpMaskArray[3] != null)
				{
					tmpMaskArray[1] = tmpMaskArray[3];
				}
				else
				{
					tmpMaskArray[1] = tmpMaskArray[0];
				}
			}
			if (tmpMaskArray[3] == null)
			{
				if (tmpMaskArray[1] != null)
				{
					tmpMaskArray[3] = tmpMaskArray[1];
				}
				else
				{
					tmpMaskArray[3] = tmpMaskArray[0];
				}
			}

            if(tmpMaskArray[5] == null)
			{
				if(tmpMaskArray[4] != null)
				{
					tmpMaskArray[5] = tmpMaskArray[4];
					eastDiagonalFlipped = DataAllowsFlip;
				}
				else
				{
					tmpMaskArray[5] = tmpMaskArray[1];
				}
			}
			if(tmpMaskArray[6] == null)
			{
				if(tmpMaskArray[7] != null)
				{
					tmpMaskArray[6] = tmpMaskArray[7];
					westDiagonalFlipped = DataAllowsFlip;
				}
				else
				{
					tmpMaskArray[6] = tmpMaskArray[3];
				}
			}
			if(tmpMaskArray[4] == null)
			{
				if(tmpMaskArray[5] != null)
				{
					tmpMaskArray[4] = tmpMaskArray[5];
					eastDiagonalFlipped = DataAllowsFlip;
				}
				else
				{
					tmpMaskArray[4] = tmpMaskArray[1];
				}
			}
			if(tmpMaskArray[7] == null)
			{
				if(tmpMaskArray[6] != null)
				{
					tmpMaskArray[7] = tmpMaskArray[6];
					westDiagonalFlipped = DataAllowsFlip;
				}
				else
				{
					tmpMaskArray[7] = tmpMaskArray[3];
				}
			}
			var mats = new Material[MatCount];
			for (int i = 0; i < MatCount; i++)
			{
				MaterialRequest req2 = default;
				req2.mainTex = textureArray[i];
				req2.shader = req.shader;
				req2.color = color;
				req2.colorTwo = colorTwo;
				req2.maskTex = tmpMaskArray[i];
				req2.shaderParameters = req.shaderParameters;
				mats[i] = MaterialPool.MatFrom(req2);
			}
			maskTexPatterns.Add(maskName, new Pair<string,Texture2D[]>(req.path, tmpMaskArray));
			maskMatPatterns.Add(maskName, new Pair<string, Material[]>(req.path, mats));
        }

		public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
		{
			return GraphicDatabase.Get<Graphic_Vehicle>(path, newShader, drawSize, newColor, newColorTwo, data);
		}

		public string GetFullPath(VehiclePawn vehicle, Rot4 dir)
        {
			var path = maskMatPatterns[vehicle.selectedMask].First;
			float angle = vehicle.Angle;

			switch (dir.AsInt)
			{
				case 0:
					return path + "_northm";
				case 1:
					if (angle == -45)
						return path + "_northEastm";
					else if (angle == 45)
						return path + "_southEastm";
					return path + "_eastm";
				case 2:
					return path + "_southm";
				case 3:
					if (angle == -45)
						return path + "_southWestm";
					else if (angle == 45)
						return path + "_northWestm";
					return path + "_westm";
				default:
					throw new NotImplementedException("Rotations beyond Rot4");
			}
        }

		public override string ToString()
		{
			return string.Concat(new object[]
			{
				"OmniDirectional(initPath=",
				path,
				", color=",
				color,
				", colorTwo=",
				colorTwo,
				")"
			});
		}

		public override int GetHashCode()
		{
			return Gen.HashCombineStruct(Gen.HashCombineStruct(Gen.HashCombine(0, path), color), colorTwo);
		}

		private const int MatCount = 8;
		private bool westFlipped;
		private bool eastFlipped;
		private float drawRotatedExtraAngleOffset;

		private Texture2D[] textureArray;

		//folderName : <filePath, texture/mat array>
		internal Dictionary<string, Pair<string, Texture2D[]>> maskTexPatterns = new Dictionary<string, Pair<string, Texture2D[]>>();
		internal Dictionary<string, Pair<string, Material[]>> maskMatPatterns = new Dictionary<string, Pair<string, Material[]>>();

        private bool eastDiagonalFlipped;
        private bool westDiagonalFlipped;
    }
}
