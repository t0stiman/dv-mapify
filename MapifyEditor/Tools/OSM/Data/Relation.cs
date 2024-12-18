using OsmSharp.Complete;
using System;

namespace Mapify.Editor.Tools.OSM.Data
{
    [Serializable]
    public class Relation : GeoBase
    {
        public Relation(CompleteRelation relation) : base(
            relation.Id,
            relation.Tags.ToNodeTagArray(),
            relation.GetNameOrId())
        {

        }
    }
}
