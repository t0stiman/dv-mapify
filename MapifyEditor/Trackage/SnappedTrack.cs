namespace Mapify.Editor
{
    internal class SnappedTrack
    {
        public Track Track { get; private set; }
        public BezierPoint Point { get; private set; }

        public SnappedTrack(Track aTrack, BezierPoint aPoint)
        {
            Track = aTrack;
            Point = aPoint;
        }

        public void UnSnapped()
        {
            if (Track == null){ return; }
            Track.UnSnapped(Point);
        }
    }
}
