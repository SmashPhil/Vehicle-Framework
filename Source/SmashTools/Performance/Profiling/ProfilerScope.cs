using System;

namespace SmashTools.Performance;

public readonly struct ProfilerScope : IDisposable
{
	private readonly Profiler.Timer timer;

	public ProfilerScope(string label)
	{
		timer = Profiler.Begin(label);
	}

	void IDisposable.Dispose()
	{
		Profiler.End(timer);
	}
}