using System;
using System.Linq;
using System.Reflection;
using System.Xml;
using HarmonyLib;
using JetBrains.Annotations;
using SmashTools.Xml;
using UnityEngine.Assertions;
using Verse;

namespace SmashTools;

[PublicAPI]
public abstract class DynamicDelegateBase
{
  internal const string ExactParamsName = "ExactParams";

  public MethodInfo method;
  public object[] args;

  private int runtimeArgs = -1;

  // Required public for reflection based instantiation from RimWorld xml parsing.
  // ReSharper disable once PublicConstructorInAbstractClass
  public DynamicDelegateBase()
  {
  }

  protected DynamicDelegateBase(MethodInfo method)
  {
    this.method = method;
    RecacheRuntimeArgCount();
    RecacheInjectedCount();
    LoadDefaultArgs();
  }

  public int InjectedCount { get; private set; }

  public int RuntimeArguments
  {
    get
    {
      if (runtimeArgs < 0)
      {
        RecacheRuntimeArgCount();
      }

      return runtimeArgs;
    }
  }

  private void RecacheRuntimeArgCount()
  {
    Type type = GetType();
    runtimeArgs = type.IsGenericType ? type.GetGenericArguments().Length : 0;
  }

  private void RecacheInjectedCount()
  {
    InjectedCount = 0;
    ParameterInfo[] parameters = method.GetParameters();
    for (int i = RuntimeArguments; i < parameters.Length; i++)
    {
      ParameterInfo parameter = parameters[i];
      // Arguments with prefix are injected arguments
      if (!parameter.Name.StartsWith("__"))
      {
        return; // Injected args must immediately follow any runtime arguments
      }
      InjectedCount++;
    }
  }

  private void LoadDefaultArgs()
  {
    ParameterInfo[] parameters = method.GetParameters();
    args = new object[parameters.Length];
    for (int i = RuntimeArguments + InjectedCount; i < parameters.Length; i++)
    {
      args[i] = parameters[i].ParameterType.GetDefaultValue();
    }
  }

  public void LoadDataFromXmlCustom([NotNull] XmlNode xmlNode)
  {
    string entry = xmlNode.InnerText;

    string[] methodInfoBody = entry.Split('(');
    if (methodInfoBody.NullOrEmpty())
    {
      Log.Error($"Unable to parse {GetType().Name} {xmlNode.Name}.\nText={entry}");
      return;
    }
    try
    {
      string[] array = methodInfoBody[0].Split('.');
      if (array.Length < 2)
      {
        Log.Error(
          "Unable to resolve method, too few arguments. Must include at least TypeName.MethodName");
        return;
      }
      string methodName = array[array.Length - 1];
      string typeName = string.Join(".", array, 0, array.Length - 1);

      Type type = GenTypes.GetTypeInAnyAssembly(typeName);
      method = AccessTools.Method(type, methodName);

      string argString = methodInfoBody[methodInfoBody.Length - 1].Replace(")", "");
      string[] argStrings = argString.Split(',');
      ParameterInfo[] parameters = method.GetParameters();

      if (argStrings.Length > parameters.Length)
      {
        Log.Error($"Number of parameters is less than number of args passed in. Xml={entry}");
        return;
      }
      Assert.IsNotNull(xmlNode.Attributes);
      bool exactParameters = false;
      if (xmlNode.Attributes[ExactParamsName] is { } exactParamsAttr)
      {
        exactParameters = bool.Parse(exactParamsAttr.Value.ToLowerInvariant());
      }

      RecacheRuntimeArgCount();
      RecacheInjectedCount();

      if (exactParameters && (argStrings.Length + RuntimeArguments) != parameters.Length)
      {
        Log.Error($"Number of parameters doesn't match number of args passed in. Xml={entry}");
      }

      LoadDefaultArgs();
      for (int i = RuntimeArguments + InjectedCount; i < parameters.Length; i++)
      {
        int argIndex = i - RuntimeArguments;
        if (argStrings.OutOfBounds(argIndex))
        {
          args[i] = Type.Missing; // Handles optional parameters
          continue;
        }

        string text = argStrings[argIndex];
        if (text.ToUpperInvariant() == "NULL" && (parameters[i].ParameterType.IsClass ||
          Nullable.GetUnderlyingType(parameters[i].ParameterType) != null))
        {
          args[i] = null;
        }
        else
        {
          args[i] = ParseArgument(parameters[i].ParameterType, text, i);
        }
      }
    }
    catch (IndexOutOfRangeException)
    {
      Log.Error($"Formatting error in {entry}. Unable to parse into resolved method.");
    }
  }

  private object ParseArgument(Type type, string entry, int index)
  {
    Type valueType = Nullable.GetUnderlyingType(type);
    if (valueType != null)
    {
      if (entry.NullOrEmpty())
      {
        return null;
      }

      return ParseHelper.FromString(entry, type);
    }

    if (ParseHelper.HandlesType(type))
    {
      return ParseHelper.FromString(entry, type);
    }

    if (type.IsSubclassOf(typeof(Def)))
    {
      if (!DelayedCrossRefResolver.Resolved)
      {
        DelayedCrossRefResolver.RegisterIndex(args, type, entry, index);
        return null;
      }

      return GenDefDatabase.GetDef(type, entry);
    }

    Log.ErrorOnce($"Unhandled type {type.Name} in ResolvedMethod arguments.", type.GetHashCode());
    return type.GetDefaultValue();
  }

  public override string ToString()
  {
    if (method == null)
      return string.Empty;

    string type = GenTypes.GetTypeNameWithoutIgnoredNamespaces(method.DeclaringType);
    string readout = $"{type}.{method.Name}";
    if (!args.NullOrEmpty())
    {
      readout += $"({string.Join(",", args.Select(obj => obj?.ToString() ?? "NULL"))})";
    }

    return readout;
  }

  public string ToStringSignature()
  {
    string readout = method.Name;
    if (!args.NullOrEmpty())
    {
      readout += $"( {string.Join(", ", args.Select(obj => obj.GetType()))} )";
    }
    return readout;
  }

  protected void InjectArguments(object[] injectedArgs)
  {
    if (InjectedCount > 0 && !injectedArgs.NullOrEmpty())
    {
      for (int i = RuntimeArguments; i < injectedArgs.Length; i++)
      {
        args[i] = injectedArgs[i];
      }
    }
  }
}