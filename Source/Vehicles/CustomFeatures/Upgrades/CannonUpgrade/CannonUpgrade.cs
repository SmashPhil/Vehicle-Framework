using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class CannonUpgrade : UpgradeNode
	{
		public Dictionary<VehicleTurret, VehicleRole> cannonUpgrades = new Dictionary<VehicleTurret, VehicleRole>();

		public Dictionary<VehicleTurret, VehicleHandler> cannonsUnlocked = new Dictionary<VehicleTurret, VehicleHandler>();

		public CannonUpgrade()
		{
		}

		public CannonUpgrade(VehiclePawn parent) : base(parent)
		{
		}

		public CannonUpgrade(CannonUpgrade reference, VehiclePawn parent) : base(reference, parent)
		{
			cannonUpgrades = reference.cannonUpgrades;
			foreach(KeyValuePair<VehicleTurret, VehicleRole> cu in cannonUpgrades)
			{
				VehicleTurret newTurret = CompCannons.CreateTurret(parent, cu.Key);
				VehicleHandler handler = new VehicleHandler(parent, cu.Value);
				cannonsUnlocked.Add(newTurret, handler);
			}
		}

		public override string UpgradeIdName => "CannonUpgrade";

		public override void OnInit()
		{
			base.OnInit();
			if (cannonsUnlocked is null)
			{
				cannonsUnlocked = new Dictionary<VehicleTurret, VehicleHandler>();
			}

			foreach(KeyValuePair<VehicleTurret,VehicleHandler> cannon in cannonsUnlocked)
			{
				if(cannon.Key.uniqueID < 0)
				{
					cannon.Key.uniqueID = Current.Game.GetCachedGameComponent<VehicleIdManager>().GetNextCannonId();
				}
				if(cannon.Value.uniqueID < 0)
				{
					cannon.Value.uniqueID = Current.Game.GetCachedGameComponent<VehicleIdManager>().GetNextHandlerId();
				}
			}
		}

		public override void Upgrade(VehiclePawn vehicle)
		{
			vehicle.CompCannons.AddCannons(cannonsUnlocked.Keys.ToList());
			vehicle.AddHandlers(cannonsUnlocked.Values.ToList());
		}

		public override void Refund(VehiclePawn vehicle)
		{
			vehicle.CompCannons.RemoveCannons(cannonsUnlocked.Keys.ToList());
			vehicle.RemoveHandlers(cannonsUnlocked.Values.ToList());
		}

		public override void DrawExtraOnGUI(Rect rect)
		{
			parent.DrawCannonTextures(rect, cannonsUnlocked.Keys.OrderBy(c => c.drawLayer), parent.pattern, true, parent.DrawColor, parent.DrawColorTwo, parent.DrawColorThree);
			parent.DrawCannonTextures(rect, parent.CompUpgradeTree.upgradeList.Where(x => x is CannonUpgrade && x.upgradeActive && !cannonsUnlocked.Keys.ToList().NullOrEmpty())
				.Cast<CannonUpgrade>().SelectMany(y => y.cannonsUnlocked.Keys).OrderBy(o => o.drawLayer), parent.pattern, true, parent.DrawColor, parent.DrawColorTwo, parent.DrawColorThree);
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref cannonsUnlocked, "cannonsUnlocked", LookMode.Deep, LookMode.Deep);
		}
	}
}
