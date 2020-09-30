using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Vehicles.UI
{
    [StaticConstructorOnStartup]
    public static class ShipPassengerCardUtility
    {
        public static void DrawPassengerList(Rect rect, Pawn pawn, List<Pawn> passengers)
        {
            GUI.color = Color.white;

            GUI.BeginGroup(rect);
            float lineHeight = Text.LineHeight;
            Rect outRect = new Rect(0f, 0f, rect.width, rect.height - lineHeight);
            //Rect viewRect = new Rect(0f, 0f, rect.width - 16f, HealthCardUtility.scrollViewHeight);
        }
    }
}
