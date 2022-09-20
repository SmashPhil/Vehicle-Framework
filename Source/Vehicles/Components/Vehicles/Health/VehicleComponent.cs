using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class VehicleComponent : IExposable
	{
		public VehiclePawn vehicle;
		public static Gradient gradient;

		public VehicleComponentProperties props;
		public float health;

		public IndicatorDef indicator;
		public Color highlightColor = Color.white;

		public VehicleComponent(VehiclePawn vehicle)
		{
			this.vehicle = vehicle;
		}

		public float HealthPercent => Ext_Math.RoundTo(health / props.health, 0.01f);

		public float Efficiency => props.efficiency.Evaluate(HealthPercent); //Allow evaluating beyond 100% via stat parts

		public Color EfficiencyColor => gradient.Evaluate(Efficiency);

		public virtual void DrawIcon(Rect rect)
		{
			if (indicator != null)
			{
				Widgets.DrawTextureFitted(rect, indicator.Icon, 1);
				TooltipHandler.TipRegion(rect, indicator.label);
			}
		}

		public void TakeDamage(VehiclePawn vehicle, DamageInfo dinfo, IntVec3 cell)
		{
			TakeDamage(vehicle, ref dinfo, cell);
		}

		public virtual void TakeDamage(VehiclePawn vehicle, ref DamageInfo dinfo, IntVec3 cell)
		{
			ReduceDamageFromArmor(ref dinfo, out bool penetrated);
			if (cell.IsValid && (dinfo.Amount <= 0 || !penetrated))
			{
				vehicle.Drawer.Notify_DamageDeflected(cell);
			}
			if (!props.reactors.NullOrEmpty())
			{
				foreach (Reactor reactor in props.reactors)
				{
					reactor.Hit(vehicle, this, ref dinfo, penetrated);
				}
			}
			health -= dinfo.Amount;
			float remainingDamage = -health;
			health = health.Clamp(0, props.health);
			if (!penetrated || health > (props.health / 2f))
			{
				dinfo.SetAmount(0);
			}
			else if (remainingDamage > 0 && remainingDamage >= dinfo.Amount / 2)
			{
				dinfo.SetAmount(remainingDamage);
			}
		}

		public virtual void HealComponent(float amount)
		{
			health += amount;
			if (health > props.health)
			{
				health = props.health;
			}
		}

		public virtual void ReduceDamageFromArmor(ref DamageInfo dinfo, out bool penetrated)
		{
			penetrated = false;
			if (dinfo.Def.armorCategory == null)
			{
				return;
			}
			StatDef damageType = dinfo.Def.armorCategory.armorRatingStat;
			float armorRating = ArmorRating(damageType);
			float armorDiff = armorRating - dinfo.ArmorPenetrationInt;
			penetrated = props.hitbox.fallthrough;
			float chance = Rand.Value;
			if (chance < (armorDiff / 2))
			{
				dinfo.SetAmount(0);
				penetrated = false;
			}
			else
			{
				if (chance < armorDiff)
				{
					float damage = GenMath.RoundRandom(dinfo.Amount / 2);
					dinfo.SetAmount(damage);
					penetrated = false;
					if (dinfo.Def.armorCategory == DamageArmorCategoryDefOf.Sharp)
					{
						dinfo.Def = DamageDefOf.Blunt;
					}
				}
			}
			Debug.Message($"Damaging: {props.label}\nArmor: {armorRating}\nPenetration: {dinfo.ArmorPenetrationInt}\nDamage: {dinfo.Amount}\nFallthrough: {penetrated}");
		}

		public float ArmorRating(StatDef statDef)
		{
			if (statDef is null)
			{
				float rating = 0;
				int count = 0;
				foreach (StatDef armorRatingDef in DefDatabase<DamageArmorCategoryDef>.AllDefs.Select(armorCategory => armorCategory.armorRatingStat))
				{
					if (armorRatingDef != null)
					{
						StatModifier sumArmorModifier = props.armor?.FirstOrDefault(rating => rating.stat == armorRatingDef);
						rating += sumArmorModifier?.value ?? vehicle.GetStatValue(armorRatingDef);
						count++;
					}
				}
				return rating / count;
			}
			StatModifier armorModifier = props.armor?.FirstOrDefault(rating => rating.stat == statDef);
			return armorModifier?.value ?? vehicle.GetStatValue(statDef);
		}

		public virtual void PostCreate()
		{
			health = props.health;
		}

		public virtual void Initialize(VehicleComponentProperties props)
		{
			this.props = props;
			indicator = props.reactors?.FirstOrDefault(reactor => reactor.indicator != null)?.indicator;
			highlightColor = props.reactors?.FirstOrDefault()?.highlightColor ?? Color.white;
			gradient = new Gradient()
			{
				colorKeys = new[] { new GradientColorKey(Color.gray, props.efficiency[0].x),
									new GradientColorKey(TexData.RedReadable, props.efficiency[1].x),
									new GradientColorKey(TexData.SevereDamage, props.efficiency[2].x),
									new GradientColorKey(TexData.ModerateDamage, props.efficiency[3].x),
									new GradientColorKey(TexData.MinorDamage, props.efficiency[4].x),
									new GradientColorKey(TexData.WorkingCondition, props.efficiency[5].x),
									new GradientColorKey(TexData.Enhanced, props.efficiency[5].x + 0.01f) //greater than 101% max efficiency
				}
			};
		}

		public virtual void ExposeData()
		{
			Scribe_Values.Look(ref health, nameof(health));
			Scribe_References.Look(ref vehicle, nameof(vehicle));
		}

		///Yes I'm aware this is very similar to BodyPartDepth, creating this for clarity that this is an entirely different set of mechanics for similar purpose
		public enum VehiclePartDepth
		{ 
			Undefined,
			External,
			Internal
		}
	}
}
