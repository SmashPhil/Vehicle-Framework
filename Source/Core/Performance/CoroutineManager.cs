using System;
using System.Collections;
using System.Collections.Concurrent;
using JetBrains.Annotations;
using CoreLib.Performance;
using UnityEngine;
using UnityEngine.Assertions;

namespace CoreLib;

/// <summary>
/// Queue up tasks that require being on the MainThread
/// </summary>
/// <remarks>Can split up loops of logic across multiple frames or queue up actions from other threads to be executed on the MainThread</remarks>
[PublicAPI]
public class CoroutineManager : MonoBehaviour
{
	// Execution time to maintain max 1 fps impact converted from ms to seconds
	private const int MaxExecutionTimePerFrame = 1000 / (60 * 1000);

	private readonly ConcurrentQueue<Enumerator> enumerators = [];
	private float executionTimeElapsed;

	public bool Running { get; private set; }

	public bool NeedsRestart => !Running && enumerators.Count > 0;

	public static CoroutineManager Instance { get; private set; }

  public static void EnsureInitialized()
  {
		Assert.IsTrue(UnityThread.IsInMainThread);
    if (Instance == null)
    {
      Instance = InjectToScene();
    }
  }

	public static void QueueInvoke(Action action)
	{
		Instance.enumerators.Enqueue(new Enumerator(action));
		Instance.RunQueue();
	}

	public static void QueueInvoke(Func<IEnumerator> enumerator)
	{
		Instance.enumerators.Enqueue(new Enumerator(enumerator));
		Instance.RunQueue();
	}

	public static void QueueOrInvoke(Action action, float waitSeconds = 0)
	{
		if (waitSeconds > 0)
		{
			QueueInvoke(() => YieldForInvoking(action, waitSeconds));
		}
		else
		{
			action();
		}
	}

	public static void StartCoroutine(Func<IEnumerator> enumerator)
	{
		Instance.StartCoroutine(enumerator());
	}

	private static IEnumerator YieldForInvoking(Action action, float waitSeconds)
	{
		action();
		yield return new WaitForSeconds(waitSeconds);
	}

	public static IEnumerator YieldTillKeyDown(KeyCode keyCode)
	{
		while (!Input.GetKeyDown(keyCode))
		{
			yield return null;
		}
	}

	private void RunQueue()
	{
		if (NeedsRestart)
		{
			StartCoroutine(ExecuteQueue());
		}
	}

	public void Clear()
	{
		while (enumerators.Count > 0)
		{
			enumerators.TryDequeue(out _);
		}
	}

	/// <summary>
	/// Run enumerator methods or actions in queue taking maximum elapsed ms allowed.
	/// </summary>
	private IEnumerator ExecuteQueue()
	{
		Running = true;

		executionTimeElapsed = Time.realtimeSinceStartup;
		while (enumerators.TryDequeue(out Enumerator enumerator))
		{
			if (enumerator.Enumerate)
			{
				IEnumerator subEnumerator = enumerator.GetEnumerator();
				while (subEnumerator.MoveNext())
				{
					yield return subEnumerator.Current;
				}

				(subEnumerator as IDisposable)?.Dispose();
			}
			else
			{
				enumerator.Invoke();
			}

			if (executionTimeElapsed > Time.realtimeSinceStartup + MaxExecutionTimePerFrame)
			{
				executionTimeElapsed = Time.realtimeSinceStartup;
				yield return null;
			}
		}

		Running = false;
	}

	private static CoroutineManager InjectToScene()
	{
		GameObject gameObject = new("CoroutineManager");
		CoroutineManager manager = gameObject.AddComponent<CoroutineManager>();
		DontDestroyOnLoad(gameObject);
		return manager;
	}

	private class Enumerator
	{
		private readonly Action action;
		private readonly Func<IEnumerator> enumerator;

		public Enumerator(Func<IEnumerator> enumerator)
		{
			this.enumerator = enumerator;
		}

		public Enumerator(Action action)
		{
			this.action = action;
		}

		public bool Enumerate => enumerator != null;

		public IEnumerator GetEnumerator()
		{
			return enumerator();
		}

		public void Invoke()
		{
			action();
		}

		public override string ToString()
		{
			return Enumerate ? enumerator.Method.Name : action.Method.Name;
		}
	}
}