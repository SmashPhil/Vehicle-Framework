using UnityEngine;
using Verse;

namespace RimShips
{
    [StaticConstructorOnStartup]
    public static class TexCommandShips
    {
        public static readonly Texture2D UnloadAll = ContentFinder<Texture2D>.Get("UI/UnloadAll", true);

        public static readonly Texture2D UnloadPassenger = ContentFinder<Texture2D>.Get("UI/UnloadPassenger", true);

        public static readonly Texture2D UnloadCaptain = ContentFinder<Texture2D>.Get("UI/UnloadCaptain", true);

        public static readonly Texture2D Anchor = ContentFinder<Texture2D>.Get("UI/Anchor", true);

        public static readonly Texture2D OnboardIcon = ContentFinder<Texture2D>.Get("UI/OnboardIcon", true);

        public static readonly Texture2D BroadsideCannon_Port = ContentFinder<Texture2D>.Get("UI/Cannon_Left", true);

        public static readonly Texture2D BroadsideCannon_Starboard = ContentFinder<Texture2D>.Get("UI/Cannon_Right", true);

        public static readonly Texture2D Rename = ContentFinder<Texture2D>.Get("UI/Buttons/Rename", true);
    }
}