using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace SmashTools;

public static class PawnOverlayRenderer
{
  public static IEnumerable<CodeInstruction> ShowBodyTranspiler(
    IEnumerable<CodeInstruction> instructions)
  {
    List<CodeInstruction> instructionList = instructions.ToList();
    MethodInfo heldPawnPropertyGetter = AccessTools.PropertyGetter(
      typeof(IThingHolderWithDrawnPawn), nameof(IThingHolderWithDrawnPawn.HeldPawnDrawPos_Y));
    for (int i = 0; i < instructionList.Count; i++)
    {
      CodeInstruction instruction = instructionList[i];

      if (instruction.Calls(heldPawnPropertyGetter) &&
        instructionList[i - 1].operand is LocalBuilder { LocalIndex: 11 })
      {
        // CallVirt IThingHolderWithDrawnPawn::HoldPawnDrawPos_Y
        yield return instruction;
        instruction = instructionList[++i];
        // Stfld : Vector3::y from IThingHolderWithDrawnPawn::get_HeldPawnDrawPos_Y
        yield return instruction;
        instruction = instructionList[++i];

        // IThingHolderWithDrawnPawn holder
        yield return new CodeInstruction(opcode: OpCodes.Ldloc_S, operand: 11);
        // out bool showBody
        yield return new CodeInstruction(opcode: OpCodes.Ldarg_3);
        // &showBody = PawnOverlayRenderer::GetShowBody
        yield return new CodeInstruction(opcode: OpCodes.Call,
          operand: AccessTools.Method(typeof(PawnOverlayRenderer), nameof(GetShowBody)));
      }

      yield return instruction;
    }
  }

  public static void GetShowBody(IThingHolderWithDrawnPawn thingHolderWithDrawnPawn,
    ref bool showBody)
  {
    if (thingHolderWithDrawnPawn is IThingHolderPawnOverlayer pawnOverlayer)
    {
      showBody = pawnOverlayer.ShowBody;
    }
  }

  public static bool LayingFacing(Pawn ___pawn, ref Rot4 __result)
  {
    if (___pawn.ParentHolder is IThingHolderPawnOverlayer pawnOverlayer)
    {
      __result = pawnOverlayer.PawnRotation;
      return false;
    }
    return true;
  }
}