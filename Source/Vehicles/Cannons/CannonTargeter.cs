using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;


namespace Vehicles
{
    public class CannonTargeter
    {
        public bool IsTargeting => this.action != null;

        public void BeginTargeting(TargetingParameters targetParams, Action<LocalTargetInfo> action, CannonHandler cannon, Action actionWhenFinished = null, Texture2D mouseAttachment = null)
        {
            this.action = action;
            this.targetParams = targetParams;
            caster = cannon.pawn;
            this.cannon = cannon;
            this.cannon.SetTarget(LocalTargetInfo.Invalid);
            this.actionWhenFinished = actionWhenFinished;
            this.mouseAttachment = mouseAttachment;
            map = cannon.pawn.Map;
            this.cannon.LockedStatusRotation = false;
        }

        public static bool TargetMeetsRequirements(CannonHandler cannon, LocalTargetInfo obj)
        {
            float distance = (cannon.TurretLocation.ToIntVec3() - obj.Cell).LengthHorizontal;
            bool result = (distance >= cannon.MinRange && (distance < cannon.MaxRange || cannon.MaxRange <= -1))
                        && cannon.AngleBetween(obj.CenterVector3);
            //Log.Message($"Result {result} Details--> distance: {distance} min: {cannon.MinRange} max: {cannon.MaxRange} angle: {cannon.AngleBetween(obj.CenterVector3)}");
            return result;
        }

        public void StopTargeting(bool canceled = true)
        {
            if(actionWhenFinished != null)
            {
                Action action = actionWhenFinished;
                actionWhenFinished = null;
                action();
            }
            if (canceled && cannon != null)
            {
                cannon.AlignToAngleRestricted(cannon.currentRotation);
                //cannon.TurretRotation = cannon.currentRotation;
            }
            cannon = null;
            this.action = null;
        }

        public void ProcessInputEvents()
        {
            ConfirmStillValid();
            if(IsTargeting)
            {
                if(Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    Event.current.Use();
                    if(action != null)
                    {                      
                        LocalTargetInfo obj = CurrentTargetUnderMouse();
                        if(obj.Cell.InBounds(map) && TargetMeetsRequirements(cannon, obj))
                        {
                            action(obj);
                            SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                            StopTargeting(false);
                        }
                        else
                        {
                            SoundDefOf.ClickReject.PlayOneShotOnCamera(null);
                        }
                    }
                    
                }
                if ((Event.current.type == EventType.MouseDown && Event.current.button == 1) || KeyBindingDefOf.Cancel.KeyDownEvent)
                {
                    StopTargeting();
                    SoundDefOf.CancelMode.PlayOneShotOnCamera(null);
                    Event.current.Use();
                }
            }
        }

        public void TargeterOnGUI()
        {
            if(action != null)
            {
                float distance = (cannon.TurretLocation.ToIntVec3() - CurrentTargetUnderMouse().Cell).LengthHorizontal;
                if (TargetMeetsRequirements(cannon, CurrentTargetUnderMouse()))
                {
                    Texture2D icon = mouseAttachment ?? TexCommand.Attack;
                    GenUI.DrawMouseAttachment(icon);
                }
            }
        }

        public void TargeterUpdate()
        {
            if(IsTargeting)
            {
                float distance = (cannon.TurretLocation.ToIntVec3() - CurrentTargetUnderMouse().Cell).LengthHorizontal;
                if (TargetMeetsRequirements(cannon, CurrentTargetUnderMouse()))
                {
                    GenDraw.DrawTargetHighlight(CurrentTargetUnderMouse());

                    if(CurrentTargetUnderMouse() != cannon.pawn)
                        cannon.AlignToAngleRestricted((float)cannon.TurretLocation.ToIntVec3().AngleToCell(CurrentTargetUnderMouse().Cell, map));
                }
                //REDO Radius Circle
                //if(cannon.MinRange > 0)
                //    GenDraw.DrawRadiusRing(cannon.TurretLocation.ToIntVec3(), cannon.cannonDef.minRange, Color.red);
                //if(cannon.cannonDef.maxRange <= GenRadial.MaxRadialPatternRadius)
                //    GenDraw.DrawRadiusRing(cannon.TurretLocation.ToIntVec3(), cannon.MaxRange, Color.white);
            }
        }

        private void ConfirmStillValid()
        {
            if(caster is null || (caster.Map != Find.CurrentMap || caster.Destroyed || !Find.Selector.IsSelected(caster)) || !caster.Drafted)
            {
                StopTargeting();
            }
        }

        private LocalTargetInfo CurrentTargetUnderMouse()
        {
            if(!IsTargeting)
                return LocalTargetInfo.Invalid;
            LocalTargetInfo localTarget = LocalTargetInfo.Invalid;
            using(IEnumerator<LocalTargetInfo> enumerator = GenUI.TargetsAtMouse_NewTemp(targetParams, false).GetEnumerator())
            {
                if(enumerator.MoveNext())
                {
                    LocalTargetInfo localTarget2 = enumerator.Current;
                    localTarget = localTarget2;
                }
            }
            return localTarget;
        }

        private Action<LocalTargetInfo> action;

        private Pawn caster;

        public CannonHandler cannon { get; private set; }

        private TargetingParameters targetParams;

        private Action actionWhenFinished;

        private Texture2D mouseAttachment;

        private Map map;
    }
}
