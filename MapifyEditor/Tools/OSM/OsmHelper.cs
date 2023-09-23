using Mapify.Editor.Utils;
using OsmSharp;
using System;
using UnityEngine;

namespace Mapify.Editor.Tools.Osm
{
    public static class OsmHelper
    {
        private const double PiOver2 = Math.PI / 2.0;
        private const double DegToRad = Math.PI / 180.0;

        // TODO: use a better average based on latitude.
        public const double EarthRadiusAprox = 6371008.7714;

        public static Vector3 NodeToVector3(Node node, double offsetLat, double offsetLon, double radius = EarthRadiusAprox)
        {
            // Creates a vector, make sure to use doubles for calculation before to not
            // lose any precision.
            var latlon = ProjectCoords(node.Latitude.Value - offsetLat, node.Longitude.Value - offsetLon, radius);
            return new Vector3((float)latlon.X, 0, (float)latlon.Z);
        }

        public static (double X, double Z) ProjectCoords(double lat, double lon, double radius = EarthRadiusAprox)
        {
            // Mercator shenanigans.
            // Z formula slightly altered because the end result was squished...
            return (radius * lon * DegToRad * MathHelper.TwoThirds,
                radius * Math.Log(Math.Tan((45.0 + lat / 2.0) * DegToRad)) * PiOver2 * MathHelper.TwoThirds);
        }
    }
}
