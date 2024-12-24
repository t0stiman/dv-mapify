using System.Collections.Generic;
using System.Linq;

namespace Mapify.Editor.Tools.OSM
{
    public class TrackWaySegment
    {
        private List<long> nodeIDs;

        public TrackWaySegment(long firstNodeID)
        {
            nodeIDs = new List<long> { firstNodeID };
        }

        public void Add(long nodeID) => nodeIDs.Add(nodeID);
        public int Count => nodeIDs.Count;
        public long First => nodeIDs[0];
        public long Last => nodeIDs.Last();

        public long this[int index]
        {
            get => nodeIDs[index];
            set => nodeIDs[index] = value;
        }
    }
}
