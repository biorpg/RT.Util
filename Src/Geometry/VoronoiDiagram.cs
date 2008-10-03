using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace RT.Util.Geometry
{
    /// <summary>
    /// Provides values to specify options on the <see cref="VoronoiDiagram.GenerateVoronoiDiagram(PointD[], SizeF, VoronoiDiagramFlags)"/> method.
    /// </summary>
    [Flags]
    public enum VoronoiDiagramFlags
    {
        /// <summary>Indicates that duplicate sites (points) should be removed from the input.</summary>
        REMOVE_DUPLICATES = 1,
        /// <summary>Indicates that input sites (points) that lie outside the bounds of the viewport should be ignored.</summary>
        REMOVE_OFFBOUNDS_SITES = 2
    }

    /// <summary>
    /// Static class providing methods for generating Voronoi diagrams from a set of input points.
    /// </summary>
    public static class VoronoiDiagram
    {
        /// <summary>
        /// Generates a Voronoi diagram from a set of input points.
        /// </summary>
        /// <param name="Sites">Input points (sites) to generate diagram from.</param>
        /// <param name="Size">Size of the viewport. The origin of the viewport is assumed to be at (0, 0).</param>
        /// <param name="Flags">Set of <see cref="VoronoiDiagramFlags"/> values that specifies additional options.</param>
        /// <returns>A list of line segments describing the Voronoi diagram.</returns>
        public static Tuple<List<EdgeD>, Dictionary<PointD, PolygonD>> GenerateVoronoiDiagram(PointD[] Sites, SizeF Size, VoronoiDiagramFlags Flags)
        {
            VoronoiDiagramData d = new VoronoiDiagramData(Sites, Size.Width, Size.Height, Flags);

            Tuple<List<EdgeD>, Dictionary<PointD, PolygonD>> Ret = new Tuple<List<EdgeD>, Dictionary<PointD, PolygonD>>();
            Ret.E1 = new List<EdgeD>();
            foreach (Edge e in d.Edges)
                Ret.E1.Add(new EdgeD(e.Start.Value, e.End.Value));
            Ret.E2 = new Dictionary<PointD, PolygonD>();
            foreach (KeyValuePair<PointD, Polygon> kvp in d.Polygons)
            {
                PolygonD Poly = kvp.Value.ToPolygonD();
                if (Poly != null)
                    Ret.E2.Add(kvp.Key, Poly);
            }
            return Ret;
        }

        /// <summary>
        /// Generates a Voronoi diagram from a set of input points.
        /// </summary>
        /// <param name="Sites">Input points (sites) to generate diagram from.
        /// If two points (sites) have identical co-ordinates, an exception is raised.</param>
        /// <param name="Size">Size of the viewport. The origin of the viewport is assumed to be at (0, 0).</param>
        /// <returns>A list of line segments describing the Voronoi diagram.</returns>
        public static Tuple<List<EdgeD>, Dictionary<PointD, PolygonD>> GenerateVoronoiDiagram(PointD[] Sites, SizeF Size)
        {
            return GenerateVoronoiDiagram(Sites, Size, 0);
        }
    }

    /// <summary>
    /// Internal class to generate Voronoi diagrams using Fortune's algorithm. Contains internal data structures and methods.
    /// </summary>
    class VoronoiDiagramData
    {
        public List<Arc> Arcs = new List<Arc>();
        public List<SiteEvent> SiteEvents = new List<SiteEvent>();
        public List<CircleEvent> CircleEvents = new List<CircleEvent>();
        public List<Edge> Edges = new List<Edge>();
        public Dictionary<PointD, Polygon> Polygons = new Dictionary<PointD, Polygon>();

        public VoronoiDiagramData(PointD[] Sites, float Width, float Height, VoronoiDiagramFlags Flags)
        {
            foreach (PointD p in Sites)
            {
                if (p.X > 0 && p.Y > 0 && p.X < Width && p.Y < Height)
                {
                    SiteEvent SiteEvent = new SiteEvent(p);
                    SiteEvents.Add(SiteEvent);
                }
                else if ((Flags & VoronoiDiagramFlags.REMOVE_OFFBOUNDS_SITES) == 0)
                    throw new Exception("The input contains a point outside the bounds or on the perimeter (coordinates " +
                        p + "). This case is not handled by this algorithm. Use the RT.Util.VoronoiDiagramFlags.REMOVE_OFFBOUNDS_SITES " +
                        "flag to automatically remove such off-bounds input points.");
            }
            SiteEvents.Sort();

            // Make sure there are no two equal points in the input
            for (int i = 1; i < SiteEvents.Count; i++)
            {
                while (i < SiteEvents.Count && SiteEvents[i - 1].Position == SiteEvents[i].Position)
                {
                    if ((Flags & VoronoiDiagramFlags.REMOVE_DUPLICATES) == VoronoiDiagramFlags.REMOVE_DUPLICATES)
                        SiteEvents.RemoveAt(i);
                    else
                        throw new Exception("The input contains two points at the same coordinates " +
                            SiteEvents[i].Position + ". Voronoi diagrams are undefined for such a situation. " +
                            "Use the RT.Util.VoronoiDiagramFlags.REMOVE_DUPLICATES flag to automatically remove such duplicate input points.");
                }
            }

            // Main loop
            while (SiteEvents.Count > 0 || CircleEvents.Count > 0)
            {
                if (CircleEvents.Count > 0 && (SiteEvents.Count == 0 || CircleEvents[0].X <= SiteEvents[0].Position.X))
                {
                    // Process a circle event
                    CircleEvent Event = CircleEvents[0];
                    CircleEvents.RemoveAt(0);
                    int ArcIndex = Arcs.IndexOf(Event.Arc);
                    if (ArcIndex == -1) continue;

                    // The two edges left and right of the disappearing arc end here
                    if (Arcs[ArcIndex - 1].Edge != null)
                        Arcs[ArcIndex - 1].Edge.SetEndPoint(Event.Center);
                    if (Event.Arc.Edge != null)
                        Event.Arc.Edge.SetEndPoint(Event.Center);

                    // Remove the arc from the beachline
                    Arcs.RemoveAt(ArcIndex);
                    // ArcIndex now points to the arc after the one that disappeared

                    // Start a new edge at the point where the other two edges ended
                    Arcs[ArcIndex - 1].Edge = new Edge(Arcs[ArcIndex - 1].Site, Arcs[ArcIndex].Site);
                    Arcs[ArcIndex - 1].Edge.SetEndPoint(Event.Center);
                    Edges.Add(Arcs[ArcIndex - 1].Edge);

                    // Recheck circle events on either side of the disappearing arc
                    if (ArcIndex > 0)
                        CheckCircleEvent(CircleEvents, ArcIndex - 1, Event.X);
                    if (ArcIndex < Arcs.Count)
                        CheckCircleEvent(CircleEvents, ArcIndex, Event.X);
                }
                else
                {
                    // Process a site event
                    SiteEvent Event = SiteEvents[0];
                    SiteEvents.RemoveAt(0);

                    if (Arcs.Count == 0)
                    {
                        Arcs.Add(new Arc(Event.Position));
                        continue;
                    }

                    // Find the current arc(s) at height e.Position.y (if there are any)
                    bool ArcFound = false;
                    for (int i = 0; i < Arcs.Count; i++)
                    {
                        PointD Intersect;
                        if (DoesIntersect(Event.Position, i, out Intersect))
                        {
                            // New parabola intersects Arc - duplicate Arc
                            Arcs.Insert(i + 1, new Arc(Arcs[i].Site));
                            Arcs[i + 1].Edge = Arcs[i].Edge;

                            // Add a new Arc for Event.Position in the right place
                            Arcs.Insert(i + 1, new Arc(Event.Position));

                            // Add new half-edges connected to Arc's endpoints
                            Arcs[i].Edge = Arcs[i + 1].Edge = new Edge(Arcs[i + 1].Site, Arcs[i + 2].Site);
                            Edges.Add(Arcs[i].Edge);

                            // Check for new circle events around the new arc:
                            CheckCircleEvent(CircleEvents, i, Event.Position.X);
                            CheckCircleEvent(CircleEvents, i + 2, Event.Position.X);

                            ArcFound = true;
                            break;
                        }
                    }

                    if (ArcFound)
                        continue;

                    // Special case: If Event.Position never intersects an arc, append it to the list.
                    // This only happens if there is more than one site event with the lowest X co-ordinate.
                    Arc LastArc = Arcs[Arcs.Count - 1];
                    Arc NewArc = new Arc(Event.Position);
                    LastArc.Edge = new Edge(LastArc.Site, NewArc.Site);
                    Edges.Add(LastArc.Edge);
                    LastArc.Edge.SetEndPoint(new PointD(0, (NewArc.Site.Y + LastArc.Site.Y) / 2));
                    Arcs.Add(NewArc);
                }
            }

            // Advance the sweep line so no parabolas can cross the bounding box
            double Var = 2 * Width + Height;

            // Extend each remaining edge to the new parabola intersections
            for (int i = 0; i < Arcs.Count - 1; i++)
                if (Arcs[i].Edge != null)
                    Arcs[i].Edge.SetEndPoint(GetIntersection(Arcs[i].Site, Arcs[i + 1].Site, 2 * Var));

            // Clip all the edges with the bounding rectangle
            foreach (Edge s in Edges)
            {
                if (s.Start.Value.X < 0)
                    s.Start = new PointD(0, s.End.Value.X / (s.End.Value.X - s.Start.Value.X) * (s.Start.Value.Y - s.End.Value.Y) + s.End.Value.Y);
                if (s.Start.Value.Y < 0)
                    s.Start = new PointD(s.End.Value.Y / (s.End.Value.Y - s.Start.Value.Y) * (s.Start.Value.X - s.End.Value.X) + s.End.Value.X, 0);
                if (s.End.Value.X < 0)
                    s.End = new PointD(0, s.Start.Value.X / (s.Start.Value.X - s.End.Value.X) * (s.End.Value.Y - s.Start.Value.Y) + s.Start.Value.Y);
                if (s.End.Value.Y < 0)
                    s.End = new PointD(s.Start.Value.Y / (s.Start.Value.Y - s.End.Value.Y) * (s.End.Value.X - s.Start.Value.X) + s.Start.Value.X, 0);

                if (s.Start.Value.X > Width)
                    s.Start = new PointD(Width, (Width - s.Start.Value.X) / (s.End.Value.X - s.Start.Value.X) * (s.End.Value.Y - s.Start.Value.Y) + s.Start.Value.Y);
                if (s.Start.Value.Y > Height)
                    s.Start = new PointD((Height - s.Start.Value.Y) / (s.End.Value.Y - s.Start.Value.Y) * (s.End.Value.X - s.Start.Value.X) + s.Start.Value.X, Height);
                if (s.End.Value.X > Width)
                    s.End = new PointD(Width, (Width - s.End.Value.X) / (s.Start.Value.X - s.End.Value.X) * (s.Start.Value.Y - s.End.Value.Y) + s.End.Value.Y);
                if (s.End.Value.Y > Height)
                    s.End = new PointD((Height - s.End.Value.Y) / (s.Start.Value.Y - s.End.Value.Y) * (s.Start.Value.X - s.End.Value.X) + s.End.Value.X, Height);
            }

            // Generate polygons from the edges
            foreach (Edge e in Edges)
            {
                if (!Polygons.ContainsKey(e.SiteA))
                    Polygons.Add(e.SiteA, new Polygon(e.SiteA));
                Polygons[e.SiteA].AddEdge(e);
                if (!Polygons.ContainsKey(e.SiteB))
                    Polygons.Add(e.SiteB, new Polygon(e.SiteB));
                Polygons[e.SiteB].AddEdge(e);
            }
        }

        // Will a new parabola at Site intersect with the arc at ArcIndex?
        bool DoesIntersect(PointD Site, int ArcIndex, out PointD Result)
        {
            Arc Arc = Arcs[ArcIndex];

            Result = new PointD(0, 0);
            if (Arc.Site.X == Site.X)
                return false;

            if ((ArcIndex == 0 || GetIntersection(Arcs[ArcIndex - 1].Site, Arc.Site, Site.X).Y <= Site.Y) &&
                (ArcIndex == Arcs.Count - 1 || Site.Y <= GetIntersection(Arc.Site, Arcs[ArcIndex + 1].Site, Site.X).Y))
            {
                Result.Y = Site.Y;

                // Plug it back into the parabola equation
                Result.X = (Arc.Site.X * Arc.Site.X + (Arc.Site.Y - Result.Y) * (Arc.Site.Y - Result.Y) - Site.X * Site.X)
                          / (2 * Arc.Site.X - 2 * Site.X);

                return true;
            }
            return false;
        }

        // Where do two parabolas intersect?
        PointD GetIntersection(PointD SiteA, PointD SiteB, double ScanX)
        {
            PointD Result = new PointD();
            PointD p = SiteA;

            if (SiteA.X == SiteB.X)
                Result.Y = (SiteA.Y + SiteB.Y) / 2;
            else if (SiteB.X == ScanX)
                Result.Y = SiteB.Y;
            else if (SiteA.X == ScanX)
            {
                Result.Y = SiteA.Y;
                p = SiteB;
            }
            else
            {
                // Use the quadratic formula
                double z0 = 2 * (SiteA.X - ScanX);
                double z1 = 2 * (SiteB.X - ScanX);

                double a = 1 / z0 - 1 / z1;
                double b = -2 * (SiteA.Y / z0 - SiteB.Y / z1);
                double c = (SiteA.Y * SiteA.Y + SiteA.X * SiteA.X - ScanX * ScanX) / z0
                         - (SiteB.Y * SiteB.Y + SiteB.X * SiteB.X - ScanX * ScanX) / z1;

                Result.Y = (-b - Math.Sqrt(b * b - 4 * a * c)) / (2 * a);
            }

            // Plug back into one of the parabola equations
            Result.X = (p.X * p.X + (p.Y - Result.Y) * (p.Y - Result.Y) - ScanX * ScanX) / (2 * p.X - 2 * ScanX);
            return Result;
        }

        // Look for a new circle event for the arc at ArcIndex
        private void CheckCircleEvent(List<CircleEvent> CircleEvents, int ArcIndex, double ScanX)
        {
            if (ArcIndex == 0 || ArcIndex == Arcs.Count - 1)
                return;

            Arc Arc = Arcs[ArcIndex];
            double MaxX;
            PointD Center;

            if (GetCircle(Arcs[ArcIndex - 1].Site, Arc.Site, Arcs[ArcIndex + 1].Site, out Center, out MaxX)/* && MaxX >= ScanX*/)
            {
                // Add the new event in the right place using binary search
                int Low = 0;
                int High = CircleEvents.Count;
                while (Low < High)
                {
                    int Middle = (Low + High) / 2;
                    CircleEvent Event = CircleEvents[Middle];
                    if (Event.X < MaxX || (Event.X == MaxX && Event.Center.Y < Center.Y))
                        Low = Middle + 1;
                    else
                        High = Middle;
                }
                CircleEvents.Insert(Low, new CircleEvent(MaxX, Center, Arc));
            }
        }

        // Find the circle through points A, B, C
        private bool GetCircle(PointD A, PointD B, PointD C, out PointD Center, out double MaxX)
        {
            MaxX = 0;
            Center = new PointD(0, 0);

            // Check that BC is a "right turn" from AB
            if ((B.X - A.X) * (C.Y - A.Y) - (C.X - A.X) * (B.Y - A.Y) > 0)
                return false;

            // Algorithm from O'Rourke 2ed p. 189.
            double a = B.X - A.X, b = B.Y - A.Y,
                   c = C.X - A.X, d = C.Y - A.Y,
                   e = a * (A.X + B.X) + b * (A.Y + B.Y),
                   f = c * (A.X + C.X) + d * (A.Y + C.Y),
                   g = 2 * (a * (C.Y - B.Y) - b * (C.X - B.X));

            if (g == 0) return false;  // Points are co-linear.

            Center.X = (d * e - b * f) / g;
            Center.Y = (a * f - c * e) / g;

            // MaxX = Center.X + radius of the circle
            MaxX = Center.X + Math.Sqrt(Math.Pow(A.X - Center.X, 2) + Math.Pow(A.Y - Center.Y, 2));
            return true;
        }
    }

    /// <summary>
    /// Internal class describing an edge in the Voronoi diagram. May be incomplete as the algorithm progresses.
    /// </summary>
    class Edge
    {
        public PointD? Start, End;
        public PointD SiteA, SiteB;
        public Edge(PointD nSiteA, PointD nSiteB)
        {
            Start = null;
            End = null;
            SiteA = nSiteA;
            SiteB = nSiteB;
        }
        public void SetEndPoint(PointD nEnd)
        {
            if (Start == null)
                Start = nEnd;
            else if (End == null)
                End = nEnd;
        }
        public override string ToString() { return (Start == null ? "?" : Start.Value.ToString()) + " ==> " + (End == null ? "?" : End.ToString()); }
    }

    /// <summary>
    /// Internal class describing a polygon in the Voronoi diagram. May be incomplete as the algorithm progresses.
    /// </summary>
    class Polygon
    {
        public bool Complete;
        public PointD Site;

        private List<PointD> ProcessedPoints;
        private List<Edge> UnprocessedEdges;

        public Polygon(PointD nSite)
        {
            Site = nSite;
            Complete = false;
            ProcessedPoints = new List<PointD>();
            UnprocessedEdges = new List<Edge>();
        }

        public PolygonD ToPolygonD()
        {
            if (!Complete)
                return null;
            return new PolygonD(ProcessedPoints);
        }

        public void AddEdge(Edge Edge)
        {
            if (Edge.Start == null || Edge.End == null)
                throw new Exception("Assertion failed: Polygon.AddEdge() called with incomplete edge.");
            
            // Ignore zero-length edges
            if (Edge.Start.Value == Edge.End.Value)
                return;

            if (Complete)
                throw new Exception("Assertion failed: Polygon.AddEdge() called when polygon already complete.");

            if (ProcessedPoints.Count == 0)
            {
                ProcessedPoints.Add(Edge.Start.Value);
                ProcessedPoints.Add(Edge.End.Value);
                return;
            }

            if (!EdgeAttach(Edge))
            {
                UnprocessedEdges.Add(Edge);
                return;
            }

            bool Found = true;
            while (Found)
            {
                Found = false;
                foreach (Edge e in UnprocessedEdges)
                {
                    if (EdgeAttach(e))
                    {
                        UnprocessedEdges.Remove(e);
                        Found = true;
                        break;
                    }
                }
            }

            if (UnprocessedEdges.Count == 0 && ProcessedPoints[0] == ProcessedPoints[ProcessedPoints.Count - 1])
            {
                ProcessedPoints.RemoveAt(ProcessedPoints.Count - 1);
                Complete = true;
            }
        }

        private bool EdgeAttach(Edge Edge)
        {
            if (Edge.Start.Value == ProcessedPoints[0])
                ProcessedPoints.Insert(0, Edge.End.Value);
            else if (Edge.End.Value == ProcessedPoints[0])
                ProcessedPoints.Insert(0, Edge.Start.Value);
            else if (Edge.Start.Value == ProcessedPoints[ProcessedPoints.Count - 1])
                ProcessedPoints.Add(Edge.End.Value);
            else if (Edge.End.Value == ProcessedPoints[ProcessedPoints.Count - 1])
                ProcessedPoints.Add(Edge.Start.Value);
            else
                return false;

            if (ProcessedPoints.Count == 3)
            {
                // When we have three points, we can test whether they make a left-turn or a right-turn.
                PointD A = ProcessedPoints[0], B = ProcessedPoints[1], C = ProcessedPoints[2];
                if ((B.X - A.X) * (C.Y - A.Y) - (C.X - A.X) * (B.Y - A.Y) < 0)
                {
                    // If they make a left-turn, we want to swap them because
                    // otherwise we end up with a counter-clockwise polygon.
                    ProcessedPoints[0] = C;
                    ProcessedPoints[2] = A;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Internal class to describe an arc on the beachline (part of Fortune's algorithm to generate Voronoi diagrams) (used by RT.Util.VoronoiDiagram).
    /// </summary>
    class Arc
    {
        // The site the arc is associated with. There may be more than one arc for the same site in the Arcs array.
        public PointD Site;

        // The edge that is formed from the breakpoint between this Arc and the next Arc in the Arcs array.
        public Edge Edge;

        public Arc(PointD nSite) { Site = nSite; Edge = null; }

        public override string ToString()
        {
            return "Site = " + Site.ToString();
        }
    }

    /// <summary>
    /// Internal class to describe a site event (part of Fortune's algorithm to generate Voronoi diagrams) (used by RT.Util.VoronoiDiagram).
    /// </summary>
    class SiteEvent : IComparable<SiteEvent>
    {
        public PointD Position;
        public SiteEvent(PointD nPosition) { Position = nPosition; }
        public override string ToString()
        {
            return Position.ToString();
        }

        public int CompareTo(SiteEvent other)
        {
            if (Position.X < other.Position.X)
                return -1;
            if (Position.X > other.Position.X)
                return 1;
            if (Position.Y < other.Position.Y)
                return -1;
            if (Position.Y > other.Position.Y)
                return 1;
            return 0;
        }
    }

    /// <summary>
    /// Internal class to describe a circle event (part of Fortune's algorithm to generate Voronoi diagrams) (used by RT.Util.VoronoiDiagram).
    /// </summary>
    class CircleEvent : IComparable<CircleEvent>
    {
        public PointD Center;
        public double X;
        public Arc Arc;
        public CircleEvent(double nX, PointD nCenter, Arc nArc) { X = nX; Center = nCenter; Arc = nArc; }
        public override string ToString()
        {
            return "(" + X + ", " + Center.Y + ") [" + Center.X + "]";
        }

        public int CompareTo(CircleEvent other)
        {
            if (X < other.X)
                return -1;
            if (X > other.X)
                return 1;
            if (Center.Y < other.Center.Y)
                return -1;
            if (Center.Y > other.Center.Y)
                return 1;
            return 0;
        }
    }
}