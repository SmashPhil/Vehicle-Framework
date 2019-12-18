using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld.Planet;
using UnityEngine;

namespace SPExtendedLibrary
{
    [StaticConstructorOnStartup]
    public static class SPExtended
    {
        /// <summary>SmashPhil's implementation of Tuples
        /// <para>Create a Tuple in .Net3.5 that functions the same as Tuple while avoiding boxing.</para>
        /// <seealso cref="SPTuple{T1, T2}"/>
        /// <seealso cref="SPTuple{T1, T2, T3}"/>
        /// </summary>
        public static class SPTuple
        {
            public static SPTuple<T1, T2> Create<T1, T2>(T1 first, T2 second)
            {
                return new SPTuple<T1, T2>(first, second);
            }

            public static SPTuple<T1, T2, T3> Create<T1, T2, T3>(T1 first, T2 second, T3 third)
            {
                return new SPTuple<T1, T2, T3>(first, second, third);
            }
        }
        /// <summary>
        /// SPTuple of 2 Types
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        public class SPTuple<T1, T2>
        {
            public T1 First { get; set; }
            public T2 Second { get; set; }
            internal SPTuple(T1 first, T2 second)
            {
                First = first;
                Second = second;
            }

            public static bool operator ==(SPTuple<T1, T2> o1, SPTuple<T1, T2> o2) { return o1.Equals(o2); }
            public static bool operator !=(SPTuple<T1,T2> o1, SPTuple<T1, T2> o2) { return !(o1 == o2); }

            private static readonly IEqualityComparer<T1> FirstComparer = EqualityComparer<T1>.Default;
            private static readonly IEqualityComparer<T2> SecondComparer = EqualityComparer<T2>.Default;

            public override int GetHashCode()
            {
                int hash = 0;
                if(First is object)
                    hash = FirstComparer.GetHashCode(First);
                if(Second is object)
                    hash = (hash << 5) + hash ^ SecondComparer.GetHashCode(Second);
                return hash;
            }

            public override bool Equals(object o)
            {
                SPTuple<T1, T2> o2 = o as SPTuple<T1, T2>;
                if (object.ReferenceEquals(o2, null))
                    return false;
                return FirstComparer.Equals(First, o2.First) && SecondComparer.Equals(Second, o2.Second);
            }
        }
        /// <summary>
        /// SPTuple of 3 types
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        public class SPTuple<T1, T2, T3> : SPTuple<T1,T2>
        {
            public T3 Third { get; set; }
            internal SPTuple(T1 first, T2 second, T3 third) : base(first, second)
            {
                Third = third;
            }
            public static bool operator ==(SPTuple<T1, T2, T3> o1, SPTuple<T1, T2, T3> o2) { return o1.Equals(o2); }
            public static bool operator !=(SPTuple<T1, T2, T3> o1, SPTuple<T1, T2, T3> o2) { return !(o1 == o2); }

            private static readonly IEqualityComparer<T1> FirstComparer = EqualityComparer<T1>.Default;
            private static readonly IEqualityComparer<T2> SecondComparer = EqualityComparer<T2>.Default;
            private static readonly IEqualityComparer<T3> ThirdComparer = EqualityComparer<T3>.Default;

            public override int GetHashCode()
            {
                int hash = 0;
                if(First is object)
                    hash = FirstComparer.GetHashCode(First);
                if(Second is object)
                    hash = (hash << 3) + hash ^ SecondComparer.GetHashCode(Second);
                if(Third is object)
                    hash = (hash << 5) + hash ^ ThirdComparer.GetHashCode(Third);
                return hash;
            }

            public override bool Equals(object o)
            {
                SPTuple<T1, T2, T3> o2 = o as SPTuple<T1, T2, T3>;
                if (object.ReferenceEquals(o2, null))
                    return false;
                return FirstComparer.Equals(First, o2.First) && SecondComparer.Equals(Second, o2.Second) && ThirdComparer.Equals(Third, o2.Third);
            }
        }

        /// <summary>
        /// Get neighbors of Tile on world map.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="offsets"></param>
        /// <param name="values"></param>
        /// <param name="index"></param>
        /// <param name="outList"></param>
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

        /// <summary>
        /// Pop random value from List
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static T PopRandom<T>(ref List<T> list)
        {
            if(list is null || !list.Any())
                return default(T);
            System.Random rand = new System.Random();
            T item = list[rand.Next(0, list.Count)];
            list.Remove(item);
            return item;
        }

