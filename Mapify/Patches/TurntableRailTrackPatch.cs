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

            var num = __instance.currentYRotation - currentYrotation;
            if (Mathf.Abs(num) == 0.0)
            {
                if (!forceConnectionRefresh)
                    return false;
            }
            else
            {
                var turnTablePosition = __instance.transform.parent.position;
                var rotationToApplyEulers = num * __instance.visuals.forward;
                var rotationToApply = Quaternion.Euler(rotationToApplyEulers);

                for (var index = 0; index < __instance.Track.curve.pointCount; ++index)
                {
                    var bezierPoint = __instance.Track.curve[index];
                    bezierPoint.position = __instance.RotateAroundPoint(bezierPoint.position, turnTablePosition, rotationToApply);

                    bezierPoint.handle1 = __instance.RotateAroundPoint(bezierPoint.handle1, Vector3.zero, rotationToApply);
                    if (bezierPoint.handleStyle != BezierPoint.HandleStyle.Connected)
                        bezierPoint.handle2 = __instance.RotateAroundPoint(bezierPoint.handle2, Vector3.zero, rotationToApply);
                }

                __instance.Track.GetPointSet().RotateAroundPoint(new Vector3d(turnTablePosition - WorldMover.currentMove), rotationToApply);

                foreach (var onTrackBogey in __instance.Track.onTrackBogies)
                {
                    onTrackBogey.Car.ForceOptimizationState(false);
                    onTrackBogey.RefreshBogiePoints();
                }
                __instance.visuals.Rotate(rotationToApplyEulers, Space.World);
            }

            __instance.UpdateTrackConnection();

            return false;
        }

        public static void Postfix(TurntableRailTrack __instance)
        {
            RailwayMeshUpdater.UpdateTrack(__instance.Track);
        }
    }
}
