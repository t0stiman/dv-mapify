using DV.CashRegister;
using HarmonyLib;

namespace Mapify.Patches
{
    [HarmonyPatch(typeof(CashRegisterWithModules), nameof(CashRegisterWithModules.Cancel))]
    public static class CashRegisterWithModules_Cancel_Patch
    {
        // Gets rid of a NullReferenceException
        private static bool Prefix(CashRegisterWithModules __instance)
        {
            Mapify.LogWarning($"skip {nameof(CashRegisterWithModules.Cancel)}");
            return __instance.registerModules == null;
        }
    }
}
