using System;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld.Planet;

namespace Vehicles.World;

[PublicAPI]
public class SplitInfo : ICaravanInfo
{
	private static readonly MethodInfo CountToTransferChangedMethod;

	private readonly Dialog_SplitCaravan splitCaravan;
	private readonly Caravan caravan;
	private readonly Action countToTransferChanged;

	static SplitInfo()
	{
		CountToTransferChangedMethod = AccessTools.Method(typeof(Dialog_SplitCaravan), "CountToTransferChanged");
	}

	public SplitInfo(Dialog_SplitCaravan splitCaravan, Caravan caravan)
	{
		this.splitCaravan = splitCaravan;
		this.caravan = caravan;

		countToTransferChanged =
			(Action)Delegate.CreateDelegate(typeof(Action), splitCaravan, CountToTransferChangedMethod);
	}

	bool ICaravanInfo.AllowSelectionOfAllVehicles => true;

	public void NotifyTransferablesChanged()
	{
		countToTransferChanged();
	}
}