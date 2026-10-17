using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using static SmashTools.Performance.Profiler;

namespace SmashTools.Performance;

[PublicAPI]
public static class Ext_Profiler
{
	private static readonly double SecondsThreshold = 0.1 * Stopwatch.Frequency;
	private static readonly double MillisecondThreshold = 0.0001 * Stopwatch.Frequency;
	private static readonly double MicrosecondThreshold = 0.0000001 * Stopwatch.Frequency;

	public static double ConvertTo(this double ticks, Measurement measurement)
	{
		return measurement switch
		{
			Measurement.Seconds      => ToSeconds(ticks),
			Measurement.Milliseconds => ToMilliseconds(ticks),
			Measurement.Microseconds => ToMicroseconds(ticks),
			Measurement.Nanoseconds  => ToNanoseconds(ticks),
			_                        => throw new NotImplementedException(nameof(Measurement)),
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double ToSeconds(double ticks)
	{
		return ticks / Stopwatch.Frequency;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double ToMilliseconds(double ticks)
	{
		return ticks * 1000 / Stopwatch.Frequency;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double ToMicroseconds(double ticks)
	{
		return ticks * 1_000_000 / Stopwatch.Frequency;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double ToNanoseconds(double ticks)
	{
		return ticks * 1_000_000_000 / Stopwatch.Frequency;
	}

	public static double ToNearestMeasurement(this double ticks, out Measurement measurement)
	{
		double absTicks = Math.Abs(ticks);
		if (absTicks >= SecondsThreshold)
		{
			measurement = Measurement.Seconds;
			return ToSeconds(ticks);
		}
		if (absTicks >= MillisecondThreshold)
		{
			measurement = Measurement.Milliseconds;
			return ToMilliseconds(ticks);
		}
		// Lowest measurement for profiler is 0.1us, anything lower than that wasn't worth instrumenting and
		// likely has more overhead from the stopwatch than from the method body itself.
		measurement = Measurement.Microseconds;
		return ToMicroseconds(ticks);
	}

	public static string ToMeasurementString(double ticks)
	{
		double rounded = ToNearestMeasurement(ticks, out Measurement measurement);
		return $"{rounded:#0.#} {MeasurementSuffix(measurement)}";

		static string MeasurementSuffix(Measurement measurement)
		{
			return measurement switch
			{
				Measurement.Seconds      => "s",
				Measurement.Milliseconds => "ms",
				Measurement.Microseconds => "us",
				Measurement.Nanoseconds  => "ns",
				_                        => throw new NotImplementedException(),
			};
		}
	}
}