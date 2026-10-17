using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using Verse;

namespace SmashTools.Performance;

[PublicAPI]
public static class GlobalObjectPool
{
  [MustDisposeResource]
  public static Receipt<T> Get<T>(out T obj) where T : new()
  {
    return new Receipt<T>(out obj);
  }

  [MustDisposeResource]
  public static StringBuilderReceipt Get(out StringBuilder stringBuilder)
  {
    return new StringBuilderReceipt(out stringBuilder);
  }

  [MustDisposeResource]
  public static CollectionReceipt<List<T>, T> Get<T>(out List<T> list)
  {
    return new CollectionReceipt<List<T>, T>(out list);
  }

  [MustDisposeResource]
  public static CollectionReceipt<HashSet<T>, T> Get<T>(out HashSet<T> set)
  {
    return new CollectionReceipt<HashSet<T>, T>(out set);
  }

  [MustDisposeResource]
  public static CollectionReceipt<Dictionary<K, V>, KeyValuePair<K, V>> Get<K, V>(out Dictionary<K, V> dictionary)
  {
    return new CollectionReceipt<Dictionary<K, V>, KeyValuePair<K, V>>(out dictionary);
  }

  [PublicAPI]
  public readonly struct Receipt<T> : IDisposable where T : new()
  {
    private readonly T obj;

    public Receipt(out T obj)
    {
      this.obj = SimplePool<T>.Get();
      obj = this.obj;
    }

    void IDisposable.Dispose()
    {
      SimplePool<T>.Return(obj);
    }
  }

  [PublicAPI]
  public readonly struct CollectionReceipt<C, T> : IDisposable where C : ICollection<T>, new()
  {
    private readonly C collection;

    public CollectionReceipt(out C collection)
    {
      this.collection = SimplePool<C>.Get();
      // We still need to clear it before use since other sources can taint the contents of SimplePool.
      this.collection.Clear();
      collection = this.collection;
    }

    void IDisposable.Dispose()
    {
      collection.Clear();
      SimplePool<C>.Return(collection);
    }
  }

  [PublicAPI]
  public readonly struct StringBuilderReceipt : IDisposable
  {
    private readonly StringBuilder stringBuilder;

    public StringBuilderReceipt(out StringBuilder stringBuilder)
    {
      this.stringBuilder = SimplePool<StringBuilder>.Get();
      stringBuilder = this.stringBuilder;
      stringBuilder.Clear();
    }

    void IDisposable.Dispose()
    {
      stringBuilder.Clear();
      SimplePool<StringBuilder>.Return(stringBuilder);
    }
  }
}