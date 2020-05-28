using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace Vehicles
{
    public class VehiclePawn : Pawn
    {

        private Vector3 smoothPos;

        public override Vector3 DrawPos
        {
            get
            {
                return RimShipMod.mod.settings.debugDisableSmoothPathing ? base.DrawPos : SmoothPos;
            }
        }

        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            //Graphic.Draw(drawLoc, flip ? Rotation.Opposite : Rotation, this, 25f);
            base.DrawAt(drawLoc, flip);
        }

        public Vector3 SmoothPos
        {
            get
            {
                return smoothPos;
            }
            set
            {
                smoothPos = value;
            }
        }

        public override Color DrawColor //ADD COLORABLE FLAGS
        {
            get => base.DrawColor;
            set => base.DrawColor = value;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach(Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            if(drafter != null)
            {
                IEnumerable<Gizmo> draftGizmos = DraftGizmos(drafter);
                foreach(Gizmo c in draftGizmos)
                {
                    yield return c;
                }

                foreach(Gizmo c2 in GetComp<CompVehicle>().CompGetGizmosExtra())
                {
                    yield return c2;
                }
                if(this.FueledVehicle())
                {
                    foreach(Gizmo c3 in GetComp<CompFueledTravel>().CompGetGizmosExtra())
                    {
                        yield return c3;
                    }
                }
                if(this.HasCannons())
                {
                    foreach (Gizmo c4 in GetComp<CompCannons>().CompGetGizmosExtra())
                    {
                        yield return c4;
                    }
                }
            }
        }

        internal static IEnumerable<Gizmo> DraftGizmos(Pawn_DraftController drafter)
        {
            Command_Toggle command_Toggle = new Command_Toggle();
			command_Toggle.hotKey = KeyBindingDefOf.Command_ColonistDraft;
			command_Toggle.isActive = (() => drafter.Drafted);
			command_Toggle.toggleAction = delegate()
			{
				drafter.Drafted = !drafter.Drafted;
				PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Drafting, KnowledgeAmount.SpecificInteraction);
				if (drafter.Drafted)
				{
					LessonAutoActivator.TeachOpportunity(ConceptDefOf.QueueOrders, OpportunityType.GoodToKnow);
				}
			};
			command_Toggle.defaultDesc = "CommandToggleDraftDesc".Translate();
			command_Toggle.icon = TexCommand.Draft;
			command_Toggle.turnOnSound = SoundDefOf.DraftOn;
			command_Toggle.turnOffSound = SoundDefOf.DraftOff;
			if (!drafter.Drafted)
			{
				command_Toggle.defaultLabel = "CommandDraftLabel".Translate();
			}
			if (drafter.pawn.Downed)
			{
				command_Toggle.Disable("IsIncapped".Translate(drafter.pawn.LabelShort, drafter.pawn));
			}
			if (!drafter.Drafted)
			{
				command_Toggle.tutorTag = "Draft";
			}
			else
			{
				command_Toggle.tutorTag = "Undraft";
			}
			yield return command_Toggle;
			if (drafter.Drafted && drafter.pawn.equipment.Primary != null && drafter.pawn.equipment.Primary.def.IsRangedWeapon)
			{
				yield return new Command_Toggle
				{
					hotKey = KeyBindingDefOf.Misc6,
					isActive = (() => drafter.FireAtWill),
					toggleAction = delegate()
					{
						drafter.FireAtWill = !drafter.FireAtWill;
					},
					icon = TexCommand.FireAtWill,
					defaultLabel = "CommandFireAtWillLabel".Translate(),
					defaultDesc = "CommandFireAtWillDesc".Translate(),
					tutorTag = "FireAtWillToggle"
				};
			}
			yield break;
        }

        public override void Kill(DamageInfo? dinfo, Hediff exactCulprit = null)
        {
            IntVec3 position = PositionHeld;
            Rot4 rotation = Rotation;
            
            Map map = Map;
            Map mapHeld = MapHeld;
            bool flag = Spawned;
            bool worldPawn = this.IsWorldPawn();
            Caravan caravan = this.GetCaravan();
            ThingDef shipDef = GetComp<CompVehicle>().Props.buildDef;

            Thing thing = ThingMaker.MakeThing(shipDef);
            thing.SetFactionDirect(Faction);

            if (Current.ProgramState == ProgramState.Playing)
            {
                Find.Storyteller.Notify_PawnEvent(this, AdaptationEvent.Died, null);
            }
            if(flag && dinfo != null && dinfo.Value.Def.ExternalViolenceFor(this))
            {
                LifeStageUtility.PlayNearestLifestageSound(this, (LifeStageAge ls) => ls.soundDeath, 1f);
            }
            if(dinfo != null && dinfo.Value.Instigator != null)
            {
                Pawn pawn = dinfo.Value.Instigator as Pawn;
                if(pawn != null)
                {
                    RecordsUtility.Notify_PawnKilled(this, pawn);
                }
            }

            if(this.GetLord() != null)
            {
                this.GetLord().Notify_PawnLost(this, PawnLostCondition.IncappedOrKilled, dinfo);
            }
            if(flag)
            {
                DropAndForbidEverything(false);
            }

            if(thing.TryGetComp<CompSavePawnReference>() != null)
            {
                thing.TryGetComp<CompSavePawnReference>().pawnReference = this;
            }

            this.meleeVerbs.Notify_PawnKilled();
            if(flag)
            {
                if (map.terrainGrid.TerrainAt(position) == TerrainDefOf.WaterOceanDeep || map.terrainGrid.TerrainAt(position) == TerrainDefOf.WaterDeep)
                {
                    IntVec3 lookCell = Position;
                    string textPawnList = "";
                    foreach (Pawn p in GetComp<CompVehicle>()?.AllPawnsAboard)
                    {
                        textPawnList += p.LabelShort + ". ";
                    }
                    Find.LetterStack.ReceiveLetter("ShipSunkLabel".Translate(), "ShipSunkDeep".Translate(LabelShort, textPawnList), LetterDefOf.NegativeEvent, new TargetInfo(lookCell, map, false), null, null);
                    Destroy();
                    return;
                }
                else
                {
                    Find.LetterStack.ReceiveLetter("ShipSunkLabel".Translate(), "ShipSunkShallow".Translate(LabelShort), LetterDefOf.NegativeEvent, new TargetInfo(position, map, false), null, null);
                    GetComp<CompVehicle>().DisembarkAll();
                    Destroy();
                }
            }
            thing.HitPoints = thing.MaxHitPoints / 10;
            Thing t = GenSpawn.Spawn(thing, position, map, rotation, WipeMode.FullRefund, false);
            return;
        }

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            if(pather.curPath != null)
            {
                pather.curPath.DrawPath(this);
            }
            jobs.DrawLinesBetweenTargets();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if(!respawningAfterLoad)
            {
                smoothPos = Position.ToVector3Shifted();
            }
            Log.Message("Check");
            if(kindDef.lifeStages.Last().bodyGraphicData.graphicClass == typeof(Graphic_OmniDirectional))
            {
                Log.Message($"Passed: {kindDef.lifeStages.Last().bodyGraphicData.Graphic}");
                
                (kindDef.lifeStages.Last().bodyGraphicData.Graphic as Graphic_OmniDirectional).pawn = this;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref smoothPos, "smoothPos");
        }
    }
}
