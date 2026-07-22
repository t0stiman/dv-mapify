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
            Distance = float.MaxValue;
        }

        // Track
        internal SnapCandidate(BezierPoint point, float distance)
        {
            Type = SnapType.Track;
            Point = point;
            Distance = distance;
        }

        // Turntable
        internal SnapCandidate(Track turnTableTrack, float distance)
        {
            Type = SnapType.Turntable;
            TurnTableTrack = turnTableTrack;
            Distance = distance;
        }

        public SnapType Type { get; private set; }
        public BezierPoint Point { get; private set; }
        public Track TurnTableTrack { get; private set; }
        public float Distance { get; private set; }
    }
}
