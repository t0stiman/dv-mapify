using System;
using UnityEngine;

namespace Mapify.Editor.Tools.OSM.Data
{
    [Serializable]
    public class TrackNodeHandle
    {
        public Vector3 Direction = Vector3.zero;
        public float Size = 0;

        public TrackNodeHandle(Vector3 direction, float size)
        {
            Direction = direction;
            Size = size;
        }

        public TrackNodeHandle() : this(Vector3.zero, 0) { }

        public Vector3 FullHandle => Direction * Size;
    }
}
