using DXFLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;

namespace DXFViewer
{
    public static class DxfConversionHelpers
    {
        internal static void ConvertDxfEntityToGeometries(DXFDocument document, DXFEntity entity, IList<Geometry> geometries)
        {
            switch (entity)
            {
                case DXFLine line:
                {
                    LineGeometry drawLine = CreateLineGeometry(line);
                    geometries.Add(drawLine);
                    break;
                }

                case DXFCircle circle:
                {
                    EllipseGeometry drawCircle = CreateCircleGeometry(circle);
                    geometries.Add(drawCircle);
                    break;
                }


                case DXFEllipse ellipse:
                {
                    Geometry drawEllipse = CreateEllipseGeometry(ellipse);
                    geometries.Add(drawEllipse);
                    break;
                }

                case DXFArc arc:
                {
                    var arcPath = CreateArcPathGeometry(arc);
                    geometries.Add(arcPath);
                    break;
                }

                case DXFPolyLine polyLine:
                {
                    var lines = CreatePolyLineGeometry(polyLine);
                    lines.ForEach(line => geometries.Add(line));
                    break;
                }

                case DXFLWPolyLine lwPolyLine:
                {
                    var lines = CreateLWPolyLineGeometry(lwPolyLine);
                    lines.ForEach(x => geometries.Add(x));
                    break;
                }

                case DXFSolid solid:
                {
                    PathGeometry path = CreateSolidGeometry(solid);
                    geometries.Add(path);
                    break;
                }

                case DXFInsert insert:
                {
                    DXFBlock block = document.Blocks.FirstOrDefault(x => x.BlockName == insert.BlockName);
                    if (block != null && block.HasChildren && !block.IsInvisible)
                    {
                        IList<Geometry> blockEntities = new List<Geometry>();
                        foreach (DXFEntity blockEntity in block.Children)
                            ConvertDxfEntityToGeometries(document, blockEntity, blockEntities);

                        double centerX = insert.InsertionPoint.X.Value;
                        double centerY = -insert.InsertionPoint.Y.Value;

                        foreach (Geometry geometry in blockEntities)
                        {
                            TranslateTransform translateTransform = new TranslateTransform(centerX, centerY);
                            RotateTransform rotateTransform = null;
                            ScaleTransform scaleTransform = null;

                            if (insert.RotationAngle.HasValue)
                                rotateTransform = new RotateTransform(insert.RotationAngle.Value > 180 ? insert.RotationAngle.Value - 180 : insert.RotationAngle.Value, centerX, centerY);
                            if (insert.Scaling.X.HasValue || insert.Scaling.Y.HasValue)
                                scaleTransform = new ScaleTransform(insert.Scaling.X ?? 1, insert.Scaling.Y ?? 1, centerX, centerY);

                            // Create a TransformGroup to contain the transforms 
                            TransformGroup transformGroup = new TransformGroup();
                            transformGroup.Children.Add(translateTransform);
                            if (rotateTransform != null)
                                transformGroup.Children.Add(rotateTransform);
                            if (scaleTransform != null)
                                transformGroup.Children.Add(scaleTransform);

                            geometry.Transform = transformGroup;
                            geometries.Add(geometry);
                        }
                    }

                    break;
                }


            }
        }

