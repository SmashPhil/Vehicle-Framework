using System.Collections;
using CoreLib.Performance;
using DevTools.Testing;

namespace CoreLib.Testing;

[TestFixture(TestType.MainMenu)]
[TestDescription("Timer util for delaying the invocation of some action.")]
[TestCategory(TestCategoryNames.Performance)]
internal class Test_Debouncer
{
	private const int DebounceDelaySeconds = 1; // s
	private const int DebounceDelayMs = DebounceDelaySeconds * 1000; // ms

	[Test]
	[TestDescription(
		"The debouncer does not begin ticking on construction and only starts counting down after Invoke() is called.")]
	private IEnumerator StartOnFirstInvoke()
	{
		TestObject obj = new();
		Debouncer debouncer = new(obj.Action, DebounceDelayMs);
		Expect.AreEqual(obj.InvocationCount, 0);
		Expect.AreApproximatelyEqual(debouncer.TimeRemaining, DebounceDelaySeconds);
		yield return null;
		Expect.AreApproximatelyEqual(debouncer.TimeRemaining, DebounceDelaySeconds);
		debouncer.Invoke();
		Expect.AreEqual(obj.InvocationCount, 0);
		yield return null;
		Expect.IsTrue(debouncer.TimeRemaining < DebounceDelaySeconds);
	}

	[Test]
	[TestDescription("The scheduled action fires exactly once when the debouncer expires.")]
	private IEnumerator InvokeOnExpiration()
	{
		// Frame takes ~16.7ms at 60fps so setting the debouncer to a low delay should guarantee execution.
		const int SingleFrame = 1; // ms

		TestObject obj = new();
		Debouncer debouncer = new(obj.Action, SingleFrame);
		debouncer.Invoke();
		yield return null;
		Expect.AreApproximatelyEqual(debouncer.TimeRemaining, 0);
		Expect.AreEqual(obj.InvocationCount, 1);
	}

	[Test]
	[TestDescription("After the first action invokes, a subsequent Invoke() resets and reschedules the debouncer.")]
	private IEnumerator InvokeAfterExpiration()
	{
		// Frame takes ~16.7ms at 60fps so setting the debouncer to a low delay should guarantee execution.
		const int SingleFrame = 1; // ms

		TestObject obj = new();
		Debouncer debouncer = new(obj.Action, SingleFrame);
		debouncer.Invoke();
		yield return null;
		Expect.AreApproximatelyEqual(debouncer.TimeRemaining, 0);
		Expect.AreEqual(obj.InvocationCount, 1);
		debouncer.Invoke();
		Expect.AreApproximatelyEqual(debouncer.TimeRemaining, SingleFrame / 1000f);
		Expect.AreEqual(obj.InvocationCount, 1);
		yield return null;
		Expect.AreApproximatelyEqual(debouncer.TimeRemaining, 0);
		Expect.AreEqual(obj.InvocationCount, 2);
	}

	[Test]
	[TestDescription(
		"Calling Invoke() while the debounce timer is still active resets the timer so the action does not fire early.")]
	private IEnumerator InvokeToReset()
	{
		TestObject obj = new();
		Debouncer debouncer = new(obj.Action, DebounceDelayMs);
		Expect.AreEqual(obj.InvocationCount, 0);
		Expect.AreApproximatelyEqual(debouncer.TimeRemaining, DebounceDelaySeconds);
		debouncer.Invoke();
		Expect.AreEqual(obj.InvocationCount, 0);
		yield return null;
		Expect.IsTrue(debouncer.TimeRemaining < DebounceDelaySeconds);
		debouncer.Invoke();
		Expect.AreApproximatelyEqual(debouncer.TimeRemaining, DebounceDelaySeconds);
		Expect.AreEqual(obj.InvocationCount, 0);
	}

	[Test]
	[TestDescription("Terminates and deschedules the debouncer without invoking the action.")]
	private IEnumerator Cancel()
	{
		TestObject obj = new();
		Debouncer debouncer = new(obj.Action, DebounceDelayMs);
		Expect.AreEqual(obj.InvocationCount, 0);
		Expect.AreApproximatelyEqual(debouncer.TimeRemaining, DebounceDelaySeconds);
		debouncer.Invoke();
		Expect.AreEqual(obj.InvocationCount, 0);
		yield return null;
		Expect.IsTrue(debouncer.TimeRemaining < DebounceDelaySeconds);
		debouncer.Cancel();
		Expect.AreEqual(obj.InvocationCount, 0);
		Expect.AreApproximatelyEqual(debouncer.TimeRemaining, 0);
		yield return null;
		Expect.AreEqual(obj.InvocationCount, 0);
		Expect.AreApproximatelyEqual(debouncer.TimeRemaining, 0);
	}

	private class TestObject
	{
		public int InvocationCount { get; private set; }

		public void Action()
		{
			InvocationCount++;
		}
	}
}