using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace SmashTools
{
  /// <summary>
  /// Concurrent implementation of a limited size stack. If capacity is reached, stack will begin 'dropping' elements from the bottom.
  /// </summary>
  public class DropOutStack<T> : IEnumerable<T>
  {
    private readonly T[] items;
    private int top;

    private object lockObj = new();

    public DropOutStack(int capacity)
    {
      this.items = new T[capacity];
    }

    public int Count { get; private set; }

    public void Push(T item)
    {
      lock (lockObj)
      {
        items[top] = item;
        top = GenMath.PositiveMod(++top, items.Length);
        Count = Mathf.Clamp(++Count, 0, items.Length);
      }
    }

    public T Pop()
    {
      if (Count == 0)
      {
        throw new InvalidOperationException("Empty stack");
      }

      lock (lockObj)
      {
        top = GenMath.PositiveMod(items.Length + --top, items.Length);
        T item = items[top];
        items[top] = default;
        Count = Mathf.Clamp(--Count, 0, items.Length);
        return item;
      }
    }

    public bool TryPop(out T item)
    {
      if (Count == 0)
      {
        item = default;
        return false;
      }

      item = Pop();
      return true;
    }

    public T Peek()
    {
      return items[top];
    }

    public IEnumerator<T> GetEnumerator()
    {
      lock (lockObj)
      {
        for (int i = 0; i < Count; i++)
        {
          yield return items[i];
        }
      }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}