        /// <summary>
        /// Grab random value from dictionary
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public static KeyValuePair<T1,T2> RandomKVPFromDictionary<T1, T2>(this IDictionary<T1, T2> dictionary)
        {
            System.Random rand = new System.Random();
            return dictionary.ElementAt(rand.Next(0, dictionary.Count));
        }

        /// <summary>
        /// Shuffle List pseudo randomly
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        public static void SPShuffle<T>(this IList<T> list)
        {
            System.Random rand = new System.Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rand.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        /// <summary>
        /// Initialize new list of object type T with object at index 0
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="typeObject"></param>
        /// <returns></returns>
        public static List<T> ConvertToList<T>(this T typeObject)
        {
            return new List<T>() { typeObject };
        }

        /// <summary>
        /// Clamp value of type T between max and min
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="val"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            if (val.CompareTo(max) > 0) return max;
            return val;
        }

        /// <summary>
        /// Check if one List is entirely contained within another List
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sourceList"></param>
        /// <param name="searchingList"></param>
        /// <returns></returns>
        public static bool ContainsAllOfList<T>(this IEnumerable<T> sourceList, IEnumerable<T> searchingList)
        {
            if(sourceList is null || searchingList is null) return false;
            return sourceList.Intersect(searchingList).Any();
        }

        /// <summary>
        /// Unbox list of objects into List of object type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objects"></param>
        /// <returns></returns>
        public static List<T> ConvertObjectList<T>(this List<object> objects)
        {
            for(int i = 0; i < objects.Count; i++)
            {
                object o = objects[i];
                if(o.GetType() != typeof(T))
                {
                    objects.Remove(o);
                }
            }
            return objects.Cast<T>().ToList();
        }

        /// <summary>
        /// Get Absolute Value of IntVec2
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static IntVec2 Abs(this IntVec2 c)
        {
            return new IntVec2(Math.Abs(c.x), Math.Abs(c.z));
        }

        /// <summary>
        /// Get Absolute Value of IntVec3
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static IntVec3 Abs(this IntVec3 c)
        {
            return new IntVec3(Math.Abs(c.x), Math.Abs(c.y), Math.Abs(c.z));
        }

        /// <summary>
        /// Find Rot4 direction with largest cell count
        /// <para>Useful for taking edge cells of specific terrain and getting edge with highest cell count</para>
        /// </summary>
        /// <param name="northCellCount"></param>
        /// <param name="eastCellCount"></param>
        /// <param name="southCellCount"></param>
        /// <param name="westCellCount"></param>
        /// <returns></returns>
        public static Rot4 Max4IntToRot(int northCellCount, int eastCellCount, int southCellCount, int westCellCount)
        {
            int ans1 = northCellCount > eastCellCount ? northCellCount : eastCellCount;
            int ans2 = southCellCount > westCellCount ? southCellCount : westCellCount;
            int ans3 = ans1 > ans2 ? ans1 : ans2;
            if(ans3 == northCellCount)
                return Rot4.North;
            if(ans3 == eastCellCount)
                return Rot4.East;
            if(ans3 == southCellCount)
                return Rot4.South;
            if(ans3 == westCellCount)
                return Rot4.West;
            return Rot4.Invalid;
        }

        /// <summary>
        /// Clamp a Pawn's exit point to their hitbox size. Avoids derendering issues for multicell-pawns
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="exitPoint"></param>
        /// <param name="map"></param>
        /// <param name="extraOffset"></param>
        public static void ClampToMap(Pawn pawn, ref IntVec3 exitPoint, Map map, int extraOffset = 0)
        {
            int x = pawn.def.size.x;
            int z = pawn.def.size.z;
            int offset = x > z ? x+extraOffset : z+extraOffset;

            if (exitPoint.x < offset)
            {
                exitPoint.x = (int)(offset / 2);
            }
            else if (exitPoint.x >= (map.Size.x - (offset / 2)))
            {
                exitPoint.x = (int)(map.Size.x - (offset / 2));
            }
            if (exitPoint.z < offset)
            {
                exitPoint.z = (int)(offset / 2);
            }
            else if (exitPoint.z > (map.Size.z - (offset / 2)))
            {
                exitPoint.z = (int)(map.Size.z - (offset / 2));
            }
        }

