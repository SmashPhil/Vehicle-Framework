using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class VehicleStatHandler : IExposable, ITweakFields
	{
		private const int TicksHighlighted = 100;

		private const float ChanceDirectHit = 1.25f;
		private const float ChanceFallthroughHit = 1;
		private const float ChanceMinorDeflectHit = 0.75f;
		private const float ChanceMajorDeflectHit = 0.75f;

		//Debugging only
		private readonly List<Pair<IntVec2, int>> debugCellHighlight = new List<Pair<IntVec2, int>>();

		//Caching lookup
		private readonly Dictionary<string, VehicleComponent> componentsByKeys = new Dictionary<string, VehicleComponent>();
		private readonly Dictionary<IntVec2, List<VehicleComponent>> componentLocations = new Dictionary<IntVec2, List<VehicleComponent>>();
		private readonly Dictionary<VehicleStatDef, List<VehicleComponent>> statComponents = new Dictionary<VehicleStatDef, List<VehicleComponent>>();

		//Caching values
		private readonly StatCache statCache;

		//Registry
		private readonly Dictionary<Thing, IntVec3> impacter = new Dictionary<Thing, IntVec3>();
		[TweakField]
		public List<VehicleComponent> components = new List<VehicleComponent>();

		private Dictionary<StatUpgradeCategoryDef, StatOffset> categoryOffsets = new Dictionary<StatUpgradeCategoryDef, StatOffset>();

		private Dictionary<VehicleStatDef, StatOffset> statOffsets = new Dictionary<VehicleStatDef, StatOffset>();

		private static readonly List<IntVec3> hitboxHighlightCells = new List<IntVec3>();

		private VehiclePawn vehicle;
		
		public VehicleStatHandler(VehiclePawn vehicle)
		{
			this.vehicle = vehicle;
			statCache = new StatCache(vehicle);
			components = new List<VehicleComponent>();
			statComponents = new Dictionary<VehicleStatDef, List<VehicleComponent>>();
			debugCellHighlight = new List<Pair<IntVec2, int>>();
			componentLocations = new Dictionary<IntVec2, List<VehicleComponent>>();
		}

		public List<VehicleComponent> ComponentsPrioritized => components.OrderBy(c => !c.props.categories.NullOrEmpty() ? c.props.categories.Min(ctg => ctg.displayPriorityInCategory) : 9999).ThenBy(c => c.HealthPercent).ToList();

		public bool NeedsRepairs => components.Any(c => c.HealthPercent < 1);

		public float HealthPercent { get; private set; }

		string ITweakFields.Category => nameof(VehicleStatHandler);

		string ITweakFields.Label => "Stat Handler";

		public void InitializeComponents()
		{
			components.Clear();
			statComponents.Clear();
			componentsByKeys.Clear();
			foreach (VehicleComponentProperties props in vehicle.VehicleDef.components)
			{
				VehicleComponent component = (VehicleComponent)Activator.CreateInstance(props.compClass, vehicle);
				components.Add(component);
				component.Initialize(props);
				component.PostCreate();
				componentsByKeys[component.props.key] = component;
				RecacheStatCategories(component);
			}
			InitializeStatOffsets();
		}

		public void InitializeStatOffsets()
		{
			statOffsets.Clear();
			categoryOffsets.Clear();

			foreach (VehicleStatDef statDef in DefDatabase<VehicleStatDef>.AllDefsListForReading)
			{
				StatOffset statOffset = new StatOffset(vehicle, statDef);
				statOffsets[statDef] = statOffset;
			}

			foreach (StatUpgradeCategoryDef upgradeCategoryDef in DefDatabase<StatUpgradeCategoryDef>.AllDefsListForReading)
			{
				StatOffset statOffset = new StatOffset(vehicle, upgradeCategoryDef);
				categoryOffsets[upgradeCategoryDef] = statOffset;
			}
		}

		public void AddStatOffset(VehicleStatDef statDef, float value)
		{
			statOffsets[statDef].Offset += value;
		}

		public void AddStatOffset(StatUpgradeCategoryDef upgradeCategoryDef, float value)
		{
			categoryOffsets[upgradeCategoryDef].Offset += value;
		}

		public void RemoveStatOffset(VehicleStatDef statDef, float value)
		{
			statOffsets[statDef].Offset -= value;
		}

		public void RemoveStatOffset(StatUpgradeCategoryDef upgradeCategoryDef, float value)
		{
			categoryOffsets[upgradeCategoryDef].Offset -= value;
		}

		public float GetStatOffset(VehicleStatDef statDef)
		{
			return statOffsets[statDef].Offset;
		}

		public float GetStatOffset(StatUpgradeCategoryDef upgradeCategoryDef)
		{
			return categoryOffsets[upgradeCategoryDef].Offset;
		}

		public float GetStatValue(VehicleStatDef statDef)
		{
			return statCache[statDef];
		}

		public void MarkStatDirty(VehicleStatDef statDef)
		{
			statCache.MarkDirty(statDef);
		}

		public void MarkAllDirty()
		{
			statCache.Reset();
			RecalculateHealthPercent();
		}

		private void RecacheStatCategories(VehicleComponent component)
		{
			if (!component.props.categories.NullOrEmpty())
			{
				foreach (VehicleStatDef category in component.props.categories)
				{
					if (statComponents.TryGetValue(category, out var list))
					{
						list.Add(component);
					}
					else
					{
						statComponents[category] = new List<VehicleComponent>() { component };
					}
				}
			}
		}

		public float GetComponentHealth(string key)
		{
			if (!componentsByKeys.TryGetValue(key, out VehicleComponent component))
			{
				Log.Error($"Unable to locate component {key} in stat handler.");
				return 0;
			}
			return component.health;
		}

		/// <summary>
		/// Set component health directly without triggering reactors
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void SetComponentHealth(string key, float value)
		{
			if (!componentsByKeys.TryGetValue(key, out VehicleComponent component))
			{
				Log.Error($"Unable to locate component {key} in stat handler.");
				return;
			}
			component.health = value;
			MarkAllDirty();
		}

		public VehicleComponent GetComponent(string key)
		{
			return componentsByKeys.TryGetValue(key);
		}

		public float GetComponentHealthPercent(string key)
		{
			if (!componentsByKeys.TryGetValue(key, out VehicleComponent component))
			{
				Log.Error($"Unable to locate component {key} in stat handler.");
				return 0;
			}
			return component.HealthPercent;
		}

		public void SetComponentHealthPercent(string key, float value)
		{
			if (!componentsByKeys.TryGetValue(key, out VehicleComponent component))
			{
				Log.Error($"Unable to locate component {key} in stat handler.");
				return;
			}
			component.health = component.props.health * value;
			MarkAllDirty();
		}

		/// <param name="statDef"></param>
		/// <returns>% efficiency of <paramref name="statDef"/> given <see cref="VehicleComponent"/> calculation.</returns>
		public float StatEfficiency(VehicleStatDef statDef)
		{
			if (statComponents.TryGetValue(statDef, out var components))
			{
				float efficiency = EfficiencyFor(statDef.operationType, components);
				float priorityEfficiency = EfficiencyFor(statDef.operationType, components.Where(component => component.props.priorityStatEfficiency));
				if (priorityEfficiency < efficiency)
				{
					return priorityEfficiency;
				}
				return efficiency;
			}
			return 1;
		}

		private float EfficiencyFor(EfficiencyOperationType operationType, IEnumerable<VehicleComponent> components)
		{
			if (components.EnumerableNullOrEmpty())
			{
				return 1;
			}
			return operationType switch
			{
				EfficiencyOperationType.None => 1,
				EfficiencyOperationType.MinValue => components.Min(c => c.Efficiency),
				EfficiencyOperationType.MaxValue => components.Max(c => c.Efficiency),
				EfficiencyOperationType.Sum => components.Sum(c => c.Efficiency).Clamp(0, 1),
				_ => components.AverageWeighted(c => c.props.efficiencyWeight, c => c.Efficiency) //Everything else falls through to Average case
			};
		}

		public void InitializeHitboxCells()
		{
			componentLocations.Clear();
			foreach (IntVec2 cell in vehicle.Hitbox.Cells2D)
			{
				componentLocations.Add(cell, new List<VehicleComponent>());
			}

			foreach (VehicleComponent component in components)
			{
				foreach (IntVec2 cell in component.props.hitbox.Hitbox)
				{
					if (!componentLocations.TryGetValue(cell, out var list))
					{
						SmashLog.Error($"Unable to add to internal component list for <field>{cell}</field>. Component = {component.props.key}");
						continue;
					}
					list.Add(component);
				}
			}
		}

		public void RegisterImpacter(DamageInfo dinfo, IntVec3 cell)
		{
			if (dinfo.Instigator != null)
			{
				RegisterImpacter(dinfo.Instigator, cell);
			}
		}

		/// <summary>
		/// Registers instigator to specific cell so that damage can be applied to the vehicle components belonging to that area of the hitbox.
		/// </summary>
		/// <remarks>The instigator is immediately deregistered upon dealing damage to make way for multiple damage instances from the same instigator (won't conflict with synchronous operations)</remarks>
		/// <param name="thing"></param>
		/// <param name="cell"></param>
		public IntVec3 RegisterImpacter(Thing thing, IntVec3 cell)
		{
			CellRect occupiedRect = vehicle.OccupiedRect();
			if (!occupiedRect.Contains(cell))
			{
				cell = occupiedRect.MinBy(c => Ext_Map.Distance(c, cell));
			}
			impacter[thing] = cell;
			return cell;
		}

		public void DeregisterImpacter(Thing thing)
		{
			if (thing != null)
			{
				impacter.Remove(thing);
			}
		}

		public static IntVec2 AdjustFromVehiclePosition(VehiclePawn vehicle, IntVec2 cell)
		{
			if (!vehicle.Spawned)
			{
				return cell;
			}
			int x = cell.x - vehicle.Position.x;
			int z = cell.z - vehicle.Position.z;
			IntVec2 hitCell = new IntVec2(x, z);
			return hitCell;
		}

		public void RecalculateHealthPercent()
		{
			HealthPercent = components.Average(component => component.HealthPercent);
		}

		public void TakeDamage(DamageInfo dinfo)
		{
			if (dinfo.Instigator is null || !impacter.TryGetValue(dinfo.Instigator, out IntVec3 cell))
			{
				if (dinfo.Instigator != null)
				{
					cell = vehicle.OccupiedRect().MinBy(cell => Ext_Map.Distance(dinfo.Instigator.Position, cell));
				}
				else
				{
					cell = vehicle.OccupiedRect().RandomCell; //TODO - randomize based on coverage
				}
			}
			IntVec2 hitCell = AdjustFromVehiclePosition(vehicle, cell.ToIntVec2);
			IntVec2 rotCell = hitCell.RotatedBy(vehicle.Rotation, vehicle.VehicleDef.Size);
			ApplyDamage(dinfo, rotCell);
		}

		public void TakeDamage(DamageInfo dinfo, IntVec2 hitCellPreRotate)
		{
			IntVec2 rotCell = hitCellPreRotate.RotatedBy(vehicle.Rotation, vehicle.VehicleDef.Size);
			ApplyDamage(dinfo, rotCell);
		}

		public void ApplyDamage(DamageInfo dinfo, IntVec2 hitCell)
		{
			StringBuilder report = VehicleMod.settings.debug.debugLogging ? new StringBuilder() : null;

			ApplyDamageToComponent(dinfo, hitCell, report);
			
			DeregisterImpacter(dinfo.Instigator);
			Debug.Message(report.ToStringSafe());
		}

		private void ApplyDamageToComponent(DamageInfo dinfo, IntVec2 hitCell, StringBuilder report = null)
		{
			DamageDef defApplied = dinfo.Def;
			float damage = dinfo.Amount;

			if (defApplied.workerClass == typeof(DamageWorker_Extinguish))
			{
				TryExtinguishFire(dinfo, hitCell);
			}
			if (!defApplied.harmsHealth)
			{
				damage = 0; //Don't apply damage to vehicles if the damage def isn't supposed to harm
			}
			try
			{
				report?.AppendLine("-- DAMAGE REPORT --");
				report?.AppendLine($"Base Damage: {damage}");
				report?.AppendLine($"DamageDef: {dinfo.Def}");
				report?.AppendLine($"HitCell: {hitCell}");

				if (dinfo.Weapon?.GetModExtension<VehicleDamageMultiplierDefModExtension>() is VehicleDamageMultiplierDefModExtension weaponMultiplier)
				{
					damage *= weaponMultiplier.multiplier;
					report?.AppendLine($"ModExtension Multiplier: {weaponMultiplier.multiplier} Result: {damage}");
				}

				if (dinfo.Instigator?.def.GetModExtension<VehicleDamageMultiplierDefModExtension>() is VehicleDamageMultiplierDefModExtension defMultiplier)
				{
					damage *= defMultiplier.multiplier;
					report?.AppendLine($"ModExtension Multiplier: {defMultiplier.multiplier} Result: {damage}");
				}

				if (!vehicle.VehicleDef.properties.damageDefMultipliers.NullOrEmpty() && vehicle.VehicleDef.properties.damageDefMultipliers.TryGetValue(dinfo.Def, out float multiplier))
				{
					damage *= multiplier;
					report?.AppendLine($"DamageDef Multiplier: {multiplier} Result: {damage}");
				}

				if (dinfo.Def.isRanged)
				{
					damage *= VehicleMod.settings.main.rangedDamageMultiplier;
					report?.AppendLine($"Settings Multiplier: {VehicleMod.settings.main.rangedDamageMultiplier} Result: {damage}");
				}
				else if (dinfo.Def.isExplosive)
				{
					damage *= VehicleMod.settings.main.explosiveDamageMultiplier;
					report?.AppendLine($"Settings Multiplier: {VehicleMod.settings.main.explosiveDamageMultiplier} Result: {damage}");
				}
				else
				{
					damage *= VehicleMod.settings.main.meleeDamageMultiplier;
					report?.AppendLine($"Settings Multiplier: {VehicleMod.settings.main.meleeDamageMultiplier} Result: {damage}");
				}

				if (damage <= 0)
				{
					report?.AppendLine($"Final Damage = {damage}. Exiting.");
					return;
				}
				dinfo.SetAmount(damage);
				Rot4 direction = DirectionFromAngle(dinfo.Angle);
				VehicleComponent.VehiclePartDepth hitDepth = VehicleComponent.VehiclePartDepth.External;
				for (int i = 0; i < Mathf.Max(vehicle.VehicleDef.Size.x, vehicle.VehicleDef.Size.z); i++)
				{
					if (vehicle.Destroyed || dinfo.Amount <= 0)
					{
						return;
					}
					VehicleComponent component = null;
					report?.AppendLine($"Damaging = {hitCell}");
					if (componentLocations.TryGetValue(hitCell, out List<VehicleComponent> components))
					{
						report?.AppendLine($"components=({string.Join(",", components.Select(c => c.props.label))})");
						report?.AppendLine($"hitDepth = {hitDepth}");
						//If no components at hit cell, fallthrough to internal
						var externalComponentsAtHitDepth = components.Where(comp => comp.props.depth == hitDepth && comp.HealthPercent > 0);
						report?.AppendLine($"components at hitDepth {hitDepth}: ({string.Join(",", externalComponentsAtHitDepth.Select(comp => comp.props.label))})");
						if (!externalComponentsAtHitDepth.TryRandomElementByWeight((component) => component.props.hitWeight, out component))
						{
							report?.AppendLine($"No components found. Hitting internal parts.");
							hitDepth = VehicleComponent.VehiclePartDepth.Internal;
							var internalComponentsAtHitDepth = components.Where(comp => comp.props.depth == hitDepth && comp.HealthPercent > 0);
							if (!internalComponentsAtHitDepth.TryRandomElementByWeight((component) => component.props.hitWeight, out component))
							{
								//If depth = internal then pick random internal component even if it does not have a hitbox
								component = this.components.Where(comp => comp.props.depth == hitDepth && comp.HealthPercent > 0).RandomElementByWeightWithFallback((component) => component.props.hitWeight);
								//If no internal components, pick random component w/ health
								component ??= this.components.Where(comp => comp.HealthPercent > 0).RandomElementByWeightWithFallback((component) => component.props.hitWeight);
								if (component is null)
								{
									return;
								}
							}
							else
							{
								report?.AppendLine($"Found Internal Component {component.props.label} at {hitCell}");
							}
						}
						else
						{
							report?.AppendLine($"Found External Component {component.props.label} at {hitCell}");
						}
					}
					else
					{
						report?.AppendLine($"No components found. Hitting internal parts.");
						hitDepth = VehicleComponent.VehiclePartDepth.Internal;
						//If depth = internal then pick random internal component even if it does not have a hitbox
						component = this.components.Where(comp => comp.props.depth == hitDepth && comp.HealthPercent > 0).RandomElementByWeightWithFallback((component) => component.props.hitWeight);
						//If no internal components, pick random component w/ health
						component ??= this.components.Where(comp => comp.HealthPercent > 0).RandomElementByWeightWithFallback((component) => component.props.hitWeight);
						if (component is null)
						{
							return;
						}
					}
					if (!hitCell.IsValid)
					{
						break;
					}
					if (VehicleMod.settings.debug.debugDrawHitbox)
					{
						IntVec2 renderCell = hitCell;
						if (vehicle.Rotation != Rot4.North)
						{
							renderCell = renderCell.RotatedBy(vehicle.Rotation, vehicle.VehicleDef.Size, reverseRotate: true);
						}
						debugCellHighlight.Add(new Pair<IntVec2, int>(renderCell, TicksHighlighted));
					}
					report?.AppendLine($"Damaging {hitCell}");
					if (HitPawn(dinfo, hitDepth, hitCell, direction, out Pawn hitPawn))
					{
						report?.AppendLine($"Hit {hitPawn} for {dinfo.Amount}. Impact site = {hitCell}");
						return;
					}
					report?.AppendLine($"Applying Damage = {dinfo.Amount} to {component.props.key} at {hitCell}");
					VehicleComponent.Penetration result = component.TakeDamage(vehicle, ref dinfo);
					//Effecters and sounds only for first hit
					if (i == 0)
					{
						IntVec3 impactCell = new IntVec3(vehicle.Position.x + hitCell.x, 0, vehicle.Position.z + hitCell.z);
						vehicle.Notify_DamageImpact(new VehicleComponent.DamageResult()
						{
							penetration = result,
							damageInfo = dinfo,
							cell = hitCell
						});
					}
					report?.AppendLine($"Fallthrough Damage = {dinfo.Amount}");
				}
			}
			finally
			{
				RecalculateHealthPercent();
			}
		}

		private void TryExtinguishFire(DamageInfo damageInfo, IntVec2 hitCell)
		{
			if (damageInfo.Def.hediff == HediffDefOf.CoveredInFirefoam)
			{
				//TODO - Enable firefoam overlay
			}
			if (vehicle.GetAttachment(ThingDefOf.Fire) is Fire fire && !fire.Destroyed)
			{
				fire.fireSize -= damageInfo.Amount * 0.01f;
				if (fire.fireSize < 0.1f)
				{
					fire.Destroy();
				}
			}
		}

		private bool HitPawn(DamageInfo dinfo, VehicleComponent.VehiclePartDepth hitDepth, IntVec2 cell, Rot4 dir, out Pawn hitPawn, StringBuilder report = null)
		{
			VehicleHandler handler;
			float multiplier = 1;
			hitPawn = null;
			if (hitDepth == VehicleComponent.VehiclePartDepth.External)
			{
				multiplier = ChanceDirectHit;
				TrySelectHandler(cell, out handler, exposed: true);
			}
			else
			{
				if (TrySelectHandler(cell, out handler))
				{
					multiplier = ChanceDirectHit;
				}
				else if (dir.IsValid)
				{
					//Check immediately behind
					if (TrySelectHandler(cell.Shifted(dir, 1), out handler))
					{
						multiplier = ChanceFallthroughHit;
					}
					else
					{
						//Directly left
						if (TrySelectHandler(cell.Shifted(dir, 0, -1), out handler))
						{
							multiplier = ChanceMajorDeflectHit;
						}
						//Directly right
						else if (TrySelectHandler(cell.Shifted(dir, 0, 1), out handler))
						{
							multiplier = ChanceMajorDeflectHit;
						}
						//Behind and left
						else if (TrySelectHandler(cell.Shifted(dir, 1, -1), out handler))
						{
							multiplier = ChanceMinorDeflectHit;
						}
						//Behind and right
						else if (TrySelectHandler(cell.Shifted(dir, 1, 1), out handler))
						{
							multiplier = ChanceMinorDeflectHit;
						}
					}
				}
			}
			if (handler != null && handler.handlers.Count > 0 && Rand.Chance(handler.role.chanceToHit * multiplier))
			{
				hitPawn = handler.handlers.InnerListForReading.RandomElement();
				report?.AppendLine($"Hitting {handler} with chance {handler.role.chanceToHit * multiplier}");
				hitPawn.TakeDamage(dinfo);
				return true;
			}
			return false;
		}

		private bool TrySelectHandler(IntVec2 cell, out VehicleHandler handler, bool exposed = false)
		{
			handler = vehicle.handlers.FirstOrDefault(handler => handler.role.hitbox != null && handler.role.hitbox.Contains(cell) && handler.handlers.Count > 0 && handler.role.exposed == exposed);
			return handler != null;
		}

		private Rot4 DirectionFromAngle(float angle)
		{
			if (angle < 0 || angle > 360)
			{
				return Rot4.Invalid;
			}
			if (angle >= 45 && angle <= 135)
			{
				return Rot4.East;
			}
			if (angle >= 135 && angle <= 225)
			{
				return Rot4.South;
			}
			if (angle >= 225 && angle <= 335)
			{
				return Rot4.West;
			}
			if (angle >= 335 || angle <= 45)
			{
				return Rot4.North;
			}
			return Rot4.Invalid;
		}

		public void DrawHitbox(VehicleComponent component)
		{
			if (component != null)
			{
				hitboxHighlightCells.Clear();
				{
					if (!component.props.hitbox.Empty)
					{
						foreach (IntVec2 cell in component.props.hitbox.Hitbox)
						{
							IntVec2 rotatedCell = cell.RotatedBy(vehicle.Rotation, vehicle.VehicleDef.Size, reverseRotate: true);
							hitboxHighlightCells.Add(new IntVec3(vehicle.Position.x + rotatedCell.x, 0, vehicle.Position.z + rotatedCell.z));
						}
					}
					else if (component.props.depth == VehicleComponent.VehiclePartDepth.External) //Dont render Internal components without a hitbox
					{
						hitboxHighlightCells.AddRange(vehicle.OccupiedRect());
					}
					
					if (hitboxHighlightCells.Count > 0)
					{
						GenDraw.DrawFieldEdges(hitboxHighlightCells, component.highlightColor);
					}
				}
				hitboxHighlightCells.Clear();
			}
			
			if (VehicleMod.settings.debug.debugDrawHitbox)
			{
				for (int i = debugCellHighlight.Count - 1; i >= 0; i--)
				{
					GenDraw.DrawFieldEdges(new List<IntVec3>() { new IntVec3(vehicle.Position.x + debugCellHighlight[i].First.x, 0, vehicle.Position.z + debugCellHighlight[i].First.z) }, Color.red);
					if (!Find.TickManager.Paused)
					{
						int tickCount = debugCellHighlight[i].Second - 1;
						if (tickCount <= 0)
						{
							debugCellHighlight.RemoveAt(i);
						}
						else
						{
							debugCellHighlight[i] = new Pair<IntVec2, int>(debugCellHighlight[i].First, tickCount);
						}
					}
				}
			}
		}

		public void ExposeData()
		{
			Scribe_References.Look(ref vehicle, nameof(vehicle), true);
			Scribe_Collections.Look(ref components, nameof(components), LookMode.Deep, vehicle);
			
			if (Scribe.mode == LoadSaveMode.LoadingVars)
			{
				InitializeStatOffsets();
			}
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				if (!components.NullOrEmpty())
				{
					for (int i = 0; i < components.Count; i++)
					{
						VehicleComponent component = components[i];
						VehicleComponentProperties props = vehicle.VehicleDef.components[i];
						component.Initialize(props);
						componentsByKeys[component.props.key] = component;
						RecacheStatCategories(component);
					}
				}
			}
		}

		void ITweakFields.OnFieldChanged()
		{
			MarkAllDirty();
		}
	}
}
