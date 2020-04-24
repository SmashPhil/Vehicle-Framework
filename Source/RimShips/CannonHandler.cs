using System;
using System.Collections.Generic;
using Verse;
using UnityEngine;
using SPExtended;

namespace RimShips
{
    public enum WeaponType { None, Broadside, Rotatable }
    public enum WeaponLocation { Port, Starboard, Turret }
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
            Log.Message("ID: " + uniqueID);

            cannonSize = reference.cannonSize;
            cannonRenderOffset = reference.cannonRenderOffset;
            cannonRenderLocation = reference.cannonRenderLocation;

            aimPieOffset = reference.aimPieOffset;
            angleRestricted = reference.angleRestricted;

            baseCannonSize = reference.baseCannonSize;
            baseCannonRenderLocation = reference.baseCannonRenderLocation;
            cannonTurretDrawSize = reference.cannonTurretDrawSize;

            cannonTurretDrawSize = reference.cannonTurretDrawSize;
            baseCannonDrawSize = reference.baseCannonDrawSize;

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
            
            Scribe_Values.Look(ref cannonSize, "cannonSize");
            Scribe_Values.Look(ref cannonRenderOffset, "cannonRenderOffset");
            Scribe_Values.Look(ref cannonRenderLocation, "cannonRenderLocation");
            Scribe_Values.Look(ref currentRotation, "currentRotation", defaultAngleRotated - 90);

            Scribe_Values.Look(ref aimPieOffset, "aimPieOffset");
            Scribe_Values.Look(ref angleRestricted, "angleRestricted");

            Scribe_Values.Look(ref baseCannonSize, "baseCannonSize");
            Scribe_Values.Look(ref baseCannonRenderLocation, "baseCannonRenderLocation");

            Scribe_Values.Look(ref cannonTurretDrawSize, "cannonTurretDrawSize");
            Scribe_Values.Look(ref baseCannonDrawSize, "baseCannonDrawSize");

            Scribe_References.Look(ref pawn, "pawn");

