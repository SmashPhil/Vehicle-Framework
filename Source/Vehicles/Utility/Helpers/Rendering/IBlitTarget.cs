using System.Collections.Generic;
using SmashTools.Rendering;
using UnityEngine;
using Verse;

namespace Vehicles.Rendering;

/// <summary>
/// Represents a target that can be 'blitted' i.e. rendered into a texture based on a <see cref="BlitRequest"/>.
/// Typically used to snapshot a vehicle and draw it in UI widgets.
/// </summary>
public interface IBlitTarget
{
  /// <summary>
  /// Calculates the pixel dimensions of the texture needed to satisfy the given blit request.
  /// </summary>
  /// <param name="request">
  ///   The <see cref="BlitRequest"/> containing camera parameters, zoom level, and other
  ///   snapshot settings for the vehicle.
  /// </param>
  /// <returns>
  ///   A tuple (<c>width</c>, <c>height</c>) specifying the required texture size, in pixels.
  /// </returns>
  (int width, int height) TextureSize(in BlitRequest request);

  /// <summary>
  /// Produce one or more <see cref="RenderData"/> entries that describe how to draw the vehicle into the UI rect.
  /// </summary>
  /// <param name="rect">
  ///   The destination rect in UI coordinates where the vehicle snapshot should appear. This may be non‐square;
  ///   implementations should center and scale the graphic appropriately.
  /// </param>
  /// <param name="request">
  ///   The <see cref="BlitRequest"/> containing rotation, pattern/mask data, and other rendering parameters
  ///   for the snapshot.
  /// </param>
  /// <returns>
  ///   A sequence of <see cref="RenderData"/> structs, each describing:
  ///   <list type="bullet">
  ///     <item>
  ///       <description>
  ///         Which texture (base, mask, etc.) and material to draw.
  ///       </description>
  ///     </item>
  ///     <item>
  ///       <description>
  ///         The adjusted source‐to‐destination mapping (scale, offsets, UV transforms).
  ///       </description>
  ///     </item>
  ///     <item>
  ///       <description>
  ///         Any per‐instance <see cref="MaterialPropertyBlock"/> overrides needed for pattern tinting.
  ///       </description>
  ///     </item>
  ///   </list>
  /// </returns>
  IEnumerable<RenderData> GetRenderData(Rect rect, BlitRequest request);
}