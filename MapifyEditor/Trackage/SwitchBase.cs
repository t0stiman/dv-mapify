// using System.Collections.Generic;
// using System.Linq;
// using Mapify.Editor.Utils;
// using UnityEditor;
using UnityEngine;

namespace Mapify.Editor
{
    // [ExecuteInEditMode] //this is necessary for snapping to work
    public abstract class SwitchBase: MonoBehaviour
    {
// #if UNITY_EDITOR
//         //TODO snap?
//         private bool snapShouldUpdate = true;
//
//         private Vector3 previousPositionSwitchFirstPoint;
//         private Vector3 previousPositionThroughTrackLastPoint;
//         private Vector3 previousPositionDivergingTrackLastPoint;
//
//
//         private SnappedTrack snappedTrackBeforeSwitch;
//         //the track connected to the through track
//         private SnappedTrack snappedTrackAfterThrough;
//         //the track connected to the diverging track
//         private SnappedTrack snappedTrackAfterDiverging;
// #endif

        public virtual Track[] GetTracks()
        {
            var tracks = gameObject.GetComponentsInChildren<Track>();
            return tracks ?? new Track[] {};
        }

        public abstract BezierPoint GetJointPoint();

        // TODO why is this unused
//         protected void Snap()
//         {
// #if UNITY_EDITOR
//             BezierPoint[] bezierPoints = FindObjectsOfType<BezierPoint>();
//             GameObject[] selectedObjects = Selection.gameObjects;
//             bool isSelected = selectedObjects.Contains(gameObject);
//
//             TrySnap(bezierPoints, isSelected, transform);
//             foreach (var track in Tracks)
//             {
//                 TrySnap(bezierPoints, isSelected, track.Curve.Last().transform);
//             }
// #endif
//         }
//
//         private void TrySnap(IEnumerable<BezierPoint> points, bool move, Transform reference)
//         {
//             var referencePosition = reference.position;
//             var closestPos = Vector3.zero;
//             var closestDist = float.MaxValue;
//
//             foreach (var otherPoint in points)
//             {
//                 if (otherPoint.Curve().GetComponentInParent<SwitchBase>() == this) continue;
//
//                 var otherPosition = otherPoint.transform.position;
//                 var dist = Mathf.Abs(Vector3.Distance(otherPosition, referencePosition));
//
//                 if (dist > Track.SNAP_RANGE || dist >= closestDist) continue;
//
//                 var aTrack = otherPoint.GetComponentInParent<Track>();
//                 if (aTrack.IsSwitch) continue;
//
//                 closestPos = otherPosition;
//                 closestDist = dist;
//             }
//
//             if (closestDist >= float.MaxValue) return;
//
//             if (move) {
//                 transform.position = closestPos + (transform.position - reference.position);
//             }
//         }

//
// #if UNITY_EDITOR
//
//         private void OnEnable()
//         {
//             snapShouldUpdate = true;
//         }
//
//         private void OnDisable()
//         {
//             UnsnapConnectedTracks();
//         }
//
//         private void OnDestroy()
//         {
//             UnsnapConnectedTracks();
//         }
//
//         private void OnDrawGizmos()
//         {
//             if (transform.DistToSceneCamera() >= Track.SNAP_UPDATE_RANGE_SQR)
//             {
//                 return;
//             }
//
//             CheckSwitchMoved();
//
//             if (snapShouldUpdate)
//             {
//                 Snap();
//                 snapShouldUpdate = false;
//             }
//         }
//
//         private void CheckSwitchMoved()
//         {
//             var positionSwitchFirstPoint = transform.position;
//             var positionThroughTrackLastPoint = ThroughTrack.Curve.Last().position;
//             var positionDivergingTrackLastPoint = DivergingTrack.Curve.Last().position;
//
//             if (positionSwitchFirstPoint != previousPositionSwitchFirstPoint ||
//                 positionThroughTrackLastPoint != previousPositionThroughTrackLastPoint ||
//                 positionDivergingTrackLastPoint != previousPositionDivergingTrackLastPoint)
//             {
//                 snapShouldUpdate = true;
//
//                 previousPositionSwitchFirstPoint = positionSwitchFirstPoint;
//                 previousPositionThroughTrackLastPoint = positionThroughTrackLastPoint;
//                 previousPositionDivergingTrackLastPoint = positionDivergingTrackLastPoint;
//             }
//         }
//
//         private void UnsnapConnectedTracks()
//         {
//             snappedTrackBeforeSwitch?.UnSnapped();
//             snappedTrackAfterThrough?.UnSnapped();
//             snappedTrackAfterDiverging?.UnSnapped();
//         }
//
//         public void Snap()
//         {
//             var bezierPoints = FindObjectsOfType<BezierPoint>();
//             bool isSelected = Selection.gameObjects.Contains(gameObject);
//
//             TrySnap(bezierPoints, isSelected, SwitchPoint.FIRST);
//             TrySnap(bezierPoints, isSelected, SwitchPoint.DIVERGING);
//             TrySnap(bezierPoints, isSelected, SwitchPoint.THROUGH);
//         }
//
//         private void TrySnap(IEnumerable<BezierPoint> points, bool move, SwitchPoint switchPoint)
//         {
//             var reference = switchPoint switch
//             {
//                 SwitchPoint.FIRST => transform,
//                 SwitchPoint.THROUGH => ThroughTrack.Curve.Last().transform,
//                 SwitchPoint.DIVERGING => DivergingTrack.Curve.Last().transform,
//                 _ => throw new ArgumentOutOfRangeException(nameof(switchPoint), switchPoint, null)
//             };
//
//             var position = reference.position;
//             var closestPosition = Vector3.zero;
//             var closestDistance = float.MaxValue;
//
//             foreach (BezierPoint otherSnapPoint in points)
//             {
//                 //don't connect to itself
//                 if (otherSnapPoint.Curve().GetComponentInParent<Switch>() == this) continue;
//
//                 Vector3 otherPosition = otherSnapPoint.transform.position;
//                 float distance = Mathf.Abs(Vector3.Distance(otherPosition, position));
//
//                 // too far away
//                 if (distance > Track.SNAP_RANGE || distance >= closestDistance) continue;
//
//                 var otherTrack = otherSnapPoint.GetComponentInParent<Track>();
//
//                 // don't snap a switch to another switch
//                 if (otherTrack.IsSwitch) continue;
//
//                 closestPosition = otherPosition;
//                 closestDistance = distance;
//
//                 otherTrack.Snapped(otherSnapPoint);
//
//                 //remember what track we snapped to
//                 switch (switchPoint)
//                 {
//                     case SwitchPoint.FIRST:
//                         snappedTrackBeforeSwitch = new SnappedTrack(otherTrack, otherSnapPoint);
//                         break;
//                     case SwitchPoint.THROUGH:
//                         snappedTrackAfterThrough = new SnappedTrack(otherTrack, otherSnapPoint);
//                         break;
//                     case SwitchPoint.DIVERGING:
//                         snappedTrackAfterDiverging = new SnappedTrack(otherTrack, otherSnapPoint);
//                         break;
//                     default:
//                         throw new ArgumentOutOfRangeException(nameof(switchPoint), switchPoint, null);
//                 }
//             }
//
//             // No snap target found
//             if (closestDistance >= float.MaxValue)
//             {
//                 switch (switchPoint)
//                 {
//                     case SwitchPoint.FIRST:
//                         snappedTrackBeforeSwitch?.UnSnapped();
//                         snappedTrackBeforeSwitch = null;
//                         break;
//                     case SwitchPoint.THROUGH:
//                         snappedTrackAfterThrough?.UnSnapped();
//                         snappedTrackAfterThrough = null;
//                         break;
//                     case SwitchPoint.DIVERGING:
//                         snappedTrackAfterDiverging?.UnSnapped();
//                         snappedTrackAfterDiverging = null;
//                         break;
//                     default:
//                         throw new ArgumentOutOfRangeException(nameof(switchPoint), switchPoint, null);
//                 }
//
//                 return;
//             }
//
//             if (move)
//             {
//                 transform.position = closestPosition + (transform.position - reference.position);
//             }
//         }
//
// #endif

    }
}
