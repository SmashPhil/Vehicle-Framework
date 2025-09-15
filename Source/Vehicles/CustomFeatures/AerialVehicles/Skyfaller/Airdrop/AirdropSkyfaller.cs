using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace Vehicles;

[PublicAPI]
[StaticConstructorOnStartup]
public class AirdropSkyfaller : Skyfaller
{
	private static readonly Material RopeMat = MaterialPool.MatFrom(GenDraw.LineTexPath,
		ShaderDatabase.SolidColor, new Color(0.15f, 0.15f, 0.15f));

	public AirdropDef AirdropDef => def as AirdropDef;

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		Thing thingForGraphic = GetThingForGraphic();
		float extraRotation = 0f;
		if (def.skyfaller.rotateGraphicTowardsDirection)
		{
			extraRotation = angle;
		}
		if (def.skyfaller.angleCurve != null)
		{
			angle = def.skyfaller.angleCurve.Evaluate(TimeInAnimation);
		}
		if (def.skyfaller.rotationCurve != null)
		{
			extraRotation += def.skyfaller.rotationCurve.Evaluate(TimeInAnimation);
		}
		if (def.skyfaller.xPositionCurve != null)
		{
			drawLoc.x += def.skyfaller.xPositionCurve.Evaluate(TimeInAnimation);
		}
		if (def.skyfaller.zPositionCurve != null)
		{
			drawLoc.z += def.skyfaller.zPositionCurve.Evaluate(TimeInAnimation);
		}
		if (thingForGraphic is VehiclePawn vehicle)
		{
			vehicle.DynamicDrawPhaseAt(DrawPhase.Draw, drawLoc, flip: flip);
		}
		if (thingForGraphic is Pawn pawn)
		{
			pawn.DrawNowAt(drawLoc, flip: flip);
		}
		else
		{
			Graphic.Draw(drawLoc, flip ? thingForGraphic.Rotation.Opposite : thingForGraphic.Rotation,
				thingForGraphic, extraRotation);
		}
		DrawParachute(drawLoc, extraRotation);
		DrawDropSpotShadowTrailing(drawLoc);
	}

	protected virtual void DrawDropSpotShadowTrailing(Vector3 apparentLoc)
	{
		Material shadowMaterial = ShadowMaterial;
		if (shadowMaterial == null)
		{
			return;
		}
		Vector3 drawLoc = this.TrueCenter();
		drawLoc.x = apparentLoc.x;
		DrawDropSpotShadow(drawLoc, Rotation, shadowMaterial, def.skyfaller.shadowSize,
			ticksToImpact);
	}

	public void DrawParachute(Vector3 drawLoc, float extraRotation)
	{
		if (AirdropDef.parachuteGraphicData is { } graphicData)
		{
			graphicData.Graphic.DrawWorker(drawLoc + Altitudes.AltIncVect * 2, Rot4.North, null, null,
				extraRotation);

			if (!AirdropDef.ropes.NullOrEmpty())
			{
				foreach (AirdropDef.AnchorPoint anchorPoint in AirdropDef.ropes)
				{
					Vector3 from = new Vector3(anchorPoint.from.x, 0, anchorPoint.from.y) +
						Altitudes.AltIncVect * anchorPoint.layer;
					Vector3 to = new Vector3(anchorPoint.to.x, 0, anchorPoint.to.y) +
						Altitudes.AltIncVect * anchorPoint.layer;
					GenDraw.DrawLineBetween(from + drawLoc, to + drawLoc, RopeMat, lineWidth: 0.05f);
				}
			}
		}
	}

	private Thing GetThingForGraphic()
	{
		if (def.graphicData != null || !innerContainer.Any)
		{
			return this;
		}
		return innerContainer[0];
	}

	protected override void Impact()
	{
		for (int i = 0; i < 6; i++)
		{
			FleckMaker.ThrowDustPuff(Position.ToVector3Shifted() + Gen.RandomHorizontalVector(1f), Map,
				1.2f);
		}
		FleckMaker.ThrowLightningGlow(Position.ToVector3Shifted(), Map, 2f);
		GenClamor.DoClamor(this, 15f, ClamorDefOf.Impact);
		base.Impact();
	}

	protected override void SpawnThings()
	{
		for (int i = innerContainer.Count - 1; i >= 0; i--)
		{
			Thing thing = innerContainer[i];
			bool placed = GenPlace.TryPlaceThing(thing, Position, Map, ThingPlaceMode.Near,
				placedAction: PlaceThing, rot: innerContainer[i].def.defaultPlacingRot);

			if (placed && thing is Pawn pawn)
			{
				if (pawn.IsColonist && pawn.Spawned && !Map.IsPlayerHome)
				{
					pawn.drafter.Drafted = true;
				}
			}
		}
		return;

		void PlaceThing(Thing thing, int _)
		{
			PawnUtility.RecoverFromUnwalkablePositionOrKill(thing.Position, thing.Map);
			if (thing.def.Fillage != FillCategory.Full)
				return;

			if (!def.skyfaller.CausesExplosion || !def.skyfaller.explosionDamage.isExplosive)
				return;

			if (!thing.Position.InHorDistOf(Position, def.skyfaller.explosionRadius))
				return;

			Map.terrainGrid.Notify_TerrainDestroyed(thing.Position);
		}
	}

	protected override void HitRoof()
	{
		base.HitRoof();

		if (this.OccupiedRect().Any(cell => cell.Fogged(Map)))
		{
			FloodFillerFog.FloodUnfog(Position, Map);
		}
	}

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		foreach (Thing thing in innerContainer)
		{
			if (thing is Pawn pawn)
			{
				pawn.Rotation = Rot4.South; // Ensure inner pawn faces south for rendering
			}
		}
	}

	[DebugAction(VehicleHarmony.VehiclesLabel, "Spawn Airdrop",
		allowedGameStates = AllowedGameStates.PlayingOnMap)]
	private static List<DebugActionNode> SpawnAirdrop()
	{
		List<DebugActionNode> debugActions = [];
		debugActions.Add(
			new DebugActionNode(SkyfallerDefOf.AirdropPackage.defName, DebugActionType.ToolMap)
			{
				action = delegate
				{
					Map map = Find.CurrentMap;
					if (map == null)
					{
						Log.Error("Attempting to use DebugRegionOptions with null map.");
						return;
					}
					IntVec3 cell = UI.MouseCell();
					List<Thing> contents =
					[
						MakeAirdropContent(ThingDefOf.MedicineIndustrial),
						MakeAirdropContent(ThingDefOf.MealSurvivalPack),
						MakeAirdropContent(ThingDefOf.MealSurvivalPack),
						MakeAirdropContent(ThingDefOf.MealSurvivalPack),
						MakeAirdropContent(ThingDefOf.Penoxycyline)
					];
					AirdropSkyfaller skyfaller =
						AirdropSkyfallerMaker.MakeAirdrop(SkyfallerDefOf.AirdropPackage, contents,
							AirdropProperties.Default with { packIntoContainer = true });
					GenSpawn.Spawn(skyfaller, cell, map);
				}
			});
		debugActions.Add(
			new DebugActionNode(SkyfallerDefOf.AirdropParatrooper.defName, DebugActionType.ToolMap)
			{
				childGetter = delegate
				{
					Map map = Find.CurrentMap;
					if (map == null)
					{
						Log.Error("Attempting to use DebugRegionOptions with null map.");
						return null;
					}
					List<DebugActionNode> debugActionPawns = [];
					foreach (Pawn pawn in map.mapPawns.FreeColonists)
					{
						debugActionPawns.Add(new DebugActionNode(pawn.Label, DebugActionType.ToolMap,
							delegate
							{
								IntVec3 cell = UI.MouseCell();
								AirdropSkyfaller skyfaller =
									AirdropSkyfallerMaker.MakeAirdrop(SkyfallerDefOf.AirdropParatrooper, pawn, AirdropProperties.Default);
								GenSpawn.Spawn(skyfaller, cell, map);
							}));
					}
					return debugActionPawns;
				}
			});
		return debugActions;

		static Thing MakeAirdropContent(ThingDef thingDef)
		{
			Thing thing = ThingMaker.MakeThing(thingDef);
			thing.stackCount = Rand.Range(1, thingDef.stackLimit);
			return thing;
		}
	}
}