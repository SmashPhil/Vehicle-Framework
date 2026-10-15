using System;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace CoreLib.Performance;

[PublicAPI]
public class AsyncLongOperationAction : AsyncAction
{
  public event Action OnInvoke;
  public event Func<bool> OnValidate;

  public override bool LongOperation => true;

  public override bool IsValid => OnInvoke != null && (OnValidate == null || OnValidate());

  public override void Invoke()
  {
    // ReSharper disable PossibleNullReferenceException
    // NOTE - AsyncAction should never be invoked if IsValid is false, so OnInvoke cannot be null
    // if the action reaches this point since it must be valid.
    Assert.IsTrue(IsValid);

    OnInvoke();
  }

  public override void ReturnToPool()
  {
    OnInvoke = null;
    OnValidate = null;
    AsyncPool<AsyncLongOperationAction>.Return(this);
  }
}