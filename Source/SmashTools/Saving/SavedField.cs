using System;
using System.Collections.Generic;
using System.Globalization;
using HarmonyLib;
using Verse;

namespace SmashTools;

public struct SavedField<T1> : IEquatable<SavedField<T1>>, INestedType
{
  private T1 value;
  private T1 endValue;

  public SavedField(T1 value)
  {
    this.value = value;
    endValue = value;
  }

  public SavedField(T1 value, T1 endValue)
  {
    this.value = value;
    this.endValue = endValue;
  }

  public Type InnerType => value.GetType();

  public T1 First
  {
    get { return value; }
    set
    {
      if (value is T1)
      {
        this.value = value;
        return;
      }
      Log.Error("Tried to assign value of different type to SavedField. T1: " + typeof(T1) +
        " value: " + value.GetType());
    }
  }

  public T1 EndValue
  {
    get { return endValue; }
    set
    {
      if (value is T1)
      {
        endValue = value;
        return;
      }
      Log.Error("Tried to assign value of different type to SavedField. T2: " + typeof(T1) +
        " value: " + value.GetType());
    }
  }

  public override string ToString()
  {
    return $"({value},{endValue})";
  }

  public static object FromTypedString(string entry, Type objType)
  {
    entry = entry.TrimStart(new char[] { '(' }).TrimEnd(new char[] { ')' });
    string[] data = entry.Split(new char[] { ',' });

    try
    {
      CultureInfo invariantCulture = CultureInfo.InvariantCulture;
      object value1 = AccessTools
       .Method(typeof(ParseHelper), nameof(ParseHelper.FromString),
          new Type[] { typeof(string), typeof(Type) })
       .Invoke(null, new object[] { data[0], objType });
      object value2 = AccessTools
       .Method(typeof(ParseHelper), nameof(ParseHelper.FromString),
          new Type[] { typeof(string), typeof(Type) })
       .Invoke(null, new object[] { data[1], objType });
      return new SavedField<object>(value1, value2);
    }
    catch (Exception ex)
    {
      Log.Error($"{entry} is not a valid SavedField format. Exception: {ex}");
      return new SavedField<object>(default);
    }
  }

  public override int GetHashCode()
  {
    int hash = 0;
    if (First is object)
    {
      hash = EqualityComparer<T1>.Default.GetHashCode(First);
    }
    if (EndValue is object)
    {
      hash = (hash << 3) + hash ^ EqualityComparer<T1>.Default.GetHashCode(EndValue);
    }
    return hash;
  }

  public override bool Equals(object obj)
  {
    return obj is SavedField<T1> field && Equals(field);
  }

  public bool Equals(SavedField<T1> other)
  {
    return EqualityComparer<T1>.Default.Equals(value, other.value) &&
      EqualityComparer<T1>.Default.Equals(endValue, other.endValue);
  }

  public static bool operator ==(SavedField<T1> lhs, SavedField<T1> rhs)
  {
    return lhs.Equals(rhs);
  }

  public static bool operator !=(SavedField<T1> lhs, SavedField<T1> rhs)
  {
    return !(lhs == rhs);
  }
}