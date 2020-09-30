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
using Vehicles.AI;
using HarmonyLib;
using Vehicles.Components;

namespace Vehicles
{
    public class VehiclePawn : Pawn
    {

        private Vector3 smoothPos;

        public Vehicle_PathFollower vPather;
        public Vehicle_DrawTracker vDrawer;
        public VehicleAI vehicleAI;

        private Color color1 = Color.white;
        private Color color2 = Color.white;

        public string selectedMask = "Default";

        private float angle = 0f; /* -45 is left, 45 is right : relative to Rot4 direction*/

        private Dictionary<Type, ThingComp> cachedComps = new Dictionary<Type, ThingComp>();

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

        public IEnumerable<IntVec3> InhabitedCells(int expandedBy = 0)
        {
            if (Angle == 0)
            {
                return CellRect.CenteredOn(Position, def.Size.x, def.Size.z).ExpandedBy(expandedBy).Cells;
            }
            return CellRect.CenteredOn(Position, def.Size.x, def.Size.z).ExpandedBy(expandedBy).Cells; //REDO FOR DIAGONALS
        }

        public float CachedAngle { get; set; }
        public float Angle
        {
            get
            {
                if (!GetCachedComp<CompVehicle>().Props.diagonalRotation)
                    return 0f;
                return angle;
            }
            set
            {
                if (value == angle)
                    return;
                angle = value;
            }
        }

        public new Vehicle_DrawTracker Drawer
        {
            get
            {
                if (vDrawer is null)
                {
                    vDrawer = new Vehicle_DrawTracker(this);
                }
                return vDrawer;
            }
        }

        private Graphic_Vehicle graphicInt;
        public Graphic_Vehicle VehicleGraphic
        {
            get
            {
                if (graphicInt is null)
                {
                    var graphicData = new GraphicData();
                    graphicData.CopyFrom(ageTracker.CurKindLifeStage.bodyGraphicData);
                    graphicData.color = DrawColor;
                    graphicData.colorTwo = DrawColorTwo;
                    graphicInt = graphicData.Graphic as Graphic_Vehicle;
                }
                return graphicInt;
            }
        }

        public override Color DrawColor //ADD COLORABLE FLAGS
        {
            get
            {
                return color1;
            }
            set
            {
                color1 = value;
            }
        }

        public new Color DrawColorTwo
        {
            get
            {
                return color2;
            }
            set
            {
                color2 = value;
            }
        }

        public override Vector3 DrawPos => Drawer.DrawPos;

        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            var drawVehicle = new Task(() => Drawer.DrawAt(drawLoc));
            drawVehicle.RunSynchronously();
            //Drawer.DrawAt(drawLoc);
        }

        public override void Notify_ColorChanged()
        {
            ResetGraphicCache();
            Drawer.renderer.graphics.ResolveAllGraphics();
            base.Notify_ColorChanged();
        }

        internal void ResetGraphicCache()
        {
            if(UnityData.IsInMainThread)
            {
                ResetMaskCache();
                var cannonComp = GetCachedComp<CompCannons>();
                if (cannonComp != null)
                {
                    foreach(CannonHandler cannon in cannonComp.Cannons)
                    {
                        cannon.ResolveCannonGraphics(this, true);
                    }
                }
            }
        }

        private void ResetMaskCache()
        {
            graphicInt = null;
            AccessTools.Field(typeof(GraphicData), "cachedGraphic").SetValue(ageTracker.CurKindLifeStage.bodyGraphicData, null);
        }

        public void UpdateRotationAndAngle()
        {
            UpdateRotation();
            UpdateAngle();
        }

        public void UpdateRotation()
        {
            if (vPather.nextCell == Position)
            {
                return;
            }
            IntVec3 intVec = vPather.nextCell - Position;
            if (intVec.x > 0)
            {
                Rotation = Rot4.East;
            }
            else if (intVec.x < 0)
            {
                Rotation = Rot4.West;
            }
            else if (intVec.z > 0)
            {
                Rotation = Rot4.North;
            }
            else
            {
                Rotation = Rot4.South;
            }
        }

