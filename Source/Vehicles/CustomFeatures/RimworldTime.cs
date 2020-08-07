using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RimWorld;
using Verse;

namespace Vehicles
{

    public struct RimworldTime
    {
        public string time;
        public int ticks;

        public RimworldTime(string time)
        {
            this.time = time;
            this.ticks = ParseToTicks(time);
        }

        public static RimworldTime FromString(string entry)
        {
            return new RimworldTime(entry);
        }

        public static int ParseToTicks(string time)
        {
            if (string.IsNullOrEmpty(time))
                return 0;
            int totalTicks = 0;
			foreach (string timeStamp in time.Split(','))
			{
				bool parsed = int.TryParse(string.Concat(timeStamp.Where(char.IsDigit)), out int numeric);
                if(!int.TryParse(timeStamp, out int _))
                {
                    numeric *= GetTickMultiplier(timeStamp[timeStamp.Length - 1]);
                }
                totalTicks += numeric;
			}
            return totalTicks;
        }

        private static int GetTickMultiplier(char c)
		{
			switch(char.ToLower(c))
			{
				case 'h':
					return 2500;
				case 'd':
					return 60000;
                case 'w':
                    return 420000;
				case 'q':
					return 900000;
				case 'y':
					return 3600000;
				case 't':
					return 1;
                default:
                    if(!char.IsNumber(c))
                    {
                        Log.Warning($"Unable to Parse {c} in RimWorldTime String.");
                    }
                    return 1;
			}
		}

        public override string ToString()
        {
            return time;
        }

        public static string TicksToRealTime(int ticks)
        {
            if (ticks <= 0)
                return "00:00:00:00";

            int seconds = ticks / 60;
            TimeSpan time = TimeSpan.FromSeconds(seconds);
            return time.ToString(@"dd\:hh\:mm\:ss");
        }

        public static string TicksToGameTime(int ticks)
        {
            if (ticks <= 0)
                return "00:00:00:00";
            int days = Math.DivRem(ticks, 60000, out int hourRemainder);
			int hours = Math.DivRem(hourRemainder, 2500, out int minuteRemainder);
			int minutes = Math.DivRem(minuteRemainder, 42, out int secondRemainder);
			int seconds = Math.DivRem(secondRemainder, 7, out int runoff);

            return $"{days:D2}:{hours:D2}:{minutes:D2}:{seconds:D2}";
        }
    }
}
