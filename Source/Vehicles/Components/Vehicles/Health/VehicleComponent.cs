using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class VehicleComponent : IExposable, ITweakFields
	{
		public VehiclePawn vehicle;
		
		[TweakField]
		public VehicleComponentProperties props;
		public float health;

		public List<ComponentOverlay> overlayRendering;

		[Unsaved]
		[Obsolete]
		public List<TurretHandling> turretHandling; //Not yet implemented

		public IndicatorDef indicator;
		public Color highlightColor = Color.white;

		public static Gradient gradient;

		public VehicleComponent(VehiclePawn vehicle)
		{
			this.vehicle = vehicle;
		}

		public float HealthPercent => Ext_Math.RoundTo(health / props.health, 0.01f);

		public float Efficiency => props.efficiency.Evaluate(HealthPercent); //Allow evaluating beyond 100% via stat parts

		public Dictionary<string, List<StatModifier>> SetArmorModifiers { get; private set; } = new Dictionary<string, List<StatModifier>>();

		public Dictionary<string, List<StatModifier>> AddArmorModifiers { get; private set; } = new Dictionary<string, List<StatModifier>>();

		public Color EfficiencyColor => gradient.Evaluate(Efficiency);

		string ITweakFields.Category => props.key;

		string ITweakFields.Label => props.key;

		public virtual void DrawIcon(Rect rect)
		{
			if (indicator != null)
			{
				Widgets.DrawTextureFitted(rect, indicator.Icon, 1);
				TooltipHandler.TipRegion(rect, indicator.label);
			}
		}

		public void TakeDamage(VehiclePawn vehicle, DamageInfo dinfo, bool ignoreArmor = false)
		{
			TakeDamage(vehicle, ref dinfo, ignoreArmor: ignoreArmor);
		}

		public virtual Penetration TakeDamage(VehiclePawn vehicle, ref DamageInfo dinfo, bool ignoreArmor = false)
		{
			Penetration penetration = Penetration.NonPenetrated;
			if (!ignoreArmor)
			{
				ReduceDamageFromArmor(ref dinfo, out penetration);
			}
			
			health -= dinfo.Amount;
			float remainingDamage = Mathf.Clamp(-health, 0, float.MaxValue);
			health = health.Clamp(0, props.health);

			if (dinfo.Amount > 0)
			{
				if (!props.reactors.NullOrEmpty())
				{
					foreach (Reactor reactor in props.reactors)
					{
						reactor.Hit(vehicle, this, ref dinfo, penetration);
					}
				}

				vehicle.EventRegistry[VehicleEventDefOf.DamageTaken].ExecuteEvents();
				if (vehicle.GetStatValue(VehicleStatDefOf.MoveSpeed) <= 0.1f)
				{
					vehicle.ignition.Drafted = false;
				}
				if (vehicle.Spawned && vehicle.GetStatValue(VehicleStatDefOf.BodyIntegrity) < 0.01f)
				{
					vehicle.Kill(dinfo);
				}
			}

			if (penetration > Penetration.Penetrated || health > (props.health / 2f))
			{
				//Don't fallthrough until part is below 50% health or damage has penetrated
				dinfo.SetAmount(0);
			}
			else if (penetration == Penetration.Penetrated || (remainingDamage > 0 && remainingDamage >= dinfo.Amount / 2))
			{
				//If penetrated or remaining damage is above half original damage amount
				dinfo.SetAmount(remainingDamage);
			}
			return penetration;
		}

		public virtual void HealComponent(float amount)
		{
			health += amount;
			if (health > props.health)
			{
				health = props.health;
			}
			vehicle.EventRegistry[VehicleEventDefOf.Repaired].ExecuteEvents();
		}

		public virtual void ReduceDamageFromArmor(ref DamageInfo dinfo, out Penetration result)
		{
			result = Penetration.NonPenetrated;
			if (dinfo.Def.armorCategory == null)
			{
				return;
			}
			DamageArmorCategoryDef armorCategoryDef = dinfo.Def.armorCategory;
			float armorRating = ArmorRating(armorCategoryDef, out _);
			float armorDiff = armorRating - dinfo.ArmorPenetrationInt;

			result = props.hitbox.fallthrough ? Penetration.Penetrated : Penetration.NonPenetrated;

			float chance = Rand.Value;
			if (chance < (armorDiff / 2))
			{
				dinfo.SetAmount(0);
				result = Penetration.Deflected;
			}
			else
			{
				if (chance < armorDiff)
				{
					float damage = GenMath.RoundRandom(dinfo.Amount / 2);
					dinfo.SetAmount(damage);
					result = Penetration.Diminished;
					if (dinfo.Def.armorCategory == DamageArmorCategoryDefOf.Sharp)
					{
						dinfo.Def = DamageDefOf.Blunt;
					}
				}
			}
		}

		/// <summary>
		/// Pulls armor rating of component for <paramref name="armorCategoryDef"/>.  If armor rating is not specified, defaults to vehicles overall armor rating for that armor category.
		/// </summary>
		/// <param name="armorCategoryDef"></param>
		/// <returns>armor rating %</returns>
		public float ArmorRating(DamageArmorCategoryDef armorCategoryDef, out bool upgraded)
		{
			upgraded = false;
			if (!SetArmorModifiers.NullOrEmpty())
			{
				foreach ((_, List<StatModifier> statModifiers) in SetArmorModifiers)
				{
					if (TryGetModifier(statModifiers, out float setValue))
					{
						upgraded = true;
						return setValue;
					}
				}
			}
			float value = vehicle.GetStatValue(armorCategoryDef.armorRatingStat);
			StatModifier armorModifier = props.armor?.FirstOrDefault(rating => rating.stat == armorCategoryDef.armorRatingStat);
			if (armorModifier != null)
			{
				value = armorModifier.value;
			}
			if (!AddArmorModifiers.NullOrEmpty())
			{
				foreach ((_, List<StatModifier> statModifiers) in AddArmorModifiers)
				{
					if (TryGetModifier(statModifiers, out float addValue))
					{
						upgraded = true;
						value += addValue;
					}
				}
			}
			return value;

			bool TryGetModifier(List<StatModifier> statModifiers, out float value)
			{
				value = 0;
				if (statModifiers.NullOrEmpty())
				{
					return false;
				}
				foreach (StatModifier statModifier in statModifiers)
				{
					if (statModifier.stat == armorCategoryDef.armorRatingStat)
					{
						value = statModifier.value;
						return true;
					}
				}
				return false;
			}
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

		void ITweakFields.OnFieldChanged()
		{
		}

		//Yes I'm aware this is very similar to BodyPartDepth, creating this for clarity that this is an entirely different set of mechanics for similar purpose
		public enum VehiclePartDepth
		{ 
			Undefined,
			External,
			Internal
		}

		public enum Penetration
		{
			Penetrated,
			NonPenetrated,
			Diminished,
			Deflected,
		}

		public struct DamageResult
		{
			public Penetration penetration;

			public DamageInfo damageInfo;
			public IntVec2 cell;
		}

		public struct ComponentOverlay
		{
			public int index;
			public float healthPercent;
		}

		public struct TurretHandling
		{
			public string key;
			public float healthPercent;
		}
	}
}
