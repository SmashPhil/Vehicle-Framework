using System.Reflection;
using JetBrains.Annotations;

namespace SmashTools;

[PublicAPI]
public class DynamicDelegate : DynamicDelegateBase
{
  public DynamicDelegate()
  {
  }

  public DynamicDelegate(MethodInfo method) : base(method)
  {
  }

  public object Invoke(object obj, params object[] injectedArgs)
  {
    InjectArguments(injectedArgs);
    return method.Invoke(obj, args);
  }
}