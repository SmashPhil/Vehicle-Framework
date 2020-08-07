using System;
using System.Collections.Generic;
using Verse;
using Verse.Sound;
using RimWorld;
using UnityEngine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Vehicles
{
    public enum WeaponType { None, Static, Rotatable }
    public enum WeaponLocation { Port, Starboard, Bow, Stern, Turret }
    public class CannonHandler : IExposable, ILoadReferenceable
    {
        public CannonHandler()
        {
        }

        public CannonHandler(VehiclePawn pawn, CannonHandler reference)
        {
            this.pawn = pawn;
            if(!ValidateCannonSetup(reference, out List<string> errors))
            {
                Log.Error($"Errors detected inside CannonHandler [{reference.cannonDef.label}].");
                foreach(string error in errors)
                {
                    Log.Error(error);
                }
            }

            uniqueID = Find.UniqueIDsManager.GetNextThingID();
            cannonDef = reference.cannonDef;

            cannonRenderOffset = reference.cannonRenderOffset;
            cannonRenderLocation = reference.cannonRenderLocation;
            defaultAngleRotated = reference.defaultAngleRotated;

            aimPieOffset = reference.aimPieOffset;
            angleRestricted = reference.angleRestricted;

            baseCannonRenderLocation = reference.baseCannonRenderLocation;

            baseCannonDrawSize = reference.baseCannonDrawSize;
            drawLayer = reference.drawLayer;

            gizmoLabel = reference.gizmoLabel;

            key = reference.key;
            parentKey = reference.parentKey;

            targetPersists = reference.targetPersists;
            autoTargeting = reference.autoTargeting;
            manualTargeting = reference.manualTargeting;

            fireModes = new List<FireMode>(reference.cannonDef.fireModes);
            currentFireMode = fireModes.FirstOrDefault();

            childCannons = new List<CannonHandler>();
            if(!string.IsNullOrEmpty(parentKey))
            {
                foreach (CannonHandler cannon in pawn.GetCachedComp<CompCannons>().Cannons.Where(c => c.key == parentKey))
                {
                    attachedTo = cannon;
                    cannon.childCannons.Add(this);
                }
            }
            
            ResolveCannonGraphics(pawn);
            DisableAnimation();

            if (cannonDef.splitCannonGroups)
            {
                if (cannonDef.cannonsPerPoint.Count != cannonDef.centerPoints.Count || (cannonDef.cannonsPerPoint.Count == 0 && cannonDef.centerPoints.Count == 0))
                {
                    Log.Warning("Could Not initialize cannon groups for " + this.pawn.LabelShort);
                    return;
                }
                int group = 0;
                for (int i = 0; i < cannonDef.numberCannons; i++)
                {
                    if ((i + 1) > (cannonDef.cannonsPerPoint[group] * (group + 1)))
                        group++;
                    cannonGroupDict.Add(i, group);
                    if (VehicleHarmony.debug)
                    {
                        Log.Message(string.Concat(new object[]
                        {
                        "Initializing ", pawn.LabelShortCap,
                        " with cannon ", cannonDef.label,
                        " with ", cannonDef.cannonsPerPoint[group],
                        " cannons in group: ", group
                        }));
                    }
                }
            }

            restrictedTheta = (int)Math.Abs(angleRestricted.x - (angleRestricted.y + 360)).ClampAngle();

            LockedStatusRotation = true;
            ResetCannonAngle();
        }
        public void ExposeData()
        {
            Scribe_Defs.Look(ref cannonDef, "cannonDef");
            Scribe_Values.Look(ref uniqueID, "uniqueID", -1);
            Scribe_Values.Look(ref reloadTicks, "reloadTicks");

            Scribe_Values.Look(ref cannonRenderOffset, "cannonRenderOffset");
            Scribe_Values.Look(ref cannonRenderLocation, "cannonRenderLocation");
            Scribe_Values.Look(ref currentRotation, "currentRotation", defaultAngleRotated - 90);
            Scribe_Values.Look(ref rotationTargeted, "rotationTargeted", defaultAngleRotated - 90);

            Scribe_Values.Look(ref targetPersists, "targetPersists");
            Scribe_Values.Look(ref autoTargeting, "autoTargeting");
            Scribe_Values.Look(ref manualTargeting, "manualTargeting");

            Scribe_Values.Look(ref aimPieOffset, "aimPieOffset");
            Scribe_Values.Look(ref angleRestricted, "angleRestricted");
            Scribe_Values.Look(ref restrictedTheta, "restrictedTheta", (int)Math.Abs(angleRestricted.x - (angleRestricted.y + 360)).ClampAngle());

            Scribe_Values.Look(ref baseCannonRenderLocation, "baseCannonRenderLocation");

            Scribe_Values.Look(ref drawLayer, "drawLayer");

            Scribe_Values.Look(ref parentKey, "parentKey");
            Scribe_Values.Look(ref key, "key");

            Scribe_Defs.Look(ref loadedAmmo, "loadedAmmo");
            Scribe_Defs.Look(ref savedAmmoType, "savedAmmoType");
            Scribe_Values.Look(ref shellCount, "shellCount");
            Scribe_Values.Look(ref gizmoLabel, "gizmoLabel");

            Scribe_Collections.Look(ref cannonGroupDict, "cannonGroupDict");
            Scribe_TargetInfo.Look(ref cannonTarget, "cannonTarget", LocalTargetInfo.Invalid);

            Scribe_Collections.Look(ref fireModes, "fireModes");
            Scribe_Values.Look(ref currentFireMode, "currentFireMode");

            Scribe_Values.Look(ref autoTargetingActive, "autoTargetingActive");
        }

        public bool IsTargetable => cannonDef?.weaponType == WeaponType.Rotatable; //Add to this later

        public bool RotationIsValid => currentRotation == rotationTargeted;

        public bool TargetLocked { get; set; }

        public bool CannonDisabled => !RelatedHandlers.NullOrEmpty() && RelatedHandlers.AnyNullified(h => h.handlers.Count < h.role.slotsToOperate);

        public List<VehicleHandler> RelatedHandlers => pawn.GetCachedComp<CompVehicle>().handlers.FindAll(h => !h.role.cannonIds.NullOrEmpty() && h.role.cannonIds.Contains(key));
        public bool ActivateTimer(bool ignoreTimer = false)
        {
            if (reloadTicks > 0 && !ignoreTimer)
                return false;
            reloadTicks = MaxTicks;
            TargetLocked = false;
            return true;
        }

        public void ActivateBurstTimer()
        {
            burstTicks = CurrentFireMode.ticksBetweenBursts;
        }

        public void DoTick()
        {
            if(!pawn.Drafted)
            {
                GizmoHighlighted = false;
            }

            if (!pawn.GetCachedComp<CompCannons>().multiFireCannon.AnyNullified(mf => mf.Second == this))
            {
                if (AutoTarget && Find.TickManager.TicksGame % AutoTargetInterval == 0 && pawn.Drafted)
                {
                    if(CannonDisabled)
                    {
                        return;
                    }
                    if(!cannonTarget.IsValid && HelperMethods.CannonTargeter.cannon != this && reloadTicks <= 0 && shellCount > 0)
                    {
                        LocalTargetInfo autoTarget = this.GetCannonTarget();
                        if(autoTarget.IsValid)
                        {
                            AlignToAngleRestricted((float)TurretLocation.AngleToPoint(autoTarget.Thing.DrawPos, pawn.Map));
                            SetTarget(autoTarget);
                        }
                    }
                }
                if(reloadTicks > 0)
                {
                    reloadTicks--;
                }
                if(burstTicks > 0)
                {
                    burstTicks--;
                }
            }
            
            if(rotationTargeted != currentRotation)
            {
                if(pawn.Drafted)
                {
                    float relativeCurrentRotation = currentRotation + 90;
                    float relativeTargetedRotation = rotationTargeted + 90;
                    if (relativeCurrentRotation < 0)
                        relativeCurrentRotation += 360;
                    else if (relativeCurrentRotation > 360)
                        relativeCurrentRotation -= 360;
                    if (relativeTargetedRotation < 0)
                        relativeTargetedRotation += 360;
                    else if (relativeTargetedRotation > 360)
                        relativeTargetedRotation -= 360;
                    if(Math.Abs(relativeCurrentRotation - relativeTargetedRotation) < cannonDef.rotationSpeed)
                    {
                        currentRotation = rotationTargeted;
                    }
                    else
                    {
                        int? sign = AngleDirectionalRestricted(relativeTargetedRotation);
                        if(relativeCurrentRotation < relativeTargetedRotation)
                        {
                            if (Math.Abs(relativeCurrentRotation - relativeTargetedRotation) < 180)
                            {
                                currentRotation += cannonDef.rotationSpeed;
                                foreach(CannonHandler cannon in childCannons)
                                {
                                    cannon.currentRotation += cannonDef.rotationSpeed;
                                }
                            }
                            else
                            {
                                currentRotation -= cannonDef.rotationSpeed;
                                foreach(CannonHandler cannon in childCannons)
                                {
                                    cannon.currentRotation -= cannonDef.rotationSpeed;
                                }
                            }
                        }
                        else
                        {
                            if (Math.Abs(relativeCurrentRotation - relativeTargetedRotation) < 180)
                            {
                                currentRotation -= cannonDef.rotationSpeed;
                                foreach(CannonHandler cannon in childCannons)
                                {
                                    cannon.currentRotation -= cannonDef.rotationSpeed;
                                }
                            }
                            else
                            {
                                currentRotation += cannonDef.rotationSpeed;
                                foreach(CannonHandler cannon in childCannons)
                                {
                                    cannon.currentRotation += cannonDef.rotationSpeed;
                                }
                            }
                        }
                    }
                }
                else
                {
                    rotationTargeted = currentRotation;
                }
            }
            if(IsTargetable)
            {
                if(cannonTarget.IsValid && currentRotation == rotationTargeted && !TargetLocked)
                {
                    TargetLocked = true;
                    ResetPrefireTimer();
                }
                if(cannonTarget.Cell.IsValid)
                {
                    if(!CannonTargeter.TargetMeetsRequirements(this, cannonTarget) || !pawn.Drafted)
                    {
                        SetTarget(LocalTargetInfo.Invalid);
                        TargetLocked = false;
                        return;
                    }
                    LockedStatusRotation = false;

                    if(PrefireTickCount > 0)
                    {
                        if(cannonTarget.HasThing)
                        {
                            rotationTargeted = (float)TurretLocation.AngleToPoint(cannonTarget.Thing.DrawPos, pawn.Map);
                            if (attachedTo != null)
                                rotationTargeted += attachedTo.TurretRotation;
                        }
                        else
                        {
                            rotationTargeted = (float)TurretLocation.ToIntVec3().AngleToCell(cannonTarget.Cell, pawn.Map);
                            if (attachedTo != null)
                                rotationTargeted += attachedTo.TurretRotation;
                        }
                        
                        if(cannonDef.autoSnapTargeting)
                        {
                            currentRotation = rotationTargeted;
                        }
                        
                        if(TargetLocked && reloadTicks <= 0 && burstTicks <= 0)
                        {
                            float facing = cannonTarget.Thing != null ? (cannonTarget.Thing.DrawPos - TurretLocation).AngleFlat() : (cannonTarget.Cell - TurretLocation.ToIntVec3()).AngleFlat;
                            GenDraw.DrawAimPieRaw(TurretLocation + new Vector3(cannonRenderOffset.x + aimPieOffset.x, 0.5f, cannonRenderOffset.y + aimPieOffset.y).RotatedBy(TurretRotation), facing, (int)(PrefireTickCount * 0.5f));
                            PrefireTickCount--;
                        }
                    }
                    else if(reloadTicks <= 0 && burstTicks <= 0 && RotationIsValid && !CannonDisabled)
                    {
                        if(targetPersists && (cannonTarget.Pawn is null || !SetTargetConditionalOnThing(LocalTargetInfo.Invalid)))
                        {
                            pawn.GetCachedComp<CompCannons>().multiFireCannon.Add(new SPTuple<int, CannonHandler, SPTuple2<int,int>>(CurrentFireMode.shotsPerBurst, this, new SPTuple2<int,int>(0, 0)));
                            ActivateBurstTimer();
                        }
                    }
                }
            }
        }

        public void StartAnimation(int ticksPerFrame, int cyclesLeft, AnimationWrapperType wrapperType)
        {
            if ((CannonGraphic as Graphic_Animate).AnimationFrameCount == 1)
                return;
            this.ticksPerFrame = ticksPerFrame;
            this.cyclesLeft = cyclesLeft;
            this.wrapperType = wrapperType;
            ticks = 0;
            currentFrame = 0;
            reverseAnimate = false;
        }

        public void DisableAnimation()
        {
            currentFrame = 0;
            cyclesLeft = 0;
            ticksPerFrame = 1;
            ticks = -1;
            wrapperType = AnimationWrapperType.Off;
        }

        public void Draw()
        {
            if(CannonGraphicData.graphicClass == typeof(Graphic_Animate))
            {
                if(ticks >= 0)
                {
                    ticks++;
                    if (ticks > ticksPerFrame)
                    {
                        if(reverseAnimate)
                            currentFrame--;
                        else
                            currentFrame++;

                        ticks = 0;
                    
                        if(currentFrame > ((CannonGraphic as Graphic_Animate).AnimationFrameCount - 1) || currentFrame < 0)
                        {
                            cyclesLeft--;

                            if (wrapperType == AnimationWrapperType.Oscillate)
                                reverseAnimate = !reverseAnimate;

                            currentFrame = reverseAnimate ? (CannonGraphic as Graphic_Animate).AnimationFrameCount - 1 : 0;
                            if(cyclesLeft <= 0)
                            {
                                DisableAnimation();
                            }
                        }
                    }
                }

                HelperMethods.DrawAttachedThing(CannonBaseTexture, CannonBaseGraphic, baseCannonRenderLocation, baseCannonDrawSize, CannonTexture, (CannonGraphic as Graphic_Animate).SubGraphicCycle(currentFrame, CannonGraphic.Shader, pawn.DrawColor, pawn.DrawColorTwo), 
                cannonRenderLocation, cannonRenderOffset, CannonBaseMaterial, (CannonGraphic as Graphic_Animate).SubMaterialCycle(currentFrame), TurretRotation, pawn, drawLayer, attachedTo);
            }
            else
            {
                HelperMethods.DrawAttachedThing(CannonBaseTexture, CannonBaseGraphic, baseCannonRenderLocation, baseCannonDrawSize, CannonTexture, CannonGraphic, 
                cannonRenderLocation, cannonRenderOffset, CannonBaseMaterial, CannonMaterial, TurretRotation, pawn, drawLayer, attachedTo);
            }

            if(GizmoHighlighted || HelperMethods.CannonTargeter.cannon == this)
            {
                //REDO

                if (angleRestricted != Vector2.zero)
                {
                    var drawLinesTask = new Task(() => { HelperMethods.DrawAngleLines(TurretLocation, angleRestricted, MinRange, MaxRange, restrictedTheta, attachedTo?.TurretRotation ?? 0f); });
                    drawLinesTask.RunSynchronously();
                }
                else
                {
                    if (MaxRange > -1)
                    {
                        Vector3 pos = TurretLocation;
                        pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
                        float currentAlpha = 0.65f; // this.GetCurrentAlpha();
                        if (currentAlpha > 0f)
                        {
                            Color value = Color.grey;
                            value.a *= currentAlpha;
                            MatPropertyBlock.SetColor(ShaderPropertyIDs.Color, value);
                            Matrix4x4 matrix = default;
                            matrix.SetTRS(pos, Quaternion.identity, new Vector3(MaxRange * 2f, 1f, MaxRange * 2f));
                            Graphics.DrawMesh(MeshPool.plane10, matrix, HelperMethods.RangeMat((int)MaxRange), 0, null, 0, MatPropertyBlock);
                        }


                    }
                    if (MinRange > 0)
                    {
                        Vector3 pos = TurretLocation;
                        pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
                        float currentAlpha = 0.65f; // this.GetCurrentAlpha();
                        if (currentAlpha > 0f)
                        {
                            Color value = Color.red;
                            value.a *= currentAlpha;
                            MatPropertyBlock.SetColor(ShaderPropertyIDs.Color, value);
                            Matrix4x4 matrix = default;
                            matrix.SetTRS(pos, Quaternion.identity, new Vector3(MinRange * 2f, 1f, MinRange * 2f));
                            Graphics.DrawMesh(MeshPool.plane10, matrix, HelperMethods.RangeMat((int)MinRange), 0, null, 0, MatPropertyBlock);
                        }
                    }
                }
            }
        }

        public int TicksPerShot
        {
            get
            {
                return CurrentFireMode.ticksBetweenShots;
            }
        }

        public float CannonIconAlphaTicked
        {
            get
            {
                if (reloadTicks <= 0)
                    return 0.5f;
                return Mathf.PingPong(reloadTicks, 25) / 100;
            }
        }

        public Material CannonMaterial
        {
            get
            {
                if (cannonMaterialLoaded is null)
                {
                    if(CannonGraphicData.graphicClass == typeof(Graphic_Animate))
                    {
                        cannonMaterialLoaded = MaterialPool.MatFrom(Graphic_Animate.GetDefaultTexPath(CannonGraphicData.texPath), ShaderDatabase.CutoutComplex, CannonGraphicData.color);
                    }
                    else
                    {
                        cannonMaterialLoaded = MaterialPool.MatFrom(CannonGraphicData.texPath, ShaderDatabase.CutoutComplex, CannonGraphicData.color);
                    }
                }
                return cannonMaterialLoaded;
            }
        }

        public Material CannonBaseMaterial
        {
            get
            {
                if (baseCannonMaterialLoaded is null)
                    ResolveCannonGraphics(pawn);
                return baseCannonMaterialLoaded;
            }
        }

        public Texture2D CannonTexture
        {
            get
            {
                if (CannonGraphicData.texPath.NullOrEmpty())
                    return null;
                if (cannonTex is null)
                {
                    if(CannonGraphicData.graphicClass == typeof(Graphic_Animate))
                    {
                        cannonTex = ContentFinder<Texture2D>.Get(Graphic_Animate.GetDefaultTexPath(CannonGraphicData.texPath));
                    }
                    else
                    {
                        cannonTex = ContentFinder<Texture2D>.Get(CannonGraphicData.texPath, true);
                    }
                }
                return cannonTex;
            }
        }

        public Texture2D CannonBaseTexture
        {
            get
            {
                if (cannonDef.baseCannonTexPath.NullOrEmpty())
                    return null;
                if (cannonBaseTex is null)
                    cannonBaseTex = ContentFinder<Texture2D>.Get(cannonDef.baseCannonTexPath, true);
                return cannonBaseTex;
            }
        }

        public Graphic CannonGraphic
        {
            get
            {
                if (cannonGraphic is null)
                    ResolveCannonGraphics(pawn);
                return cannonGraphic;
            }
        }

        public Graphic CannonBaseGraphic
        {
            get
            {
                if (baseCannonGraphic is null)
                    ResolveCannonGraphics(pawn);
                return baseCannonGraphic;
            }
        }

        public GraphicData CannonGraphicData
        {
            get
            {
                if (cachedGraphicData is null)
                    ResolveCannonGraphics(pawn);
                return cachedGraphicData;
            }
        }

        public void ResolveCannonGraphics(VehiclePawn forPawn)
        {
            if(cachedGraphicData is null)
            {
                cachedGraphicData = new GraphicData();
                cachedGraphicData.CopyFrom(cannonDef.graphicData);
                if(cannonDef.matchParentColor)
                {
                    cachedGraphicData.color = forPawn.kindDef.lifeStages.Last().bodyGraphicData.color;
                    cachedGraphicData.colorTwo = forPawn.kindDef.lifeStages.Last().bodyGraphicData.colorTwo;
                }
            }

            if (cannonDef.baseCannonTexPath.NullOrEmpty())
            {
                baseCannonMaterialLoaded = null;
            }
            else if (baseCannonMaterialLoaded is null)
            {
                baseCannonMaterialLoaded = MaterialPool.MatFrom(cannonDef.baseCannonTexPath);
            }

            if (cannonDef.baseCannonTexPath.NullOrEmpty())
            {
                baseCannonGraphic = null;
            }
            else if (baseCannonGraphic is null)
            {
                baseCannonGraphic = GraphicDatabase.Get<Graphic_Single>(cannonDef.baseCannonTexPath, ShaderDatabase.DefaultShader);
            }

            if (cannonGraphic is null)
            {
                if (cannonDef.graphicData is null)
                {
                    cannonGraphic = BaseContent.BadGraphic;
                }
                cannonGraphic = CannonGraphicData.Graphic; //GraphicDatabase.Get<Graphic_Animate>(cannonDef.cannonTexPath, ShaderDatabase.DefaultShader, cannonTurretDrawSize, Color.white);
            }
        }
        
        public Texture2D GizmoIcon
        {
            get
            {
                if (!string.IsNullOrEmpty(cannonDef.gizmoIconTexPath) && gizmoIcon is null)
                {
                    gizmoIcon = ContentFinder<Texture2D>.Get(cannonDef.gizmoIconTexPath);
                }
                else if (gizmoIcon is null)
                {
                    if(CannonTexture != null)
                    {
                        gizmoIcon = CannonTexture;
                    }
                    else
                    {
                        gizmoIcon = TexCommandVehicles.BroadsideCannon_Port;
                    }
                }
                return gizmoIcon;
            }
        }

        public Vector3 TurretLocation
        {
            get
            {
                float locationRotation = 0f;
                if(attachedTo != null)
                {
                    locationRotation = attachedTo.TurretRotation;
                }
                SPTuple2<float, float> turretLoc = HelperMethods.ShipDrawOffset(pawn, cannonRenderLocation.x, cannonRenderLocation.y, out SPTuple2<float,float> renderOffsets, locationRotation, attachedTo);
                return new Vector3(pawn.DrawPos.x + turretLoc.First + renderOffsets.First, pawn.DrawPos.y + drawLayer, pawn.DrawPos.z + turretLoc.Second + renderOffsets.Second);
            }
        }


        public float TurretRotation
        {
            get
            {
                ValidateLockStatus();

                if(currentRotation > 360)
                {
                    currentRotation -= 360;
                }
                else if(currentRotation < 0)
                {
                    currentRotation += 360;
                }

                float rotation = 270 - currentRotation;
                if(rotation < 0)
                    rotation += 360;

                if (attachedTo != null)
                {
                    return rotation + attachedTo.TurretRotation;
                }
                return rotation;
            }
        }

        public bool AngleBetween(Vector3 mousePosition)
        {
            if(angleRestricted == Vector2.zero)
                return true;

            float rotationOffset = attachedTo != null ? attachedTo.TurretRotation : pawn.Rotation.AsInt * 90 + pawn.Angle;

            float start = angleRestricted.x + rotationOffset;
            float end = angleRestricted.y + rotationOffset;

            if (start > 360)
                start -= 360;
            if(end > 360)
                end -= 360;

            float mid = (mousePosition - TurretLocation).AngleFlat();
            end = (end - start) < 0f ? end - start + 360 : end - start;
            mid = (mid - start) < 0f ? mid - start + 360 : mid - start;
            return mid < end;
        }
        
        public void AlignToTargetRestricted()
        {
            if(cannonTarget.HasThing)
            {
                rotationTargeted = (float)TurretLocation.AngleToPoint(cannonTarget.Thing.DrawPos, pawn.Map);
                if (attachedTo != null)
                    rotationTargeted += attachedTo.TurretRotation;
            }
            else
            {
                rotationTargeted = (float)TurretLocation.ToIntVec3().AngleToCell(cannonTarget.Cell, pawn.Map);
                if (attachedTo != null)
                    rotationTargeted += attachedTo.TurretRotation;
            }
        }

        public void AlignToAngleRestricted(float angle)
        {
            float additionalAngle = attachedTo is null ? 0f : attachedTo.TurretRotation;
            if (cannonDef.autoSnapTargeting)
            {
                currentRotation = angle + additionalAngle;
                rotationTargeted = currentRotation;
            }
            else
            {
                rotationTargeted = angle + additionalAngle;
            }
        }

        private int? AngleDirectionalRestricted(float angle)
        {
            if (angleRestricted != Vector2.zero)
            {
                
            }
            return null;
        }

        public void ReloadCannon(ThingDef ammo = null, bool ignoreTimer = false)
        {
            if( (ammo == savedAmmoType || ammo is null) && shellCount == cannonDef.magazineCapacity)
            {
                return;
            }

            if (loadedAmmo is null || (ammo != null && shellCount < cannonDef.magazineCapacity) || shellCount <= 0 || ammo != null)
            {
                try
                {
                    if (pawn.inventory.innerContainer.Contains(savedAmmoType) || pawn.inventory.innerContainer.Contains(ammo))
                    {
                        Thing storedAmmo = null;
                        if (ammo != null)
                        {
                            storedAmmo = pawn.inventory.innerContainer.FirstOrFallback(x => x.def == ammo);
                            savedAmmoType = ammo;
                            TryRemoveShell();
                        }
                        else if (savedAmmoType != null)
                        {
                            storedAmmo = pawn.inventory.innerContainer.FirstOrFallback(x => x.def == savedAmmoType);
                        }
                        else
                        {
                            Log.Error("No saved or specified shell upon reload");
                            return;
                        }
                        int countToTake = storedAmmo.stackCount >= cannonDef.magazineCapacity - shellCount ? cannonDef.magazineCapacity - shellCount : storedAmmo.stackCount;
                        Thing loadedThing = pawn.inventory.innerContainer.Take(storedAmmo, countToTake);
                        int additionalCount = 0;
                        if(countToTake + shellCount < cannonDef.magazineCapacity)
                        {
                            foreach(Thing t in pawn.inventory.innerContainer)
                            {
                                if(t.def == storedAmmo.def)
                                {
                                    additionalCount = t.stackCount >= cannonDef.magazineCapacity - (shellCount + countToTake) ? cannonDef.magazineCapacity - (shellCount + countToTake) : t.stackCount;
                                    Thing additionalItem = pawn.inventory.innerContainer.Take(t, additionalCount);
                                    if (additionalCount + countToTake >= cannonDef.magazineCapacity)
                                        break;
                                }    
                            }
                        }
                        
                        loadedAmmo = loadedThing.def;
                        shellCount = loadedThing.stackCount + additionalCount;
                        if(cannonDef.reloadSound != null)
                            cannonDef.reloadSound.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map, false));
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Unable to reload Cannon: {uniqueID} on Pawn: {pawn.LabelShort}. Exception: {ex.Message}");
                    return;
                }
            }
            else if( (loadedAmmo != null || cannonDef.genericAmmo ) && shellCount > 0)
            {
                ActivateBurstTimer();
                return;
            }
            ActivateTimer(ignoreTimer);
        }

        public void ConsumeShellChambered()
        {
            shellCount--;
            if (shellCount <= 0 && pawn.inventory.innerContainer.FirstOrFallback(x => x.def == loadedAmmo) is null)
            {
                loadedAmmo = null;
                shellCount = 0;
            }
        }

        public void TryRemoveShell()
        {
            if(loadedAmmo != null && shellCount > 0)
            {
                Thing thing = ThingMaker.MakeThing(loadedAmmo);
                thing.stackCount = shellCount;
                pawn.inventory.innerContainer.TryAdd(thing);
                loadedAmmo = null;
                shellCount = 0;
                ActivateTimer(true);
            }
        }

        public void CycleFireMode()
        {
            SoundDefOf.Click.PlayOneShotOnCamera(pawn.Map);
            currentFireMode = fireModes.Next(currentFireMode);
        }

        public void SwitchAutoTarget()
        {
            if(autoTargeting)
            {
                SoundDefOf.Click.PlayOneShotOnCamera(pawn.Map);
                AutoTarget = !AutoTarget;
            }
            else
            {
                Messages.Message("AutoTargetingDisabled".Translate(), MessageTypeDefOf.RejectInput);
            }
        }

        public float MaxRange
        {
            get
            {
                if (cannonDef.maxRange < 0)
                    return 9999;
                return cannonDef.maxRange;
            }
        }

        public float MinRange
        {
            get
            {
                return cannonDef.minRange;
            }
        }

        public int CannonGroup(int cannonNumber)
        {
            if(cannonDef.centerPoints.Count == 0 || cannonDef.cannonsPerPoint.Count == 0 || cannonDef.centerPoints.Count != cannonDef.cannonsPerPoint.Count)
            {
                Log.Error("Error in Cannon Group. CenterPoints is 0, CannonsPerPoint is 0, or CannonsPerPoint and CenterPoints do not have same number of entries");
                return 0;
            }
            return cannonGroupDict[cannonNumber];
        }

        public void SetTarget(LocalTargetInfo target)
        {
            cannonTarget = target;
            TargetLocked = false;
            if (target.Pawn is Pawn)
            {
                if (target.Pawn.Downed)
                    CachedPawnTargetStatus = PawnStatusOnTarget.Down;
                else if (target.Pawn.Dead)
                    CachedPawnTargetStatus = PawnStatusOnTarget.Dead;
                else
                    CachedPawnTargetStatus = PawnStatusOnTarget.Alive;
            }
            else
            {
                CachedPawnTargetStatus = PawnStatusOnTarget.None;
            }

            if(!target.IsValid)
            {
                rotationTargeted = currentRotation;
            }
        }

        /// <summary>
        /// Set target only if cannonTarget is no longer valid or if target is cell based
        /// </summary>
        /// <param name="target"></param>
        /// <returns>true if cannonTarget set to target, false if target is still valid</returns>
        public enum PawnStatusOnTarget { Alive, Down, Dead, None}
        public PawnStatusOnTarget CachedPawnTargetStatus { get; set; }
        public bool SetTargetConditionalOnThing(LocalTargetInfo target, bool resetPrefireTimer = true)
        {
            if(cannonTarget.IsValid && cannonTarget.HasThing || (CurrentFireMode.ticksBetweenBursts == CurrentFireMode.ticksBetweenShots && shellCount > 0))
            {
                if(cannonTarget.Pawn != null)
                {
                    if ( (cannonTarget.Pawn.Dead && CachedPawnTargetStatus != PawnStatusOnTarget.Dead ) || (cannonTarget.Pawn.Downed && CachedPawnTargetStatus != PawnStatusOnTarget.Down) )
                    {
                        SetTarget(target);
                        return true;
                    }
                }
                else if(cannonTarget.Thing != null)
                {
                    if (cannonTarget.Thing.HitPoints > 0)
                    {
                        SetTarget(target);
                        return true;
                    }
                }
                return false;
            }
            SetTarget(target);
            return true;
        }

        public void ResetCannonAngle()
        {
            currentRotation = defaultAngleRotated - 90;
            if (currentRotation < 360)
                currentRotation += 360;
            else if (currentRotation > 360)
                currentRotation -= 360;
            rotationTargeted = currentRotation;
        }

        public void ResetPrefireTimer()
        {
            PrefireTickCount = WarmupTicks;
        }

        private void ValidateLockStatus()
        {
            if(!cannonTarget.IsValid && HelperMethods.CannonTargeter.cannon != this)
            {
                float angleDifference = pawn.Angle - parentAngleCached;
                currentRotation -= 90 * (pawn.Rotation.AsInt - parentRotCached.AsInt) + angleDifference;
                rotationTargeted = currentRotation;
                LockedStatusRotation = true;
            }
            parentRotCached = pawn.Rotation;
            parentAngleCached = pawn.Angle;
        }

        private bool ValidateCannonSetup(CannonHandler reference, out List<string> errors)
        {
            errors = new List<string>();
            try
            {
                if (reference.cannonDef is null)
                {
                    errors.Add("CannonDef is a required field for CannonHandler.");
                }
                if (string.IsNullOrEmpty(reference.key))
                {
                    errors.Add("Key must be included for each CannonHandler");
                }
                else if (pawn.GetCachedComp<CompCannons>().Cannons.AnyNullified(c => c.key == reference.key))
                {
                    errors.Add($"Duplicate cannon key {reference.key}");
                }
                else if (reference.cannonDef.fireModes.NullOrEmpty() || reference.cannonDef.fireModes.Any(f => !f.IsValid))
                {
                    errors.Add($"Invalid FireMode in fireModes list. Must include at least 1 entry and all non-negative numbers.");
                }
                else if(reference.cannonDef.ammoAllowed.NullOrEmpty() && reference.cannonDef.projectile is null)
                {
                    errors.Add($"Must include either an Ammo list or a default projectile.");
                }
                else if(reference.cannonDef.ammoAllowed.AnyNullified() && !reference.cannonDef.genericAmmo && !reference.cannonDef.ammoAllowed.Any(c => c.projectile != null || c.projectileWhenLoaded != null))
                {
                    errors.Add("Non-generic ammo must have either a projectile or a projectileWhenLoaded.");
                }
                else if(reference.cannonDef.genericAmmo && (!reference.cannonDef.ammoAllowed.AnyNullified() || reference.cannonDef.projectile is null))
                {
                    errors.Add("Generic ammo must include a default projectile and populate Ammo-Allowed with the ammo that will reload the cannon");
                }
                else if(reference.cannonDef.genericAmmo && reference.cannonDef.ammoAllowed.AnyNullified() && reference.cannonDef.ammoAllowed.Count != 1)
                {
                    errors.Add("Generic ammo turrets will only use the first ThingDef in the <ammoAllowed/> property. Consider removing all other entries but the first.");
                }
                else if(reference.cannonDef.fireModes.Any(f => f.ticksBetweenShots > f.ticksBetweenBursts))
                {
                    errors.Add("Setting ticksBetweenBursts with a lower tick count than ticksBetweenShots will produce odd shooting behavior. Please set to either the same amount (fully automatic) or more than.");
                }
            }
            catch(Exception ex)
            {
                errors.Add($"Exception thrown during CannonHandler validation. Exception: {ex.Message}\nStackTrace: {ex.StackTrace}\nSource: {ex.Source}");
            }
            
            return errors.NullOrEmpty();
        }

        public string GetUniqueLoadID()
        {
            return "CannonHandlerGroup_" + uniqueID;
        }

        public override string ToString()
        {
            return $"{cannonDef.LabelCap} : {GetUniqueLoadID()}";
        }

        public bool ContainsAmmoDefOrShell(ThingDef def)
        {
            ThingDef projectile = null;
            if(def.projectileWhenLoaded != null)
            {
                projectile = def.projectileWhenLoaded;
            }
            return cannonDef.ammoAllowed.Contains(def) || cannonDef.ammoAllowed.Contains(projectile);
        }

        public FireMode CurrentFireMode
        {
            get
            {
                return currentFireMode;
            }
            set
            {
                if(value != currentFireMode)
                {
                    currentFireMode = value;
                }
            }
        }

        public List<FireMode> fireModes = new List<FireMode>();
        private FireMode currentFireMode;

        public bool AutoTarget
        {
            get
            {
                return autoTargetingActive;
            }
            set
            {
                if (!autoTargeting || value == autoTargetingActive)
                    return;
                autoTargetingActive = value;
            }
        }
        private bool autoTargetingActive;

        private Dictionary<int, int> cannonGroupDict = new Dictionary<int, int>();

        public int reloadTicks;
        public int burstTicks;

        public int uniqueID = -1;
        public int MaxTicks => Mathf.CeilToInt(cannonDef.reloadTimer * 60f);
        public int WarmupTicks => Mathf.CeilToInt(cannonDef.warmUpTimer * 60f);

        private float restrictedTheta;

        private Texture2D cannonTex;
        private Texture2D cannonBaseTex;

        private Texture2D gizmoIcon;

        private Graphic cannonGraphic;
        private Graphic baseCannonGraphic;

        public CannonDef cannonDef;

        public bool targetPersists = true;
        public bool autoTargeting = true;
        public bool manualTargeting = true;

        public bool GizmoHighlighted { get; set; }
        public MaterialPropertyBlock MatPropertyBlock
        {
            get
            {
                if (mtb is null)
                    mtb = new MaterialPropertyBlock();
                return mtb;
            }
            set
            {
                mtb = value;
            }
        }

        private MaterialPropertyBlock mtb;
        
        public CannonHandler attachedTo;
        public List<CannonHandler> childCannons = new List<CannonHandler>();
        public string parentKey;
        public string key;

        private Material cannonMaterialLoaded;
        public Vector2 cannonRenderOffset;
        public Vector2 cannonRenderLocation;

        private Material baseCannonMaterialLoaded;
        public Vector2 baseCannonRenderLocation;

        public Vector2 baseCannonDrawSize = Vector2.one;

        public Vector2 aimPieOffset = Vector2.zero;

        public Vector2 angleRestricted = Vector2.zero;
        public float defaultAngleRotated = 0f;

        public LocalTargetInfo cannonTarget;
        public int drawLayer = 1;

        public int PrefireTickCount { get; private set; }

        public float currentRotation = 0f;
        private float rotationTargeted = 0f;

        public VehiclePawn pawn;

        public ThingDef loadedAmmo;
        public ThingDef savedAmmoType;
        public int shellCount;
        public bool ammoWindowOpened;
        public string gizmoLabel;


        private GraphicData cachedGraphicData;

        public bool LockedStatusRotation { get; set; }

        private Rot4 parentRotCached = default;
        private float parentAngleCached = 0f;

        private const int AutoTargetInterval = 50;

        private int currentFrame = 0;

        private int ticksPerFrame = 1;

        private int ticks;

        private int cyclesLeft = 0;

        private bool reverseAnimate;

        private AnimationWrapperType wrapperType;
    }
}