        internal static void ConvertDxfEntityToShapes(DXFDocument document, DXFEntity entity, IList<Shape> shapes, Brush stroke)
        {
            switch (entity)
            {
                case DXFLine line:
                {
                    Line drawLine = CreateLine(stroke, line);
                    shapes.Add(drawLine);
                    break;
                }

                case DXFCircle circle:
                {
                    Ellipse drawCircle = CreateCircle(stroke, circle);
                    shapes.Add(drawCircle);
                    break;
                }

                case DXFEllipse ellipse:
                {
                    Path drawEllipse = CreateEllipse(stroke, ellipse);
                    shapes.Add(drawEllipse);
                    break;
                }

                case DXFArc arc:
                {
                    var path = CreateArcPath(stroke, arc);
                    shapes.Add(path);
                    break;
                }

                case DXFPolyLine polyLine:
                {
                    List<Shape> lines = CreatePolyLine(stroke, polyLine);
                    lines.ForEach(x => shapes.Add(x));
                    break;
                }

                case DXFLWPolyLine lwPolyLine:
                {
                    List<Shape> lines = CreateLWPolyLine(stroke, lwPolyLine);
                    lines.ForEach(x => shapes.Add(x));
                    break;
                }

                case DXFSolid solid:
                {
                    Path path = CreateSolid(stroke, solid);
                    shapes.Add(path);
                    break;
                }

                case DXFInsert insert:
                {
                    DXFBlock block = document.Blocks.FirstOrDefault(x => x.BlockName == insert.BlockName);
                    if (block != null && block.HasChildren && !block.IsInvisible)
                    {
                        SolidColorBrush brush = new SolidColorBrush(Colors.Red);
                        brush.Freeze();

                        IList<Shape> blockEntities = new List<Shape>();
                        foreach (DXFEntity blockEntity in block.Children)
                            ConvertDxfEntityToShapes(document, blockEntity, blockEntities, brush);

                        double centerX = insert.InsertionPoint.X.Value;
                        double centerY = -insert.InsertionPoint.Y.Value;

                        foreach (Shape shape in blockEntities)
                        {
                            TranslateTransform translateTransform = new TranslateTransform(centerX, centerY);
                            RotateTransform rotateTransform = null;
                            ScaleTransform scaleTransform = null;

                            if (insert.RotationAngle.HasValue)
                                rotateTransform = new RotateTransform(insert.RotationAngle.Value > 180 ? insert.RotationAngle.Value - 180 : insert.RotationAngle.Value, centerX, centerY);
                            if (insert.Scaling.X.HasValue || insert.Scaling.Y.HasValue)
                                scaleTransform = new ScaleTransform(insert.Scaling.X ?? 1, insert.Scaling.Y ?? 1, centerX, centerY);

                            // Create a TransformGroup to contain the transforms 
                            TransformGroup transformGroup = new TransformGroup();
                            transformGroup.Children.Add(translateTransform);
                            if (rotateTransform != null)
                                transformGroup.Children.Add(rotateTransform);
                            if (scaleTransform != null)
                                transformGroup.Children.Add(scaleTransform);

                            shape.RenderTransform = transformGroup;
                            shapes.Add(shape);
                        }
                    }

                    break;
                }

                case DXFText text:
                {
                    break;
                }

                case DXFMText _:
                    break;
                case DXFDimension _:
                    break;
                case DXFHatch _:
                    break;
                case DXF3DFace _:
                    break;
                case DXFTrace _:
                    break;
                case DXFSpline _:
                    break;
                case DXFPointEntity _:
                    break;
                case DXFXLine _:
                    break;
                case DXFViewPort _:
                    break;
                case DXFImage _:
                    break;

                default:
                    if (Debugger.IsAttached)
                        Debugger.Break();
                    break;
            }
        }

        internal static Path CreateSolid(Brush stroke, DXFSolid solid)
        {
            Path path = new Path();
            PathGeometry geometry = CreateSolidGeometry(solid);
            path.Data = geometry;
            path.Fill = stroke;
            path.StrokeThickness = 0;
            return path;
        }

        internal static PathGeometry CreateSolidGeometry(DXFSolid solid)
        {
            PathGeometry geometry = new PathGeometry();
            PathFigure figure = new PathFigure();
            PathSegmentCollection group = new PathSegmentCollection
            {
                //reverse 3, 2 because ordering of vertices is different in WPF
                new LineSegment(new Point(solid.Corners[1].X.Value, -solid.Corners[1].Y.Value), true),
                new LineSegment(new Point(solid.Corners[3].X.Value, -solid.Corners[3].Y.Value), true),
                new LineSegment(new Point(solid.Corners[2].X.Value, -solid.Corners[2].Y.Value), true),
                new LineSegment(new Point(solid.Corners[0].X.Value, -solid.Corners[0].Y.Value), true)
            };

            figure.IsFilled = true;
            figure.StartPoint = new Point(solid.Corners[0].X.Value, -solid.Corners[0].Y.Value);
            figure.Segments = group;

            geometry.Figures.Add(figure);

            return geometry;
        }

        internal static Path CreateArcPath(Brush stroke, DXFArc arc)
        {
            Path path = new Path();
            path.Stroke = stroke;

            PathGeometry geometry = CreateArcPathGeometry(arc);
            path.Data = geometry;
            path.IsHitTestVisible = false;

            return path;
        }

