//'Pachyderm-Acoustic: Geometrical Acoustics for Rhinoceros (GPL)  
//' 
//'This file is part of Pachyderm-Acoustic. 
//' 
//'Copyright (c) 2008-2025, Open Research in Acoustical Science and Education, Inc. - a 501(c)3 nonprofit 
//'Pachyderm-Acoustic is free software; you can redistribute it and/or modify 
//'it under the terms of the GNU General Public License as published 
//'by the Free Software Foundation; either version 3 of the License, or 
//'(at your option) any later version. 
//'Pachyderm-Acoustic is distributed in the hope that it will be useful, 
//'but WITHOUT ANY WARRANTY; without even the implied warranty of 
//'MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
//'GNU General Public License for more details. 
//' 
//'You should have received a copy of the GNU General Public 
//'License along with Pachyderm-Acoustic; if not, write to the Free Software 
//'Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA. 

using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;

namespace Pachyderm_Acoustic
{
    namespace UI
    {
        /// <summary>
        /// Handles Feedback for Auralisation.
        /// </summary>
        public class AuralisationConduit : Rhino.Display.DisplayConduit
        {
            private readonly DummyHeadGlyph Head = new DummyHeadGlyph(0.105);

            public AuralisationConduit()
            {
                Instance = this;
            }

            ///<summary>The only instance of this conduit.</summary>
            public static AuralisationConduit Instance
            {
                get;
                private set;
            }

            private void ClearConduit(object sender, EventArgs e)
            {
                this.Enabled = false;
            }

            protected override void DrawForeground(Rhino.Display.DrawEventArgs e)
            {
                if (Recs != null) e.Display.DrawPointCloud(Recs, 5, System.Drawing.Color.Green);
                if (Srcs != null) e.Display.DrawPointCloud(Srcs, 5, System.Drawing.Color.Red);
                if (BinauralHeadVisible) Head.Draw(e.Display, BinauralHeadOrigin, BinauralHeadDirection, System.Drawing.Color.Black, System.Drawing.Color.DimGray, System.Drawing.Color.Red);
                else if (Dir != null) e.Display.DrawLineArrow(Dir, System.Drawing.Color.Red, 3, .1);
                if (Reflections != null) foreach (Rhino.Geometry.Polyline L in Reflections) e.Display.DrawDottedPolyline(L.AsEnumerable<Point3d>(), System.Drawing.Color.GreenYellow, false);
                if (Speakers != null)
                    foreach (Rhino.Geometry.Line Sp in Speakers)
                    {
                        //Draw Speaker Cabinets, for clarity.
                        Rhino.Geometry.Box BB = new Box(new Rhino.Geometry.Plane(Sp.From, Sp.UnitTangent), new BoundingBox(-.125, -.15, -.18, .125, .15, .18));
                        e.Display.DrawBox(BB, System.Drawing.Color.Blue);
                        e.Display.DrawLineArrow(Sp, System.Drawing.Color.Blue, 1, .05);
                    }
            }

            PointCloud Recs;
            PointCloud Srcs;
            List<Polyline> Reflections;
            List<Line> Speakers;
            Line Dir;
            bool BinauralHeadVisible;
            Point3d BinauralHeadOrigin;
            Vector3d BinauralHeadDirection;

            public void add_Receivers(IEnumerable<Hare.Geometry.Point> pts)
            {
                this.Enabled = true;
                List<Point3d> PTS = new List<Point3d>();
                foreach (Hare.Geometry.Point p in pts) PTS.Add(Utilities.RCPachTools.HPttoRPt(p));
                Recs = new PointCloud(PTS);
            }

            public void add_Sources(IEnumerable<Hare.Geometry.Point> pts)
            {
                List<Rhino.Geometry.Point3d> PTS = new List<Point3d>();
                foreach(Hare.Geometry.Point p in pts) PTS.Add(Utilities.RCPachTools.HPttoRPt(p));
                add_Sources(PTS);
            }

            public void add_Sources(IEnumerable<Rhino.Geometry.Point3d> pts)
            {
                this.Enabled = true;
                Srcs = new PointCloud(pts);
            }

            public void add_Reflections(IEnumerable<Rhino.Geometry.Polyline> refs)
            {
                this.Enabled = true;
                Reflections = refs.ToList<Rhino.Geometry.Polyline>();
            }

