using System.Reflection;
using JetBrains.Annotations;

namespace SmashTools;

[PublicAPI]
public class DynamicDelegate<T> : DynamicDelegateBase
{
  public DynamicDelegate()
  {
  }

  public DynamicDelegate(MethodInfo method) : base(method)
  {
  }

  public object Invoke(object obj, T param, params object[] injectedArgs)
  {
    InjectArguments(injectedArgs);
    args[0] = param;
    return method.Invoke(obj, args);
  }
}