using System;
using System.Collections.Generic;
using Verse;
using Verse.Sound;
using RimWorld;
using UnityEngine;
using SPExtended;

namespace Vehicles
{
    public enum WeaponType { None, Static, Rotatable }
    public enum WeaponLocation { Port, Starboard, Bow, Stern, Turret }
    public class CannonHandler : IExposable, ILoadReferenceable
    {
        public CannonHandler()
        {
        }

        public CannonHandler(Pawn pawn, CannonHandler reference)
        {
            this.pawn = pawn;
            uniqueID = Find.UniqueIDsManager.GetNextThingID();
            
            cannonDef = reference.cannonDef;

            cannonRenderOffset = reference.cannonRenderOffset;
            cannonRenderLocation = reference.cannonRenderLocation;
            defaultAngleRotated = reference.defaultAngleRotated;

            aimPieOffset = reference.aimPieOffset;
            angleRestricted = reference.angleRestricted;

            baseCannonRenderLocation = reference.baseCannonRenderLocation;
            cannonTurretDrawSize = reference.cannonTurretDrawSize;

            cannonTurretDrawSize = reference.cannonTurretDrawSize;
            baseCannonDrawSize = reference.baseCannonDrawSize;
            drawLayer = reference.drawLayer;

            attachableKey = reference.attachableKey;
            parentKey = reference.parentKey;

            targetPersists = reference.targetPersists;
            autoTargeting = reference.autoTargeting;
            manualTargeting = reference.manualTargeting;

            (CannonGraphic as Graphic_Animate)?.DisableAnimation();

            LockedStatusRotation = true;
            ResetCannonAngle();

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
                    if (ShipHarmony.debug)
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
        }
        public void ExposeData()
        {
            Scribe_Defs.Look<CannonDef>(ref cannonDef, "cannonDef");
            Scribe_Values.Look(ref uniqueID, "uniqueID", -1);
            Scribe_Values.Look(ref cooldownTicks, "cooldownTicks");
            
            Scribe_Values.Look(ref cannonRenderOffset, "cannonRenderOffset");
            Scribe_Values.Look(ref cannonRenderLocation, "cannonRenderLocation");
            Scribe_Values.Look(ref currentRotation, "currentRotation", defaultAngleRotated - 90);
            Scribe_Values.Look(ref rotationTargeted, "rotationTargeted", defaultAngleRotated - 90);

            Scribe_Values.Look(ref targetPersists, "targetPersists");
            Scribe_Values.Look(ref autoTargeting, "autoTargeting");
            Scribe_Values.Look(ref manualTargeting, "manualTargeting");

            Scribe_Values.Look(ref aimPieOffset, "aimPieOffset");
            Scribe_Values.Look(ref angleRestricted, "angleRestricted");

            Scribe_Values.Look(ref baseCannonRenderLocation, "baseCannonRenderLocation");

            Scribe_Values.Look(ref cannonTurretDrawSize, "cannonTurretDrawSize");
            Scribe_Values.Look(ref baseCannonDrawSize, "baseCannonDrawSize");

            Scribe_Values.Look(ref drawLayer, "drawLayer");

            Scribe_Values.Look(ref parentKey, "parentKey");
            Scribe_Values.Look(ref attachableKey, "attachableKey");

            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Defs.Look(ref loadedAmmo, "loadedAmmo");
            Scribe_Defs.Look(ref savedAmmoType, "savedAmmoType");
            Scribe_Values.Look(ref shellCount, "shellCount");
            Scribe_Values.Look(ref gizmoLabel, "gizmoLabel");

            Scribe_Collections.Look(ref cannonGroupDict, "cannonGroupDict");
            Scribe_TargetInfo.Look(ref cannonTarget, "cannonTarget", LocalTargetInfo.Invalid);
        }

        public bool IsTargetable => cannonDef?.weaponType == WeaponType.Rotatable; //Add to this later

        public bool RotationIsValid => currentRotation == rotationTargeted;

        public bool ActivateTimer()
        {
            if (cooldownTicks > 0)
                return false;
            cooldownTicks = MaxTicks;
            return true;
        }

