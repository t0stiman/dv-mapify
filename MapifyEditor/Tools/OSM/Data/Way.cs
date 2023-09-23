using OsmSharp.Complete;
using System;
using System.Linq;

namespace Mapify.Editor.Tools.OSM.Data
{
    [Serializable]
    public class Way : GeoBase
    {
        public long[] Nodes;

        public Way(CompleteWay way) : base(
            way.Id,
            way.Tags.ToNodeTagArray(),
            way.GetNameOrId())
        {
            Nodes = way.Nodes.Select(x => x.Id.Value).ToArray();
        }
    }
}
