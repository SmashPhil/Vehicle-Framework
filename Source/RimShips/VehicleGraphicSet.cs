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
    public class VehicleGraphicSet
    {
		public VehicleGraphicSet(VehiclePawn vehicle)
		{
			this.vehicle = vehicle;
			flasher = new DamageFlasher(vehicle);
		}

        public bool AllResolved
		{
			get
			{
				return nakedGraphic != null;
			}
		}

		public List<Material> MatsBodyBaseAt(Rot4 facing, float angle, float cachedAngle, RotDrawMode bodyCondition = RotDrawMode.Fresh)
		{
			if(facing.IsHorizontal && angle != cachedAngle)
            {
                cachedMatsBodyBase.Clear();
                cachedMatsBodyBaseHash = -1;
                vehicle.GetComp<CompVehicle>().CachedAngle = vehicle.GetComp<CompVehicle>().Angle;
            }

			int num = facing.AsInt + 1000 * (int)bodyCondition;
			if (num != cachedMatsBodyBaseHash)
			{
				cachedMatsBodyBase.Clear();
				cachedMatsBodyBaseHash = num;
				
				cachedMatsBodyBase.Add(nakedGraphic.MatAt(facing, vehicle));
			}
			return cachedMatsBodyBase;
		}

		public void ClearCache()
		{
			cachedMatsBodyBaseHash = -1;
		}


		public void ResolveAllGraphics()
		{
			ClearCache();
			if (vehicle.RaceProps.Humanlike)
			{
				nakedGraphic = GraphicDatabase.Get<Graphic_OmniDirectional>(vehicle.story.bodyType.bodyNakedGraphicPath, ShaderDatabase.CutoutSkin, Vector2.one, vehicle.story.SkinColor);
				rottingGraphic = GraphicDatabase.Get<Graphic_OmniDirectional>(vehicle.story.bodyType.bodyNakedGraphicPath, ShaderDatabase.CutoutSkin, Vector2.one, PawnGraphicSet.RottingColor);
				dessicatedGraphic = GraphicDatabase.Get<Graphic_OmniDirectional>(vehicle.story.bodyType.bodyDessicatedGraphicPath, ShaderDatabase.Cutout);
				return;
			}
			PawnKindLifeStage curKindLifeStage = vehicle.ageTracker.CurKindLifeStage;
			if (vehicle.gender != Gender.Female || curKindLifeStage.femaleGraphicData == null)
			{
				nakedGraphic = curKindLifeStage.bodyGraphicData.Graphic;
			}
			else
			{
				nakedGraphic = curKindLifeStage.femaleGraphicData.Graphic;
			}
			if (vehicle.RaceProps.packAnimal)
			{
				packGraphic = GraphicDatabase.Get<Graphic_OmniDirectional>(nakedGraphic.path + "Pack", ShaderDatabase.Cutout, nakedGraphic.drawSize, Color.white);
			}
			rottingGraphic = nakedGraphic.GetColoredVersion(ShaderDatabase.CutoutSkin, PawnGraphicSet.RottingColor, PawnGraphicSet.RottingColor);
			if (!vehicle.kindDef.alternateGraphics.NullOrEmpty())
			{
				Rand.PushState(vehicle.thingIDNumber ^ 46101);
				if (Rand.Value <= vehicle.kindDef.alternateGraphicChance)
				{
					nakedGraphic = vehicle.kindDef.alternateGraphics.RandomElementByWeight((AlternateGraphic x) => x.Weight).GetGraphic(nakedGraphic);
				}
				Rand.PopState();
			}
		}

		public void SetAllGraphicsDirty()
		{
			if (AllResolved)
			{
				ResolveAllGraphics();
			}
		}

		public VehiclePawn vehicle;

		public Graphic nakedGraphic;

		public Graphic rottingGraphic;

		public Graphic dessicatedGraphic;

		public Graphic packGraphic;

		public DamageFlasher flasher;

		private List<Material> cachedMatsBodyBase = new List<Material>();

		private int cachedMatsBodyBaseHash = -1;
    }
}
