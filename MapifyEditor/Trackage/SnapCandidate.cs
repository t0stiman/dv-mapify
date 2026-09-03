using UnityEngine;

namespace Mapify.Editor
{
    internal enum SnapType
    {
        None,
        Track,
        Turntable
    }

    internal sealed class SnapCandidate
    {
        // None
        internal SnapCandidate()
        {
            Type = SnapType.None;
            SquaredDistance = float.MaxValue;
        }

        // Track
        internal SnapCandidate(BezierPoint point, float squaredDistance)
        {
            Type = SnapType.Track;
            Point = point;
            SnapPosition = point.position;
            SquaredDistance = squaredDistance;
        }

        // Turntable
        internal SnapCandidate(Turntable _, float squaredDistance, Vector3 snapPosition)
        {
            Type = SnapType.Turntable;
            SquaredDistance = squaredDistance;
            SnapPosition = snapPosition;
        }

        public SnapType Type { get; private set; }
        public BezierPoint Point { get; private set; }
        public float SquaredDistance { get; private set; }
        public Vector3 SnapPosition { get; private set; }
    }
}
