using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class VehicleComponent : IExposable
	{
		public static Color NeedsReplacement = new Color(0.75f, 0.45f, 0.45f);
		public static Color ModerateDamage = new Color(0.55f, 0.55f, 0.55f);
		public static Color MinorDamage = new Color(0.7f, 0.7f, 0.7f);
		public static Color WorkingCondition = new Color(0.6f, 0.8f, 0.65f);

		public static Gradient gradient;

		public VehicleComponentProperties props;
		public float health;

		public VehicleComponent()
		{
		}

		public float HealthPercent => Ext_Math.RoundTo(health / props.health, 0.01f);

		public float Efficiency => props.efficiency.Evaluate(HealthPercent);

		public string EfficiencyPercent => "VehicleStatPercent".Translate(Ext_Math.RoundTo(Efficiency * 100, 0.01f).ToString()).ToString().Colorize(EfficiencyColor);

		public string ArmorPercent => "VehicleStatPercent".Translate(Ext_Math.RoundTo(props.armor * 100, 0.1f).ToString());

		public string HealthPercentStringified => "VehicleStatPercent".Translate(HealthPercent * 100).ToString().Colorize(EfficiencyColor);

		public Color EfficiencyColor => gradient.Evaluate(Efficiency);

		public float TakeDamage(VehiclePawn vehicle, DamageInfo dinfo, IntVec3 cell)
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

		public void HealComponent(float amount)
		{
			health += amount;
			if (health > props.health)
			{
				health = props.health;
			}
		}

		public void ReduceDamageFromArmor(ref float damage, float armorPenetration, out bool penetrate)
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

		public void PostCreate()
		{
			health = props.health;
		}

		public void Initialize(VehicleComponentProperties props)
		{
			this.props = props;
			
			gradient = new Gradient()
			{
				colorKeys = new[] { new GradientColorKey(TexData.RedReadable, props.efficiency[0].x),
									new GradientColorKey(NeedsReplacement, props.efficiency[1].x),
									new GradientColorKey(ModerateDamage, props.efficiency[2].x),
									new GradientColorKey(MinorDamage, props.efficiency[3].x),
									new GradientColorKey(WorkingCondition, props.efficiency[4].x) }
			};
		}

		public void ExposeData()
		{
			Scribe_Values.Look(ref health, "health");
		}
	}
}
