using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Vehicles
{
	public class IndicatorDef : Def
	{
		private string iconPath;

		//TODO - add options for additional info panel

		public Texture2D Icon { get; private set; }

		public override void PostLoad()
		{
			if (!string.IsNullOrEmpty(iconPath))
			{
				LongEventHandler.ExecuteWhenFinished(delegate
				{
					Icon = ContentFinder<Texture2D>.Get(iconPath);
				});
			}
		}
	}
}
