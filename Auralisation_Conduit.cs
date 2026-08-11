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

                    foreach (Line l in HeadOutline(origin, front, right, up)) display.DrawLine(l, body, 2);
                    foreach (Line l in EarLines(origin, front, right, up)) display.DrawLine(l, ears, 2);
                    foreach (Line l in FrontMarker(origin, front, right, up)) display.DrawLine(l, face, 3);
                }

                private List<Line> HeadOutline(Point3d origin, Vector3d front, Vector3d right, Vector3d up)
                {
                    List<Line> lines = new List<Line>();
                    AddEllipse(lines, origin + up * r * 0.15, right, up, r * 0.42, r * 0.55, 28);
                    return lines;
                }

                private List<Line> EarLines(Point3d origin, Vector3d front, Vector3d right, Vector3d up)
                {
                    List<Line> lines = new List<Line>();
                    Point3d left = origin - right * r * 0.47 + up * r * 0.15;
                    Point3d rightEar = origin + right * r * 0.47 + up * r * 0.15;

                    AddEllipse(lines, left, front, up, r * 0.08, r * 0.16, 12);
                    AddEllipse(lines, rightEar, front, up, r * 0.08, r * 0.16, 12);

                    return lines;
                }

                private List<Line> FrontMarker(Point3d origin, Vector3d front, Vector3d right, Vector3d up)
                {
                    List<Line> lines = new List<Line>();
                    Point3d noseBase = origin + up * r * 0.15;
                    Point3d noseTip = noseBase + front * r * 0.58;
                    lines.Add(new Line(noseBase, noseTip));
                    lines.Add(new Line(noseTip, noseTip - front * r * 0.14 + right * r * 0.08));
                    lines.Add(new Line(noseTip, noseTip - front * r * 0.14 - right * r * 0.08));
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

            }
        }
    }
}
