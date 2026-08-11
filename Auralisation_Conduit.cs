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
                if (BinauralHeadVisible) Head.Draw(e.Display, BinauralHeadOrigin, BinauralHeadDirection, System.Drawing.Color.Black, System.Drawing.Color.DimGray, System.Drawing.Color.Black);
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
                    Point3d skull = origin + up * r * 0.13 - front * r * 0.02;
                    AddEllipse(lines, skull, right, up, r * 0.34, r * 0.51, 28);
                    AddEllipse(lines, skull - front * r * 0.03, front, up, r * 0.30, r * 0.51, 28);

                    AddCubic(
                        lines,
                        origin + front * r * -0.22 + up * r * 0.53,
                        origin + front * r * 0.03 + up * r * 0.57,
                        origin + front * r * 0.25 + up * r * 0.47,
                        origin + front * r * 0.32 + up * r * 0.28,
                        12);

                    AddCubic(
                        lines,
                        origin + front * r * 0.27 + up * r * -0.19,
                        origin + front * r * 0.16 + up * r * -0.38,
                        origin + front * r * -0.10 + up * r * -0.47,
                        origin + front * r * -0.28 + up * r * -0.31,
                        12);

                    AddCubic(
                        lines,
                        origin + front * r * -0.11 + right * r * 0.20 + up * r * -0.36,
                        origin + front * r * -0.18 + right * r * 0.17 + up * r * -0.50,
                        origin + front * r * -0.25 + right * r * 0.09 + up * r * -0.61,
                        origin + front * r * -0.27 + right * r * 0.02 + up * r * -0.72,
                        8);

                    AddCubic(
                        lines,
                        origin + front * r * -0.11 + right * r * -0.20 + up * r * -0.36,
                        origin + front * r * -0.18 + right * r * -0.17 + up * r * -0.50,
                        origin + front * r * -0.25 + right * r * -0.09 + up * r * -0.61,
                        origin + front * r * -0.27 + right * r * -0.02 + up * r * -0.72,
                        8);

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
                        origin + front * r * 0.27 + up * r * 0.28,
                        origin + front * r * 0.40 + up * r * 0.22,
                        origin + front * r * 0.48 + up * r * 0.12,
                        origin + front * r * 0.46 + up * r * 0.04,
                        8);

                    AddCubic(
                        lines,
                        origin + front * r * 0.46 + up * r * 0.04,
                        origin + front * r * 0.37 + up * r * 0.00,
                        origin + front * r * 0.33 + up * r * -0.04,
                        origin + front * r * 0.34 + up * r * -0.10,
                        6);

                    AddCubic(
                        lines,
                        origin + front * r * 0.30 + right * r * -0.12 + up * r * 0.16,
                        origin + front * r * 0.33 + right * r * -0.05 + up * r * 0.19,
                        origin + front * r * 0.33 + right * r * 0.05 + up * r * 0.19,
                        origin + front * r * 0.30 + right * r * 0.12 + up * r * 0.16,
                        6);

                    AddCubic(
                        lines,
                        origin + front * r * 0.29 + right * r * -0.10 + up * r * -0.13,
                        origin + front * r * 0.32 + right * r * -0.03 + up * r * -0.16,
                        origin + front * r * 0.32 + right * r * 0.03 + up * r * -0.16,
                        origin + front * r * 0.29 + right * r * 0.10 + up * r * -0.13,
                        6);

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

                    AddCubic(
                        lines,
                        center + f * r * -0.030 + s * r * 0.008 + u * r * 0.135,
                        center + f * r * 0.058 + s * r * 0.036 + u * r * 0.145,
                        center + f * r * 0.082 + s * r * 0.034 + u * r * -0.070,
                        center + f * r * -0.018 + s * r * 0.010 + u * r * -0.135,
                        10);

                    AddCubic(
                        lines,
                        center + f * r * -0.018 + s * r * 0.010 + u * r * -0.135,
                        center + f * r * -0.078 + s * r * 0.000 + u * r * -0.070,
                        center + f * r * -0.074 + s * r * 0.000 + u * r * 0.070,
                        center + f * r * -0.030 + s * r * 0.008 + u * r * 0.135,
                        8);

                    AddCubic(
                        lines,
                        center + f * r * -0.020 + s * r * 0.026 + u * r * 0.085,
                        center + f * r * 0.034 + s * r * 0.042 + u * r * 0.045,
                        center + f * r * 0.034 + s * r * 0.040 + u * r * -0.012,
                        center + f * r * -0.002 + s * r * 0.028 + u * r * -0.062,
                        7);

                    AddCubic(
                        lines,
                        center + f * r * -0.012 + s * r * 0.026 + u * r * 0.008,
                        center + f * r * -0.048 + s * r * 0.018 + u * r * -0.008,
                        center + f * r * -0.044 + s * r * 0.016 + u * r * -0.060,
                        center + f * r * -0.002 + s * r * 0.026 + u * r * -0.096,
                        6);

                    AddCubic(
                        lines,
                        center + f * r * 0.040 + s * r * 0.030 + u * r * 0.082,
                        center + f * r * 0.062 + s * r * 0.006 + u * r * 0.028,
                        center + f * r * 0.058 + s * r * 0.006 + u * r * -0.044,
                        center + f * r * 0.036 + s * r * 0.028 + u * r * -0.086,
                        6);
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
