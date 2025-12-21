using Mapify.Editor.Utils;
using UnityEditor;
using UnityEngine;
using static Mapify.Editor.Tools.ToolEnums;

#if UNITY_EDITOR
namespace Mapify.Editor.Tools
{
    public partial class TrackToolsWindow
    {
        private FreeformTrackHelper _freeformTrackHelper = null;

        private void HandleMouseEvents(SceneView scene)
        {
            if (IsInFreeformMode)
            {
                // Since this mode requires mouse interaction and previews where the mouse is,
                // force the window to send events when the mouse moves.
                scene.wantsMouseMove = true;
                FreeformCreationMouse(scene);
            }
            else
            {
                scene.wantsMouseMove = false;
            }
        }

        private void FreeformCreationMouse(SceneView scene)
        {
            // You have no power here.
            if (!_creating)
            {
                return;
            }

            // Deselect everything so no trouble is caused.
            Selection.activeGameObject = null;

            Event e = Event.current;

            Vector3 mousePos = SceneViewHelper.GetMousePosition(scene, e);

            Ray ray = scene.camera.ScreenPointToRay(mousePos);
            Vector3 hitPosition;
            Vector3 hitNormal;

            if (Physics.Raycast(ray, out RaycastHit rhit))
            {
                // We hit something, like a terrain.
                hitPosition = rhit.point;
                hitNormal = rhit.normal;
            }
            else if (MathHelper.YPlane.Raycast(ray, out float fhit))
            {
                // No object hit, so "clamp" the raycast to Y = 0.
                hitPosition = ray.GetPoint(fhit);
                hitNormal = MathHelper.YPlane.normal;
            }
            else
            {
                // If we're under everything, or something is wrong, ignore.
                return;
            }

            // Repaint when the mouse moves.
            if (e.type == EventType.MouseMove)
            {
                scene.Repaint();
            }

            // Draw mouse position.
            float handleSize = HandleUtility.GetHandleSize(hitPosition);
            float snapRadius = Mathf.Max(handleSize * MathHelper.OneThird, Track.SNAP_RANGE);
            bool snapped = TrackToolsHelper.CheckForTrackSnap(hitPosition, snapRadius, out Vector3 snap, out Vector3 snapHandle);

            if (!snapped)
            {
                // Adjust result with offset.
                hitPosition.y += _heightOffset;
            }

            // Wire sphere showing snap radius.
            Handles.color = snapRadius <= Track.SNAP_RANGE ? Color.red : Color.yellow;
            Handles.DrawWireDisc(hitPosition, Vector3.up, snapRadius);
            Handles.DrawWireDisc(hitPosition, Vector3.forward, snapRadius);
            Handles.DrawWireDisc(hitPosition, Vector3.right, snapRadius);

            // Actual position where the point will be (either midle of the sphere or the snapped point).
            Handles.color = Color.red;
            Handles.DrawSolidDisc(snap, Vector3.up, handleSize * 0.1f);

            // Normal at the mouse.
            Handles.DrawLine(hitPosition, hitPosition + hitNormal * handleSize);
            Handles.color = Color.white;

            Vector3[] curve = System.Array.Empty<Vector3>();

            // Only do previews if there's at least one point to connect to.
            if (_freeformTrackHelper != null)
            {
                ClearPreviews();

                if (!_freeformTrackHelper.Locked)
                {
                    if (snapped && _freeformTrackHelper.HasStart)
                    {
                        _freeformTrackHelper.Next = snap;
                        _freeformTrackHelper.NextHandle = snapHandle;
                    }
                    else
                    {
                        _freeformTrackHelper.Next = null;
                        _freeformTrackHelper.NextHandle = null;
                    }
                }

                // Get the current attach point.
                AttachPoint ap = _freeformTrackHelper.ToAttachPoint();

                // Curve should go to the next point if there is one.
                // Use the formula used in the "Connect2" option.
                if (_freeformTrackHelper.IsNextSnapped)
                {
                    float length = (_freeformTrackHelper.Next.Value - ap.Position).magnitude * MathHelper.OneThird * _lengthMultiplier;
                    Vector3 dir = (_freeformTrackHelper.NextHandle.Value - _freeformTrackHelper.Next.Value).normalized;

                    curve = new Vector3[]{
                        ap.Position,
                        ap.Position - (ap.Handle - ap.Position).normalized * length,
                        _freeformTrackHelper.Next.Value - dir * length,
                        _freeformTrackHelper.Next.Value };
                }
                else
                {
                    // If not, then go to the mouse position.
                    curve = TrackToolsHelper.GetSmoothBezierToConnectMix(
                        ap.Position,
                        ap.Handle,
                        hitPosition, 135.0f, _smoothMix);
                }

                // In case we aren't snapped at the start...
                if (!_freeformTrackHelper.IsStartSnapped)
                {
                    // Make the curve in reverse, to create a handle for the start.
                    curve = TrackToolsHelper.GetSmoothBezierToConnectMix(
                        curve[3],
                        MathHelper.MirrorAround(curve[2], curve[3]),
                        curve[0], 135.0f, _smoothMix);

                    // Fix normal at the start too, then reverse it back.
                    curve[2] = Vector3.Lerp(curve[2],
                        TrackToolsHelper.HandleMatchNormal(curve[3], curve[2], _freeformTrackHelper.StartNormal), _fixToNormal);
                    curve = TrackToolsHelper.ReverseCurve(curve);

                    // This handle is not stored back or else the tool would consider it snapped,
                    // so it is recreated every time.
                }

                // Use the normal to try to match the curve with the terrain better.
                curve[2] = Vector3.Lerp(curve[2],
                    TrackToolsHelper.HandleMatchNormal(curve[3], curve[2], hitNormal), _fixToNormal);

                // Preview cache for the curve.
                // Curve itself.
                _newCache.Add(new PreviewPointCache(ap));
                _newCache[0].Lines = new Vector3[][] { MathHelper.SampleBezier(curve, _sampleCount) };
                _newCache[0].DrawButton = false;

                // If multi-track mode is enabled, preview parallel tracks
                if (_multiTrackMode)
                {
                    SimpleBezier mainBezier = new SimpleBezier(curve[0], curve[1], curve[2], curve[3]);

                    // Preview tracks on the right side (or only side if not both)
                    for (int i = 1; i <= _parallelTrackCount; i++)
                    {
                        float offset = _parallelTrackSpacing * i;
                        SimpleBezier offsetBezier = TrackToolsCreator.OffsetBezier(mainBezier, offset,
                            _parallelBothSides ? false : (_parallelTrackSide == TrackOrientation.Left));

                        Vector3[] parallelCurve = new Vector3[] { offsetBezier.P0, offsetBezier.P1, offsetBezier.P2, offsetBezier.P3 };
                        AttachPoint parallelAp = new AttachPoint(offsetBezier.P0, offsetBezier.P1);

                        _newCache.Add(new PreviewPointCache(parallelAp));
                        _newCache[_newCache.Count - 1].Lines = new Vector3[][] { MathHelper.SampleBezier(parallelCurve, _sampleCount) };
                        _newCache[_newCache.Count - 1].DrawButton = false;
                    }

                    // Preview tracks on the left side if both sides enabled
                    if (_parallelBothSides)
                    {
                        for (int i = 1; i <= _parallelTrackCount; i++)
                        {
                            float offset = _parallelTrackSpacing * i;
                            SimpleBezier offsetBezier = TrackToolsCreator.OffsetBezier(mainBezier, offset, true);

                            Vector3[] parallelCurve = new Vector3[] { offsetBezier.P0, offsetBezier.P1, offsetBezier.P2, offsetBezier.P3 };
                            AttachPoint parallelAp = new AttachPoint(offsetBezier.P0, offsetBezier.P1);

                            _newCache.Add(new PreviewPointCache(parallelAp));
                            _newCache[_newCache.Count - 1].Lines = new Vector3[][] { MathHelper.SampleBezier(parallelCurve, _sampleCount) };
                            _newCache[_newCache.Count - 1].DrawButton = false;
                        }
                    }
                }

                // Handle at the next position.
                _nextCache.Add(new PreviewPointCache(ap));
                _nextCache[0].Lines = new Vector3[][] { new Vector3[] { curve[0], curve[1] } };
                _nextCache[0].DrawButton = false;

                // Handle at the previous position.
                ap = new AttachPoint(curve[3], curve[2]);
                ap.Handle = MathHelper.MirrorAround(ap.Handle, ap.Position);

                _backCache.Add(new PreviewPointCache(ap));
                _backCache[0].Lines = new Vector3[][] { new Vector3[] { curve[3], curve[2] } };
                _backCache[0].DrawButton = false;

                // Grade visualisers along the curve.
                DrawCurveGrades(curve, false, _sampleCount);

                // If there's a track already created, draw the extra stuff as if it was selected.
                if (_freeformTrackHelper.WorkingTrack)
                {
                    DrawTrackExtraPreviews(scene, _freeformTrackHelper.WorkingTrack, _zTestTrack, _sampleCount);
                }
            }
            else
            {
                // Nothing created, so make sure it's clean.
                ClearPreviews();
            }

            // When clicking with the mouse...
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                // If the helper does not exist yet (completely new track)...
                if (_freeformTrackHelper == null)
                {
                    _freeformTrackHelper = new FreeformTrackHelper(hitNormal);
                    _freeformTrackHelper.CheckForSnap(hitPosition, snapRadius);
                }
                else
                {
                    // If there's no track yet...
                    if (!_freeformTrackHelper.WorkingTrack)
                    {
                        StartNewTrack(curve);
                    }
                    else
                    {
                        ContinueTrack(curve);
                    }

                    // If this point snapped, stop creating the track.
                    if (snapped)
                    {
                        StopFreeform();
                        Repaint();
                    }
                }

                e.Use();
            }
        }

