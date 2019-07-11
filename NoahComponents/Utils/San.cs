using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noah.Utils
{
    public static class San
    {
        public enum ClockDirection
        {
            None,
            Clockwise,
            Counterclockwise
        }

        public enum LineSide
        {
            None,
            Left,
            Right
        }

        public enum PolygonType
        {
            /// <summary>
            /// 无
            /// </summary>
            None,

            /// <summary>
            /// 凸
            /// </summary>
            Convex,

            /// <summary>
            /// 凹
            /// </summary>
            Concave
        }

        public static double Map(double val, double s1, double e1, double s2, double e2)
        {
            return s2 + (e2 - s2) * ((val - s1) / (e1 - s1));
        }

        public static LineSide X2D(Line l, Point3d p)
        {
            Vector3d s = new Vector3d(l.From);
            Vector3d dir = l.Direction;
            double res = (p.X - s.X) * dir.Y - (p.Y - s.Y) * dir.X;
            if (res > 0)
            {
                return LineSide.Right;
            }
            else if (res < 0)
            {
                return LineSide.Left;
            }
            else return LineSide.None;
        }

        public static ClockDirection CalculateClockDirection(List<Point3d> points)
        {
            int i, j, k;
            int count = 0;
            double z;
            if (points == null || points.Count < 3)
            {
                return (0);
            }
            int n = points.Count;
            for (i = 0; i < n; i++)
            {
                j = (i + 1) % n;
                k = (i + 2) % n;
                z = (points[j].X - points[i].X) * (points[k].Y - points[j].Y);
                z -= (points[j].Y - points[i].Y) * (points[k].X - points[j].X);
                if (z < 0)
                {
                    count--;
                }
                else if (z > 0)
                {
                    count++;
                }
            }
            if (count > 0)
            {
                return (ClockDirection.Counterclockwise);
            }
            else if (count < 0)
            {
                return (ClockDirection.Clockwise);
            }
            else
            {
                return (ClockDirection.None);
            }
        }

        public static PolygonType CalculatePolygonType(List<Point3d> points, bool isYAxixToDown)
        {
            int i, j, k;
            int flag = 0;
            double z;

            if (points == null || points.Count < 3)
            {
                return (0);
            }
            int n = points.Count;
            int yTrans = isYAxixToDown ? (-1) : (1);
            for (i = 0; i < n; i++)
            {
                j = (i + 1) % n;
                k = (i + 2) % n;
                z = (points[j].X - points[i].X) * (points[k].Y * yTrans - points[j].Y * yTrans);
                z -= (points[j].Y * yTrans - points[i].Y * yTrans) * (points[k].X - points[j].X);
                if (z < 0)
                {
                    flag |= 1;
                }
                else if (z > 0)
                {
                    flag |= 2;
                }
                if (flag == 3)
                {
                    return (PolygonType.Concave);
                }
            }
            if (flag != 0)
            {
                return (PolygonType.Convex);
            }
            else
            {
                return (PolygonType.None);
            }
        }
    }
}