        private static PathGeometry CreateArcPathGeometry(DXFArc arc)
        {
            Point endPoint = new Point(
                (arc.Center.X.Value + Math.Cos(arc.EndAngle * Math.PI / 180) * arc.Radius),
                (-arc.Center.Y.Value - Math.Sin(arc.EndAngle * Math.PI / 180) * arc.Radius));

            Point startPoint = new Point(
                (arc.Center.X.Value + Math.Cos(arc.StartAngle * Math.PI / 180) * arc.Radius),
                (-arc.Center.Y.Value - Math.Sin(arc.StartAngle * Math.PI / 180) * arc.Radius));

            ArcSegment arcSegment = new ArcSegment();
            double sweep;
            if (arc.EndAngle < arc.StartAngle)
                sweep = (360 + arc.EndAngle) - arc.StartAngle;
            else sweep = Math.Abs(arc.EndAngle - arc.StartAngle);

            arcSegment.IsLargeArc = sweep >= 180;
            arcSegment.Point = endPoint;
            arcSegment.Size = new Size(arc.Radius, arc.Radius);
            arcSegment.SweepDirection = arc.ExtrusionDirection.Z >= 0 ? SweepDirection.Counterclockwise : SweepDirection.Clockwise;

            PathGeometry geometry = new PathGeometry();
            PathFigure pathFigure = new PathFigure();
            pathFigure.StartPoint = startPoint;
            pathFigure.Segments.Add(arcSegment);
            geometry.Figures.Add(pathFigure);
            return geometry;
        }

        internal static Ellipse CreateCircle(Brush stroke, DXFCircle circle)
        {
            Ellipse drawCircle = new Ellipse
            {
                Stroke = stroke,
                Width = circle.Radius * 2,
                Height = circle.Radius * 2,
                IsHitTestVisible = false
            };

            Point center = new Point((double)circle.Center.X, (double)circle.Center.Y);
            var left = (center.X - circle.Radius);
            var top = (-center.Y - circle.Radius);
            Canvas.SetLeft(drawCircle, left);
            Canvas.SetTop(drawCircle, top);

            return drawCircle;
        }

        internal static EllipseGeometry CreateCircleGeometry(DXFCircle circle)
        {
            Point center = new Point((double)circle.Center.X, (double)circle.Center.Y);
            EllipseGeometry drawCircle = new EllipseGeometry
            {
                RadiusX = circle.Radius,
                RadiusY = circle.Radius,
                Center = center
            };

            return drawCircle;
        }

        internal static Path CreateEllipse(Brush stroke, DXFEllipse ellipse)
        {
            Path path = new Path();
            path.Stroke = stroke;
            path.IsHitTestVisible = false;

            var angle = (180 / Math.PI) * Math.Atan2(-ellipse.MainAxis.Y.Value, ellipse.MainAxis.X.Value);
            path.RenderTransform = new RotateTransform(angle, ellipse.Center.X.Value, -ellipse.Center.Y.Value);

            Geometry geometry = CreateEllipseGeometry(ellipse);
            path.Data = geometry;

            return path;
        }

        private static Geometry CreateEllipseGeometry(DXFEllipse ellipse)
        {
            Geometry geometry;
            var radiusX = Math.Sqrt(Math.Pow(ellipse.MainAxis.X.Value, 2) + Math.Pow(ellipse.MainAxis.Y.Value, 2));
            var radiusY = radiusX * ellipse.AxisRatio;
            var startAngle = ellipse.StartParam * 180 / Math.PI;
            var endAngle = ellipse.EndParam * 180 / Math.PI;

            if (endAngle - startAngle < 360)
            {
                Point endPoint = new Point(
                    (ellipse.Center.X.Value + Math.Cos(ellipse.EndParam) * radiusX),
                    (-ellipse.Center.Y.Value - Math.Sin(ellipse.EndParam) * radiusY));

                Point startPoint = new Point(
                    (ellipse.Center.X.Value + Math.Cos(ellipse.StartParam) * radiusX),
                    (-ellipse.Center.Y.Value - Math.Sin(ellipse.StartParam) * radiusY));

                ArcSegment arcSegment = new ArcSegment();
                double sweep;
                if (endAngle < startAngle)
                    sweep = (360 + endAngle) - startAngle;
                else sweep = Math.Abs(endAngle - startAngle);

                arcSegment.IsLargeArc = sweep >= 180;
                arcSegment.Point = endPoint;
                arcSegment.Size = new Size(radiusX, radiusY);
                arcSegment.SweepDirection = ellipse.ExtrusionDirection.Z >= 0 ? SweepDirection.Counterclockwise : SweepDirection.Clockwise;

                PathGeometry pathGeometry = new PathGeometry();
                PathFigure pathFigure = new PathFigure();
                pathFigure.StartPoint = startPoint;
                pathFigure.Segments.Add(arcSegment);
                pathGeometry.Figures.Add(pathFigure);
                geometry = pathGeometry;
            }
            else
            {
                EllipseGeometry ellipseGeometry = new EllipseGeometry();
                ellipseGeometry.Center = new Point(ellipse.Center.X.Value, -ellipse.Center.Y.Value);
                ellipseGeometry.RadiusX = Math.Sqrt(Math.Pow(ellipse.MainAxis.X.Value, 2) + Math.Pow(ellipse.MainAxis.Y.Value, 2));
                ellipseGeometry.RadiusY = ellipseGeometry.RadiusX * ellipse.AxisRatio;
                geometry = ellipseGeometry;
            }

            return geometry;
        }

