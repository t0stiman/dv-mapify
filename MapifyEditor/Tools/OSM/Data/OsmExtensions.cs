using OsmSharp;
using OsmSharp.Complete;
using OsmSharp.Tags;
using System.Collections.Generic;
using System.Linq;

namespace Mapify.Editor.Tools.OSM.Data
{
    public static class OsmExtensions
    {
        public static bool IsValid(this Node node)
        {
            return node.Id.HasValue && node.Longitude.HasValue && node.Latitude.HasValue;
        }

        public static string GetNameOrId(this OsmGeo osmGeo)
        {
            return osmGeo.Tags != null && osmGeo.Tags.ContainsKey("name") ? osmGeo.Tags["name"] : osmGeo.Id.ToString();
        }

        public static string GetNameOrId(this CompleteOsmGeo osmGeo)
        {
            return osmGeo.Tags != null && osmGeo.Tags.ContainsKey("name") ? osmGeo.Tags["name"] : osmGeo.Id.ToString();
        }

        public static Dictionary<string, string> ToDictionary(this TagsCollectionBase tags)
        {
            return tags.ToDictionary(i => i.Key, i => i.Value);
        }

        public static NodeTag[] ToNodeTagArray(this TagsCollectionBase tags)
        {
            if (tags == null)
            {
                return new NodeTag[0];
            }

            return tags.Select(x => (NodeTag)x).ToArray();
        }

        public static bool ContainsKey(this NodeTag[] tags, string key, out int index)
        {
            index = -1;

            for (int i = 0; i < tags.Length; i++)
            {
                if (tags[i].Key == key)
                {
                    index = i;
                    return true;
                }
            }

            return false;
        }

        public static bool ContainsKey(this NodeTag[] tags, string key)
        {
            for (int i = 0; i < tags.Length; i++)
            {
                if (tags[i].Key == key)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
