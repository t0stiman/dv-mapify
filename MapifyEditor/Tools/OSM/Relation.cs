using OsmSharp.Complete;
using System;

namespace Mapify.Editor.Tools.Osm
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
