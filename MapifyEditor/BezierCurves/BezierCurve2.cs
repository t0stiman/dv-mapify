using System;
using UnityEngine;

namespace Mapify.Editor.BezierCurves
{
    public class BezierCurve2 : BezierCurve
    {
        // todo better performance
        private new void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            if (this.points.Length <= 1)
                return;
            for (int index = 0; index < this.points.Length - 1; ++index)
                BezierCurve.DrawCurve(this.points[index], this.points[index + 1], this.resolution);
            if (this.close)
                BezierCurve.DrawCurve(this.points[this.points.Length - 1], this.points[0], this.resolution);
            if (!this.mirror)
                return;
            for (int index = 0; index < this.points.Length - 1; ++index)
                BezierCurve.DrawCurveMirrored(this.transform, this.points[index], this.points[index + 1], this.resolution, this.axis);
        }
    }
}
