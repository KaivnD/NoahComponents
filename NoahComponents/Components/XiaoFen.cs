﻿using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noah.Components
{
    public class XiaoFen : GH_Component
    {
        public XiaoFen()
            : base("小分", "分割多段线", "指定多段线长边随机切割至指定面积", "Noah", "Utils")
        {

        }

        public override Guid ComponentGuid => new Guid("2D646A1F-EE0C-472E-832C-3F38ADA11F56");

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("目标曲线", "C", "指定一条被分割的PolyLine", GH_ParamAccess.item);
            pManager.AddNumberParameter("面积限制", "L", "当划分用地大于面积限制时进行进一步划分", GH_ParamAccess.item, 15000);
            pManager.AddIntegerParameter("随机因子", "S", "随机因子", GH_ParamAccess.item, 35);
            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("生成的分割线", "D", "生成的分割线列表", GH_ParamAccess.list);
        }

        private List<Line> dividers = new List<Line>();

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve C = null;
            double L = 15000;
            int S = 35;
            DA.GetData(0, ref C);
            DA.GetData(1, ref L);
            DA.GetData(2, ref S);
            if (!C.IsClosed || L < 100) return;
            dividers.Clear();
            Polyline pl = null;
            C.TryGetPolyline(out pl);
            if (pl != null)
            {
                List<Polyline> pls = new List<Polyline>() { pl };
                double maxArea = AreaMassProperties.Compute(pl.ToPolylineCurve()).Area;
                List<double> areaList = new List<double>();
                while (maxArea > L)
                {                    
                    Gen(pls, S, out pls, out areaList);
                    maxArea = areaList.Max();
                }
                DA.SetDataList(0, dividers);
            }
            else return;
        }
        private void Gen(List<Polyline> pls, int seed, out List<Polyline> outPls, out List<double> areaLis)
        {
            List<Polyline> nextGen = new List<Polyline>();
            List<double> nextArea = new List<double>();
            foreach (Polyline pl in pls)
            {
                Point3d[] pts = pl.ToArray();
                double[] ptsLength = new double[pts.Length - 1];
                for (int i = 0; i < pts.Length; i++)
                {
                    if (i > 0)
                    {
                        Point3d prev = pts[i - 1];
                        ptsLength[i - 1] = pts[i].DistanceTo(prev);
                    }
                }
                int maxIdx = Array.IndexOf(ptsLength, ptsLength.Max());
                Line longestLine = new Line(pts[maxIdx], pts[maxIdx + 1]);

                Random rand = new Random(seed);
                double t2 = rand.NextDouble();
                double t = Map(t2, 0, 1, 0.2, 0.7);
                Vector3d normal = new Vector3d(longestLine.Direction);
                Point3d basePoint = longestLine.PointAt(t);
                Plane basePlane = new Plane(basePoint, normal);
                var intersection = Intersection.CurvePlane(pl.ToPolylineCurve(), basePlane, 0);
                Line divider = new Line(intersection[0].PointA, intersection[1].PointA);
                dividers.Add(divider);
                Vector3d dividerDir = divider.Direction;
                Polyline leftPl = new Polyline();
                Polyline rightPl = new Polyline();
                for (int i = 0; i < pl.Count; i++)
                {
                    if (X2D(divider, pts[i]) == LineSide.Right)
                    {
                        rightPl.Add(pts[i]);
                    }
                    else if (X2D(divider, pts[i]) == LineSide.Left) leftPl.Add(pts[i]);
                }

                ClockDirection cDir = CalculateClockDirection(pl.ToList());
                LineSide cSide = X2D(divider, pl.First);
                if (cDir == ClockDirection.Clockwise
                  && cSide == LineSide.Left)
                {
                    rightPl.Add(divider.From);
                    rightPl.Add(divider.To);
                    rightPl.Add(rightPl[0]);

                    int index = pl.IndexOf(rightPl[0]);
                    leftPl.Insert(index, divider.To);
                    leftPl.Insert(index + 1, divider.From);
                }
                else if (cDir == ClockDirection.Counterclockwise
                && cSide == LineSide.Left)
                {
                    rightPl.Add(divider.To);
                    rightPl.Add(divider.From);
                    rightPl.Add(rightPl[0]);

                    int index = pl.IndexOf(rightPl[0]);
                    leftPl.Insert(index, divider.From);
                    leftPl.Insert(index + 1, divider.To);
                }
                else if (cDir == ClockDirection.Clockwise
                && cSide == LineSide.Right)
                {
                    leftPl.Add(divider.To);
                    leftPl.Add(divider.From);
                    leftPl.Add(leftPl[0]);

                    int index = pl.IndexOf(leftPl[0]);
                    rightPl.Insert(index, divider.From);
                    rightPl.Insert(index + 1, divider.To);
                }
                else if (cDir == ClockDirection.Counterclockwise
                && cSide == LineSide.Right)
                {
                    leftPl.Add(divider.From);
                    leftPl.Add(divider.To);
                    leftPl.Add(leftPl[0]);

                    int index = pl.IndexOf(leftPl[0]);
                    rightPl.Insert(index, divider.To);
                    rightPl.Insert(index + 1, divider.From);
                }
                nextGen.Add(leftPl);
                nextGen.Add(rightPl);

                double areaL = AreaMassProperties.Compute(leftPl.ToPolylineCurve()).Area;
                double areaR = AreaMassProperties.Compute(leftPl.ToPolylineCurve()).Area;
                nextArea.Add(areaL);
                nextArea.Add(areaR);
            }
            areaLis = nextArea;
            outPls = nextGen;
        }

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

        public double Map(double val, double s1, double e1, double s2, double e2)
        {
            return s2 + (e2 - s2) * ((val - s1) / (e1 - s1));
        }

        public LineSide X2D(Line l, Point3d p)
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
