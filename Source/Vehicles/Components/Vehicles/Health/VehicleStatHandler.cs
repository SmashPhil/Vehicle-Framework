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
	public class VehicleStatHandler : IExposable
	{
		private const int TicksHighlighted = 100;

		private VehiclePawn vehicle;
		public List<VehicleComponent> components = new List<VehicleComponent>();
		private readonly Dictionary<IntVec2, List<VehicleComponent>> componentLocations = new Dictionary<IntVec2, List<VehicleComponent>>();
		public readonly Dictionary<VehicleStatDef, List<VehicleComponent>> statComponents = new Dictionary<VehicleStatDef, List<VehicleComponent>>();

		private readonly List<Pair<IntVec2, int>> debugCellHighlight = new List<Pair<IntVec2, int>>();

		public Dictionary<Thing, IntVec3> impacter = new Dictionary<Thing, IntVec3>();

		public VehicleStatHandler(VehiclePawn vehicle)
		{
			this.vehicle = vehicle;
			components = new List<VehicleComponent>();
			statComponents = new Dictionary<VehicleStatDef, List<VehicleComponent>>();
			debugCellHighlight = new List<Pair<IntVec2, int>>();
			componentLocations = new Dictionary<IntVec2, List<VehicleComponent>>();
		}

		public List<VehicleComponent> ComponentsPrioritized => components.OrderBy(c => !c.props.categories.NullOrEmpty() ? c.props.categories.Min(ctg => ctg.displayPriorityInCategory) : 9999).ThenBy(c => c.HealthPercent).ToList();

		public bool NeedsRepairs => components.Any(c => c.HealthPercent < 1);

		public void InitializeComponents()
		{
			components.Clear();
			foreach (VehicleComponentProperties props in vehicle.VehicleDef.components)
			{
				VehicleComponent component = (VehicleComponent)Activator.CreateInstance(props.compClass, vehicle);
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
				statComponents.Clear();
				foreach (VehicleStatDef category in comp.props.categories)
				{
					if (statComponents.TryGetValue(category, out var list))
					{
						list.Add(comp);
					}
					else
					{
						statComponents[category] = new List<VehicleComponent>() { comp };
					}
				}
			}
		}

		/// <param name="statDef"></param>
		/// <returns>% efficiency of <paramref name="statDef"/> given <see cref="VehicleComponent"/> calculation.</returns>
		public float StatEfficiency(VehicleStatDef statDef)
		{
			if (statComponents.TryGetValue(statDef, out var categories))
			{
				if (categories.FirstOrDefault(component => component.props.priorityStatEfficiency) is VehicleComponent component)
				{
					return component.Efficiency;
				}
				return statDef.operationType switch
				{
					EfficiencyOperationType.None => 1,
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

		public void RegisterImpacter(Thing launcher, IntVec3 cell)
		{
			CellRect occupiedRect = vehicle.OccupiedRect();
			if (!occupiedRect.Contains(cell))
			{
				cell = occupiedRect.MinBy(c => Ext_Map.Distance(c, cell));
			}
			impacter[launcher] = cell;
		}

		private IntVec2 AdjustFromVehiclePosition(IntVec2 cell)
		{
			if (!vehicle.Spawned)
			{
				return cell;
			}
			int x = cell.x - vehicle.Position.x;
			int z = cell.z - vehicle.Position.z;
			IntVec2 hitCell = new IntVec2(x, z);
			switch (vehicle.FullRotation.AsInt)
			{
				case 0: //North
					break;
				case 4: //NorthEast
				case 5: //SouthEast
				case 1: //East
					hitCell.x = -z;
					hitCell.z = x;
					break;
				case 2: //South
					hitCell.x = -x;
					hitCell.z = -z;
					break;
				case 6: //SouthWest
				case 7: //NorthWest
				case 3: //West
					hitCell.x = z;
					hitCell.z = -x;
					break;
			}
			return hitCell;
		}

		public void TakeDamage(DamageInfo dinfo, bool explosive = false)
		{
			if (!impacter.TryGetValue(dinfo.Instigator, out IntVec3 cell))
			{
				if (dinfo.Instigator != null)
				{
					cell = vehicle.OccupiedRect().MinBy(cell => Ext_Map.Distance(dinfo.Instigator.Position, vehicle.Position));
				}
				else
				{
					cell = vehicle.OccupiedRect().RandomCell; //TODO - randomize based on coverage
				}
			}
			IntVec2 hitCell = AdjustFromVehiclePosition(cell.ToIntVec2);
			TakeDamage(dinfo, hitCell, explosive);
		}

		public void TakeDamage(DamageInfo dinfo, IntVec2 hitCell, bool explosive = false)
		{
			StringBuilder report = VehicleMod.settings.debug.debugLogging ? new StringBuilder() : null;

			ApplyDamageToComponent(dinfo, hitCell, explosive, report);

			if (dinfo.Instigator != null)
			{
				impacter.Remove(dinfo.Instigator);
			}

			Debug.Message(report.ToStringSafe());
			
			//if (vehicle.Spawned && Mathf.Approximately(vehicle.GetStatValue(VehicleStatDefOf.BodyIntegrity), 0))
			//{
			//	vehicle.Kill(dinfo);
			//}
		}

		private void ApplyDamageToComponent(DamageInfo dinfo, IntVec2 hitCell, bool explosive = false, StringBuilder report = null)
		{
			DamageDef defApplied = dinfo.Def;
			float damage = dinfo.Amount;

			report?.AppendLine("-- DAMAGE REPORT --");
			report?.AppendLine($"Base Damage: {damage}");
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
			if (explosive)
			{
				IntVec2 cell = new IntVec2(hitCell.x, hitCell.z);
				Rot4 direction = DirectionFromAngle(dinfo.Angle);
				for (int i = 0; i < Mathf.Max(vehicle.VehicleDef.Size.x, vehicle.VehicleDef.Size.z); i++)
				{
					IntVec2 cellOffset = cell;
					for (int e = 0, seq = 0; e < 1 + i * 2; seq += e % 2 == 0 ? 1 : 0, e++)
					{
						if (!vehicle.Spawned)
						{
							return;
						}
						int seqAlt = e % 2 == 0 ? seq : -seq;
						float stepDamage = dinfo.Amount / (1 + i * 2);
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
						component.TakeDamage(vehicle, ref dinfo, new IntVec3(vehicle.Position.x + hitCell.x, 0, vehicle.Position.z + hitCell.z));
					}
					if (dinfo.Amount > 0 && direction.IsValid)
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
				IntVec2 cell = hitCell;
				Rot4 direction = DirectionFromAngle(dinfo.Angle);
				for (int i = 0; i < Mathf.Max(vehicle.VehicleDef.Size.x, vehicle.VehicleDef.Size.z); i++)
				{
					if (!vehicle.Spawned)
					{
						return;
					}
					VehicleComponent component = componentLocations.TryGetValue(cell, componentLocations[IntVec2.Zero]).Where(c => c.HealthPercent > 0).RandomElementWithFallback();
					if (component is null)
					{
						if (dinfo.Amount > 0 && direction.IsValid)
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
					report?.AppendLine($"Applying Damage = {dinfo.Amount} to {component.props.key}");
					DamageRoles(dinfo, cell);
					component.TakeDamage(vehicle, ref dinfo, new IntVec3(vehicle.Position.x + hitCell.x, 0, vehicle.Position.z + hitCell.z));
					report?.AppendLine($"Fallthrough Damage = {dinfo.Amount}");
					if (dinfo.Amount > 0 && direction.IsValid)
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
				GenDraw.DrawFieldEdges(hitboxCells, component.highlightColor);
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
