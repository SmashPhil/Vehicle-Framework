using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CoreLib.Performance;
using SmashTools;
using SmashTools.Rendering;
using UnityEngine;

namespace Vehicles.Rendering;

/// <summary>
/// Caches the GL-composed RenderTexture for a vehicle UI draw, keyed by everything that affects the
/// rendered image. A vehicle is blitted once per distinct visual state and the texture is reused until
/// it idles out, so <see cref="VehicleGui.DrawVehicleOnGUI"/> can draw it flat (un-rotated) every frame.
/// </summary>
internal static class VehicleGuiCache
{
	private const float IdlerTimeExpiry = 10f; // seconds

	private const float SweepInterval = 5f; // seconds

	private static readonly Dictionary<BlitCacheKey, RenderTextureIdler> Cache = [];

	private static readonly List<BlitCacheKey> SweepBuffer = [];

	private static bool sweepRegistered;
	private static float sweepTimer;

	/// <summary>
	/// Returns the idler whose <see cref="RenderTextureIdler.Read"/> holds the composed vehicle, blitting
	/// on a cache miss. Returns null only when the request resolves to a zero-sized texture.
	/// </summary>
	internal static RenderTextureIdler GetOrBlit(Rect rect, in BlitRequest request, float iconScale,
		bool forceCentering)
	{
		EnsureSweep();

		(int width, int height) = VehicleGui.GetOptimalTextureSize(rect, in request);
		if (width <= 0 || height <= 0)
			return null;

		BlitCacheKey key = KeyFor(in request, width, height, iconScale, forceCentering);
		if (Cache.TryGetValue(key, out RenderTextureIdler idler))
		{
			if (!idler.Disposed)
				return idler;
			Cache.Remove(key);
		}

		RenderTextureBuffer buffer = VehicleGui.CreateRenderTextureBuffer(rect, in request);
		idler = new RenderTextureIdler(buffer, IdlerTimeExpiry);
		VehicleGui.Blit(idler.GetWrite(), rect, in request, iconScale, forceCentering);
		Cache[key] = idler;
		return idler;
	}

	private static BlitCacheKey KeyFor(in BlitRequest request, int width, int height, float iconScale,
		bool forceCentering)
	{
		PatternData pattern = request.patternData;

		// Order-independent signature over the blit targets, so the cache re-blits when turrets or
		// overlays are added/removed (target identity covers per-vehicle instances).
		int targetSig = request.blitTargets.Count;
		foreach (IBlitTarget target in request.blitTargets)
			targetSig ^= RuntimeHelpers.GetHashCode(target);

		return new BlitCacheKey(
			request.vehicleDef?.shortHash ?? 0,
			request.rot,
			request.iconFrame,
			pattern?.color ?? Color.white,
			pattern?.colorTwo ?? Color.white,
			pattern?.colorThree ?? Color.white,
			pattern?.tiles ?? 1f,
			pattern?.displacement ?? Vector2.zero,
			pattern?.patternDef?.shortHash ?? 0,
			width,
			height,
			iconScale,
			forceCentering,
			targetSig);
	}

	private static void EnsureSweep()
	{
		if (sweepRegistered)
			return;
		sweepRegistered = true;
		UnityThread.StartUpdate(Sweep);
	}

	// Drops cache entries whose idler has freed its textures, bounding growth for keys never revisited.
	private static bool Sweep()
	{
		sweepTimer += Time.deltaTime;
		if (sweepTimer < SweepInterval)
			return true;
		sweepTimer = 0f;

		SweepBuffer.Clear();
		foreach (KeyValuePair<BlitCacheKey, RenderTextureIdler> entry in Cache)
		{
			if (entry.Value.Disposed)
				SweepBuffer.Add(entry.Key);
		}
		foreach (BlitCacheKey key in SweepBuffer)
			Cache.Remove(key);
		SweepBuffer.Clear();
		return true;
	}

	private readonly record struct BlitCacheKey(
		int VehicleDef,
		Rot8 Rot,
		bool IconFrame,
		Color Color,
		Color ColorTwo,
		Color ColorThree,
		float Tiles,
		Vector2 Displacement,
		int PatternDef,
		int Width,
		int Height,
		float IconScale,
		bool ForceCentering,
		int TargetSig);
}
