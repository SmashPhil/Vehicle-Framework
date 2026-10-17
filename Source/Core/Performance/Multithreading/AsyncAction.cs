using System;
using JetBrains.Annotations;

namespace CoreLib.Performance;

[PublicAPI]
public abstract class AsyncAction
{
  public virtual bool LongOperation => false;

  public virtual bool IsValid => true;

  public abstract void Invoke();

  public abstract void ReturnToPool();

  public virtual void ExceptionThrown(Exception ex)
  {
  }
}