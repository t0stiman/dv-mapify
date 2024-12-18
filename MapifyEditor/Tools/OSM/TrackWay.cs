using Mapify.Editor.Tools.OSM.Data;
using UnityEngine;

namespace Mapify.Editor.Tools.OSM
{
    public class TrackWay : MonoBehaviour
    {
        public long Id;
        public long[] Nodes;
        public NodeTag[] Tags = new NodeTag[0];
        public long[][] Segments = new long[0][];
    }
}
