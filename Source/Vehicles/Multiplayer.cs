using Multiplayer.API;
using Verse;
using System;

namespace VehicleFramework.MultiplayerCompatibility;


[StaticConstructorOnStartup]
public static class Multiplayer
{
    static Multiplayer()
    {
        try
        {
            
            if (!MP.enabled) return;
            MP.RegisterAll();
            
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to initialize Multiplayer support for Vehicle Framework in Multiplayer.cs: {ex.Message}");
        }
    }
}