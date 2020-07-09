using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using RimWorld;

namespace Vehicles
{
    public enum AnimationWrapperType { Oscillate, Reset, Off}
    
    public class Graphic_Animate : Graphic
    {
        public override Material MatSingle
        {
            get
            {
                return materials[0]; 
            }
        }

        public int AnimationFrameCount
        {
            get
            {
                return graphicPaths.Length;
            }
        }

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
			List<Texture2D> list = (from x in ContentFinder<Texture2D>.GetAllInFolder(req.path)
			                        where !x.name.EndsWith(Graphic_Single.MaskSuffix)
			                        orderby x.name
			                        select x).ToList();
            List<Texture2D> listM = (from x in ContentFinder<Texture2D>.GetAllInFolder(req.path)
                                    where x.name.EndsWith(Graphic_Single.MaskSuffix)
                                    orderby x.name
                                    select x).ToList();
			if (list.NullOrEmpty())
			{
				Log.Error("Collection cannot init: No textures found at path " + req.path, false);
				graphicPaths = new string[]
				{
					BaseContent.BadGraphic.path
				};
                materials = new Material[]
                {
                    BaseContent.BadMat
                };
				return;
			}
			graphicPaths = new string[list.Count];
            materials = new Material[list.Count];
            if(list.Count != listM.Count && !listM.NullOrEmpty())
            {
                Log.Error($"[Vehicles] Could not apply masks for animation classes. Mask and texture count do not match up. Either have a mask for each texture or none at all. \n Graphics: {list.Count} Masks: {listM.Count}");
                graphicPaths = new string[]
				{
					BaseContent.BadGraphic.path
				};
                materials = new Material[]
                {
                    BaseContent.BadMat
                };
                return;
            }
			for (int i = 0; i < list.Count; i++)
			{
				string path = req.path + "/" + list[i].name;
                graphicPaths[i] = path;

                MaterialRequest mReq = new MaterialRequest()
                {
                    mainTex = ContentFinder<Texture2D>.Get(path),
                    shader = req.shader,
                    color = this.color,
                    colorTwo = this.colorTwo,
                    shaderParameters = req.shaderParameters
                };
                if (req.shader.SupportsMaskTex() && !listM.NullOrEmpty())
                    mReq.maskTex = ContentFinder<Texture2D>.Get(path + Graphic_Single.MaskSuffix);
                materials[i] = MaterialPool.MatFrom(mReq);
			}
        }

        public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
		{
			return GraphicDatabase.Get<Graphic_Animate>(path, newShader, drawSize, newColor, newColorTwo, data);
		}

        public Graphic SubGraphicCycle(int index, Shader newShader, Color color1, Color color2)
        {
            if(index > (graphicPaths.Length - 1) )
            {
                Log.Warning($"Graphic Retrieval for Graphic_Animate indexed past maximum length of {graphicPaths.Length}. Self correcting.");
                while (index > (graphicPaths.Length - 1))
                    index -= (graphicPaths.Length - 1);
            }
            return GraphicDatabase.Get<Graphic_Single>(graphicPaths[index], newShader, drawSize, color1, color2, data);
        }

        public Material SubMaterialCycle(int index)
        {
            if(index > (materials.Length - 1) )
            {
                Log.Warning($"Graphic Retrieval for Graphic_Animate indexed past maximum length of {materials.Length}. Self correcting.");
                while (index > (materials.Length - 1))
                    index -= (materials.Length - 1);
            }
            return materials[index];
        }

        public override string ToString()
        {
	        return string.Concat(new object[]
	        {
		        "AnimationCount(path=",
		        path,
		        ", count=",
		        materials.Length,
		        ")"
	        });
        }

        public static string GetDefaultTexPath(string animationFolder)
        {
            return animationFolder + "/" + ContentFinder<Texture2D>.GetAllInFolder(animationFolder).FirstOrDefault().name;
        }

        private string[] graphicPaths;

        private Material[] materials;
    }
}