        // Create a track from the curve.
        private void StartNewTrack(Vector3[] curve)
        {
            if (_multiTrackMode)
            {
                _freeformTrackHelper.WorkingTracks = TrackToolsCreator.CreateBezierMulti(_currentParent,
                    curve[0], curve[1], curve[2], curve[3],
                    _parallelTrackCount, _parallelTrackSpacing,
                    _parallelTrackSide == TrackOrientation.Left, _parallelBothSides);

                _freeformTrackHelper.UndoIndex = Undo.GetCurrentGroup();

                // Apply settings to all tracks
                foreach (Track track in _freeformTrackHelper.WorkingTracks)
                {
                    ApplySettingsToTrack(track);
                }

                // Set main track reference for compatibility
                _freeformTrackHelper.WorkingTrack = _freeformTrackHelper.WorkingTracks[0];
                _freeformTrackHelper.WorkingTrack.transform.parent.name = "Freeform Multi Track";
            }
            else
            {
                _freeformTrackHelper.WorkingTrack = TrackToolsCreator.CreateBezier(_currentParent,
                    curve[0], curve[1], curve[2], curve[3]);

                _freeformTrackHelper.UndoIndex = Undo.GetCurrentGroup();
                _freeformTrackHelper.WorkingTrack.name = "Freeform Track";
                ApplySettingsToTrack(_freeformTrackHelper.WorkingTrack);
            }
        }

