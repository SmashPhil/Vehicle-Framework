using System;

namespace SmashTools
{
  public class Toggle
  {
    // Using name convention of compiler property names to clarify what these
    // represent for toggle state property.
    private readonly Func<bool> get_State;
    private readonly Action<bool> set_State;

    private readonly Action<bool> onToggle;

    public Toggle(string id, Func<bool> stateGetter = null, Action<bool> stateSetter = null,
      Action<bool> onToggle = null)
    {
      Id = id;
      DisplayName = id;
      Category = string.Empty;
      get_State = stateGetter;
      set_State = stateSetter;
      this.onToggle = onToggle;
    }

    public Toggle(string id, string category, Func<bool> stateGetter = null,
      Action<bool> stateSetter = null,
      Action<bool> onToggle = null)
    {
      Id = id;
      DisplayName = id;
      Category = category;
      get_State = stateGetter;
      set_State = stateSetter;
      this.onToggle = onToggle;
    }

    public Toggle(string id, string name, string category, Func<bool> stateGetter = null,
      Action<bool> stateSetter = null, Action<bool> onToggle = null)
    {
      Id = id;
      DisplayName = name;
      Category = category;
      get_State = stateGetter;
      set_State = stateSetter;
      this.onToggle = onToggle;
    }

    public string Id { get; private set; }

    public string DisplayName { get; private set; }

    public string Category { get; private set; }

    public bool Disabled { get; set; }

    public bool Active
    {
      get { return get_State?.Invoke() ?? false; }
      set
      {
        if (Active == value)
          return;

        set_State?.Invoke(value);
        onToggle?.Invoke(value);
      }
    }
  }
}