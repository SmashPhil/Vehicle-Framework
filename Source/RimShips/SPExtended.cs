using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;

namespace SPExtendedLibrary
{
    [StaticConstructorOnStartup]
    public static class SPExtended
    {
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
                if(!object.ReferenceEquals(First, null))
                    hash = FirstComparer.GetHashCode(First);
                if (!object.ReferenceEquals(Second, null))
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
                if (!object.ReferenceEquals(First, null))
                    hash = FirstComparer.GetHashCode(First);
                if(!object.ReferenceEquals(Second, null))
                    hash = (hash << 3) + hash ^ SecondComparer.GetHashCode(Second);
                if (!object.ReferenceEquals(Third, null))
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

        public static IntVec2 Abs(this IntVec2 c)
        {
            return new IntVec2(Math.Abs(c.x), Math.Abs(c.z));
        }
        public static IntVec3 Abs(this IntVec3 c)
        {
            return new IntVec3(Math.Abs(c.x), Math.Abs(c.y), Math.Abs(c.z));
        }
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

            SPTuple<Rot4, int> hDistance = Math.Abs(position.x) < Math.Abs(position.x - mapSize.x) ? new SPTuple<Rot4, int>(Rot4.West, position.x) : new SPTuple<Rot4, int>(Rot4.East, Math.Abs(position.x - mapSize.x));
            SPTuple<Rot4, int> vDistance = Math.Abs(position.z) < Math.Abs(position.z - mapSize.z) ? new SPTuple<Rot4, int>(Rot4.South, position.z) : new SPTuple<Rot4, int>(Rot4.North, Math.Abs(position.z - mapSize.z));

            return hDistance.Second <= vDistance.Second ? hDistance.First : vDistance.First;
        }

        public static SPTuple<float, float> RotatePointClockwise(float x, float y, float theta)
        {
            theta = -theta;
            float xPrime = (float)(x * Math.Cos(theta.DegreesToRadians())) - (float)(y * Math.Sin(theta.DegreesToRadians()));
            float yPrime = (float)(x * Math.Sin(theta.DegreesToRadians())) + (float)(y * Math.Cos(theta.DegreesToRadians()));
            return new SPTuple<float, float>(xPrime, yPrime);
        }

        public static SPTuple<float, float> RotatePointCounterClockwise(float x, float y, float theta)
        {
            float xPrime = (float)(x * Math.Cos(theta.DegreesToRadians())) - (float)(y * Math.Sin(theta.DegreesToRadians()));
            float yPrime = (float)(x * Math.Sin(theta.DegreesToRadians())) + (float)(y * Math.Cos(theta.DegreesToRadians()));
            return new SPTuple<float, float>(xPrime, yPrime);
        }

        public static SPTuple<float, float> ReflectPointAcrossAxis(float x, float y)
        {
            return new SPTuple<float, float>(y, x);
        }
        public static double DegreesToRadians(this double deg)
        {
            return deg * Math.PI / 180;
        }

        public static double DegreesToRadians(this float deg)
        {
            return Convert.ToDouble(deg).DegreesToRadians();
        }

        public static Rect VerticalFillableBar(Rect rect, float fillPercent, bool flip = false)
        {
            return SPExtended.VerticalFillableBar(rect, fillPercent, FillableBarTexture, flip);
        }

        public static Rect VerticalFillableBar(Rect rect, float fillPercent, Texture2D fillTex, bool flip = false)
        {
            bool doBorder = rect.height > 15f && rect.width > 20f;
            return SPExtended.VerticalFillableBar(rect, fillPercent, fillTex, ClearBarTexture, doBorder, flip);
        }

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
