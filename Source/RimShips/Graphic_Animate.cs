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
    
    public class Graphic_Animate : Graphic_Collection
    {
        public override Material MatSingle
        {
            get
            {
                return subGraphics[0].MatSingle; 
            }
        }

        public int AnimationFrameCount
        {
            get
            {
                return subGraphics.Length;
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
			select x).ToList<Texture2D>();
			if (list.NullOrEmpty())
			{
				Log.Error("Collection cannot init: No textures found at path " + req.path, false);
				subGraphics = new Graphic[]
				{
					BaseContent.BadGraphic
				};
                subMaterials = new Material[]
                {
                    BaseContent.BadMat
                };
				return;
			}
			subGraphics = new Graphic[list.Count];
            subMaterials = new Material[list.Count];
			for (int i = 0; i < list.Count; i++)
			{
				string path = req.path + "/" + list[i].name;
				subGraphics[i] = GraphicDatabase.Get(typeof(Graphic_Single), path, req.shader, drawSize, color, colorTwo, null, req.shaderParameters);
                subMaterials[i] = MaterialPool.MatFrom(path);
			}
        }

        public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
		{
			return GraphicDatabase.Get<Graphic_Animate>(path, newShader, drawSize, newColor, newColorTwo, data);
		}

        public Graphic SubGraphicCycle(int index)
        {
            if(index > (subGraphics.Length - 1) )
            {
                Log.Warning($"Graphic Retrieval for Graphic_Animate indexed past maximum length of {subGraphics.Length}. Self correcting.");
                while (index > (subGraphics.Length - 1))
                    index -= (subGraphics.Length - 1);
            }
            return subGraphics[index];
        }

        public Material SubMaterialCycle(int index)
        {
            if(index > (subMaterials.Length - 1) )
            {
                Log.Warning($"Graphic Retrieval for Graphic_Animate indexed past maximum length of {subMaterials.Length}. Self correcting.");
                while (index > (subMaterials.Length - 1))
                    index -= (subMaterials.Length - 1);
            }
            return subMaterials[index];
        }

        public override string ToString()
        {
	        return string.Concat(new object[]
	        {
		        "AnimationCount(path=",
		        path,
		        ", count=",
		        subGraphics.Length,
		        ")"
	        });
        }

        public static string GetDefaultTexPath(string animationFolder)
        {
            return animationFolder + "/" + ContentFinder<Texture2D>.GetAllInFolder(animationFolder).FirstOrDefault().name;
        }

        

        private Material[] subMaterials;
    }
}
