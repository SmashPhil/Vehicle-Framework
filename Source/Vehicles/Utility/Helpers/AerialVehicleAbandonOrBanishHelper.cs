using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace Vehicles
{
	public static class AerialVehicleAbandonOrBanishHelper
	{
		public static void TryAbandonOrBanishViaInterface(Thing thing, AerialVehicleInFlight aerialVehicle)
		{
			Pawn pawn = thing as Pawn;
			if (pawn == null)
			{
				Dialog_MessageBox window = Dialog_MessageBox.CreateConfirmation("ConfirmAbandonItemDialog".Translate(thing.Label), delegate
				{
					Pawn ownerOf = GetOwnerOf(aerialVehicle, thing);
					if (ownerOf == null)
					{
						Log.Error($"Could not find owner of {thing}");
						return;
					}
					thing.Notify_AbandonedAtTile(aerialVehicle.Tile);
					ownerOf.inventory.innerContainer.Remove(thing);
					thing.Destroy(DestroyMode.Vanish);
				}, true, null, WindowLayer.Dialog);
				Find.WindowStack.Add(window);
				return;
			}
			if (!aerialVehicle.vehicle.AllCapablePawns.Any((Pawn innerPawn) => innerPawn != pawn && !innerPawn.NonHumanlikeOrWildMan()))
			{
				Messages.Message("MessageCantBanishLastColonist".Translate(), aerialVehicle, MessageTypeDefOf.RejectInput, historical: false);
				return;
			}
			PawnBanishUtility.ShowBanishPawnConfirmationDialog(pawn);
		}

		public static void TryAbandonOrBanishViaInterface(TransferableImmutable transferable, AerialVehicleInFlight aerialVehicle)
		{
			if (transferable.AnyThing is Pawn pawn)
			{
				TryAbandonOrBanishViaInterface(pawn, aerialVehicle);
				return;
			}
			Dialog_MessageBox window = Dialog_MessageBox.CreateConfirmation("ConfirmAbandonItemDialog".Translate(transferable.LabelWithTotalStackCount), delegate
			{
				for (int i = 0; i < transferable.things.Count; i++)
				{
					Thing thing = transferable.things[i];
					Pawn ownerOf = GetOwnerOf(aerialVehicle, thing);
					if (ownerOf == null)
					{
						Log.Error("Could not find owner of " + thing);
						return;
					}
					thing.Notify_AbandonedAtTile(aerialVehicle.Tile);
					ownerOf.inventory.innerContainer.Remove(thing);
					thing.Destroy(DestroyMode.Vanish);
				}
			}, true, null, WindowLayer.Dialog);
			Find.WindowStack.Add(window);
		}

		public static void TryAbandonSpecificCountViaInterface(Thing thing, AerialVehicleInFlight aerialVehicle)
		{
			Find.WindowStack.Add(new Dialog_Slider("AbandonSliderText".Translate(thing.LabelNoCount), 1, thing.stackCount, delegate (int x)
			{
				Pawn ownerOf = GetOwnerOf(aerialVehicle, thing);
				if (ownerOf == null)
				{
					Log.Error($"Could not find owner of {thing}");
					return;
				}
				if (x >= thing.stackCount)
				{
					thing.Notify_AbandonedAtTile(aerialVehicle.Tile);
					ownerOf.inventory.innerContainer.Remove(thing);
					thing.Destroy(DestroyMode.Vanish);
				}
				else
				{
					Thing thingSplit = thing.SplitOff(x);
					thingSplit.Notify_AbandonedAtTile(aerialVehicle.Tile);
					thingSplit.Destroy(DestroyMode.Vanish);
				}
			}, int.MinValue, 1f));
		}

		public static void TryAbandonSpecificCountViaInterface(TransferableImmutable transferable, AerialVehicleInFlight aerialVehicle)
		{
			Find.WindowStack.Add(new Dialog_Slider("AbandonSliderText".Translate(transferable.Label), 1, transferable.TotalStackCount, delegate (int x)
			{
				int num = x;
				int num2 = 0;
				while (num2 < transferable.things.Count && num > 0)
				{
					Thing thing = transferable.things[num2];
					Pawn ownerOf = GetOwnerOf(aerialVehicle, thing);
					if (ownerOf == null)
					{
						Log.Error("Could not find owner of " + thing);
						return;
					}
					if (num >= thing.stackCount)
					{
						num -= thing.stackCount;
						thing.Notify_AbandonedAtTile(aerialVehicle.Tile);
						ownerOf.inventory.innerContainer.Remove(thing);
						thing.Destroy(DestroyMode.Vanish);
					}
					else
					{
						Thing thing2 = thing.SplitOff(num);
						thing2.Notify_AbandonedAtTile(aerialVehicle.Tile);
						thing2.Destroy(DestroyMode.Vanish);
						num = 0;
					}
					num2++;
				}
			}, int.MinValue, 1f));
		}

		public static string GetAbandonOrBanishButtonTooltip(Thing thing, bool abandonSpecificCount)
		{
			if (thing is Pawn pawn)
			{
				return PawnBanishUtility.GetBanishButtonTip(pawn);
			}
			return GetAbandonItemButtonTooltip(thing.stackCount, abandonSpecificCount);
		}

		public static string GetAbandonOrBanishButtonTooltip(TransferableImmutable transferable, bool abandonSpecificCount)
		{
			if (transferable.AnyThing is Pawn pawn)
			{
				return PawnBanishUtility.GetBanishButtonTip(pawn);
			}
			return GetAbandonItemButtonTooltip(transferable.TotalStackCount, abandonSpecificCount);
		}

		private static string GetAbandonItemButtonTooltip(int currentStackCount, bool abandonSpecificCount)
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (currentStackCount == 1)
			{
				stringBuilder.AppendLine("AbandonTip".Translate());
			}
			else if (abandonSpecificCount)
			{
				stringBuilder.AppendLine("AbandonSpecificCountTip".Translate());
			}
			else
			{
				stringBuilder.AppendLine("AbandonAllTip".Translate());
			}
			stringBuilder.AppendLine();
			stringBuilder.Append("AbandonItemTipExtraText".Translate());
			return stringBuilder.ToString();
		}

		public static Pawn GetOwnerOf(AerialVehicleInFlight aerialVehicle, Thing item)
		{
			IThingHolder parentHolder = item.ParentHolder;
			if (parentHolder is Pawn_InventoryTracker)
			{
				Pawn pawn = (Pawn)parentHolder.ParentHolder;
				if (pawn == aerialVehicle.vehicle || aerialVehicle.vehicle.AllPawnsAboard.Contains(pawn))
				{
					return pawn;
				}
			}
			return null;
		}
	}
}