        internal static Line CreateLine(Brush stroke, DXFLine line)
        {
            Point start = new Point((float)line.Start.X, (float)-line.Start.Y);
            Point end = new Point((float)line.End.X, (float)-line.End.Y);

            Line drawLine = new Line
            {
                Stroke = stroke,
                X1 = end.X,
                X2 = start.X,
                Y1 = end.Y,
                Y2 = start.Y,
                IsHitTestVisible = false
            };

            return drawLine;
        }

        internal static LineGeometry CreateLineGeometry(DXFLine line)
        {
            Point start = new Point((float)line.Start.X, (float)-line.Start.Y);
            Point end = new Point((float)line.End.X, (float)-line.End.Y);

            LineGeometry drawLine = new LineGeometry
            {
                StartPoint = start,
                EndPoint = end,
            };

            return drawLine;
        }

        internal static List<Shape> CreateLWPolyLine(Brush stroke, DXFLWPolyLine polyLine)
        {
            bool isClosed = polyLine.Flags == DXFLWPolyLine.FlagsEnum.closed;

            int count = isClosed ? polyLine.Elements.Count : polyLine.Elements.Count - 1;
            List<Shape> lines = new List<Shape>();
            for (int i = 1; i <= count; i++)
            {
                DXFPoint vertex1 = (i == polyLine.Elements.Count) ? polyLine.Elements[0].Vertex : polyLine.Elements[i].Vertex;
                DXFPoint vertex2 = polyLine.Elements[i - 1].Vertex;

                // TODO: Handle Element.Bulge http://www.afralisp.net/archive/lisp/Bulges1.htm

                Point start = new Point((float)vertex1.X, (float)-vertex1.Y);
                Point end = new Point((float)vertex2.X, (float)-vertex2.Y);

                Line drawLine = new Line
                {
                    Stroke = stroke,
                    X1 = end.X,
                    X2 = start.X,
                    Y1 = end.Y,
                    Y2 = start.Y,
                    IsHitTestVisible = false
                };

                lines.Add(drawLine);
            }

            return lines;
        }

        internal static List<LineGeometry> CreateLWPolyLineGeometry(DXFLWPolyLine polyLine)
        {
            bool isClosed = polyLine.Flags == DXFLWPolyLine.FlagsEnum.closed;

            int count = isClosed ? polyLine.Elements.Count : polyLine.Elements.Count - 1;
            List<LineGeometry> lines = new List<LineGeometry>();
            for (int i = 1; i <= count; i++)
            {
                DXFPoint vertex1 = (i == polyLine.Elements.Count) ? polyLine.Elements[0].Vertex : polyLine.Elements[i].Vertex;
                DXFPoint vertex2 = polyLine.Elements[i - 1].Vertex;

                // TODO: Handle Element.Bulge http://www.afralisp.net/archive/lisp/Bulges1.htm

                Point start = new Point((float)vertex1.X, (float)-vertex1.Y);
                Point end = new Point((float)vertex2.X, (float)-vertex2.Y);

                LineGeometry drawLine = new LineGeometry
                {
                    StartPoint = start,
                    EndPoint = end,
                };

                lines.Add(drawLine);
            }

            return lines;
        }

