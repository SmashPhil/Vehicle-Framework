using System;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace Vehicles;

[PublicAPI]
public struct VehicleGenerationRequest
{
	public readonly VehicleDef vehicleDef;
	public Faction faction;

	public bool cleanSlate;

	public bool randomizeColors;
	public Color colorOne;
	public Color colorTwo;
	public Color colorThree;
	public float tiling;
	public Vector2 displacement;

	public VehicleGenerationRequest(VehicleDef vehicleDef)
	{
		this.vehicleDef = vehicleDef;
	}

	public VehicleGenerationRequest(VehicleDef vehicleDef, Faction faction) : this(vehicleDef)
	{
		this.faction = faction;
	}

	public VehicleGenerationRequest(VehicleDef vehicleDef, Faction faction,
		bool randomizeColors = false, bool randomizeMask = false, bool cleanSlate = true) : this(vehicleDef, faction)
	{
		this.randomizeColors = randomizeColors;
		this.randomizeColors |= randomizeMask;
		this.cleanSlate = cleanSlate;

		AssignForBackCompatibility();
	}

	public VehicleGenerationRequest(VehicleDef vehicleDef, Faction faction, Color colorOne, Color colorTwo,
		Color colorThree, float tiling, Vector2 displacement) : this(vehicleDef, faction)
	{
		this.colorOne = colorOne;
		this.colorTwo = colorTwo;
		this.colorThree = colorThree;
		this.tiling = tiling;
		this.displacement = displacement;
	}

	// TODO 1.6.2091
	[Obsolete("Deprecated - Use constructor instead.", error: true)]
	public VehicleDef VehicleDef { get; set; }

	// TODO 1.6.2091
	[Obsolete("Deprecated - Use constructor instead.")]
	public Faction Faction { get; set; }

	// TODO 1.6.2091
	[Obsolete("Deprecated - Use field or constructor instead.")]
	public bool RandomizeMask { get; set; }

	// TODO 1.6.2091
	[Obsolete("Deprecated - Use field or constructor instead.")]
	public int Upgrades { get; set; }

	// TODO 1.6.2091
	[Obsolete("Deprecated - Use field or constructor instead.")]
	public bool CleanSlate { get; set; }

	// TODO 1.6.2091
	[Obsolete("Deprecated - Use field or constructor instead.")]
	public Color ColorOne { get; set; }

	// TODO 1.6.2091
	[Obsolete("Deprecated - Use field or constructor instead.")]
	public Color ColorTwo { get; set; }

	// TODO 1.6.2091
	[Obsolete("Deprecated - Use field or constructor instead.")]
	public Color ColorThree { get; set; }

	// TODO 1.6.2091
	[Obsolete("Deprecated - Use field or constructor instead.")]
	public float Tiling { get; set; }

	// TODO 1.6.2091
	[Obsolete("Deprecated - Use field or constructor instead.")]
	public Vector2 Displacement { get; set; }

	private void AssignForBackCompatibility()
	{
		// TODO 1.6.2091
		#pragma warning disable CS0618
		faction ??= Faction;
		randomizeColors |= RandomizeMask;
		colorOne = ColorOne != Color.clear ? ColorOne : colorOne;
		colorTwo = ColorTwo != Color.clear ? ColorTwo : colorTwo;
		colorThree = ColorThree != Color.clear ? ColorThree : colorThree;
		tiling = Tiling != 0 ? Tiling : 0;
		displacement = Displacement != Vector2.zero ? Displacement : Vector2.zero;
		#pragma warning restore CS0618
	}

	public static (Color colorOne, Color colorTwo, Color colorThree) GetCompletelyRandomColors()
	{
		float r1 = Rand.Range(0.25f, .75f);
		float g1 = Rand.Range(0.25f, .75f);
		float b1 = Rand.Range(0.25f, .75f);
		Color colorOne = new(r1, g1, b1, 1);
		float r2 = Rand.Range(0.25f, .75f);
		float g2 = Rand.Range(0.25f, .75f);
		float b2 = Rand.Range(0.25f, .75f);
		Color colorTwo = new(r2, g2, b2, 1);
		float r3 = Rand.Range(0.25f, .75f);
		float g3 = Rand.Range(0.25f, .75f);
		float b3 = Rand.Range(0.25f, .75f);
		Color colorThree = new(r3, g3, b3, 1);

		return (colorOne, colorTwo, colorThree);
	}
}