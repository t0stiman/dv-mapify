using System;
using DV.PointSet;
using HarmonyLib;
using UnityEngine;

namespace Mapify.Patches
{
    [HarmonyPatch(typeof(EquiPointSet), nameof(EquiPointSet.ResampleEquidistant))]
    public static class EquiPointSet_ResampleEquidistant_Patch
    {
        private static bool Prefix(EquiPointSet source, float pointSpacing, ref EquiPointSet __result)
        {
            if (pointSpacing <= source.span / 2.0)
                return true;
            __result = new EquiPointSet {
                points = Array.Empty<EquiPointSet.Point>()
            };
            return false;
        }
    }

    [HarmonyPatch(typeof(EquiPointSet), nameof(EquiPointSet.RotateAroundPoint))]
    public static class EquiPointSet_RotateAroundPoint_Patch
    {
        private static bool Prefix(ref EquiPointSet __instance, Vector3d rotateAnchorWorldPosition, Quaternion rotationToApply)
        {
            for (int index = 0; index < __instance.points.Length; ++index)
            {
                Vector3d vector3d = __instance.points[index].position - rotateAnchorWorldPosition;
                Vector3 v3 = rotationToApply * (Vector3) vector3d;
                __instance.points[index].position = rotateAnchorWorldPosition + new Vector3d(v3);
                __instance.points[index].forward = rotationToApply * __instance.points[index].forward;

                __instance.points[index].up = rotationToApply * __instance.points[index].up;
            }

            return false;
        }
    }


}