        internal static List<Shape> CreatePolyLine(Brush stroke, DXFPolyLine polyLine)
        {
            bool isClosed = polyLine.Flags == DXFPolyLine.FlagsEnum.closed;

            int count = isClosed ? polyLine.Children.Count : polyLine.Children.Count - 1;
            List<Shape> lines = new List<Shape>();
            for (int i = 1; i <= count; i++)
            {
                DXFVertex vertex1 = (i == polyLine.Children.Count) ? (DXFVertex)polyLine.Children[0] : (DXFVertex)polyLine.Children[i];
                DXFVertex vertex2 = (DXFVertex)polyLine.Children[i - 1];

                Point start = new Point((float)vertex1.Location.X, (float)-vertex1.Location.Y);
                Point end = new Point((float)vertex2.Location.X, (float)-vertex2.Location.Y);

                // TODO: Handle Vertex.Buldge http://www.afralisp.net/archive/lisp/Bulges1.htm

                Line drawLine = new Line
                {
                    Stroke = stroke,
                    X1 = end.X,
                    X2 = start.X,
                    Y1 = end.Y,
                    Y2 = start.Y,
                    IsHitTestVisible = false
                };

                lines.Add(drawLine);
            }

            return lines;
        }

        internal static List<LineGeometry> CreatePolyLineGeometry(DXFPolyLine polyLine)
        {
            bool isClosed = polyLine.Flags == DXFPolyLine.FlagsEnum.closed;

            int count = isClosed ? polyLine.Children.Count : polyLine.Children.Count - 1;
            List<LineGeometry> lines = new List<LineGeometry>();
            for (int i = 1; i <= count; i++)
            {
                DXFVertex vertex1 = (i == polyLine.Children.Count) ? (DXFVertex)polyLine.Children[0] : (DXFVertex)polyLine.Children[i];
                DXFVertex vertex2 = (DXFVertex)polyLine.Children[i - 1];

                Point start = new Point((float)vertex1.Location.X, (float)-vertex1.Location.Y);
                Point end = new Point((float)vertex2.Location.X, (float)-vertex2.Location.Y);

                // TODO: Handle Vertex.Buldge http://www.afralisp.net/archive/lisp/Bulges1.htm

                LineGeometry drawLine = new LineGeometry
                {
                    StartPoint = start,
                    EndPoint = end,
                };

                lines.Add(drawLine);
            }

            return lines;
        }

        public static (DXFDocument Document, IList<Shape> Shapes) LoadDxf(string fileName)
        {
            if (File.Exists(fileName))
            {
                // Parse DXF
                DXFDocument document = ReadDXF(fileName);

                if (document.Entities.Count > 0)
                {
                    // Create shapes
                    DateTime start = DateTime.UtcNow;

                    IList<Shape> shapes = new List<Shape>();

                    SolidColorBrush brush = new SolidColorBrush(Colors.SteelBlue);
                    brush.Freeze();

                    foreach (DXFEntity entity in document.Entities)
                        ConvertDxfEntityToShapes(document, entity, shapes, brush);

                    Debug.WriteLine("Created shapes in {0}ms", DateTime.UtcNow.Subtract(start).TotalMilliseconds);

                    return (document, shapes);
                }
                else if (Debugger.IsAttached)
                    Debugger.Break();
            }
            else if (Debugger.IsAttached)
                Debugger.Break();

            return (null, null);
        }

        public static (DXFDocument Document, Path Path) LoadDxfGeometries(string fileName)
        {
            if (File.Exists(fileName))
            {
                // Parse DXF
                DXFDocument document = ReadDXF(fileName);

                if (document.Entities.Count > 0)
                {
                    // Create shapes
                    DateTime start = DateTime.UtcNow;

                    IList<Geometry> geometries = new List<Geometry>();

                    SolidColorBrush brush = new SolidColorBrush(Colors.SteelBlue);
                    brush.Freeze();

                    foreach (DXFEntity entity in document.Entities)
                        ConvertDxfEntityToGeometries(document, entity, geometries);

                    Debug.WriteLine("Created shapes in {0}ms", DateTime.UtcNow.Subtract(start).TotalMilliseconds);

                    GeometryGroup group = new GeometryGroup();
                    foreach (var geometry in geometries)
                        group.Children.Add(geometry);

                    Path path = new Path() { Data = group, Stroke = brush };

                    return (document, path);
                }
                else if (Debugger.IsAttached)
                    Debugger.Break();
            }
            else if (Debugger.IsAttached)
                Debugger.Break();

            return (null, null);
        }

        public static DXFDocument ReadDXF(string fileName)
        {
            DateTime start = DateTime.UtcNow;
            DXFDocument document = new DXFDocument();
            document.Load(fileName);
            Debug.WriteLine("Loaded {0} in {1}ms", System.IO.Path.GetFileName(fileName), DateTime.UtcNow.Subtract(start).TotalMilliseconds);
            return document;
        }

    }
}
