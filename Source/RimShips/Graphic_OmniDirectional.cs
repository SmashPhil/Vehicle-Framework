using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
    public class Graphic_OmniDirectional : Graphic
    {
        public string GraphicPath
		{
			get
			{
				return path;
			}
		}

		public override Material MatSingle
		{
			get
			{
				return MatSouth;
			}
		}

		public override Material MatWest
		{
			get
			{
				return mats[3];
			}
		}

		public override Material MatSouth
		{
			get
			{
				return mats[2];
			}
		}

		public override Material MatEast
		{
			get
			{
				return mats[1];
			}
		}

		public override Material MatNorth
		{
			get
			{
				return mats[0];
			}
		}

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

        public Material MatNorthEast
        {
            get
            {
                return mats[4];
            }
        }
        public Material MatSouthEast
        {
            get
            {
                return mats[5];
            }
        }
        public Material MatSouthWest
        {
            get
            {
                return mats[6];
            }
        }

        public Material MatNorthWest
        {
            get
            {
                return mats[7];
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
            if (thing is null || thing as Pawn is null || !HelperMethods.IsVehicle(thing as VehiclePawn))
                return base.MatAt(rot, thing);

            float angle = (thing as VehiclePawn).GetComp<CompVehicle>().Angle;
	        switch (rot.AsInt)
	        {
	            case 0:
		            return MatNorth;
	            case 1:
                    if (angle == -45)
                        return MatNorthEast;
                    else if (angle == 45)
                        return MatSouthEast;
		            return MatEast;
	            case 2:
		            return MatSouth;
	            case 3:
                    if (angle == -45)
                        return MatSouthWest;
                    else if (angle == 45)
                        return MatNorthWest;
		            return MatWest;
	            default:
		            return BaseContent.BadMat;
	        }
        }

        public override void Init(GraphicRequest req)
		{
			data = req.graphicData;
			path = req.path;
			color = req.color;
			colorTwo = req.colorTwo;
			drawSize = req.drawSize;
			Texture2D[] array = new Texture2D[mats.Length];
			array[0] = ContentFinder<Texture2D>.Get(req.path + "_north", false);
			array[1] = ContentFinder<Texture2D>.Get(req.path + "_east", false);
			array[2] = ContentFinder<Texture2D>.Get(req.path + "_south", false);
			array[3] = ContentFinder<Texture2D>.Get(req.path + "_west", false);
            array[4] = ContentFinder<Texture2D>.Get(req.path + "_northEast", false);
            array[5] = ContentFinder<Texture2D>.Get(req.path + "_southEast", false);
            array[6] = ContentFinder<Texture2D>.Get(req.path + "_southWest", false);
            array[7] = ContentFinder<Texture2D>.Get(req.path + "_northWest", false);
            
			if (array[0] == null)
			{
				if (array[2] != null)
				{
					array[0] = array[2];
					drawRotatedExtraAngleOffset = 180f;
				}
				else if (array[1] != null)
				{
					array[0] = array[1];
					drawRotatedExtraAngleOffset = -90f;
				}
				else if (array[3] != null)
				{
					array[0] = array[3];
					drawRotatedExtraAngleOffset = 90f;
				}
				else
				{
					array[0] = ContentFinder<Texture2D>.Get(req.path, false);
				}
			}
			if (array[0] == null)
			{
				Log.Error("Failed to find any textures at " + req.path + " while constructing " + this.ToStringSafe<Graphic_OmniDirectional>(), false);
				return;
			}
			if (array[2] == null)
			{
				array[2] = array[0];
			}
			if (array[1] == null)
			{
				if (array[3] != null)
				{
					array[1] = array[3];
					eastFlipped = DataAllowsFlip;
				}
				else
				{
					array[1] = array[0];
				}
			}
			if (array[3] == null)
			{
				if (array[1] != null)
				{
					array[3] = array[1];
					westFlipped = DataAllowsFlip;
				}
				else
				{
					array[3] = array[0];
				}
			}

            if(array[5] == null)
            {
                if(array[4] != null)
                {
                    array[5] = array[4];
                    eastDiagonalFlipped = DataAllowsFlip;
                }
                else
                {
                    array[5] = array[1];
                }
            }
            if(array[6] == null)
            {
                if(array[7] != null)
                {
                    array[6] = array[7];
                    westDiagonalFlipped = DataAllowsFlip;
                }
                else
                {
                    array[6] = array[3];
                }
            }
            if(array[4] == null)
            {
                if(array[5] != null)
                {
                    array[4] = array[5];
                    eastDiagonalFlipped = DataAllowsFlip;
                }
                else
                {
                    array[4] = array[1];
                }
            }
            if(array[7] == null)
            {
                if(array[6] != null)
                {
                    array[7] = array[6];
                    westDiagonalFlipped = DataAllowsFlip;
                }
                else
                {
                    array[7] = array[3];
                }
            }
            
			Texture2D[] array2 = new Texture2D[mats.Length];
			if (req.shader.SupportsMaskTex())
			{
				array2[0] = ContentFinder<Texture2D>.Get(req.path + "_northm", false);
				array2[1] = ContentFinder<Texture2D>.Get(req.path + "_eastm", false);
				array2[2] = ContentFinder<Texture2D>.Get(req.path + "_southm", false);
				array2[3] = ContentFinder<Texture2D>.Get(req.path + "_westm", false);
                array2[4] = ContentFinder<Texture2D>.Get(req.path + "_northEastm", false);
                array2[5] = ContentFinder<Texture2D>.Get(req.path + "_southEastm", false);
                array2[6] = ContentFinder<Texture2D>.Get(req.path + "_southWestm", false);
                array2[7] = ContentFinder<Texture2D>.Get(req.path + "_northWestm", false);
				if (array2[0] == null)
				{
					if (array2[2] != null)
					{
						array2[0] = array2[2];
					}
					else if (array2[1] != null)
					{
						array2[0] = array2[1];
					}
					else if (array2[3] != null)
					{
						array2[0] = array2[3];
					}
				}
				if (array2[2] == null)
				{
					array2[2] = array2[0];
				}
				if (array2[1] == null)
				{
					if (array2[3] != null)
					{
						array2[1] = array2[3];
					}
					else
					{
						array2[1] = array2[0];
					}
				}
				if (array2[3] == null)
				{
					if (array2[1] != null)
					{
						array2[3] = array2[1];
					}
					else
					{
						array2[3] = array2[0];
					}
				}

                if(array2[5] == null)
            {
                if(array2[4] != null)
                {
                    array2[5] = array2[4];
                    eastDiagonalFlipped = DataAllowsFlip;
                }
                else
                {
                    array2[5] = array2[1];
                }
            }
            if(array2[6] == null)
            {
                if(array2[7] != null)
                {
                    array2[6] = array2[7];
                    westDiagonalFlipped = DataAllowsFlip;
                }
                else
                {
                    array2[6] = array2[3];
                }
            }
            if(array2[4] == null)
            {
                if(array2[5] != null)
                {
                    array2[4] = array2[5];
                    eastDiagonalFlipped = DataAllowsFlip;
                }
                else
                {
                    array2[4] = array2[1];
                }
            }
            if(array2[7] == null)
            {
                if(array2[6] != null)
                {
                    array2[7] = array2[6];
                    westDiagonalFlipped = DataAllowsFlip;
                }
                else
                {
                    array2[7] = array2[3];
                }
            }
			}
			for (int i = 0; i < mats.Length; i++)
			{
				MaterialRequest req2 = default(MaterialRequest);
				req2.mainTex = array[i];
				req2.shader = req.shader;
				req2.color = color;
				req2.colorTwo = colorTwo;
				req2.maskTex = array2[i];
				req2.shaderParameters = req.shaderParameters;
				mats[i] = MaterialPool.MatFrom(req2);
			}
		}

		public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
		{
			return GraphicDatabase.Get<Graphic_OmniDirectional>(path, newShader, drawSize, newColor, newColorTwo, data);
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
			return Gen.HashCombineStruct<Color>(Gen.HashCombineStruct<Color>(Gen.HashCombine<string>(0, path), color), colorTwo);
		}

		private Material[] mats = new Material[8];
		private bool westFlipped;
		private bool eastFlipped;
		private float drawRotatedExtraAngleOffset;

        private bool eastDiagonalFlipped;
        private bool westDiagonalFlipped;
    }
}