        public void DoTick()
        {
            if (pawn.GetComp<CompCannons>().multiFireCannon.NullOrEmpty())
            {
                if (autoTargeting && Find.TickManager.TicksGame % AutoTargetInterval == 0 && pawn.Drafted)
                {
                    
                    if(!cannonTarget.IsValid)
                    {
                        LocalTargetInfo autoTarget = this.GetCannonTarget();
                        if(autoTarget.IsValid)
                        {
                            SetTarget(autoTarget);
                            ResetPrefireTimer();
                        }
                    }
                }
                if(cooldownTicks > 0 && RotationIsValid)
                {
                    cooldownTicks--;
                    if(cooldownTicks <= 0)
                    {
                        ReloadCannon();
                    }
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
                            if(Math.Abs(relativeCurrentRotation - relativeTargetedRotation) < 180)
                               currentRotation += cannonDef.rotationSpeed;
                            else currentRotation -= cannonDef.rotationSpeed;
                        }
                        else
                        {
                            if(Math.Abs(relativeCurrentRotation - relativeTargetedRotation) < 180)
                               currentRotation -= cannonDef.rotationSpeed;
                            else currentRotation += cannonDef.rotationSpeed;
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
                if(cannonTarget.Cell.IsValid)
                {
                    if(!CannonTargeter.TargetMeetsRequirements(this, cannonTarget) || !pawn.Drafted)
                    {
                        SetTarget(LocalTargetInfo.Invalid);
                        return;
                    }
                    LockedStatusRotation = false;
                    if(PrefireTickCount > 0)
                    {
                        rotationTargeted = (float)TurretLocation.ToIntVec3().AngleToPoint(cannonTarget.Cell, pawn.Map);
                        if(cannonDef.autoSnapTargeting)
                        {
                            currentRotation = rotationTargeted;
                        }
                        
                        if(RotationIsValid)
                        {
                            float facing = cannonTarget.Thing != null ? (cannonTarget.Thing.DrawPos - TurretLocation).AngleFlat() : (cannonTarget.Cell - TurretLocation.ToIntVec3()).AngleFlat;
                            GenDraw.DrawAimPieRaw(TurretLocation + new Vector3(cannonRenderOffset.x + aimPieOffset.x, 0.5f, cannonRenderOffset.y + aimPieOffset.y).RotatedBy(TurretRotation), facing, (int)(PrefireTickCount * 0.5f));
                            PrefireTickCount--;
                        }
                    }
                    else if(cooldownTicks <= 0 && RotationIsValid)
                    {
                        if(!targetPersists || !SetTargetConditionalOnThing(LocalTargetInfo.Invalid, true))
                        {
                            CompCannon.multiFireCannon.Add(new SPTuple<int, CannonHandler, SPTuple2<int,int>>(cannonDef.numberOfShots, this, new SPTuple2<int,int>(0, 0)));
                            ActivateTimer();
                        }
                    }
                }
            }
        }

        public void Draw()
        {
            if(cannonDef.graphicData.graphicClass == typeof(Graphic_Animate))
            {
                (CannonGraphic as Graphic_Animate).DrawAnimationWorker(this);
            }
            else
            {
                HelperMethods.DrawAttachedThing(CannonBaseTexture, CannonBaseGraphic, baseCannonRenderLocation, baseCannonDrawSize, CannonTexture, CannonGraphic, 
                cannonRenderLocation, cannonRenderOffset, CannonBaseMaterial, CannonMaterial, TurretRotation, pawn, drawLayer, attachedTo);
            }
        }

        public int TicksPerShot
        {
            get
            {
                return cannonDef.baseTicksBetweenShots;
            }
        }

        public float CannonIconAlphaTicked
        {
            get
            {
                if (cooldownTicks <= 0)
                    return 0.5f;
                return Mathf.PingPong(cooldownTicks, 25) / 100;
            }
        }

        public Material CannonMaterial
        {
            get
            {
                if(cannonDef.graphicData.texPath.NullOrEmpty())
                    return null;
                if (cannonMaterialLoaded is null)
                {
                    if(cannonDef.graphicData.graphicClass == typeof(Graphic_Animate))
                    {
                        cannonMaterialLoaded = MaterialPool.MatFrom(Graphic_Animate.GetDefaultTexPath(cannonDef.graphicData.texPath));
                    }
                    else
                    {
                        cannonMaterialLoaded = MaterialPool.MatFrom(cannonDef.graphicData.texPath);
                    }
                }
                return cannonMaterialLoaded;
            }
        }

        public Material CannonBaseMaterial
        {
            get
            {
                if(cannonDef.baseCannonTexPath.NullOrEmpty())
                    return null;
                if (baseCannonMaterialLoaded is null)
                    baseCannonMaterialLoaded = MaterialPool.MatFrom(cannonDef.baseCannonTexPath);
                return baseCannonMaterialLoaded;
            }
        }

        public Texture2D CannonTexture
        {
            get
            {
                if (cannonDef.graphicData.texPath.NullOrEmpty())
                    return null;
                if (cannonTex is null)
                {
                    if(cannonDef.graphicData.graphicClass == typeof(Graphic_Animate))
                    {
                        cannonTex = ContentFinder<Texture2D>.Get(Graphic_Animate.GetDefaultTexPath(cannonDef.graphicData.texPath));
                    }
                    else
                    {
                        cannonTex = ContentFinder<Texture2D>.Get(cannonDef.graphicData.texPath, true);
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
                {
                    if (cannonDef.graphicData is null)
                    {
                        return BaseContent.BadGraphic;
                    }
                    cannonGraphic = cannonDef.graphicData.Graphic; //GraphicDatabase.Get<Graphic_Animate>(cannonDef.cannonTexPath, ShaderDatabase.DefaultShader, cannonTurretDrawSize, Color.white);
                }
                return cannonGraphic;
            }
        }

        public Graphic CannonBaseGraphic
        {
            get
            {
                if (cannonDef.baseCannonTexPath.NullOrEmpty())
                    return null;
                if (baseCannonGraphic is null)
                    baseCannonGraphic = GraphicDatabase.Get<Graphic_Multi>(cannonDef.baseCannonTexPath, ShaderDatabase.DefaultShader);
                return baseCannonGraphic;
            }
        }

        public Texture2D GizmoIcon
        {
            get
            {
                if (!string.IsNullOrEmpty(cannonDef.gizmoIconTexPath) && gizmoIcon is null)
                    gizmoIcon = ContentFinder<Texture2D>.Get(cannonDef.gizmoIconTexPath);
                else if (gizmoIcon is null)
                    gizmoIcon = CannonTexture;

                if (gizmoIcon is null)
                    gizmoIcon = TexCommandVehicles.BroadsideCannon_Port;
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
                SPTuple2<float, float> turretLoc = HelperMethods.ShipDrawOffset(CompVehicle, cannonRenderLocation.x, cannonRenderLocation.y, out SPTuple2<float,float> renderOffsets, locationRotation, attachedTo);
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

                if(LockedStatusRotation && attachedTo != null && !attachedTo.LockedStatusRotation)
                    return rotation + attachedTo.TurretRotation;
                return rotation;
            }
        }

        public bool AngleBetween(Vector3 mousePosition)
        {
            if(angleRestricted == Vector2.zero)
                return true;

            float rotationOffset = pawn.Rotation.AsInt * 90 + pawn.GetComp<CompVehicle>().Angle;
            if (attachedTo != null)
                rotationOffset += attachedTo.TurretRotation;
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

        public void AlignToAngleRestricted(float angle)
        {
            if (cannonDef.autoSnapTargeting)
            {
                currentRotation = angle;
                rotationTargeted = angle;
            }
            else
            {
                rotationTargeted = angle;
            }
        }

        private int? AngleDirectionalRestricted(float angle)
        {
            if (angleRestricted != Vector2.zero)
            {
                
            }
            return null;
        }

        public bool ReloadCannon(ThingDef ammo = null)
        {
            if (loadedAmmo is null || shellCount < cannonDef.magazineCapacity || ammo != null)
            {
                try
                {
                    if(pawn.inventory.innerContainer.Contains(savedAmmoType) || pawn.inventory.innerContainer.Contains(ammo))
                    {
                        Thing storedAmmo = null;
                        if (ammo != null)
                        {
                            storedAmmo = pawn.inventory.innerContainer.FirstOrFallback(x => x.def == ammo);
                            savedAmmoType = ammo;
                            TryRemoveShell();
                        }
                        else if(savedAmmoType != null)
                        {
                            storedAmmo = pawn.inventory.innerContainer.FirstOrFallback(x => x.def == savedAmmoType);
                        }
                        else
                        {
                            throw new NotImplementedException("No saved or specified shell upon reload");
                        }

                        int countToTake = storedAmmo.stackCount >= cannonDef.magazineCapacity - shellCount ? cannonDef.magazineCapacity - shellCount : storedAmmo.stackCount;
                        Thing loadedThing = pawn.inventory.innerContainer.Take(storedAmmo, countToTake);
                        loadedAmmo = loadedThing.def;
                        shellCount = loadedThing.stackCount;
                        SoundDefOf.Artillery_ShellLoaded.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map, false));
                    }    
                }
                catch(Exception ex)
                {
                    Log.Error($"Unable to reload Cannon: {uniqueID} on Pawn: {pawn.LabelShort}. Exception: {ex.Message}");
                    return false;
                }
            } 
            return loadedAmmo != null;
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
        }

        /// <summary>
        /// Set target only if cannonTarget is no longer valid or if target is cell based
        /// </summary>
        /// <param name="target"></param>
        /// <param name="checkOnly"></param>
        /// <returns>true if cannonTarget set to target, false if target is still valid</returns>
        public LocalTargetInfo CachedTarget { get; set; }
        public bool SetTargetConditionalOnThing(LocalTargetInfo target, bool validationOnly = false)
        {
            if(validationOnly && cannonTarget.IsValid)
            {
                return false;
            }

            if(cannonTarget.IsValid && (cannonTarget.HasThing || !cannonTarget.IsValid))
            {
                if(cannonTarget.Thing is Pawn pawn)
                {
                    if (pawn.Dead || pawn.Downed)
                    {
                        cannonTarget = target;
                        return true;
                    }
                }
                else
                {
                    if (cannonTarget.Thing.HitPoints > 0)
                    {
                        cannonTarget = target;
                        return true;
                    }
                }
                ResetPrefireTimer();
                return false;
            }
            cannonTarget = target;
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
            if(parentRotCached != pawn.Rotation || parentAngleCached != CompVehicle.Angle)
            {
                if(!cannonTarget.IsValid && HelperMethods.CannonTargeter.cannon != this)
                {
                    float angleDifference = CompVehicle.Angle - parentAngleCached;
                    currentRotation -= 90 * (pawn.Rotation.AsInt - parentRotCached.AsInt) + angleDifference;
                    rotationTargeted = currentRotation;
                    LockedStatusRotation = true;
                }
                parentRotCached = pawn.Rotation;
                parentAngleCached = CompVehicle.Angle;
            }
        }

        public string GetUniqueLoadID()
        {
            return "CannonHandlerGroup_" + uniqueID;
        }

        private Dictionary<int, int> cannonGroupDict = new Dictionary<int, int>();

        public int cooldownTicks;

        public int uniqueID = -1;
        public int MaxTicks => Mathf.CeilToInt(cannonDef.cooldownTimer * 60f);
        public int WarmupTicks => Mathf.CeilToInt(cannonDef.warmUpTimer * 60f);

        private Texture2D cannonTex;
        private Texture2D cannonBaseTex;

        private Texture2D gizmoIcon;

        private Graphic cannonGraphic;
        private Graphic baseCannonGraphic;

        public CannonDef cannonDef;

        public bool targetPersists;
        public bool autoTargeting = false;
        public bool manualTargeting = true;

        /* Optional */
        public CannonHandler attachedTo;
        public string parentKey;
        public string attachableKey;

        private Material cannonMaterialLoaded;
        public Vector2 cannonRenderOffset;
        public Vector2 cannonRenderLocation;

        private Material baseCannonMaterialLoaded;
        public Vector2 baseCannonRenderLocation;

        public Vector2 cannonTurretDrawSize = Vector2.one;
        public Vector2 baseCannonDrawSize = Vector2.one;

        public Vector2 aimPieOffset = Vector2.zero;

        public Vector2 angleRestricted = Vector2.zero;
        public float defaultAngleRotated = 0f;

        public LocalTargetInfo cannonTarget;
        public int drawLayer = 1;

        public int PrefireTickCount { get; private set; }

        public float currentRotation = 0f;
        private float rotationTargeted = 0f;

        public Pawn pawn;

        public ThingDef loadedAmmo;
        public ThingDef savedAmmoType;
        public int shellCount;
        public bool ammoWindowOpened;
        public string gizmoLabel;

        private CompVehicle CompVehicle => pawn.TryGetComp<CompVehicle>();
        private CompCannons CompCannon => pawn.TryGetComp<CompCannons>();

        public bool LockedStatusRotation { get; set; }

        private Rot4 parentRotCached = default;
        private float parentAngleCached = 0f;

        private const int AutoTargetInterval = 50;
    }
}
