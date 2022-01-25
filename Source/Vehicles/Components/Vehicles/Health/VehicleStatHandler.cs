using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class VehicleStatHandler : IExposable
	{
		private const int TicksHighlighted = 100;

		private VehiclePawn vehicle;
		public List<VehicleComponent> components = new List<VehicleComponent>();
		private readonly Dictionary<IntVec2, List<VehicleComponent>> componentLocations = new Dictionary<IntVec2, List<VehicleComponent>>();
		public readonly Dictionary<VehicleStatCategoryDef, List<VehicleComponent>> statComponents = new Dictionary<VehicleStatCategoryDef, List<VehicleComponent>>();

		private readonly List<Pair<IntVec2, int>> debugCellHighlight = new List<Pair<IntVec2, int>>();
		public readonly HashSet<Explosion> explosionsAffectingVehicle = new HashSet<Explosion>();

		public VehicleStatHandler(VehiclePawn vehicle)
		{
			this.vehicle = vehicle;
			components = new List<VehicleComponent>();
			statComponents = new Dictionary<VehicleStatCategoryDef, List<VehicleComponent>>();
			debugCellHighlight = new List<Pair<IntVec2, int>>();
			componentLocations = new Dictionary<IntVec2, List<VehicleComponent>>();
		}

		public List<VehicleComponent> ComponentsPrioritized => components.OrderBy(c => !c.props.categories.NullOrEmpty() ? c.props.categories.Min(ctg => ctg.priority) : 999).ThenBy(c => c.HealthPercent).ToList();

		public bool NeedsRepairs => components.Any(c => c.HealthPercent < 1);

		public void InitializeComponents()
		{
			components.Clear();
			foreach (VehicleComponentProperties props in vehicle.VehicleDef.components)
			{
				VehicleComponent component = (VehicleComponent)Activator.CreateInstance(props.compClass);
				components.Add(component);
				component.Initialize(props);
				component.PostCreate();
				RecacheStatCategories(component);
			}
		}

		private void RecacheStatCategories(VehicleComponent comp)
		{
			if (!comp.props.categories.NullOrEmpty())
			{
				foreach (VehicleStatCategoryDef category in comp.props.categories)
				{
					if (statComponents.TryGetValue(category, out var list))
					{
						list.Add(comp);
					}
					else
					{
						statComponents.Add(category, new List<VehicleComponent>() { comp });
					}
				}
			}
		}

		public float StatEfficiency(VehicleStatCategoryDef category)
		{
			if (statComponents.TryGetValue(category, out var categories))
			{
				return category.operationType switch
				{
					EfficiencyOperationType.MinValue => categories.Min(c => c.Efficiency),
					EfficiencyOperationType.MaxValue => categories.Max(c => c.Efficiency),
					EfficiencyOperationType.Sum => categories.Sum(c => c.Efficiency).Clamp(0, 1),
					_ => categories.AverageWeighted(c => c.props.efficiencyWeight, c => c.Efficiency) //Everything else falls through to Average case
				};
			}
			return 1;
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
						SmashLog.Error($"Unable to add to internal component list for <field>{cell}</field>");
						continue;
					}
					list.Add(component);
				}
			}
		}

		public void TakeDamage(DamageInfo dinfo, IntVec3 hitCell, bool explosive = false)
		{
			ApplyDamageToComponent(dinfo, new IntVec2(hitCell.x - vehicle.Position.x, hitCell.z - vehicle.Position.z), explosive);

			if (vehicle.ActualMoveSpeed <= 0.1f && vehicle.Spawned)
			{
				vehicle.drafter.Drafted = false;
			}
		}

		private void ApplyDamageToComponent(DamageInfo dinfo, IntVec2 hitCell, bool explosive = false)
		{
			float damage = dinfo.Amount;
			DamageDef defApplied = dinfo.Def;

			if (vehicle.VehicleDef.damageMultipliers?.FirstOrDefault(d => d.damageDef == defApplied) is DamageMultiplier dMultiplier)
			{
				dinfo.SetAmount(dMultiplier.multiplier);
			}
			else
			{
				if (dinfo.Def.isRanged)
				{
					damage *= SettingsCache.TryGetValue(vehicle.VehicleDef, typeof(VehicleDamageMultipliers), nameof(VehicleDamageMultipliers.rangedDamageMultiplier), vehicle.VehicleDef.properties.vehicleDamageMultipliers.rangedDamageMultiplier);
				}
				else if (dinfo.Def.isExplosive)
				{
					damage *= SettingsCache.TryGetValue(vehicle.VehicleDef, typeof(VehicleDamageMultipliers), nameof(VehicleDamageMultipliers.explosiveDamageMultiplier), vehicle.VehicleDef.properties.vehicleDamageMultipliers.explosiveDamageMultiplier);
				}
				else
				{
					damage *= SettingsCache.TryGetValue(vehicle.VehicleDef, typeof(VehicleDamageMultipliers), nameof(VehicleDamageMultipliers.meleeDamageMultiplier), vehicle.VehicleDef.properties.vehicleDamageMultipliers.meleeDamageMultiplier);
				}
			}
			if (explosive)
			{
				IntVec2 cell = new IntVec2(hitCell.x, hitCell.z);
				Rot4 direction = DirectionFromAngle(dinfo.Angle);
				for (int i = 0; i < Mathf.Max(vehicle.VehicleDef.Size.x, vehicle.VehicleDef.Size.z); i++)
				{
					float lastDamage = 0;
					IntVec2 cellOffset = cell;
					for (int e = 0, seq = 0; e < 1 + i * 2; seq += e % 2 == 0 ? 1 : 0, e++)
					{
						int seqAlt = e % 2 == 0 ? seq : -seq;
						float stepDamage = damage / (1 + i * 2);
						if (direction.IsHorizontal)
						{
							cellOffset.z = seqAlt;
						}
						else
						{
							cellOffset.x = seqAlt;
						}
						VehicleComponent component = componentLocations.TryGetValue(cellOffset, componentLocations[new IntVec2(0, 0)]).Where(c => c.HealthPercent > 0).RandomElementWithFallback();
						if (component is null)
						{
							continue;
						}
						if (!cell.IsValid)
						{
							break;
						}
						if (VehicleMod.settings.debug.debugDrawHitbox)
						{
							debugCellHighlight.Add(new Pair<IntVec2, int>(new IntVec2(cellOffset.x, cellOffset.z), TicksHighlighted));
						}
						dinfo.SetAmount(stepDamage);
						DamageRoles(dinfo, cellOffset);
						lastDamage = component.TakeDamage(vehicle, dinfo, new IntVec3(vehicle.Position.x + hitCell.x, 0, vehicle.Position.z + hitCell.z));
						if (vehicle.Spawned && Rand.Range(0, 1) < component.props.explosionProperties.chance)
						{
							GenExplosion.DoExplosion(new IntVec3(vehicle.Position.x + cellOffset.x, 0, vehicle.Position.z + cellOffset.z), vehicle.Map, component.props.explosionProperties.radius, component.props.explosionProperties.Def, dinfo.Instigator,
								component.props.explosionProperties.Def.defaultDamage, component.props.explosionProperties.Def.defaultArmorPenetration);
						}
					}
					damage = lastDamage;
					if (damage > 0 && direction.IsValid)
					{
						switch (direction.AsInt)
						{
							case 0:
								cell.z += 1;
								break;
							case 1:
								cell.x += 1;
								break;
							case 2:
								cell.z -= 1;
								break;
							case 3:
								cell.x -= 1;
								break;
						}
					}
					else
					{
						break;
					}
				}
			}
			else
			{
				IntVec2 cell = new IntVec2(hitCell.x, hitCell.z);
				Rot4 direction = DirectionFromAngle(dinfo.Angle);
				for (int i = 0; i < Mathf.Max(vehicle.VehicleDef.Size.x, vehicle.VehicleDef.Size.z); i++)
				{
					VehicleComponent component = componentLocations.TryGetValue(cell, componentLocations[new IntVec2(0, 0)]).Where(c => c.HealthPercent > 0).RandomElementWithFallback();
					if (component is null)
					{
						if (damage > 0 && direction.IsValid)
						{
							switch (direction.AsInt)
							{
								case 0:
									cell.z += 1;
									break;
								case 1:
									cell.x += 1;
									break;
								case 2:
									cell.z -= 1;
									break;
								case 3:
									cell.x -= 1;
									break;
							}
						}
						continue;
					}
					if (!cell.IsValid)
					{
						break;
					}
					if (VehicleMod.settings.debug.debugDrawHitbox)
					{
						debugCellHighlight.Add(new Pair<IntVec2, int>(new IntVec2(cell.x, cell.z), TicksHighlighted));
					}
					dinfo.SetAmount(damage);
					DamageRoles(dinfo, cell);
					damage = component.TakeDamage(vehicle, dinfo, new IntVec3(vehicle.Position.x + hitCell.x, 0, vehicle.Position.z + hitCell.z));
					if (vehicle.Spawned && Rand.Range(0, 1) < component.props.explosionProperties.chance)
					{
						GenExplosion.DoExplosion(new IntVec3(vehicle.Position.x + cell.x, 0, vehicle.Position.z + cell.z), vehicle.Map, component.props.explosionProperties.radius, component.props.explosionProperties.Def, dinfo.Instigator,
							component.props.explosionProperties.Def.defaultDamage, component.props.explosionProperties.Def.defaultArmorPenetration);
					}
					if (damage > 0 && direction.IsValid)
					{
						switch (direction.AsInt)
						{
							case 0:
								cell.z += 1;
								break;
							case 1:
								cell.x += 1;
								break;
							case 2:
								cell.z -= 1;
								break;
							case 3:
								cell.x -= 1;
								break;
						}
					}
					else
					{
						break;
					}
				}
			}
			vehicle.Map?.GetCachedMapComponent<ListerVehiclesRepairable>().Notify_VehicleTookDamage(vehicle);
		}

		private void DamageRoles(DamageInfo dinfo, IntVec2 cell)
		{
			var effectedHandlers = vehicle.handlers.Where(h => h.role.hitbox.Contains(cell)).ToList();
			foreach (VehicleHandler handler in effectedHandlers)
			{
				foreach (Pawn pawn in handler.handlers.InnerListForReading)
				{
					pawn.TakeDamage(dinfo);
				}
			}
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
				List<IntVec3> hitboxCells = new List<IntVec3>();
				foreach (var cell in component.props.hitbox.Hitbox)
				{
					int x = vehicle.Position.x;
					int z = vehicle.Position.z;
					switch (vehicle.FullRotation.AsInt)
					{
						case 0:
							x += cell.x;
							z += cell.z;
							break;
						case 1:
							x += cell.z;
							z += -cell.x;
							break;
						case 2:
							x += -cell.x;
							z += -cell.z;
							break;
						case 3:
							x += -cell.z;
							z += cell.x;
							break;
						case 4:
							x += cell.z;
							z += -cell.x;
							break;
						case 5:
							x += cell.z;
							z += -cell.x;
							break;
						case 6:
							x += -cell.z;
							z += cell.x;
							break;
						case 7:
							x += -cell.z;
							z += cell.x;
							break;
					}
					hitboxCells.Add(new IntVec3(x, 0, z));
				}
				GenDraw.DrawFieldEdges(hitboxCells, component.props.explosionProperties.Empty ? Color.white : new Color(1, 0.5f, 0));
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
			Scribe_References.Look(ref vehicle, "vehicle", true);
			Scribe_Collections.Look(ref components, "components", LookMode.Deep);
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				if (!components.NullOrEmpty())
				{
					for (int i = 0; i < components.Count; i++)
					{
						var component = components[i];
						var props = vehicle.VehicleDef.components[i];
						component.Initialize(props);
						RecacheStatCategories(component);
					}
				}
			}
		}
	}
}
