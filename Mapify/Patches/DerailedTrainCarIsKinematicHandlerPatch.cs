using DV;
using HarmonyLib;
using Mapify.Map;

namespace Mapify.Patches;

/// <summary>
/// Without this patch, trains instantly come to a stop when derailing
/// </summary>
[HarmonyPatch(typeof(DerailedTrainCarIsKinematicHandler), nameof(DerailedTrainCarIsKinematicHandler.UpdateIsKinematicDependingOnLoadedCells ))]
public static class DerailedTrainCarIsKinematicHandler_UpdateIsKinematicDependingOnLoadedCells_Patch
{
    private static bool Prefix()
    {
        return false;
    }
}
