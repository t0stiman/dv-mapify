using OsmSharp.Tags;
using System;

namespace Mapify.Editor.Tools.Osm
{
    [Serializable]
    public struct NodeTag
    {
        public string Key;
        public string Value;

        public NodeTag(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public static explicit operator Tag(NodeTag tag) => new Tag(tag.Key, tag.Value);
        public static explicit operator NodeTag(Tag tag) => new NodeTag(tag.Key, tag.Value);

        public bool HasValue => !string.IsNullOrEmpty(Value);
    }
}
