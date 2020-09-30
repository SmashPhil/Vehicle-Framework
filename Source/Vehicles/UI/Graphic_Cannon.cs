using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
    public class Graphic_Cannon : Graphic
    {
		public override Material MatSingle => maskMatPatterns.FirstOrDefault().Value.Second[0];

        public override void Init(GraphicRequest req)
        {
            data = req.graphicData;
			if (req.path.NullOrEmpty())
			{
				throw new ArgumentNullException("folderPath");
			}
			if (req.shader == null)
			{
				throw new ArgumentNullException("shader");
			}
			path = req.path;
			color = req.color;
			colorTwo = req.colorTwo;
			drawSize = req.drawSize;
            var files = ContentFinder<Texture2D>.GetAllInFolder(req.path);

			Texture2D mainTex = (from x in files
			                        where !x.name.EndsWith(MaskSuffix)
			                        orderby x.name
			                        select x).FirstOrDefault();
            Texture2D maskMatTex = (from x in files
                                    where x.name.EndsWith(MaskSuffix)
                                    orderby x.name
                                    select x).FirstOrDefault();
			
			if (mainTex is null)
			{
				Log.Error($"{VehicleHarmony.LogLabel} Graphic_Cannon cannot init: No textures found at path " + req.path, false);
				graphicPath = BaseContent.BadGraphic.path;
				material = BaseContent.BadMat;
				return;
			}

			int sIndex = req.path.LastIndexOf("/");
			var colorPath = req.path;

			if (sIndex > 0)
				colorPath = string.Concat(colorPath.Substring(0, sIndex), "/Color");
			List<string> additionalMaskLocations = HelperMethods.GetAllFolderNamesInFolder(colorPath);

			if (req.shader.SupportsMaskTex())
			{
				var textureName = req.path.Substring(req.path.LastIndexOf('/') + 1); //TankCannonTop
				GenerateMasks(req, "Default", mainTex);
                foreach (string extraMask in additionalMaskLocations)
                {
                    int index = extraMask.LastIndexOf("/");
                    string folderName = extraMask.Substring(index + 1);
                    string path = string.Concat(colorPath, '/', folderName, '/', textureName);
                    GraphicRequest gReq = new GraphicRequest(req.graphicClass, path, req.shader, req.drawSize, req.color, req.colorTwo, req.graphicData, req.renderQueue, req.shaderParameters);
                    GenerateMasks(gReq, folderName, mainTex);
                }
            }
        }

        public override Material MatAt(Rot4 rot, Thing thing = null)
        {
			if (thing is null || !(thing is VehiclePawn vehicle))
			{
				return base.MatAt(rot, thing);
			}

			if(maskMatPatterns.TryGetValue(vehicle.selectedMask, out var values))
            {
				return values.Second[CurrentIndex()];
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
					Log.Message($"{VehicleHarmony.LogLabel} Additional Information:\n" +
						$"MatCount: {maskMatPatterns.Count}\n" +
						$"{folders}");
                }
            }
			return BaseContent.BadMat;
        }

		public virtual int CurrentIndex()
        {
			return 0;
        }

        protected virtual void GenerateMasks(GraphicRequest req, string maskName, Texture2D mainTex)
        {
			string path = string.Concat(req.path, '/', mainTex.name); //string path = Vehicles/Tank/TankCannonTop/TankCannonTop_a
			Texture2D maskTex = ContentFinder<Texture2D>.Get(path + MaskSuffix, false);

			MaterialRequest mReq = new MaterialRequest()
			{
				mainTex = mainTex,
				shader = req.shader,
				color = color,
				colorTwo = colorTwo,
				shaderParameters = req.shaderParameters,
				maskTex = req.shader.SupportsMaskTex() ? maskTex : null
            };

            material = MaterialPool.MatFrom(mReq);

			maskTexPatterns.Add(maskName, new Pair<string, Texture2D[]>(req.path, new Texture2D[1] { maskTex } ));
			maskMatPatterns.Add(maskName, new Pair<string, Material[]>(req.path, new Material[1] { material } ));
        }

        public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
		{
			return GraphicDatabase.Get<Graphic_Cannon>(path, newShader, drawSize, newColor, newColorTwo, data);
		}

        public override string ToString()
        {
	        return string.Concat(new object[]
	        {
		        "Graphic_Cannon(path=",
		        path,
		        ", maskCount=",
		        maskMatPatterns.Count,
				", shader=",
				Shader,
		        ")"
	        });
        }

        public static string GetDefaultTexPath(string folder)
        {
            return folder + "/" + ContentFinder<Texture2D>.GetAllInFolder(folder).FirstOrDefault().name;
        }

        public static string MaskSuffix = "_m";

        protected int matCount;

		private float drawRotatedExtraAngleOffset;

		protected string graphicPath;
		protected Material material;

		private Texture2D[] textureArray;

		//folderName : <filePath, texture/mat array>
		internal Dictionary<string, Pair<string, Texture2D[]>> maskTexPatterns = new Dictionary<string, Pair<string, Texture2D[]>>();
		internal Dictionary<string, Pair<string, Material[]>> maskMatPatterns = new Dictionary<string, Pair<string, Material[]>>();
    }
}
