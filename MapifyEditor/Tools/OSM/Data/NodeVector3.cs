using OsmSharp;
using System;
using UnityEngine;

namespace Mapify.Editor.Tools.OSM.Data
{
    [Serializable]
    public class NodeVector3 : GeoBase
    {
        public Vector3 Position;

        public NodeVector3(Node node, double offsetLat, double offsetLon, Vector3 centre) : base(
            node.Id.Value,
            node.Tags.ToNodeTagArray(),
            node.GetNameOrId())
        {
            Position = OsmHelper.NodeToVector3(node, offsetLat, offsetLon) + centre;
        }
    }
}
