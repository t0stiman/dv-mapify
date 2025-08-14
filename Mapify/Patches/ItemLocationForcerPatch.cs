using DV;
using DV.Shops;
using HarmonyLib;
using Mapify.Components;
using Mapify.Map;
using VLB;

namespace Mapify.Patches
{
    /// <summary>
    /// Something keeps setting the shop scanner inactive. This patch prevents that.
    /// </summary>
    [HarmonyPatch(typeof(ItemLocationForcer), nameof(ItemLocationForcer.Awake))]
    public class ItemLocationForcer_Awake_Patch
    {
        private static void Postfix(ItemLocationForcer __instance)
        {
            if(__instance.itemGO == null || Maps.IsDefaultMap) return;
            if(!__instance.itemGO.TryGetComponent<ShopScanner>(out var shopScanner)) return;

            Mapify.LogDebug($"Forcing scanner at '{shopScanner.gameObject.GetPath()}' active");
            var forceActive = __instance.gameObject.GetOrAddComponent<ForceActive>();
            forceActive.enabled = true;
            forceActive.target = shopScanner.gameObject;
        }
    }

    [HarmonyPatch(typeof(ItemLocationForcer), nameof(ItemLocationForcer.OnDisable))]
    public class ItemLocationForcer_OnDisable_Patch
    {
        private static void Postfix(ItemLocationForcer __instance)
        {
            if(__instance.itemGO == null || Maps.IsDefaultMap) return;
            if(!__instance.itemGO.TryGetComponent<ForceActive>(out var forceActive)) return;
            forceActive.enabled = false;
        }
    }

}
