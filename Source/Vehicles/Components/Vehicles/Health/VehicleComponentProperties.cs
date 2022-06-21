using System;
using System.Collections.Generic;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class VehicleComponentProperties
	{
		[NoTranslate]
		public string key;
		public string label;

		public Type compClass;

		public int health;
		public float armor;
		public ExplosionProperties explosionProperties;
		public int efficiencyWeight = 1;

		public ComponentHitbox hitbox = new ComponentHitbox();
		public List<VehicleStatDef> categories;

		public SimpleCurve efficiency;

		public virtual void ResolveReferences(VehicleDef def)
		{
			if (efficiency is null)
			{
				efficiency = new SimpleCurve()
				{
					new CurvePoint(0.25f, 0),
					new CurvePoint(0.35f, 0.35f),
					new CurvePoint(0.75f, 0.75f),
					new CurvePoint(0.85f, 1),
					new CurvePoint(1, 1)
				};
			}
			if (categories is null)
			{
				categories = new List<VehicleStatDef>();
			}
			if (compClass is null)
			{
				compClass = typeof(VehicleComponent);
			}
			if (!explosionProperties.Empty)
			{
				explosionProperties.Def = DefDatabase<DamageDef>.GetNamed(explosionProperties.damageDef);
			}
			hitbox.Initialize(def);
		}

		public virtual IEnumerable<string> ConfigErrors()
		{
			if (key.NullOrEmpty())
			{
				yield return $"{key}: <field>key</field> field must be implemented.".ConvertRichText();
			}
			if (health <= 0)
			{
				yield return $"{key}: <field>health</field> must be greater than 0.".ConvertRichText();
			}
			if (efficiency != null && efficiency.PointsCount < 5)
			{
				yield return $"{key}: <field>efficiency</field> must include at least 5 points for proper color gradient construction.".ConvertRichText();
			}
			if (hitbox is null)
			{
				yield return $"{key}: <field>hitbox</field> must be specified.".ConvertRichText();
			}
			if (efficiencyWeight == 0)
			{
				yield return $"{key}: <field>efficiencyWeight</field> cannot = 0. If average weight = 0, resulting damage will be NaN, causing an instant-kill on the vehicle.";
			}
		}

		public struct ExplosionProperties
		{
			public float chance;
			public int radius;
			public string damageDef;

			public DamageDef Def { get; set; }

			public bool Empty => string.IsNullOrEmpty(damageDef);
		}
	}
}
