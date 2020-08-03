using System.Globalization;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;
using RimWorld;

namespace Vehicles
{
    public struct FireMode : IEquatable<FireMode>
    {
        public int ticksBetweenBursts;
        public int shotsPerBurst;
        public int ticksBetweenShots;
        public string label;
        public string texPath;

        private Texture2D _icon;

        public Texture2D Icon
        {
            get
            {
                if(_icon is null)
                {
                    if(!string.IsNullOrEmpty(texPath))
                        _icon = ContentFinder<Texture2D>.Get(texPath, false);
                    if(_icon is null)
                    {
                        _icon = BaseContent.BadTex;
                        Log.Warning($"Unable to load {texPath} in any active mod or base resources.");
                    }
                }
                return _icon;
            }
        }

        public FireMode(int shotsPerBurst, int ticksBetweenShots, int ticksBetweenBursts, string label, string texPath)
        {
            this.ticksBetweenBursts = ticksBetweenBursts;
            this.shotsPerBurst = shotsPerBurst;
            this.ticksBetweenShots = ticksBetweenShots;
            this.label = label;
            this.texPath = texPath;
            _icon = null;
        }

        public static FireMode Invalid
        {
            get
            {
                return new FireMode(-1, -1, -1, "Invalid", string.Empty);
            }
        }

        public bool IsValid
        {
            get
            {
                return shotsPerBurst > 0;
            }
        }

        public static FireMode FromString(string entry)
        {
            entry = entry.TrimStart(new char[] { '(' }).TrimEnd(new char[] { ')' });
            string[] data = entry.Split(new char[] { ',' });

            try
            {
                CultureInfo invariantCulture = CultureInfo.InvariantCulture;
	            int newX = Convert.ToInt32(data[0], invariantCulture);
	            int newY = Convert.ToInt32(data[1], invariantCulture);
	            int newZ = Convert.ToInt32(data[2], invariantCulture);
                string label = Convert.ToString(data[3], invariantCulture).Trim();
                string texPath = Convert.ToString(data[4], invariantCulture).Trim();
	            return new FireMode(newX, newY, newZ, label, texPath);
            }
            catch(Exception ex)
            {
                Log.Error($"{entry} is not a valid FireMode format. Exception: {ex}");
                return FireMode.Invalid;
            }
        }

        public override string ToString()
        {
            return $"({shotsPerBurst}, {ticksBetweenShots}, {ticksBetweenBursts}, {label}, {texPath})";
        }

        public (float timeBetweenBursts, float timeBetweenShots) GetRelativeTime()
        {
            return (ticksBetweenBursts / 60f, ticksBetweenShots / 60f);
        }

        public static bool operator ==(FireMode fm1, FireMode fm2) => fm1.Equals(fm2);
        public static bool operator !=(FireMode fm1, FireMode fm2) => !fm1.Equals(fm2);

        public override bool Equals(object obj)
        {
            return obj is FireMode && this.Equals((FireMode)obj);
        }

        public bool Equals(FireMode fireMode2)
        {
            return fireMode2.ticksBetweenBursts == ticksBetweenBursts && fireMode2.shotsPerBurst == shotsPerBurst && fireMode2.ticksBetweenShots == ticksBetweenShots;
        }

        public override int GetHashCode()
        {
            return Gen.HashCombineInt(Gen.HashCombineInt(Gen.HashCombineInt(0, ticksBetweenBursts), shotsPerBurst), ticksBetweenShots);
        }
    }
}
