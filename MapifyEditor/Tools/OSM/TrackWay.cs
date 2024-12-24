using Mapify.Editor.Tools.OSM.Data;
using UnityEngine;
using UnityEngine.Serialization;

namespace Mapify.Editor.Tools.OSM
{
    public class TrackWay : MonoBehaviour
    {
        public long Id;
        [FormerlySerializedAs("Nodes")] public long[] NodeIDs;
        public NodeTag[] Tags = new NodeTag[0];
        public TrackWaySegment[] Segments = {};
    }
}