            public void add_Speakers(IEnumerable<Hare.Geometry.Point> pts, IEnumerable<Rhino.Geometry.Vector3d> vec)
            {
                this.Enabled = true;
                BinauralHeadVisible = false;
                Speakers = new List<Line>();
                for (int i = 0; i < pts.Count<Hare.Geometry.Point>(); i++)
                {
                    Hare.Geometry.Vector V = new Hare.Geometry.Vector(-vec.ElementAt<Vector3d>(i).X, -vec.ElementAt<Vector3d>(i).Y, -vec.ElementAt<Vector3d>(i).Z);
                    Speakers.Add(new Line(Utilities.RCPachTools.HPttoRPt(pts.ElementAt(i)), Utilities.RCPachTools.HPttoRPt(pts.ElementAt(i) - V)));
                }
            }

            public void set_direction(Rhino.Geometry.Point3d rec, Rhino.Geometry.Point3d dir)
            {
                this.Enabled = true;
                BinauralHeadVisible = false;
                Rhino.Geometry.Vector3d V = new Vector3d(dir.X, dir.Y, dir.Z);
                V.Unitize();
                Dir = new Rhino.Geometry.Line(rec, rec + V);
            }

            public void set_direction(Rhino.Geometry.Point3d rec, Rhino.Geometry.Vector3d V)
            {
                this.Enabled = true;
                BinauralHeadVisible = false;
                V.Unitize();
                Dir = new Rhino.Geometry.Line(rec, rec + V);
            }

            public void set_binaural_head(Rhino.Geometry.Point3d rec, Rhino.Geometry.Point3d dir)
            {
                Rhino.Geometry.Vector3d V = new Vector3d(dir.X, dir.Y, dir.Z);
                set_binaural_head(rec, V);
            }

            public void set_binaural_head(Rhino.Geometry.Point3d rec, Rhino.Geometry.Vector3d V)
            {
                this.Enabled = true;
                if (!V.Unitize()) V = Vector3d.XAxis;
                BinauralHeadVisible = true;
                BinauralHeadOrigin = rec;
                BinauralHeadDirection = V;
                Speakers = null;
                Dir = new Rhino.Geometry.Line(rec, rec + V);
            }

            private class DummyHeadGlyph
            {
                private readonly double r;

                public DummyHeadGlyph(double radius)
                {
                    r = radius;
                }

                public void Draw(Rhino.Display.DisplayPipeline display, Point3d origin, Vector3d front, System.Drawing.Color body, System.Drawing.Color ears, System.Drawing.Color face)
                {
                    Vector3d right;
                    Vector3d up;
                    MakeFrame(front, out right, out up);

                    foreach (Line l in HeadForm(origin, front, right, up)) display.DrawLine(l, body, 2);
                    foreach (Line l in EarLines(origin, front, right, up)) display.DrawLine(l, ears, 2);
                    foreach (Line l in FaceLines(origin, front, right, up)) display.DrawLine(l, face, 2);
                }

                private List<Line> HeadForm(Point3d origin, Vector3d front, Vector3d right, Vector3d up)
                {
                    List<Line> lines = new List<Line>();
                    Point3d skull = origin + up * r * 0.13;
                    AddEllipse(lines, skull, right, up, r * 0.42, r * 0.56, 30);
                    AddEllipse(lines, skull - front * r * 0.02, front, up, r * 0.34, r * 0.56, 30);
                    AddEllipse(lines, origin + up * r * 0.08, front, right, r * 0.34, r * 0.42, 28);

                    AddCubic(
                        lines,
                        origin + front * r * 0.26 + up * r * -0.34,
                        origin + front * r * 0.12 + up * r * -0.54,
                        origin + front * r * -0.22 + up * r * -0.60,
                        origin + front * r * -0.28 + up * r * -0.30,
                        14);

                    AddCubic(
                        lines,
                        origin + front * r * 0.05 + right * r * 0.27 + up * r * -0.42,
                        origin + front * r * -0.04 + right * r * 0.33 + up * r * -0.56,
                        origin + front * r * -0.24 + right * r * 0.22 + up * r * -0.70,
                        origin + front * r * -0.30 + right * r * 0.08 + up * r * -0.78,
                        10);

                    AddCubic(
                        lines,
                        origin + front * r * 0.05 + right * r * -0.27 + up * r * -0.42,
                        origin + front * r * -0.04 + right * r * -0.33 + up * r * -0.56,
                        origin + front * r * -0.24 + right * r * -0.22 + up * r * -0.70,
                        origin + front * r * -0.30 + right * r * -0.08 + up * r * -0.78,
                        10);

                    return lines;
                }