            Scribe_Collections.Look(ref cannonGroupDict, "cannonGroupDict");
            Scribe_TargetInfo.Look(ref cannonTarget, "cannonTarget", LocalTargetInfo.Invalid);
        }

        public bool IsTargetable => cannonDef?.weaponType == WeaponType.Rotatable; //Add to this later

        public bool ActivateTimer()
        {
            if (cooldownTicks > 0)
                return false;
            cooldownTicks = MaxTicks;
            return true;
        }

        public void DoTick()
        {
            Log.Message("PAWN: " + this.pawn?.thingIDNumber);
            if (cooldownTicks > 0)
            {
                cooldownTicks--;
            }
            if(IsTargetable)
            {
                if(cannonTarget.Cell.IsValid)
                {
                    LockedStatusRotation = false;
                    if(PrefireTickCount > 0)
                    {
                        if(!CannonTargeter.TargetMeetsRequirements(this, cannonTarget) || !pawn.Drafted)
                        {
                            SetTarget(LocalTargetInfo.Invalid);
                            return;
                        }
                        currentRotation = (float)TurretLocation.ToIntVec3().AngleToPoint(cannonTarget.Cell, pawn.Map);

                        float facing = cannonTarget.Thing != null ? (cannonTarget.Thing.DrawPos - TurretLocation).AngleFlat() : (cannonTarget.Cell - TurretLocation.ToIntVec3()).AngleFlat;
                        GenDraw.DrawAimPieRaw(TurretLocation + new Vector3(cannonRenderOffset.x + aimPieOffset.x, 0.5f, cannonRenderOffset.y + aimPieOffset.y).RotatedBy(TurretRotation), facing, (int)(PrefireTickCount * 0.5f));

                        PrefireTickCount--;
                    }
                    else if(cooldownTicks <= 0)
                    {
                        CompCannon.multiFireCannon.Add(new SPTuples.SPTuple<int, CannonHandler, int>(cannonDef.numberOfShots, this, 0));
                        ActivateTimer();
                    }
                }
            }
        }


        public int TicksPerShot
        {
            get
            {
                return cannonDef.baseTicksBetweenShots;
            }
        }

        public Material CannonMaterial
        {
            get
            {
                if(cannonDef.cannonTexPath.NullOrEmpty())
                    return null;
                if (cannonMaterialLoaded is null)
                    cannonMaterialLoaded = MaterialPool.MatFrom(cannonDef.cannonTexPath);
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
                if (cannonDef.cannonTexPath.NullOrEmpty())
                    return null;
                if (cannonTex is null)
                    cannonTex = ContentFinder<Texture2D>.Get(cannonDef.cannonTexPath, true);
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

        public Vector3 TurretLocation
        {
            get
            {
                Pair<float, float> turretLoc = HelperMethods.ShipDrawOffset(CompShip, cannonRenderLocation.x, cannonRenderLocation.y);
                return new Vector3(pawn.DrawPos.x + turretLoc.First, pawn.DrawPos.y, pawn.DrawPos.z + turretLoc.Second);
            }
        }

        public Vector3 TurretLocationRotated
        {
            get
            {
                Pair<float, float> turretLoc = HelperMethods.ShipDrawOffset(CompShip, cannonRenderLocation.x + cannonRenderOffset.x, cannonRenderLocation.y + cannonRenderOffset.y);
                return new Vector3(pawn.DrawPos.x + turretLoc.First, pawn.DrawPos.y, pawn.DrawPos.z + turretLoc.Second).RotatedBy(currentRotation);
            }
        }

        public float TurretRotation
        {
            get
            {
                float trueRotation = currentRotation;
                if(!cannonTarget.IsValid && HelperMethods.CannonTargeter.cannon != this && LockedStatusRotation)
                {
                    trueRotation -= 90 * pawn.Rotation.AsInt + CompShip.Angle;
                }
                float rotation = 270 - trueRotation;
                if(rotation < 0)
                {
                    return 360 + rotation;
                }
                return rotation;
            }
        }

        public bool AngleBetween(Vector3 mousePosition)
        {
            if(angleRestricted == Vector2.zero)
                return true;
            float rotationOffset = pawn.Rotation.AsInt * 90;
            float start = angleRestricted.x + rotationOffset;
            float end = angleRestricted.y + rotationOffset;
            if (start > 360)
                start -= 360;
            if(end > 360)
                end -= 360;
            Log.Message($"RotationOffset: {rotationOffset} Start: {start} End: {end}");
            float mid = (mousePosition - TurretLocation).AngleFlat();
            end = (end - start) < 0f ? end - start + 360 : end - start;
            mid = (mid - start) < 0f ? mid - start + 360 : mid - start;
            return mid < end;
        }

        public float MaxRange
        {
            get
            {
                if(cannonDef.maxRange > GenRadial.MaxRadialPatternRadius)
                {
                    return (float)Math.Floor(GenRadial.MaxRadialPatternRadius);
                }
                return cannonDef.maxRange;
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

        public void ResetCannonAngle()
        {
            currentRotation = defaultAngleRotated - 90;
        }

        public void ResetPrefireTimer()
        {
            PrefireTickCount = WarmupTicks;
        }

        public void ValidateLockStatus()
        {
            if (parentRotCached != pawn.Rotation)
            {
                parentRotCached = pawn.Rotation;
                if(!cannonTarget.IsValid && HelperMethods.CannonTargeter.cannon != this)
                {
                    LockedStatusRotation = true;
                }
            }
        }

        public string GetUniqueLoadID()
        {
            if(uniqueID < 0)
            {
                Log.Message("Finding New ID : " + cannonDef?.label);
                uniqueID = Find.UniqueIDsManager.GetNextThingID();
            }
            return "CannonHandlerGroup_" + uniqueID;
        }

        private Dictionary<int, int> cannonGroupDict = new Dictionary<int, int>();

        public int cooldownTicks;

        public int uniqueID = -1;
        public int MaxTicks => Mathf.CeilToInt(cannonDef.cooldownTimer * 60f);
        public int WarmupTicks => Mathf.CeilToInt(cannonDef.warmUpTimer * 60f);

        private Texture2D cannonTex;
        private Texture2D cannonBaseTex;

        public CannonDef cannonDef;

        private Material cannonMaterialLoaded;
        public Vector2 cannonSize;
        public Vector2 cannonRenderOffset;
        public Vector2 cannonRenderLocation;

        private Material baseCannonMaterialLoaded;
        public Vector2 baseCannonSize;
        public Vector2 baseCannonRenderLocation;

        public Vector2 cannonTurretDrawSize = Vector2.one;
        public Vector2 baseCannonDrawSize = Vector2.one;

        public Vector2 aimPieOffset = Vector2.zero;

        public Vector2 angleRestricted = Vector2.zero;
        public float defaultAngleRotated = 0f;

        public LocalTargetInfo cannonTarget;
        public int drawLayer = 0;

        public int PrefireTickCount { get; private set; }

        public float currentRotation = 0f;

        public Pawn pawn;

        private CompShips CompShip => pawn.TryGetComp<CompShips>();
        private CompCannons CompCannon => pawn.TryGetComp<CompCannons>();

        public bool LockedStatusRotation { get; private set; }
        private Rot4 parentRotCached = default;
    }
}
