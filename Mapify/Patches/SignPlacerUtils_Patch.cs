using System.Collections.Generic;
using DV.Signs;
using HarmonyLib;

namespace Mapify.Patches;

/// <summary>
/// Without this patch, dead end tracks break the speed limit sign generation. I do not understand why.
/// </summary>
[HarmonyPatch(typeof(SignPlacerUtils), nameof(SignPlacerUtils.ChunkifyNumbers))]
public static class SignPlacerUtils_ChunkifyNumbers_Patch
{
    private static void Prefix(List<float> numbers, float minSum)
    {
        for (int i = 0; i < numbers.Count; i++)
        {
            if(numbers[i] > 0) continue;
            numbers[i] = 1;
        }
    }
}