                private List<Line> EarLines(Point3d origin, Vector3d front, Vector3d right, Vector3d up)
                {
                    List<Line> lines = new List<Line>();
                    Point3d left = origin - right * r * 0.46 + up * r * 0.12;
                    Point3d rightEar = origin + right * r * 0.46 + up * r * 0.12;

                    AddPinna(lines, left, front, -right, up);
                    AddPinna(lines, rightEar, front, right, up);

                    return lines;
                }

                private List<Line> FaceLines(Point3d origin, Vector3d front, Vector3d right, Vector3d up)
                {
                    List<Line> lines = new List<Line>();

                    AddCubic(
                        lines,
                        origin + front * r * 0.18 + up * r * 0.43,
                        origin + front * r * 0.34 + up * r * 0.34,
                        origin + front * r * 0.46 + up * r * 0.25,
                        origin + front * r * 0.53 + up * r * 0.13,
                        10);

                    AddCubic(
                        lines,
                        origin + front * r * 0.53 + up * r * 0.13,
                        origin + front * r * 0.42 + up * r * 0.07,
                        origin + front * r * 0.36 + up * r * 0.02,
                        origin + front * r * 0.38 + up * r * -0.04,
                        8);

                    AddCubic(
                        lines,
                        origin + front * r * 0.36 + up * r * -0.13,
                        origin + front * r * 0.28 + up * r * -0.18,
                        origin + front * r * 0.25 + up * r * -0.25,
                        origin + front * r * 0.29 + up * r * -0.34,
                        8);

                    AddCubic(
                        lines,
                        origin + front * r * 0.26 + right * r * -0.20 + up * r * 0.20,
                        origin + front * r * 0.32 + right * r * -0.11 + up * r * 0.24,
                        origin + front * r * 0.32 + right * r * -0.03 + up * r * 0.23,
                        origin + front * r * 0.27 + right * r * 0.00 + up * r * 0.19,
                        7);

                    AddCubic(
                        lines,
                        origin + front * r * 0.26 + right * r * 0.20 + up * r * 0.20,
                        origin + front * r * 0.32 + right * r * 0.11 + up * r * 0.24,
                        origin + front * r * 0.32 + right * r * 0.03 + up * r * 0.23,
                        origin + front * r * 0.27 + right * r * 0.00 + up * r * 0.19,
                        7);

                    AddCubic(
                        lines,
                        origin + front * r * 0.28 + right * r * -0.16 + up * r * -0.08,
                        origin + front * r * 0.33 + right * r * -0.06 + up * r * -0.11,
                        origin + front * r * 0.33 + right * r * 0.06 + up * r * -0.11,
                        origin + front * r * 0.28 + right * r * 0.16 + up * r * -0.08,
                        8);

                    return lines;
                }

                private static void MakeFrame(Vector3d front, out Vector3d right, out Vector3d up)
                {
                    if (!front.Unitize()) front = Vector3d.XAxis;

                    Vector3d seed = Math.Abs(front * Vector3d.ZAxis) > 0.92 ? Vector3d.YAxis : Vector3d.ZAxis;
                    right = Vector3d.CrossProduct(front, seed);
                    if (!right.Unitize()) right = Vector3d.YAxis;
                    up = Vector3d.CrossProduct(right, front);
                    if (!up.Unitize()) up = Vector3d.ZAxis;
                }

                private static void AddEllipse(List<Line> lines, Point3d center, Vector3d axisA, Vector3d axisB, double radiusA, double radiusB, int segments)
                {
                    axisA.Unitize();
                    axisB.Unitize();

                    Point3d last = center + axisA * radiusA;
                    for (int i = 1; i <= segments; i++)
                    {
                        double a = 2.0 * Math.PI * i / segments;
                        Point3d next = center + axisA * (Math.Cos(a) * radiusA) + axisB * (Math.Sin(a) * radiusB);
                        lines.Add(new Line(last, next));
                        last = next;
                    }
                }

