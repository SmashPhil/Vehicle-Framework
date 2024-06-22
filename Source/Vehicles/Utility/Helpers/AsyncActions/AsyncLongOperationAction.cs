using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using SmashTools;
using SmashTools.Performance;

namespace Vehicles
{
	public class AsyncLongOperationAction : AsyncAction
	{
		private DedicatedThread dedicatedThread;
		private bool value;

		public override bool IsValid => dedicatedThread.thread.IsAlive;

		public void Set(DedicatedThread dedicatedThread, bool value)
		{
			this.dedicatedThread = dedicatedThread;
			this.value = value;
		}

		public override void Invoke()
		{
			dedicatedThread.InLongOperation = value;
		}

		public override void ReturnToPool()
		{
			dedicatedThread = null;
			value = false;
			AsyncPool<AsyncLongOperationAction>.Return(this);
		}
	}
}
