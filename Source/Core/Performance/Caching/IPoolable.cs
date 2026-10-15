using JetBrains.Annotations;

namespace CoreLib.Performance;

/// <summary>
/// Interface for objects that can be managed by <see cref="IObjectPool"/>
/// </summary>
[PublicAPI]
public interface IPoolable
{
	/// <summary>
	/// Object is currently in the object pool
	/// </summary>
	bool InPool { get; set; }

	/// <summary>
	/// Clear all references as object is being returned to pool
	/// </summary>
  void Reset();
}