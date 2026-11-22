using System;
using JetBrains.Annotations;

namespace CoreLib.Rendering;

/// <summary>
/// Checks whether cells in a flattened grid block a ray.
/// </summary>
[PublicAPI]
public interface ICellBlocker
{
  /// <summary>
  /// Checks whether the cell at <paramref name="index"/> blocks a ray.
  /// </summary>
  /// <param name="index">The cell index, calculated as y * map width + x.</param>
  /// <returns><see langword="true"/> if the cell blocks the ray.</returns>
  bool IsBlocked(int index);
}

/// <summary>
/// Casts rays from a fixed origin across a grid.
/// </summary>
/// <typeparam name="TBlocker">Value type used to check blocked cells.</typeparam>
[PublicAPI]
public struct RayCast<TBlocker> where TBlocker : struct, ICellBlocker
{
  private readonly int originX;
  private readonly int originY;
  private readonly int mapWidth;
  private TBlocker blocker;

  /// <summary>
  /// Initializes a ray cast from the given origin cell.
  /// </summary>
  /// <param name="x">The origin cell's x coordinate.</param>
  /// <param name="y">The origin cell's y coordinate.</param>
  /// <param name="settings">Grid width and cell blocker for the ray cast.</param>
  public RayCast(int x, int y, in Settings settings)
  {
    originX = x;
    originY = y;
    mapWidth = settings.mapWidth;
    blocker = settings.blocker;
  }

  private bool IsBlocked(int rayX, int rayY, int previousX, int previousY)
  {
    if (blocker.IsBlocked(rayY * mapWidth + rayX))
      return true;

    return rayX != previousX && rayY != previousY &&
           blocker.IsBlocked(previousY * mapWidth + rayX) &&
           blocker.IsBlocked(rayY * mapWidth + previousX);
  }

  /// <summary>
  /// Checks whether a ray reaches the target cell without hitting a blocker.
  /// </summary>
  /// <param name="x">The target cell's x coordinate.</param>
  /// <param name="y">The target cell's y coordinate.</param>
  /// <returns><see langword="true"/> if the ray reaches the target cell; otherwise, <see langword="false"/>.</returns>
  /// <remarks>
  /// The origin cell is not checked. A diagonal step is blocked when both adjacent side cells are blocked.
  /// </remarks>
  public bool Hit(int x, int y)
  {
    int offsetX = x - originX;
    int offsetY = y - originY;
    int rayX = originX;
    int rayY = originY;
    int deltaX = Math.Abs(offsetX);
    int deltaY = Math.Abs(offsetY);
    int stepX = Math.Sign(offsetX);
    int stepY = Math.Sign(offsetY);
    int error = deltaX - deltaY;
    bool hit = true;
    while (rayX != x || rayY != y)
    {
      int previousX = rayX;
      int previousY = rayY;
      int twiceError = 2 * error;
      if (twiceError > -deltaY)
      {
        error -= deltaY;
        rayX += stepX;
      }
      if (twiceError < deltaX)
      {
        error += deltaX;
        rayY += stepY;
      }
      if (IsBlocked(rayX, rayY, previousX, previousY))
      {
        hit = false;
        break;
      }
    }
    return hit;
  }

  /// <summary>
  /// Settings for grid ray casting.
  /// </summary>
  public struct Settings
  {
    /// <summary>
    /// Width used to convert cell coordinates to indices.
    /// </summary>
    public required int mapWidth;

    /// <summary>
    /// Cell blocker used to check each ray step.
    /// </summary>
    public required TBlocker blocker;
  }
}