        /// <summary>
        /// Clamp a Pawn's spawn point to their hitbox size. Avoids derendering issues for multicell-pawns
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="spawnPoint"></param>
        /// <param name="map"></param>
        /// <param name="extraOffset"></param>
        /// <returns></returns>
        public static IntVec3 ClampToMap(this Pawn pawn, IntVec3 spawnPoint, Map map, int extraOffset = 0)
        {
            int x = pawn.def.size.x;
            int z = pawn.def.size.z;
            int offset = x > z ? x + extraOffset : z + extraOffset;
            if (spawnPoint.x < offset)
            {
                spawnPoint.x = (int)(offset / 2);
            }
            else if (spawnPoint.x >= (map.Size.x - (offset / 2)))
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

        /// <summary>
        /// Clamp a pawns active location based on their hitbox size. Avoids derendering issues for multicell-pawns
        /// </summary>
        /// <param name="p"></param>
        /// <param name="nextCell"></param>
        /// <param name="map"></param>
        /// <returns></returns>
        public static bool ClampHitboxToMap(Pawn p, IntVec3 nextCell, Map map)
        {
            int x = p.def.size.x % 2 == 0 ? p.def.size.x / 2 : (p.def.size.x + 1) / 2;
            int z = p.def.size.z % 2 == 0 ? p.def.size.z / 2 : (p.def.size.z + 1) / 2;

            int hitbox = x > z ? x : z;
            if (nextCell.x + hitbox >= map.Size.x || nextCell.z + hitbox >= map.Size.z)
            {
                return true;
            }
            if (nextCell.x - hitbox <= 0 || nextCell.z - hitbox <= 0)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get occupied cells of pawn with hitbox larger than 1x1
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="centerPoint"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static List<IntVec3> PawnOccupiedCells(this Pawn pawn, IntVec3 centerPoint, Rot4 direction)
        {
            int sizeX;
            int sizeZ;
            switch(direction.AsInt)
            {
                case 0:
                    sizeX = pawn.def.size.x;
                    sizeZ = pawn.def.size.z;
                    break;
                case 1:
                    sizeX = pawn.def.size.z;
                    sizeZ = pawn.def.size.x;
                    break;
                case 2:
                    sizeX = pawn.def.size.x;
                    sizeZ = pawn.def.size.z;
                    break;
                case 3:
                    sizeX = pawn.def.size.z;
                    sizeZ = pawn.def.size.x;
                    break;
                default:
                    throw new NotImplementedException("MoreThan4Rotations");
            }
            return CellRect.CenteredOn(centerPoint, sizeX, sizeZ).Cells.ToList();
        }

        /// <summary>
        /// Get edge of map the pawn is closest too
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="map"></param>
        /// <returns></returns>
        public static Rot4 ClosestEdge(Pawn pawn, Map map)
        {
            IntVec2 mapSize = new IntVec2(map.Size.x, map.Size.z);
            IntVec2 position = new IntVec2(pawn.Position.x, pawn.Position.z);

            SPTuple<Rot4, int> hDistance = Math.Abs(position.x) < Math.Abs(position.x - mapSize.x) ? new SPTuple<Rot4, int>(Rot4.West, position.x) : new SPTuple<Rot4, int>(Rot4.East, Math.Abs(position.x - mapSize.x));
            SPTuple<Rot4, int> vDistance = Math.Abs(position.z) < Math.Abs(position.z - mapSize.z) ? new SPTuple<Rot4, int>(Rot4.South, position.z) : new SPTuple<Rot4, int>(Rot4.North, Math.Abs(position.z - mapSize.z));

            return hDistance.Second <= vDistance.Second ? hDistance.First : vDistance.First;
        }

        /// <summary>
        /// Check if pawn is within certain distance of edge of map. Useful for multicell pawns who are clamped to the map beyond normal edge cell checks.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="distance"></param>
        /// <param name="map"></param>
        /// <returns></returns>
        public static bool WithinDistanceToEdge(this IntVec3 position, int distance, Map map)
        {
            return position.x < distance || position.z < distance || (map.Size.x - position.x < distance) || (map.Size.z - position.z < distance);
        }

        /// <summary>
        /// Get direction of river in Rot4 value. (Can be either start or end of River)
        /// </summary>
        /// <param name="map"></param>
        /// <returns></returns>
        public static Rot4 RiverDirection(Map map)
        {
            List<Tile.RiverLink> rivers = Find.WorldGrid[map.Tile].Rivers;

            float angle = Find.WorldGrid.GetHeadingFromTo(map.Tile, (from r1 in rivers
                                                                     orderby -r1.river.degradeThreshold
                                                                     select r1).First<Tile.RiverLink>().neighbor);
            if (angle < 45)
            {
                return Rot4.South;
            }
            else if (angle < 135)
            {
                return Rot4.East;
            }
            else if (angle < 225)
            {
                return Rot4.North;
            }
            else if (angle < 315)
            {
                return Rot4.West;
            }
            else
            {
                return Rot4.South;
            }
        }
        
        /// <summary>
        /// Draw selection brackets for pawn with angle
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bracketLocs"></param>
        /// <param name="obj"></param>
        /// <param name="worldPos"></param>
        /// <param name="worldSize"></param>
        /// <param name="dict"></param>
        /// <param name="textureSize"></param>
        /// <param name="pawnAngle"></param>
        /// <param name="jumpDistanceFactor"></param>
        public static void CalculateSelectionBracketPositionsWorldForMultiCellPawns<T>(Vector3[] bracketLocs, T obj, Vector3 worldPos, Vector2 worldSize, Dictionary<T, float> dict, Vector2 textureSize, float pawnAngle = 0f, float jumpDistanceFactor = 1f)
        {
            float num;
            float num2;
            if (!dict.TryGetValue(obj, out num))
            {
                num2 = 1f;
            }
            else
            {
                num2 = Mathf.Max(0f, 1f - (Time.realtimeSinceStartup - num) / 0.07f);
            }
            float num3 = num2 * 0.2f * jumpDistanceFactor;
            float num4 = 0.5f * (worldSize.x - textureSize.x) + num3;
            float num5 = 0.5f * (worldSize.y - textureSize.y) + num3;
            float y = AltitudeLayer.MetaOverlays.AltitudeFor();
            bracketLocs[0] = new Vector3(worldPos.x - num4, y, worldPos.z - num5);
            bracketLocs[1] = new Vector3(worldPos.x + num4, y, worldPos.z - num5);
            bracketLocs[2] = new Vector3(worldPos.x + num4, y, worldPos.z + num5);
            bracketLocs[3] = new Vector3(worldPos.x - num4, y, worldPos.z + num5);

            switch(pawnAngle)
            {
                case 45f:
                    for(int i = 0; i < 4; i++)
                    {
                        float xPos = bracketLocs[i].x - worldPos.x;
                        float yPos = bracketLocs[i].z - worldPos.z;
                        SPTuple<float, float> newPos = RotatePointClockwise(xPos, yPos, 45f);
                        bracketLocs[i].x = newPos.First + worldPos.x;
                        bracketLocs[i].z = newPos.Second + worldPos.z;
                    }
                    break;
                case -45:
                    for (int i = 0; i < 4; i++)
                    {
                        float xPos = bracketLocs[i].x - worldPos.x;
                        float yPos = bracketLocs[i].z - worldPos.z;
                        SPTuple<float, float> newPos = RotatePointCounterClockwise(xPos, yPos, 45f);
                        bracketLocs[i].x = newPos.First + worldPos.x;
                        bracketLocs[i].z = newPos.Second + worldPos.z;
                    }
                    break;
            }
        }

        /// <summary>
        /// Draw selection brackets transformed on position x,z for pawns whose selection brackets have been shifted
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static Vector3 DrawPosTransformed(this Pawn pawn, float x, float z, float angle = 0)
        {
            Vector3 drawPos = pawn.DrawPos;
            switch (pawn.Rotation.AsInt)
            {
                case 0:
                    drawPos.x += x;
                    drawPos.z += z;
                    break;
                case 1:
                    if (angle == -45)
                    {
                        drawPos.x += x == 0 ? z / (float)Math.Sqrt(2d) : x / (float)Math.Sqrt(2d);
                        drawPos.z += x == 0 ? z / (float)Math.Sqrt(2d) : x / (float)Math.Sqrt(2d);
                        break;
                    }
                    else if (angle == 45)
                    {
                        drawPos.x += x == 0 ? z / (float)Math.Sqrt(2d) : x / (float)Math.Sqrt(2d);
                        drawPos.z -= x == 0 ? z / (float)Math.Sqrt(2d) : x / (float)Math.Sqrt(2d);
                        break;
                    }
                    drawPos.x += z;
                    drawPos.z += x;
                    break;
                case 2:
                    drawPos.x -= x;
                    drawPos.z -= z;
                    break;
                case 3:
                    if (angle == -45)
                    {
                        drawPos.x -= x == 0 ? z / (float)Math.Sqrt(2d) : x / (float)Math.Sqrt(2d);
                        drawPos.z -= x == 0 ? z / (float)Math.Sqrt(2d) : x / (float)Math.Sqrt(2d);
                        break;
                    }
                    else if (angle == 45)
                    {
                        drawPos.x -= x == 0 ? z / (float)Math.Sqrt(2d) : x / (float)Math.Sqrt(2d);
                        drawPos.z += x == 0 ? z / (float)Math.Sqrt(2d) : x / (float)Math.Sqrt(2d);
                        break;
                    }
                    drawPos.x -= z;
                    drawPos.z -= x;
                    break;
                default:
                    throw new NotImplementedException("Pawn Rotation outside Rot4");
            }
            return drawPos;
        }

        /// <summary>
        /// Rotate point clockwise by angle theta
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="theta"></param>
        /// <returns></returns>
        public static SPTuple<float, float> RotatePointClockwise(float x, float y, float theta)
        {
            theta = -theta;
            float xPrime = (float)(x * Math.Cos(theta.DegreesToRadians())) - (float)(y * Math.Sin(theta.DegreesToRadians()));
            float yPrime = (float)(x * Math.Sin(theta.DegreesToRadians())) + (float)(y * Math.Cos(theta.DegreesToRadians()));
            return new SPTuple<float, float>(xPrime, yPrime);
        }

        /// <summary>
        /// Rotate point counter clockwise by angle theta
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="theta"></param>
        /// <returns></returns>
        public static SPTuple<float, float> RotatePointCounterClockwise(float x, float y, float theta)
        {
            float xPrime = (float)(x * Math.Cos(theta.DegreesToRadians())) - (float)(y * Math.Sin(theta.DegreesToRadians()));
            float yPrime = (float)(x * Math.Sin(theta.DegreesToRadians())) + (float)(y * Math.Cos(theta.DegreesToRadians()));
            return new SPTuple<float, float>(xPrime, yPrime);
        }

        /// <summary>
        /// Convert degrees (double) to radians
        /// </summary>
        /// <param name="deg"></param>
        /// <returns></returns>
        public static double DegreesToRadians(this double deg)
        {
            return deg * Math.PI / 180;
        }

        /// <summary>
        /// Convert degrees (float) to radians
        /// </summary>
        /// <param name="deg"></param>
        /// <returns></returns>
        public static double DegreesToRadians(this float deg)
        {
            return Convert.ToDouble(deg).DegreesToRadians();
        }

        /// <summary>
        /// Draw vertical fillable bar
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="fillPercent"></param>
        /// <param name="flip"></param>
        /// <returns></returns>
        public static Rect VerticalFillableBar(Rect rect, float fillPercent, bool flip = false)
        {
            return SPExtended.VerticalFillableBar(rect, fillPercent, FillableBarTexture, flip);
        }

        /// <summary>
        /// Draw vertical fillable bar with texture
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="fillPercent"></param>
        /// <param name="fillTex"></param>
        /// <param name="flip"></param>
        /// <returns></returns>
        public static Rect VerticalFillableBar(Rect rect, float fillPercent, Texture2D fillTex, bool flip = false)
        {
            bool doBorder = rect.height > 15f && rect.width > 20f;
            return SPExtended.VerticalFillableBar(rect, fillPercent, fillTex, ClearBarTexture, doBorder, flip);
        }

        /// <summary>
        /// Draw vertical fillable bar with background texture
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="fillPercent"></param>
        /// <param name="fillTex"></param>
        /// <param name="bgTex"></param>
        /// <param name="doBorder"></param>
        /// <param name="flip"></param>
        /// <returns></returns>
        public static Rect VerticalFillableBar(Rect rect, float fillPercent, Texture2D fillTex, Texture2D bgTex, bool doBorder = false, bool flip = false)
        {
            if(doBorder)
            {
                GUI.DrawTexture(rect, bgTex);
                rect = rect.ContractedBy(3f);
            }
            if(bgTex != null)
            {
                GUI.DrawTexture(rect, bgTex);
            }
            if(!flip)
            {
                rect.y += rect.height;
                rect.height *= -1;
            }
            Rect result = rect;
            rect.height *= fillPercent;
            GUI.DrawTexture(rect, fillTex);
            return result;
        }

        /// <summary>
        /// Convert RenderTexture to Texture2D
        /// <para>Warning: This is very costly. Do not do often.</para>
        /// </summary>
        /// <param name="rTex"></param>
        /// <returns></returns>
        public static Texture2D ConvertToTexture2D(this RenderTexture rTex)
        {
            Texture2D tex2d = new Texture2D(512, 512, TextureFormat.RGB24, false);
            RenderTexture.active = rTex;
            tex2d.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
            tex2d.Apply();
            return tex2d;
        }

        private static readonly Texture2D FillableBarTexture = SolidColorMaterials.NewSolidColorTexture(0.5f, 0.5f, 0.5f, 0.5f);
        private static readonly Texture2D ClearBarTexture = BaseContent.ClearTex;
    }

}
