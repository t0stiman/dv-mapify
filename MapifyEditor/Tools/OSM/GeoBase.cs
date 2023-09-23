using System;

namespace Mapify.Editor.Tools.Osm
{
    [Serializable]
    public class GeoBase
    {
        public long Id;
        public NodeTag[] Tags;
        // Name should be one of the tags but is stored separately too
        // for ease of access.
        public string Name;

        public GeoBase(long id, NodeTag[] tags, string name)
        {
            Id = id;
            Tags = tags;
            Name = name;
        }
    }
}
