using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace Vehicles
{
    public class WorldOceanReachability : WorldComponent
    {
        public WorldOceanReachability(World world) : base(world)
		{
            this.world = world;
			fields = new int[Find.WorldGrid.TilesCount];
			nextFieldID = 1;
			InvalidateAllFields();
		}

		public void ClearCache()
		{
			InvalidateAllFields();
		}

		public bool CanReach(Caravan c, int tile)
		{
			return CanReach(c.Tile, tile);
		}

        public bool CanReach(int startTile, int destTile)
        {
            if(ShipHarmony.currentFormingCaravan != null || ( (Find.WorldSelector.SelectedObjects.Any() && Find.WorldSelector.SelectedObjects.All(x => x is Caravan && (x as Caravan).IsPlayerControlled && 
                HelperMethods.HasBoat(x as Caravan))) && !ShipHarmony.routePlannerActive) )
            {
                List<Pawn> pawns = ShipHarmony.currentFormingCaravan is null ? (Find.WorldSelector.SingleSelectedObject as Caravan)?.PawnsListForReading : 
                    TransferableUtility.GetPawnsFromTransferables(ShipHarmony.currentFormingCaravan.transferables);
                if (pawns != null && HelperMethods.HasBoat(pawns))
                {
                    if(ShipHarmony.currentFormingCaravan is null && !pawns.All(x => HelperMethods.IsBoat(x)))
                    {
                        if(HelperMethods.AbleToEmbark(pawns))
                        {
                            HelperMethods.BoardAllCaravanPawns(Find.WorldSelector.SingleSelectedObject as Caravan);
                        }
                        else
                        {
                            Messages.Message("CantMoveDocked".Translate(), MessageTypeDefOf.RejectInput, false);
                            return false;
                        }
                    }
                    if(ShipHarmony.currentFormingCaravan != null || fields is null)
                    {
                        FloodFillAt(startTile);
                    }
                    if (startTile < 0 || destTile >= fields.Length || destTile < 0 || destTile >= fields.Length)
                        return false;
                    if (!HelperMethods.IsWaterTile(startTile, pawns) || !HelperMethods.IsWaterTile(destTile, pawns))
                        return false;
                    if(fields[startTile] == impassableFieldID || fields[destTile] == impassableFieldID)
                        return false;

                    if (IsValidField(startTile) || IsValidField(destTile))
                    {
                        return fields[startTile] == fields[destTile] || (HelperMethods.IsWaterTile(startTile, pawns) && HelperMethods.IsWaterTile(destTile, pawns));
                    }
                    return fields[startTile] != impassableFieldID && fields[startTile] == fields[destTile];
                }
            }
            if(ShipHarmony.routePlannerActive)
            {
                if (startTile < 0 || destTile >= fields.Length || destTile < 0 || destTile >= fields.Length)
                    return false;
                if ((fields[startTile] == impassableFieldID && !HelperMethods.WaterCovered(startTile)) || (fields[destTile] == impassableFieldID && !HelperMethods.WaterCovered(destTile)))
                    return false;
                FloodFillAt(startTile);
                if (IsValidField(startTile) || IsValidField(destTile))
                {
                    return fields[startTile] == fields[destTile] || ( (HelperMethods.WaterCovered(startTile) || Find.World.CoastDirectionAt(startTile).IsValid) && 
                        (HelperMethods.WaterCovered(destTile) || Find.World.CoastDirectionAt(destTile).IsValid) );
                }
                return ((fields[startTile] != impassableFieldID) && fields[startTile] == fields[destTile]) || (HelperMethods.WaterCovered(startTile) && HelperMethods.WaterCovered(destTile)) ||
                    (Find.World.CoastDirectionAt(startTile).IsValid && HelperMethods.WaterCovered(destTile));
            }
            return true;
        }
		private void InvalidateAllFields()
		{
			if (nextFieldID == int.MaxValue)
			{
				nextFieldID = 1;
			}
			minValidFieldID = nextFieldID;
			impassableFieldID = nextFieldID;
			nextFieldID++;
		}

		private bool IsValidField(int fieldID)
		{
			return fieldID >= minValidFieldID;
		}

		private void FloodFillAt(int tile)
		{
            if (fields is null) fields = new int[Find.WorldGrid.TilesCount];
            World world = Find.World;
            if(HelperMethods.BoatCantTraverse(tile))
            {
                fields[tile] = impassableFieldID;
            }
            int tmpID = nextFieldID;
            Find.WorldFloodFiller.FloodFill(tile, (int x) => !HelperMethods.BoatCantTraverse(x), delegate (int x)
            {
                fields[x] = tmpID;
            }, int.MaxValue, null);
            nextFieldID++;
		}

		private int[] fields;

		private int nextFieldID;

		private int impassableFieldID;

		private int minValidFieldID;
    }
}
