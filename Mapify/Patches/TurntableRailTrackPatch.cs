using HarmonyLib;
using Mapify.Utils;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace Mapify.Patches
{
    [HarmonyPatch(typeof(TurntableRailTrack), nameof(TurntableRailTrack.RotateToTargetRotation))]
    public class TurntableRailTrack_RotateToTargetRotation_Patch
    {
        /// roll instead of yaw
        public static bool Prefix(ref TurntableRailTrack __instance, bool forceConnectionRefresh)
        {
            var currentYrotation = __instance.currentYRotation;
            __instance.currentYRotation = __instance.targetYRotation;

            var rotationDelta = __instance.currentYRotation - currentYrotation;
            if (Mathf.Abs(rotationDelta) == 0.0)
            {
                if (!forceConnectionRefresh)
                    return false;

                __instance.UpdateTrackConnection();
            }
            else
            {
                var turnTablePosition = __instance.transform.position;
                var rotationToApplyEulers = rotationDelta * __instance.visuals.forward;
                var rotationToApply = Quaternion.Euler(rotationToApplyEulers);

                for (var index = 0; index < __instance.Track.curve.pointCount; ++index)
                {
                    var bezierPoint = __instance.Track.curve[index];
                    bezierPoint.position = __instance.RotateAroundPoint(bezierPoint.position, turnTablePosition, rotationToApply);

                    bezierPoint.handle1 = __instance.RotateAroundPoint(bezierPoint.handle1, Vector3.zero, rotationToApply);
                    if (bezierPoint.handleStyle != BezierPoint.HandleStyle.Connected)
                        bezierPoint.handle2 = __instance.RotateAroundPoint(bezierPoint.handle2, Vector3.zero, rotationToApply);
                }

                __instance.Track.GetKinkedPointSet().RotateAroundPoint(new Vector3d(turnTablePosition - DV.OriginShift.OriginShift.currentMove), rotationToApply);

                __instance.Track.TrackPointsUpdated_Invoke();

                __instance.visuals.RotateAround(__instance.visuals.parent.position, __instance.visuals.forward, rotationDelta);
                __instance.UpdateTrackConnection();
            }

            return false;
        }

        public static void Postfix(TurntableRailTrack __instance)
        {
            RailwayMeshUpdater.UpdateTrack(__instance.Track);
        }
    }
}
