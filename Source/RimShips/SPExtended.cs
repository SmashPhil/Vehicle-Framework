using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;

namespace RimShips
{
    public static class SPExtended
    {
        public static void GetList<T>(List<int> offsets, List<T> values, int index, List<T> outList)
        {
            outList.Clear();
            int num = offsets[index];
            int num2 = values.Count;
            if (index + 1 < offsets.Count)
            {
                num2 = offsets[index + 1];
            }

            for (int i = num; i < num2; i++)
            {
                outList.Add(values[i]);
            }
        }

        public static T PopRandom<T>(ref List<T> list)
        {
            System.Random rand = new System.Random();
            T item = list[rand.Next(0, list.Count)];
            list.Remove(item);
            return item;
        }

        public static List<T> ConvertToList<T>(this T typeObject)
        {
            return new List<T>() { typeObject };
        }

        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            if (val.CompareTo(max) > 0) return max;
            return val;
        }
        public static bool ContainsAllOfList<T>(this IEnumerable<T> sourceList, IEnumerable<T> searchingList)
        {
            if(sourceList is null || searchingList is null) return false;
            return sourceList.Intersect(searchingList).Any();
        }

        public static void ClampToMap(Pawn pawn, ref IntVec3 entryPoint, Map map, int extraOffset = 0)
        {
            int x = pawn.def.size.x;
            int z = pawn.def.size.z;
            int offset = x > z ? x+extraOffset : z+extraOffset;

            if (entryPoint.x < offset)
            {
                entryPoint.x = (int)(offset / 2);
            }
            else if (entryPoint.x >= (map.Size.x - (offset / 2)))
            {
                entryPoint.x = (int)(map.Size.x - (offset / 2));
            }
            if (entryPoint.z < offset)
            {
                entryPoint.z = (int)(offset / 2);
            }
            else if (entryPoint.z > (map.Size.z - (offset / 2)))
            {
                entryPoint.z = (int)(map.Size.z - (offset / 2));
            }
        }
        public static IntVec3 ClampToMap(this Pawn pawn, IntVec3 spawnPoint, Map map, int extraOffset = 0)
        {
            int x = pawn.def.size.x;
            int z = pawn.def.size.z;
            int offset = x > z ? x + extraOffset : z + extraOffset;
            if (spawnPoint.x < offset)
            {
                spawnPoint.x = (int)(offset / 2);
            }
            else if (spawnPoint.x >= (map.Size.x - Mathf.Ceil(offset / 2)))
            {
                spawnPoint.x = (int)(map.Size.x - (offset / 2));
            }
            if (spawnPoint.z < offset)
            {
                spawnPoint.z = (int)(offset / 2);
            }
            else if (spawnPoint.z > (map.Size.z - (offset / 2)))
            {
                spawnPoint.z = (int)(map.Size.z - (offset / 2));
            }
            return spawnPoint;
        }

        public static List<IntVec3> PawnOccupiedCells(this Pawn pawn, IntVec3 centerPoint)
        {
            int x = pawn.def.size.x % 2 == 0 ? pawn.def.size.x + 1 : pawn.def.size.x;
            int z = pawn.def.size.z % 2 == 0 ? pawn.def.size.z + 1 : pawn.def.size.z;

            List<IntVec3> pawnCells = new List<IntVec3>();
            //Edge case
            int xEdgeCase = centerPoint.x - ((x - 1) / 2);
            for(int i = 0; i < x; i++)
            {
                pawnCells.Add(new IntVec3(xEdgeCase + i, centerPoint.y, centerPoint.z));
            }
            //Upper half
            int xUpperhalf = centerPoint.x - ((x - 1) / 2);
            int zUpperhalf = centerPoint.z;
            for(int i = 0; i < x; i++)
            {
                for(int j = 1; j < (z - 1)/2; j++)
                {
                    pawnCells.Add(new IntVec3(xUpperhalf + i, centerPoint.y, xUpperhalf + j));
                }
            }
            //Lower half
            int xLowerhalf = centerPoint.x - ((x - 1) / 2);
            int zLowerhalf = centerPoint.z;
            for (int i = 0; i < x; i++)
            {
                for (int j = 1; j < (z - 1)/2; j++)
                {
                    pawnCells.Add(new IntVec3(xUpperhalf + i, centerPoint.y, xUpperhalf - j));
                }
            }
            return pawnCells;
        }

        public static Rot4 ClosestEdge(Pawn pawn, Map map)
        {
            IntVec2 mapSize = new IntVec2(map.Size.x, map.Size.z);
            IntVec2 position = new IntVec2(pawn.Position.x, pawn.Position.z);

            Pair<Rot4, int> hDistance = Math.Abs(position.x) < Math.Abs(position.x - mapSize.x) ? new Pair<Rot4, int>(Rot4.West, position.x) : new Pair<Rot4, int>(Rot4.East, Math.Abs(position.x - mapSize.x));
            Pair<Rot4, int> vDistance = Math.Abs(position.z) < Math.Abs(position.z - mapSize.z) ? new Pair<Rot4, int>(Rot4.South, position.z) : new Pair<Rot4, int>(Rot4.North, Math.Abs(position.z - mapSize.z));

            return hDistance.Second <= vDistance.Second ? hDistance.First : vDistance.First;
        }

        public static double DegreesToRadians(this double deg)
        {
            return deg * Math.PI / 180;
        }

        public static double DegreesToRadians(this float deg)
        {
            return Convert.ToDouble(deg).DegreesToRadians();
        }
    }

}
