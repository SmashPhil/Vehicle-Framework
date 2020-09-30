using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
    [StaticConstructorOnStartup]
	public class Projectile_CustomFlags : Projectile
	{
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Defs.Look(ref hitFlag, "hitFlag");
		}

		public override Vector3 ExactPosition
		{
			get
			{
				Vector3 b = (destination - origin).Yto0() * DistanceCoveredCustom;
				return origin.Yto0() + b + Vector3.up * def.Altitude;
			}
		}

		protected float DistanceCoveredCustom
		{
			get
			{
				return 1f - ticksToImpact / StartingTicksToImpact;
			}
		}

		public override void Tick()
		{
			if (landed)
			{
				return;
			}
			Vector3 exactPosition = ExactPosition;
			ticksToImpact--;
			if (!ExactPosition.InBounds(Map))
			{
				ticksToImpact++;
				Position = ExactPosition.ToIntVec3();
				Destroy(DestroyMode.Vanish);
				return;
			}
			Vector3 exactPosition2 = ExactPosition;
			if (CheckForFreeInterceptBetween(exactPosition, exactPosition2))
			{
				return;
			}
			Position = ExactPosition.ToIntVec3();
			if (ticksToImpact == 60 && Find.TickManager.CurTimeSpeed == TimeSpeed.Normal && def.projectile.soundImpactAnticipate != null)
			{
				def.projectile.soundImpactAnticipate.PlayOneShot(this);
			}
			if (ticksToImpact <= 0)
			{
				if (hitFlag.flyPastTarget)
                {
					var things = Position.GetThingList(Map);
					foreach(Thing t in things)
                    {
						if (CanHit(t))
                        {
							ImpactSomething();
							break;
                        }
                    }
					flewPast = true;
					return;
                }
				if (DestinationCell.InBounds(Map))
				{
					Position = DestinationCell;
				}
				ImpactSomething();
				return;
			}
			if (ambientSustainer != null)
			{
				ambientSustainer.Maintain();
			}
		}

		public override void Draw()
		{
			float num = ArcHeightFactor * GenMath.InverseParabola(DistanceCoveredCustom);
			Vector3 drawPos = DrawPos;
			Vector3 position = drawPos + new Vector3(0f, 0f, 1f) * num;
			if (def.projectile.shadowSize > 0f)
			{
				DrawShadow(drawPos, num);
			}
			Graphics.DrawMesh(MeshPool.GridPlane(def.graphicData.drawSize), position, ExactRotation, def.DrawMatSingle, 0);
			Comps_PostDraw();
		}

		private void DrawShadow(Vector3 drawLoc, float height)
		{
			if (shadowMaterial == null)
			{
				return;
			}
			float num = def.projectile.shadowSize * Mathf.Lerp(1f, 0.6f, height);
			Vector3 s = new Vector3(num, 1f, num);
			Vector3 b = new Vector3(0f, -0.01f, 0f);
			Matrix4x4 matrix = default(Matrix4x4);
			matrix.SetTRS(drawLoc + b, Quaternion.identity, s);
			Graphics.DrawMesh(MeshPool.plane10, matrix, shadowMaterial, 0);
		}


		protected bool CheckForFreeInterceptBetween(Vector3 lastExactPos, Vector3 newExactPos)
		{
			if (lastExactPos == newExactPos)
			{
				return false;
			}
			List<Thing> list = Map.listerThings.ThingsInGroup(ThingRequestGroup.ProjectileInterceptor);
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].TryGetComp<CompProjectileInterceptor>().CheckIntercept(this, lastExactPos, newExactPos))
				{
					Destroy(DestroyMode.Vanish);
					return true;
				}
			}
			IntVec3 intVec = lastExactPos.ToIntVec3();
			IntVec3 intVec2 = newExactPos.ToIntVec3();
			if (intVec2 == intVec)
			{
				return false;
			}
			if (!intVec.InBounds(Map) || !intVec2.InBounds(Map))
			{
				return false;
			}
			if (intVec2.AdjacentToCardinal(intVec))
			{
				return CheckForFreeIntercept(intVec2);
			}
			if (VerbUtility.InterceptChanceFactorFromDistance(origin, intVec2) <= 0f)
			{
				return false;
			}
			Vector3 vector = lastExactPos;
			Vector3 v = newExactPos - lastExactPos;
			Vector3 b = v.normalized * 0.2f;
			int num = (int)(v.MagnitudeHorizontal() / 0.2f);
			checkedCells.Clear();
			int num2 = 0;
			for (;;)
			{
				vector += b;
				IntVec3 intVec3 = vector.ToIntVec3();
				if (!checkedCells.Contains(intVec3))
				{
					if (CheckForFreeIntercept(intVec3))
					{
						break;
					}
					checkedCells.Add(intVec3);
				}
				num2++;
				if (num2 > num)
				{
					return false;
				}
				if (intVec3 == intVec2)
				{
					return false;
				}
			}
			return true;
		}

		protected bool CheckForFreeIntercept(IntVec3 c)
		{
			if (destination.ToIntVec3() == c)
			{
				return false;
			}

			if (!def.projectile.flyOverhead)
            {
				RoofDef roofDef = Map.roofGrid.RoofAt(Position);
				if (roofDef != null && roofDef.isThickRoof)
				{
					ThrowDebugText("hit-thick-roof", Position);
					def.projectile.soundHitThickRoof.PlayOneShot(new TargetInfo(Position, Map, false));
					Destroy(DestroyMode.Vanish);
					return true;
				}
            }

			float num = VerbUtility.InterceptChanceFactorFromDistance(origin, c);
			if (num <= 0f)
			{
				return false;
			}
			bool flag = false;
			List<Thing> thingList = c.GetThingList(Map);
			for (int i = 0; i < thingList.Count; i++)
			{
				Thing thing = thingList[i];
				
				if (CanHit(thing))
				{
					bool flag2 = false;
					if (thing.def.Fillage == FillCategory.Full)
					{
						Building_Door building_Door = thing as Building_Door;
						if (building_Door == null || !building_Door.Open)
						{
							ThrowDebugText("int-wall", c);
							Impact(thing);
							return true;
						}
						flag2 = true;
					}
					float num2 = 0f;
					Pawn pawn = thing as Pawn;
					if (pawn != null)
					{
						num2 = 0.4f * Mathf.Clamp(pawn.BodySize, 0.1f, 2f);
						if (pawn.GetPosture() != PawnPosture.Standing)
						{
							num2 *= 0.1f;
						}
						if (launcher != null && pawn.Faction != null && launcher.Faction != null && !pawn.Faction.HostileTo(launcher.Faction))
						{
							num2 *= Find.Storyteller.difficultyValues.friendlyFireChanceFactor;
						}
					}
					else if (thing.def.fillPercent > 0.2f)
					{
						if (flag2)
						{
							num2 = 0.05f;
						}
						else if (DestinationCell.AdjacentTo8Way(c))
						{
							num2 = thing.def.fillPercent * 1f;
						}
						else
						{
							num2 = thing.def.fillPercent * 0.15f;
						}
					}
					num2 *= num;
					if (num2 > 1E-05f)
					{
						if (Rand.Chance(num2))
						{
							ThrowDebugText("int-" + num2.ToStringPercent(), c);
							Impact(thing);
							return true;
						}
						flag = true;
						ThrowDebugText(num2.ToStringPercent(), c);
					}
				}
			}
			if (!flag)
			{
				ThrowDebugText("o", c);
			}
			return false;
		}

		private void ThrowDebugText(string text, IntVec3 c)
		{
			if (DebugViewSettings.drawShooting)
			{
				MoteMaker.ThrowText(c.ToVector3Shifted(), Map, text, -1f);
			}
		}

		private float ArcHeightFactor
		{
			get
			{
				float num = def.projectile.arcHeightFactor;
				float num2 = (destination - origin).MagnitudeHorizontalSquared();
				if (num * num > num2 * 0.2f * 0.2f)
				{
					num = Mathf.Sqrt(num2) * 0.2f;
				}
				return num;
			}
		}

		protected new bool CanHit(Thing thing)
		{
			if (!thing.Spawned)
			{
				return false;
			}
			if (thing == launcher)
			{
				return false;
			}
			
			bool flag = false;
			foreach (IntVec3 c in thing.OccupiedRect())
			{
				List<Thing> thingList = c.GetThingList(Map);
				bool flag2 = false;
				for (int i = 0; i < thingList.Count; i++)
				{
					if (thingList[i] != thing && thingList[i].def.fillPercent >= hitFlag.minFillPercent && thingList[i].def.Altitude >= thing.def.Altitude)
					{
						flag2 = true;
						break;
					}
				}
				if (!flag2)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}

			ProjectileHitFlags hitFlags = HitFlags;
			if (thing == intendedTarget && (hitFlags & ProjectileHitFlags.IntendedTarget) != ProjectileHitFlags.None)
			{
				return true;
			}
			if (thing != intendedTarget)
			{
				if (thing is Pawn pawn)
				{
					if ((hitFlags & ProjectileHitFlags.NonTargetPawns) != ProjectileHitFlags.None)
					{
						return true;
					}
					if (hitFlag.hitThroughPawns && !pawn.Dead && !pawn.Downed)
                    {
						thing.TakeDamage(new DamageInfo(DamageDefOf.Blunt, def.projectile.speed * 2, 0, 0, this));
                    }
				}
				else if ((hitFlags & ProjectileHitFlags.NonTargetWorld) != ProjectileHitFlags.None)
				{
					return true;
				}
			}
			if (flewPast || hitFlag.minFillPercent > 0)
            {
				return thing.def.fillPercent >= hitFlag.minFillPercent;
            }
			return thing == intendedTarget && thing.def.fillPercent >= hitFlag.minFillPercent;
		}

		private void ImpactSomething()
		{
			if (def.projectile.flyOverhead)
			{
				RoofDef roofDef = Map.roofGrid.RoofAt(Position);
				if (roofDef != null)
				{
					if (roofDef.isThickRoof)
					{
						ThrowDebugText("hit-thick-roof", Position);
						def.projectile.soundHitThickRoof.PlayOneShot(new TargetInfo(Position, Map, false));
						Destroy(DestroyMode.Vanish);
						return;
					}
					if (Position.GetEdifice(Map) == null || Position.GetEdifice(Map).def.Fillage != FillCategory.Full)
					{
						RoofCollapserImmediate.DropRoofInCells(Position, Map, null);
					}
				}
			}
			if (!usedTarget.HasThing || !CanHit(usedTarget.Thing))
			{
				cellThingsFiltered.Clear();
				List<Thing> thingList = Position.GetThingList(Map);
				for (int i = 0; i < thingList.Count; i++)
				{
					Thing thing = thingList[i];
					if ((thing.def.category == ThingCategory.Building || thing.def.category == ThingCategory.Pawn || thing.def.category == ThingCategory.Item || thing.def.category == ThingCategory.Plant) && this.CanHit(thing))
					{
						cellThingsFiltered.Add(thing);
					}
				}
				cellThingsFiltered.Shuffle<Thing>();
				for (int j = 0; j < cellThingsFiltered.Count; j++)
				{
					Thing thing2 = cellThingsFiltered[j];
					Pawn pawn = thing2 as Pawn;
					float num;
					if (pawn != null)
					{
						num = 0.5f * Mathf.Clamp(pawn.BodySize, 0.1f, 2f);
						if (pawn.GetPosture() != PawnPosture.Standing && (origin - destination).MagnitudeHorizontalSquared() >= 20.25f)
						{
							num *= 0.2f;
						}
						if (launcher != null && pawn.Faction != null && launcher.Faction != null && !pawn.Faction.HostileTo(launcher.Faction))
						{
							num *= VerbUtility.InterceptChanceFactorFromDistance(origin, Position);
						}
					}
					else
					{
						num = 1.5f * thing2.def.fillPercent;
					}
					if (Rand.Chance(num))
					{
						ThrowDebugText("hit-" + num.ToStringPercent(), Position);
						Impact(cellThingsFiltered.RandomElement<Thing>());
						return;
					}
					ThrowDebugText("miss-" + num.ToStringPercent(), Position);
				}
				Impact(null);
				return;
			}
			Pawn pawn2 = usedTarget.Thing as Pawn;
			if (pawn2 != null && pawn2.GetPosture() != PawnPosture.Standing && (origin - destination).MagnitudeHorizontalSquared() >= 20.25f && !Rand.Chance(0.2f))
			{
				ThrowDebugText("miss-laying", Position);
				Impact(null);
				return;
			}
			Impact(usedTarget.Thing);
		}

		protected override void Impact(Thing hitThing)
		{
			GenClamor.DoClamor(this, 2.1f, ClamorDefOf.Impact);
			Destroy(DestroyMode.Vanish);
		}

		protected bool flewPast;
		public CustomHitFlags hitFlag;
		private static readonly Material shadowMaterial = MaterialPool.MatFrom("Things/Skyfaller/SkyfallerShadowCircle", ShaderDatabase.Transparent);
		private Sustainer ambientSustainer;

		private static List<IntVec3> checkedCells = new List<IntVec3>();
		private static readonly List<Thing> cellThingsFiltered = new List<Thing>();
	}
}