        public void UpdateAngle()
        {
            if (vPather.Moving)
            {
                IntVec3 c = vPather.nextCell - Position;
                if (c.x > 0 && c.z > 0)
                {
                    angle = -45f;
                }
                else if (c.x > 0 && c.z < 0)
                {
                    angle = 45f;
                }
                else if (c.x < 0 && c.z < 0)
                {
                    angle = -45f;
                }
                else if (c.x < 0 && c.z > 0)
                {
                    angle = 45f;
                }
                else
                {
                    angle = 0f;
                }
            }
        }

        public override void DrawGUIOverlay()
		{
			Drawer.ui.DrawPawnGUIOverlay();
		}

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach(Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            if (/*Faction == Faction.OfPlayer && */drafter != null)
            {
                IEnumerable<Gizmo> draftGizmos = DraftGizmos(drafter);
                foreach(Gizmo c in draftGizmos)
                {
                    yield return c;
                }

                foreach(Gizmo c2 in GetCachedComp<CompVehicle>().CompGetGizmosExtra())
                {
                    yield return c2;
                }
                if (this.FueledVehicle())
                {
                    foreach(Gizmo c3 in GetCachedComp<CompFueledTravel>().CompGetGizmosExtra())
                    {
                        yield return c3;
                    }
                }
                if (this.HasCannons())
                {
                    foreach (Gizmo c4 in GetCachedComp<CompCannons>().CompGetGizmosExtra())
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

        public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;
            DamageDef defApplied = dinfo.Def;
	        if (def.damageMultipliers != null && def.damageMultipliers.AnyNullified(x => x.damageDef == defApplied))
	        {
                return;
	        }
            bool customDamage = dinfo.Instigator.def.HasModExtension<CustomVehicleDamageMultiplier>();

            float num = dinfo.Amount;
            float armorPoints = GetCachedComp<CompVehicle>().ArmorPoints;
            if (!customDamage || !dinfo.Instigator.def.GetModExtension<CustomVehicleDamageMultiplier>().ignoreArmor)
            {
                num -= num * (float)(1 - Math.Exp(-0.15 * (armorPoints / 10d))); // ( 1-e ^ { -0.15x } ) -> x = armorPoints / 10
                if (num < 1)
                    num = 0;
            }
            
            if (customDamage && (dinfo.Instigator.def.GetModExtension<CustomVehicleDamageMultiplier>().vehicleSpecifics.NullOrEmpty() || dinfo.Instigator.def.GetModExtension<CustomVehicleDamageMultiplier>().vehicleSpecifics.Contains(kindDef)))
            {
                num *= dinfo.Instigator.def.GetModExtension<CustomVehicleDamageMultiplier>().damageMultiplier;
            }
            else
            {
                if (dinfo.Def.isRanged)
                {
                    num *= GetCachedComp<CompVehicle>().Props.vehicleDamageMultipliers.rangedDamageMultiplier;
                }
                else if (dinfo.Def.isExplosive)
                {
                    num *= GetCachedComp<CompVehicle>().Props.vehicleDamageMultipliers.explosiveDamageMultiplier;
                }
                else
                {
                    num *= GetCachedComp<CompVehicle>().Props.vehicleDamageMultipliers.meleeDamageMultiplier;
                }
            }
            
            dinfo.SetAmount(num);
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
            ThingDef vehicleDef = GetCachedComp<CompVehicle>().Props.buildDef;

            VehicleBuilding thing = (VehicleBuilding)ThingMaker.MakeThing(vehicleDef);
            thing.SetFactionDirect(Faction);

            if (Current.ProgramState == ProgramState.Playing)
            {
                Find.Storyteller.Notify_PawnEvent(this, AdaptationEvent.Died, null);
            }
            if (flag && dinfo != null && dinfo.Value.Def.ExternalViolenceFor(this))
            {
                LifeStageUtility.PlayNearestLifestageSound(this, (LifeStageAge ls) => ls.soundDeath, 1f);
            }
            if (dinfo != null && dinfo.Value.Instigator != null)
            {
                Pawn pawn = dinfo.Value.Instigator as Pawn;
                if(pawn != null)
                {
                    RecordsUtility.Notify_PawnKilled(this, pawn);
                }
            }

            if (this.GetLord() != null)
            {
                this.GetLord().Notify_PawnLost(this, PawnLostCondition.IncappedOrKilled, dinfo);
            }
            if (flag)
            {
                DropAndForbidEverything(false);
            }

            thing.vehicleReference = this;

            meleeVerbs.Notify_PawnKilled();
            if (flag)
            {
                if (map.terrainGrid.TerrainAt(position) == TerrainDefOf.WaterOceanDeep || map.terrainGrid.TerrainAt(position) == TerrainDefOf.WaterDeep)
                {
                    IntVec3 lookCell = Position;
                    string textPawnList = "";
                    foreach (Pawn p in GetCachedComp<CompVehicle>()?.AllPawnsAboard)
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
                    GetCachedComp<CompVehicle>().DisembarkAll();
                    Destroy();
                }
            }
            thing.HitPoints = thing.MaxHitPoints / 10;
            Thing t = GenSpawn.Spawn(thing, position, map, rotation, WipeMode.FullRefund, false);
            return;
        }
        
        public T GetCachedComp<T>() where T : ThingComp
        {
            if (cachedComps.TryGetValue(typeof(T), out ThingComp compMatch))
            {
                return (T)compMatch;
            }
            T comp = GetComp<T>();
            if (comp is null)
            {
                return default;
            }
            cachedComps.Add(typeof(T), comp);
            return (T)cachedComps[typeof(T)];
        }

        //REDO
        public IEnumerable<VehicleComp> GetAllAIComps()
        {
            foreach (VehicleComp comp in cachedComps.Where(c => c.Key.IsAssignableFrom(typeof(VehicleComp))).Cast<VehicleComp>())
            {
                yield return comp;
            }
        }

        public override void DrawExtraSelectionOverlays()
        {
            if (vPather.curPath != null)
            {
                vPather.curPath.DrawPath(this);
            }
            HelperMethods.DrawLinesBetweenTargets(this, jobs.curJob, jobs.jobQueue);
        }

        public override TipSignal GetTooltip()
        {
            return base.GetTooltip();
        }

        public override void PostMapInit()
        {
            base.PostMapInit();
            vPather.TryResumePathingAfterLoading();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            if (cachedComps is null)
                cachedComps = new Dictionary<Type, ThingComp>();
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                smoothPos = Position.ToVector3Shifted();
                vPather.ResetToCurrentPosition();
                if (DrawColor == Color.white)
                {
                    DrawColor = ageTracker.CurKindLifeStage.bodyGraphicData.color;
                }
                if (DrawColorTwo == Color.white)
                {
                    DrawColorTwo = ageTracker.CurKindLifeStage.bodyGraphicData.colorTwo;
                }
            }
            if (Faction != Faction.OfPlayer)
            {
                drafter.Drafted = true;
                CompCannons cannonComp = GetCachedComp<CompCannons>();
                if(cannonComp != null)
                {
                    foreach(var cannon in cannonComp.Cannons)
                    {
                        cannon.autoTargeting = true;
                        cannon.AutoTarget = true;
                    }
                }
            }
            ResetGraphicCache();
            Drawer.Notify_Spawned();
        }

        public float VehicleMovedPercent()
        {
            if (!vPather.Moving)
			{
                return 0f;
			}
			if (vPather.BuildingBlockingNextPathCell() != null)
			{
				return 0f;
			}
			if (vPather.NextCellDoorToWaitForOrManuallyOpen() != null)
			{
				return 0f;
			}
			if (vPather.WillCollideWithPawnOnNextPathCell())
			{
				return 0f;
			}
			return 1f - vPather.nextCellCostLeft / vPather.nextCellCostTotal;
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            base.DeSpawn(mode);
            if (vPather != null)
            {
                vPather.StopDead();
            }
        }

        public override void Tick()
        {
            base.Tick();
            if (Spawned)
            {
                vPather.PatherTick();
            }
            if (Faction != Faction.OfPlayer)
            {
                vehicleAI?.AITick();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref smoothPos, "smoothPos");
            Scribe_Deep.Look(ref vPather, "vPather", new object[] { this });

            Scribe_Values.Look(ref angle, "angle");

            Scribe_Values.Look(ref color1, "color1", Color.white);
            Scribe_Values.Look(ref color2, "color2", Color.white);

            Scribe_Values.Look(ref selectedMask, "selectedMask", "Default");
        }
    }
}