        private void ContinueTrack(Vector3[] curve)
        {
            if (_multiTrackMode && _freeformTrackHelper.WorkingTracks != null)
            {
                // Continue all parallel tracks
                // The main track (index 0) uses the curve as-is
                // Parallel tracks need to be offset from their own last positions

                for (int trackIndex = 0; trackIndex < _freeformTrackHelper.WorkingTracks.Length; trackIndex++)
                {
                    Track track = _freeformTrackHelper.WorkingTracks[trackIndex];
                    BezierCurve bezierCurve = track.Curve;

                    if (trackIndex == 0)
                    {
                        // Main track - use the curve as-is
                        bezierCurve.Last().handleStyle = BezierPoint.HandleStyle.Broken;
                        bezierCurve.Last().globalHandle2 = curve[1];
                        bezierCurve.AddPointAt(curve[3]);
                        bezierCurve.Last().handleStyle = BezierPoint.HandleStyle.Broken;
                        bezierCurve.Last().globalHandle1 = curve[2];
                    }
                    else
                    {
                        // Calculate which side and offset amount
                        bool isLeft;
                        int sideIndex;

                        if (_parallelBothSides)
                        {
                            // Right side tracks: 1 to _parallelTrackCount
                            // Left side tracks: _parallelTrackCount+1 to _parallelTrackCount*2
                            if (trackIndex <= _parallelTrackCount)
                            {
                                isLeft = false;
                                sideIndex = trackIndex;
                            }
                            else
                            {
                                isLeft = true;
                                sideIndex = trackIndex - _parallelTrackCount;
                            }
                        }
                        else
                        {
                            isLeft = _parallelTrackSide == TrackOrientation.Left;
                            sideIndex = trackIndex;
                        }

                        float offset = _parallelTrackSpacing * sideIndex;

                        // Use the main track's NEW segment to calculate offset using the improved OffsetBezier method
                        SimpleBezier mainSegment = new SimpleBezier(curve[0], curve[1], curve[2], curve[3]);
                        SimpleBezier offsetSegment = TrackToolsCreator.OffsetBezier(mainSegment, offset, isLeft);

                        // Use the calculated offset bezier for all points
                        Vector3 offsetP1 = offsetSegment.P1;
                        Vector3 offsetP2 = offsetSegment.P2;
                        Vector3 offsetP3 = offsetSegment.P3;

                        bezierCurve.Last().handleStyle = BezierPoint.HandleStyle.Broken;
                        bezierCurve.Last().globalHandle2 = offsetP1;
                        bezierCurve.AddPointAt(offsetP3);
                        bezierCurve.Last().handleStyle = BezierPoint.HandleStyle.Broken;
                        bezierCurve.Last().globalHandle1 = offsetP2;
                    }
                }
            }
            else
            {
                BezierCurve track = _freeformTrackHelper.WorkingTrack.Curve;
                track.Last().handleStyle = BezierPoint.HandleStyle.Broken;
                track.Last().globalHandle2 = curve[1];
                track.AddPointAt(curve[3]);
                track.Last().handleStyle = BezierPoint.HandleStyle.Broken;
                track.Last().globalHandle1 = curve[2];
            }
        }
    }
}
#endif
