using JetBrains.Annotations;
using SmashTools;
using Verse;

namespace Vehicles;

[PublicAPI]
public class ComponentRequirement
{
	private string key;
	private float healthPercent;
	private ComparisonType comparison = ComparisonType.GreaterThan;

	public VehicleComponent Component { get; private set; }

	public string Label => Component.props.label;

	public bool MeetsRequirements { get; private set; } = true;

	public void RecacheComponent(VehiclePawn vehicle)
	{
		Component = vehicle.statHandler.GetComponent(key);
	}

	public void RegisterEvents(VehiclePawn vehicle)
	{
		vehicle.RemoveEvent(VehicleEventDefOf.HealthChanged, OnHealthChanged);
		vehicle.AddEvent(VehicleEventDefOf.HealthChanged, OnHealthChanged);
	}

	private void OnHealthChanged()
	{
		MeetsRequirements =
			Component != null && comparison.Compare(Component.HealthPercent, healthPercent);
	}

	public static ComponentRequirement CopyFrom(ComponentRequirement reference)
	{
		return new ComponentRequirement
		{
			key = reference.key,
			healthPercent = reference.healthPercent,
			comparison = reference.comparison,
		};
	}
}