                private void AddPinna(List<Line> lines, Point3d center, Vector3d front, Vector3d lateral, Vector3d up)
                {
                    Vector3d f = front;
                    Vector3d s = lateral;
                    Vector3d u = up;
                    f.Unitize();
                    s.Unitize();
                    u.Unitize();

                    AddEllipse(lines, center - s * r * 0.020, f, u, r * 0.070, r * 0.170, 14);

                    AddCubic(
                        lines,
                        center + f * r * -0.052 + s * r * 0.020 + u * r * 0.165,
                        center + f * r * 0.088 + s * r * 0.072 + u * r * 0.178,
                        center + f * r * 0.128 + s * r * 0.055 + u * r * -0.112,
                        center + f * r * -0.025 + s * r * 0.018 + u * r * -0.174,
                        12);

                    AddCubic(
                        lines,
                        center + f * r * -0.025 + s * r * 0.018 + u * r * -0.174,
                        center + f * r * -0.122 + s * r * 0.030 + u * r * -0.052,
                        center + f * r * -0.112 + s * r * 0.038 + u * r * 0.098,
                        center + f * r * -0.052 + s * r * 0.020 + u * r * 0.165,
                        10);

                    AddEllipse(lines, center + f * r * 0.012 + s * r * 0.045 + u * r * -0.026, f, u, r * 0.042, r * 0.070, 12);

                    AddCubic(
                        lines,
                        center + f * r * -0.034 + s * r * 0.055 + u * r * 0.112,
                        center + f * r * 0.038 + s * r * 0.072 + u * r * 0.070,
                        center + f * r * 0.045 + s * r * 0.072 + u * r * -0.018,
                        center + f * r * -0.004 + s * r * 0.052 + u * r * -0.082,
                        9);

                    AddCubic(
                        lines,
                        center + f * r * -0.018 + s * r * 0.052 + u * r * 0.020,
                        center + f * r * -0.074 + s * r * 0.040 + u * r * 0.002,
                        center + f * r * -0.070 + s * r * 0.035 + u * r * -0.074,
                        center + f * r * -0.008 + s * r * 0.048 + u * r * -0.122,
                        8);

                    AddCubic(
                        lines,
                        center + f * r * 0.025 + s * r * 0.050 + u * r * 0.048,
                        center + f * r * 0.096 + s * r * 0.060 + u * r * 0.022,
                        center + f * r * 0.084 + s * r * 0.052 + u * r * -0.088,
                        center + f * r * 0.018 + s * r * 0.044 + u * r * -0.128,
                        8);

                    AddCubic(
                        lines,
                        center + f * r * 0.082 + s * r * 0.040 + u * r * 0.118,
                        center + f * r * 0.118 + s * r * 0.016 + u * r * 0.050,
                        center + f * r * 0.112 + s * r * 0.010 + u * r * -0.036,
                        center + f * r * 0.074 + s * r * 0.036 + u * r * -0.100,
                        8);

                    lines.Add(new Line(center - s * r * 0.020 + u * r * 0.110, center + s * r * 0.035 + u * r * 0.130));
                    lines.Add(new Line(center - s * r * 0.020 + u * r * -0.110, center + s * r * 0.030 + u * r * -0.132));
                }

                private static void AddCubic(List<Line> lines, Point3d p0, Point3d p1, Point3d p2, Point3d p3, int segments)
                {
                    Point3d last = p0;
                    for (int i = 1; i <= segments; i++)
                    {
                        double t = (double)i / segments;
                        double u = 1.0 - t;
                        Point3d next = new Point3d(
                            u * u * u * p0.X + 3.0 * u * u * t * p1.X + 3.0 * u * t * t * p2.X + t * t * t * p3.X,
                            u * u * u * p0.Y + 3.0 * u * u * t * p1.Y + 3.0 * u * t * t * p2.Y + t * t * t * p3.Y,
                            u * u * u * p0.Z + 3.0 * u * u * t * p1.Z + 3.0 * u * t * t * p2.Z + t * t * t * p3.Z);
                        lines.Add(new Line(last, next));
                        last = next;
                    }
                }

            }
        }
    }
}
