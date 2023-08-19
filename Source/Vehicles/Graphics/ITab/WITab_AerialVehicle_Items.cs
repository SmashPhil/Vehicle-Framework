using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using RimWorld.Planet;

namespace Vehicles
{
	public class WITab_AerialVehicle_Items : WITab_AerialVehicle
	{
		private const float SortersSpace = 25f;
		private const float AssignDrugPoliciesButtonHeight = 27f;

		private Vector2 scrollPosition;
		private float scrollViewHeight;
		private TransferableSorterDef sorter1;
		private TransferableSorterDef sorter2;
		private List<TransferableImmutable> cachedItems = new List<TransferableImmutable>();
		private int cachedItemsHash;
		private int cachedItemsCount;
		
		public WITab_AerialVehicle_Items()
		{
			labelKey = "TabCaravanItems";
		}

		protected override void FillTab()
		{
			CheckCreateSorters();
			Rect rect = new Rect(0f, 0f, size.x, size.y);
			//if (Widgets.ButtonText(new Rect(rect.x + 10f, rect.y + 10f, 200f, AssignDrugPoliciesButtonHeight), "AssignDrugPolicies".Translate(), true, true, true, null))
			{
				//Find.WindowStack.Add(new Dialog_AssignCaravanDrugPolicies(SelCaravan));
			}

			float ammoWeight = 0f;
			if (SelAerialVehicle.vehicle.CompVehicleTurrets != null)
			{
				foreach (VehicleTurret turret in SelAerialVehicle.vehicle.CompVehicleTurrets.turrets)
				{
					ammoWeight += turret.loadedAmmo is null ? 0f : turret.loadedAmmo.BaseMass * turret.shellCount;
				}
			}

			Rect massLabelRect = rect.ContractedBy(10);
			float mass = MassUtility.GearAndInventoryMass(SelAerialVehicle.vehicle) + ammoWeight;
			float capacity = SelAerialVehicle.vehicle.GetStatValue(VehicleStatDefOf.CargoCapacity);
			Widgets.Label(massLabelRect, "MassCarried".Translate(mass.ToString("0.##"), capacity.ToString("0.##")));

			rect.yMin += 37f;
			Widgets.BeginGroup(rect.ContractedBy(10f));
			TransferableUIUtility.DoTransferableSorters(sorter1, sorter2, delegate (TransferableSorterDef sorter)
			{
				sorter1 = sorter;
				CacheItems();
			}, delegate (TransferableSorterDef sorter)
			{
				sorter2 = sorter;
				CacheItems();
			});
			Widgets.EndGroup();
			rect.yMin += SortersSpace;
			Widgets.BeginGroup(rect);
			CheckCacheItems();
			AerialVehicleTabHelper.DoRows(rect.size, cachedItems, SelAerialVehicle, ref scrollPosition, ref scrollViewHeight);
			Widgets.EndGroup();
		}

		protected override void UpdateSize()
		{
			base.UpdateSize();
			CheckCacheItems();
			size = CaravanItemsTabUtility.GetSize(cachedItems, PaneTopY, true);
		}

		private void CheckCacheItems()
		{
			List<Thing> list = WorldHelper.AllInventoryItems(SelAerialVehicle);
			if (list.Count != cachedItemsCount)
			{
				CacheItems();
				return;
			}
			int num = 0;
			for (int i = 0; i < list.Count; i++)
			{
				num = Gen.HashCombineInt(num, list[i].GetHashCode());
			}
			if (num != cachedItemsHash)
			{
				CacheItems();
			}
		}

		private void CacheItems()
		{
			CheckCreateSorters();
			cachedItems.Clear();
			List<Thing> list = WorldHelper.AllInventoryItems(SelAerialVehicle);
			int seed = 0;
			for (int i = 0; i < list.Count; i++)
			{
				TransferableImmutable transferableImmutable = TransferableUtility.TransferableMatching(list[i], cachedItems, TransferAsOneMode.Normal);
				if (transferableImmutable == null)
				{
					transferableImmutable = new TransferableImmutable();
					cachedItems.Add(transferableImmutable);
				}
				transferableImmutable.things.Add(list[i]);
				seed = Gen.HashCombineInt(seed, list[i].GetHashCode());
			}
			cachedItems = cachedItems.OrderBy((TransferableImmutable tr) => tr, sorter1.Comparer).ThenBy((TransferableImmutable tr) => tr, sorter2.Comparer).ThenBy((TransferableImmutable tr) => TransferableUIUtility.DefaultListOrderPriority(tr)).ToList<TransferableImmutable>();
			cachedItemsCount = list.Count;
			cachedItemsHash = seed;
		}

		private void CheckCreateSorters()
		{
			if (sorter1 == null)
			{
				sorter1 = TransferableSorterDefOf.Category;
			}
			if (sorter2 == null)
			{
				sorter2 = TransferableSorterDefOf.MarketValue;
			}
		}
	}
}
