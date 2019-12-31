using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SPExtended
{
    public static class SPTuples
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
            public SPTuple(T1 first, T2 second)
            {
                First = first;
                Second = second;
            }

            public static bool operator ==(SPTuple<T1, T2> o1, SPTuple<T1, T2> o2) { return o1.Equals(o2); }
            public static bool operator !=(SPTuple<T1, T2> o1, SPTuple<T1, T2> o2) { return !(o1 == o2); }

            private static readonly IEqualityComparer<T1> FirstComparer = EqualityComparer<T1>.Default;
            private static readonly IEqualityComparer<T2> SecondComparer = EqualityComparer<T2>.Default;

            public override int GetHashCode()
            {
                int hash = 0;
                if (First is object)
                    hash = FirstComparer.GetHashCode(First);
                if (Second is object)
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
        public class SPTuple<T1, T2, T3> : SPTuple<T1, T2>
        {
            public T3 Third { get; set; }
            public SPTuple(T1 first, T2 second, T3 third) : base(first, second)
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
                if (First is object)
                    hash = FirstComparer.GetHashCode(First);
                if (Second is object)
                    hash = (hash << 3) + hash ^ SecondComparer.GetHashCode(Second);
                if (Third is object)
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

        public struct SPTuple2<T1, T2> : IEquatable<SPTuple2<T1, T2>>
        {
            public SPTuple2(T1 first, T2 second)
            {
                this.first = first;
                this.second = second;
            }

            public T1 First
            {
                get
                {
                    return this.first;
                }
                set
                {
                    if (value is T1)
                        this.first = value;
                    Log.Error("Tried to assign value of different type to Tuple of type " + typeof(T1));
                }
            }

            public T2 Second
            {
                get
                {
                    return this.second;
                }
                set
                {
                    if (value is T2)
                        this.second = value;
                    Log.Error("Tried to assign value of different type to Tuple of type " + typeof(T2));
                }
            }

            public override int GetHashCode()
            {
                int hash = 0;
                if (First is object)
                    hash = EqualityComparer<T1>.Default.GetHashCode(First);
                if (Second is object)
                    hash = (hash << 3) + hash ^ EqualityComparer<T2>.Default.GetHashCode(Second);
                return hash;
            }

            public override bool Equals(object obj)
            {
                return (obj is SPTuple2<T1, T2> && this.Equals((SPTuple2<T1, T2>)obj)) || (obj is Pair<T1, T2> && this.Equals((Pair<T1, T2>)obj));
            }

            public bool Equals(SPTuple2<T1, T2> other)
            {
                return EqualityComparer<T1>.Default.Equals(this.first, other.first) && EqualityComparer<T2>.Default.Equals(this.second, other.second);
            }

            public bool Equals(Pair<T1, T2> other)
            {
                return EqualityComparer<T1>.Default.Equals(this.first, other.First) && EqualityComparer<T2>.Default.Equals(this.second, other.Second);
            }

            public static bool operator ==(SPTuple2<T1, T2> lhs, SPTuple2<T1, T2> rhs)
            {
                return lhs.Equals(rhs);
            }

            public static bool operator !=(SPTuple2<T1, T2> lhs, SPTuple2<T1, T2> rhs)
            {
                return !(lhs == rhs);
            }

            public static bool operator ==(Pair<T1, T2> lhs, SPTuple2<T1, T2> rhs)
            {
                return lhs.Equals(rhs);
            }

            public static bool operator !=(Pair<T1, T2> lhs, SPTuple2<T1, T2> rhs)
            {
                return !(lhs == rhs);
            }

            private T1 first;

            private T2 second;
        }
    }
}
