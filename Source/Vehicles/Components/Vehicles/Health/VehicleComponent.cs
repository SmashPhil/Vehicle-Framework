using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class VehicleComponent : IExposable
	{
		public static Gradient gradient;

		public VehicleComponentProperties props;
		public float health;

		public VehicleComponent()
		{
		}

		public float HealthPercent => Ext_Math.RoundTo(health / props.health, 0.01f);

		public float Efficiency => props.efficiency.Evaluate(HealthPercent); //Allow evaluating beyond 100% via stat parts

		public float ArmorRating => Ext_Math.RoundTo(props.armor, 0.1f);

		public Color EfficiencyColor => gradient.Evaluate(Efficiency);

		public virtual bool ComponentIndicator => !props.explosionProperties.Empty;

		public virtual void DrawIcon(Rect rect)
		{
			Widgets.DrawTextureFitted(rect, VehicleTex.WarningIcon, 1);
			TooltipHandler.TipRegionByKey(rect, "VF_ExplosiveComponent");
		}

		public virtual float TakeDamage(VehiclePawn vehicle, DamageInfo dinfo, IntVec3 cell)
		{
			float damage = dinfo.Amount;
			ReduceDamageFromArmor(ref damage, dinfo.ArmorPenetrationInt, out bool penetrated);
			if (damage <= 0 || !penetrated)
			{
				vehicle.Drawer.Notify_DamageDeflected(cell);
			}
			health -= damage;
			float remainingDamage = -health;
			health = health.Clamp(0, props.health);
			if (!penetrated)
			{
				return 0;
			}
			if (remainingDamage > 0 && remainingDamage >= damage / 2)
			{
				return remainingDamage;
			}
			return health > (props.health / 2) ? 0 : damage / 2;
		}

		public virtual void HealComponent(float amount)
		{
			health += amount;
			if (health > props.health)
			{
				health = props.health;
			}
		}

		public virtual void ReduceDamageFromArmor(ref float damage, float armorPenetration, out bool penetrate)
		{
			float armorRating = props.armor - armorPenetration; 
			penetrate = props.hitbox.fallthrough;
			if (damage < (armorRating / 2))
			{
				damage = 0;
				penetrate = false;
			}
			else
			{
				if (damage < armorRating)
				{
					damage = Mathf.Lerp(damage / 2, damage, armorPenetration / props.armor);
					penetrate = false;
				}
			}
			if (damage < 1)
			{
				damage = 0;
			}
			Debug.Message($"Damaging: {props.label}\nArmor: {props.armor}\nPenetration: {armorPenetration}\nDamage: {damage}\nFallthrough: {penetrate}");
		}

		public virtual void PostCreate()
		{
			health = props.health;
		}

		public virtual void Initialize(VehicleComponentProperties props)
		{
			this.props = props;
			
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
			Scribe_Values.Look(ref health, "health");
		}
	}
}
