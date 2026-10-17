namespace CoreLib;

/// <summary>
/// Reference wrapper for tracking original reference values with readonly views.
/// </summary>
public class Ref<T>
{
  public T Value { get; set; }

  public ReadOnly View => new(this);

  public static implicit operator T(Ref<T> reference)
  {
    return reference.Value;
  }

  public readonly struct ReadOnly(Ref<T> reference)
  {
    public T Value => reference.Value;

    public static implicit operator T(ReadOnly view)
    {
      return view.Value;
    }
  }
}
