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

		private const float ChanceDirectHit = 1.25f;
		private const float ChanceFallthroughHit = 1;
		private const float ChanceMinorDeflectHit = 0.75f;
		private const float ChanceMajorDeflectHit = 0.75f;

		//Debugging only
		private readonly List<Pair<IntVec2, int>> debugCellHighlight = new List<Pair<IntVec2, int>>();

		//Caching lookup
		private readonly Dictionary<IntVec2, List<VehicleComponent>> componentLocations = new Dictionary<IntVec2, List<VehicleComponent>>();
		private readonly Dictionary<VehicleStatDef, List<VehicleComponent>> statComponents = new Dictionary<VehicleStatDef, List<VehicleComponent>>();

		//Caching values
		private readonly StatCache statCache;

		//Registry
		private readonly Dictionary<Thing, IntVec3> impacter = new Dictionary<Thing, IntVec3>();
		public List<VehicleComponent> components = new List<VehicleComponent>();

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

		public void InitializeComponents()
		{
			components.Clear();
			statComponents.Clear();
			foreach (VehicleComponentProperties props in vehicle.VehicleDef.components)
			{
				VehicleComponent component = (VehicleComponent)Activator.CreateInstance(props.compClass, vehicle);
				components.Add(component);
				component.Initialize(props);
				component.PostCreate();
				RecacheStatCategories(component);
			}
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
		}

		private void RecacheStatCategories(VehicleComponent comp)
		{
			if (!comp.props.categories.NullOrEmpty())
			{
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

		/// <summary>
		/// Registers instigator to specific cell so that damage can be applied to the vehicle components belonging to that area of the hitbox.
		/// </summary>
		/// <remarks>The instigator is immediately deregistered upon dealing damage to make way for multiple damage instances from the same instigator (won't conflict with synchronous operations)</remarks>
		/// <param name="thing"></param>
		/// <param name="cell"></param>
		public void RegisterImpacter(Thing thing, IntVec3 cell)
		{
			CellRect occupiedRect = vehicle.OccupiedRect();
			if (!occupiedRect.Contains(cell))
			{
				cell = occupiedRect.MinBy(c => Ext_Map.Distance(c, cell));
			}
			impacter[thing] = cell;
		}

		public void DeregisterImpacter(Thing thing)
		{
			if (thing != null)
			{
				impacter.Remove(thing);
			}
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

		public void TakeDamage(DamageInfo dinfo)
		{
			if (dinfo.Instigator is null || !impacter.TryGetValue(dinfo.Instigator, out IntVec3 cell))
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
			TakeDamage(dinfo, hitCell);
		}

		public void TakeDamage(DamageInfo dinfo, IntVec2 hitCell)
		{
			StringBuilder report = VehicleMod.settings.debug.debugLogging ? new StringBuilder() : null;

			ApplyDamageToComponent(dinfo, hitCell, report);
			vehicle.Notify_TookDamage();
			DeregisterImpacter(dinfo.Instigator);
			Debug.Message(report.ToStringSafe());
		}

		private void ApplyDamageToComponent(DamageInfo dinfo, IntVec2 hitCell, StringBuilder report = null)
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
			Rot4 direction = DirectionFromAngle(dinfo.Angle);
			VehicleComponent.VehiclePartDepth hitDepth = VehicleComponent.VehiclePartDepth.External;
			for (int i = 0; i < Mathf.Max(vehicle.VehicleDef.Size.x, vehicle.VehicleDef.Size.z); i++)
			{
				if (!vehicle.Spawned || dinfo.Amount <= 0)
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
					if (!components.Where(comp => comp.props.depth == hitDepth && comp.HealthPercent > 0).TryRandomElementByWeight((component) => component.props.hitWeight, out component))
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
					else
					{
						report?.AppendLine($"Found {component} at {hitCell}");
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
					debugCellHighlight.Add(new Pair<IntVec2, int>(hitCell, TicksHighlighted));
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
				GenDraw.DrawFieldEdges(hitboxCells, component.highlightColor, AltitudeLayer.MetaOverlays.AltitudeFor());
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
