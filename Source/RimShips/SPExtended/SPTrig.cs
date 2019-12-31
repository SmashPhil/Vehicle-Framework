using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SPExtended
{
    public static class SPTrig
    {
        /// <summary>
        /// Rotate point clockwise by angle theta
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="theta"></param>
        /// <returns></returns>
        public static SPTuples.SPTuple2<float, float> RotatePointClockwise(float x, float y, float theta)
        {
            theta = -theta;
            float xPrime = (float)(x * Math.Cos(theta.DegreesToRadians())) - (float)(y * Math.Sin(theta.DegreesToRadians()));
            float yPrime = (float)(x * Math.Sin(theta.DegreesToRadians())) + (float)(y * Math.Cos(theta.DegreesToRadians()));
            return new SPTuples.SPTuple2<float, float>(xPrime, yPrime);
        }

        /// <summary>
        /// Rotate point counter clockwise by angle theta
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="theta"></param>
        /// <returns></returns>
        public static SPTuples.SPTuple2<float, float> RotatePointCounterClockwise(float x, float y, float theta)
        {
            float xPrime = (float)(x * Math.Cos(theta.DegreesToRadians())) - (float)(y * Math.Sin(theta.DegreesToRadians()));
            float yPrime = (float)(x * Math.Sin(theta.DegreesToRadians())) + (float)(y * Math.Cos(theta.DegreesToRadians()));
            return new SPTuples.SPTuple2<float, float>(xPrime, yPrime);
        }

        /// <summary>
        /// Convert degrees (double) to radians
        /// </summary>
        /// <param name="deg"></param>
        /// <returns></returns>
        public static double DegreesToRadians(this double deg)
        {
            return deg * Math.PI / 180;
        }

        /// <summary>
        /// Convert degrees (float) to radians
        /// </summary>
        /// <param name="deg"></param>
        /// <returns></returns>
        public static double DegreesToRadians(this float deg)
        {
            return Convert.ToDouble(deg).DegreesToRadians();
        }

        /// <summary>
        /// Convert Radians to degrees
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static double RadiansToDegrees(this double radians)
        {
            return radians * (180 / Math.PI);
        }

        /// <summary>
        /// Calculate angle from origin to point on map relative to positive x axis
        /// </summary>
        /// <param name="c"></param>
        /// <param name="map"></param>
        /// <returns></returns>
        public static double AngleThroughOrigin(this IntVec3 c, Map map)
        {
            int xPrime = c.x - (map.Size.x / 2);
            int yPrime = c.z - (map.Size.z / 2);
            double slope = (double)yPrime / (double)xPrime;
            double angleRadians = Math.Atan(slope);
            double angle = Math.Abs(angleRadians.RadiansToDegrees());
            switch (SPExtra.Quadrant.QuadrantOfIntVec3(c, map).AsInt)
            {
                case 2:
                    return 360 - angle;
                case 3:
                    return 180 + angle;
                case 4:
                    return 180 - angle;
            }
            return angle;
        }

        /// <summary>
        /// Calculate angle between 2 points on Cartesian coordinate plane.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="point"></param>
        /// <param name="map"></param>
        /// <returns></returns>
        public static double AngleToPoint(this IntVec3 pos, IntVec3 point, Map map)
        {
            int xPrime = pos.x - point.x;
            int yPrime = pos.z - point.z;
            double slope = (double)yPrime / (double)xPrime;
            double angleRadians = Math.Atan(slope);
            double angle = Math.Abs(angleRadians.RadiansToDegrees());
            switch(SPExtra.Quadrant.QuadrantRelativeToPoint(pos, point, map).AsInt)
            {
                case 2:
                    return 360 - angle;
                case 3:
                    return 180 + angle;
                case 4:
                    return 180 - angle;
            }
            return angle;
        }

        /// <summary>
        /// Determine whether point C is left or right of the line from point A looking towards point B
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <param name="C"></param>
        /// <returns></returns>
        public static int LeftRightOfLine(IntVec3 A, IntVec3 B, IntVec3 C)
        {
            return Math.Sign((B.x - A.x) * (C.z - A.z) - (B.z - A.z) * (C.x - A.x));
        }

        /// <summary>
        /// Get point on edge of square map given angle (0 to 360) relative to x axis from origin
        /// </summary>
        /// <param name="angle"></param>
        /// <param name="map"></param>
        /// <returns></returns>
        public static IntVec3 PointFromOrigin(double angle, Map map)
        {
            int a = map.Size.x;
            int b = map.Size.z;

            if (angle < 0 || angle > 360)
                return IntVec3.Invalid;

            Rot4 rayDir = Rot4.Invalid;
            if (angle <= 45 || angle > 315)
                rayDir = Rot4.East;
            else if (angle <= 135 && angle >= 45)
                rayDir = Rot4.North;
            else if (angle <= 225 && angle >= 135)
                rayDir = Rot4.West;
            else if (angle <= 315 && angle >= 225)
                rayDir = Rot4.South;
            else
                return new IntVec3(b / 2, 0, 1);
            var v = Math.Tan(angle.DegreesToRadians());
            switch (rayDir.AsInt)
            {
                case 0: //North
                    return new IntVec3((int)(b / (2 * v) + b / 2), 0, b - 1);
                case 1: //East
                    return new IntVec3(a - 1, 0, (int)(a / 2 * v) + a / 2);
                case 2: //South
                    return new IntVec3((int)(b - (b / (2 * v) + b / 2)), 0, 1);
                case 3: //West
                    return new IntVec3(1, 0, (int)(a - ((a / 2 * v) + a / 2)));
            }

            return IntVec3.Invalid;
        }
    }
}
