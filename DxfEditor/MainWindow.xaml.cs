using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DxfEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private float scaleX = .05f;
        private float scaleY = .05f;

        // DXF Document
        DXFLib.DXFDocument doc;

        // Last mouse position
        private System.Windows.Point LastMousePosition;

        public MainWindow()
        {
            InitializeComponent();
        }

        #region Event handlers

        private void OnClickOpen(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "layout"; 
            dlg.DefaultExt = ".dxf";
            dlg.InitialDirectory = System.IO.Directory.GetCurrentDirectory();
            dlg.Filter = "Autocad DXF Files (.dxf)|*.dxf";

            // Show open file dialog box 
            Nullable<bool> result = dlg.ShowDialog();

            if (result.HasValue && result.Value)
            {
                // Load DXF
                IList<Shape> background = LoadDxf(dlg.FileName);

                // Add it to canvas
                canvas.Children.Clear();
                foreach (Shape shape in background)
                    canvas.Children.Add(shape);

                Autofill();
            }
        }

        private void OnClickAutofill(object sender, RoutedEventArgs e)
        {
            Autofill();
        }

        private void OnClickZoomIn(object sender, RoutedEventArgs e)
        {
            ZoomIn();
        }

        private void OnClickZoomOut(object sender, RoutedEventArgs e)
        {
            ZoomOut();
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            var x = Math.Pow(2, e.Delta / 3.0 / Mouse.MouseWheelDeltaForOneLine);
            canvas.Scale *= x;

            // Adjust the offset to make the point under the mouse stay still.
            var position = (Vector)LastMousePosition;
            canvas.Offset = (System.Windows.Point)((Vector)(canvas.Offset + position) * x - position);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            var position = e.GetPosition(this);

            // Print position
            lblPosition.Content = position.ToString();

            // Handle Pan
            if (e.MiddleButton == MouseButtonState.Pressed || (e.LeftButton == MouseButtonState.Pressed && btnPan.IsChecked.HasValue && btnPan.IsChecked.Value))
            {
                this.Cursor = Cursors.Hand;
                canvas.Offset -= position - LastMousePosition;

            }
            else this.Cursor = Cursors.Arrow;

            LastMousePosition = position;
        }

        #endregion

        #region Private methods

        private IList<Shape> LoadDxf(string filename)
        {
            // Process open file dialog box results 
            if (!string.IsNullOrEmpty(filename))
            {
                // Parse DXF
                DateTime start = DateTime.UtcNow;

                doc = new DXFLib.DXFDocument();

                doc.Load(filename);

                System.Diagnostics.Debug.WriteLine("Loaded {0} in {1}ms", System.IO.Path.GetFileName(filename), DateTime.UtcNow.Subtract(start).TotalMilliseconds);

                // Process entities
                if (doc.Entities.Count > 0)
                {
                    // Create shapes
                    start = DateTime.UtcNow;

                    IList<Shape> shapes = new List<Shape>();
                    foreach (DXFLib.DXFEntity entity in doc.Entities)
                        ConvertDxfEntityToShapes(doc, entity, shapes);

                    System.Diagnostics.Debug.WriteLine("Created shapes in {0}ms", DateTime.UtcNow.Subtract(start).TotalMilliseconds);

                    return shapes;
                }
            }

            return null;
        }

        private void ConvertDxfEntityToShapes(DXFLib.DXFDocument doc, DXFLib.DXFEntity entity, IList<Shape> shapes)
        {
            if (entity is DXFLib.DXFLine)
            {
                DXFLib.DXFLine line = (DXFLib.DXFLine)entity;
                PointF start = new PointF((float)line.Start.X, (float)line.Start.Y);
                PointF end = new PointF((float)line.End.X, (float)line.End.Y);

                Line drawLine = new Line();
                drawLine.Stroke = System.Windows.Media.Brushes.SteelBlue;
                drawLine.X1 = end.X * scaleX;
                drawLine.X2 = start.X * scaleX;
                drawLine.Y1 = end.Y * scaleY;
                drawLine.Y2 = start.Y * scaleY;
                drawLine.StrokeThickness = 1;
                drawLine.IsHitTestVisible = false;

                shapes.Add(drawLine);
            }
            else if (entity is DXFLib.DXFCircle)
            {
                DXFLib.DXFCircle circle = (DXFLib.DXFCircle)entity;
                Ellipse drawCircle = new Ellipse();
                drawCircle.StrokeThickness = 1;
                drawCircle.Stroke = System.Windows.Media.Brushes.SteelBlue;
                drawCircle.Width = circle.Radius * 2 * scaleX;
                drawCircle.Height = circle.Radius * 2 * scaleY;
                drawCircle.Margin = new Thickness((circle.Center.X.Value - circle.Radius) * scaleX, (circle.Center.Y.Value - circle.Radius) * scaleY, 0, 0);
                drawCircle.IsHitTestVisible = false;

                shapes.Add(drawCircle);
            }
            else if (entity is DXFLib.DXFArc)
            {
                DXFLib.DXFArc arc = (DXFLib.DXFArc)entity;

                Path path = new Path();
                path.Stroke = System.Windows.Media.Brushes.SteelBlue;
                path.StrokeThickness = 1;

                System.Windows.Point endPoint = new System.Windows.Point(
                    (arc.Center.X.Value + Math.Cos(arc.EndAngle * Math.PI / 180) * arc.Radius) * scaleX,
                    (arc.Center.Y.Value + Math.Sin(arc.EndAngle * Math.PI / 180) * arc.Radius) * scaleY);

                System.Windows.Point startPoint = new System.Windows.Point(
                    (arc.Center.X.Value + Math.Cos(arc.StartAngle * Math.PI / 180) * arc.Radius) * scaleX,
                    (arc.Center.Y.Value + Math.Sin(arc.StartAngle * Math.PI / 180) * arc.Radius) * scaleY);

                ArcSegment arcSegment = new ArcSegment();
                double sweep = 0.0;
                if (arc.EndAngle < arc.StartAngle)
                    sweep = (360 + arc.EndAngle) - arc.StartAngle;
                else sweep = Math.Abs(arc.EndAngle - arc.StartAngle);

                arcSegment.IsLargeArc = sweep >= 180;
                arcSegment.Point = endPoint;
                arcSegment.Size = new System.Windows.Size(arc.Radius * scaleX, arc.Radius * scaleY);
                arcSegment.SweepDirection = arc.ExtrusionDirection.Z >= 0 ? SweepDirection.Clockwise : SweepDirection.Counterclockwise;

                PathGeometry geometry = new PathGeometry();
                PathFigure pathFigure = new PathFigure();
                pathFigure.StartPoint = startPoint;
                pathFigure.Segments.Add(arcSegment);
                geometry.Figures.Add(pathFigure);

                path.Data = geometry;
                path.IsHitTestVisible = false;
                shapes.Add(path);
            }
            else if (entity is DXFLib.DXFPolyLine)
            {
                DXFLib.DXFPolyLine polyLine = (DXFLib.DXFPolyLine)entity;
                bool isClosed = polyLine.Flags == DXFLib.DXFPolyLine.FlagsEnum.closed;

                int count = isClosed ? polyLine.Children.Count : polyLine.Children.Count - 1;
                for (int i = 1; i <= count; i++)
                {
                    DXFLib.DXFVertex vertex1 = (i == polyLine.Children.Count) ? (DXFLib.DXFVertex)polyLine.Children[0] : (DXFLib.DXFVertex)polyLine.Children[i];
                    DXFLib.DXFVertex vertex2 = (DXFLib.DXFVertex)polyLine.Children[i - 1];

                    PointF start = new PointF((float)vertex1.Location.X, (float)vertex1.Location.Y);
                    PointF end = new PointF((float)vertex2.Location.X, (float)vertex2.Location.Y);

                    // TODO: Handle Vertex.Buldge http://www.afralisp.net/archive/lisp/Bulges1.htm

                    Line drawLine = new Line();
                    drawLine.Stroke = System.Windows.Media.Brushes.Blue;
                    drawLine.X1 = end.X * scaleX;
                    drawLine.X2 = start.X * scaleX;
                    drawLine.Y1 = end.Y * scaleY;
                    drawLine.Y2 = start.Y * scaleY;
                    drawLine.StrokeThickness = 1;
                    drawLine.IsHitTestVisible = false;
                    shapes.Add(drawLine);
                }
            }
            else if (entity is DXFLib.DXFLWPolyLine)
            {
                DXFLib.DXFLWPolyLine polyLine = (DXFLib.DXFLWPolyLine)entity;
                bool isClosed = polyLine.Flags == DXFLib.DXFLWPolyLine.FlagsEnum.closed;

                int count = isClosed ? polyLine.Elements.Count : polyLine.Elements.Count - 1;
                for (int i = 1; i <= count; i++)
                {
                    DXFLib.DXFPoint vertex1 = (i == polyLine.Elements.Count) ? polyLine.Elements[0].Vertex : polyLine.Elements[i].Vertex;
                    DXFLib.DXFPoint vertex2 = polyLine.Elements[i - 1].Vertex;

                    // TODO: Handle Element.Bulge http://www.afralisp.net/archive/lisp/Bulges1.htm

                    PointF start = new PointF((float)vertex1.X, (float)vertex1.Y);
                    PointF end = new PointF((float)vertex2.X, (float)vertex2.Y);

                    Line drawLine = new Line();
                    drawLine.Stroke = System.Windows.Media.Brushes.Blue;
                    drawLine.X1 = end.X * scaleX;
                    drawLine.X2 = start.X * scaleX;
                    drawLine.Y1 = end.Y * scaleY;
                    drawLine.Y2 = start.Y * scaleY;
                    drawLine.StrokeThickness = 1;
                    drawLine.IsHitTestVisible = false;

                    shapes.Add(drawLine);
                }
            }
            else if (entity is DXFLib.DXFMText)
            {
            }
            else if (entity is DXFLib.DXFHatch)
            {
            }
            else if (entity is DXFLib.DXF3DFace)
            {
            }
            else if (entity is DXFLib.DXFInsert)
            {
                //DXFLib.DXFInsert insert = (DXFLib.DXFInsert)entity;
                //DXFLib.DXFBlock block = doc.Blocks.FirstOrDefault(x => x.BlockName == insert.BlockName);
                //if (block != null)
                //{
                //    foreach (DXFLib.DXFEntity blockEntity in block.Children)
                //        AddEntity(doc, blockEntity);
                //}
            }
            else if (entity is DXFLib.DXFSolid)
            {
            }
            else if (entity is DXFLib.DXFText)
            {
            }
            else if (entity is DXFLib.DXFTrace)
            {
            }
            else if (entity is DXFLib.DXFSpline)
            {
            }
            else if (entity is DXFLib.DXFPointEntity)
            {
            }
            else if (entity is DXFLib.DXFXLine)
            {
            }
            else if (entity is DXFLib.DXFViewPort)
            {
            }
            else
            {
                //                    
            }
        }

        private void Autofill()
        {
            if (canvas.Children.Count == 0) return;

            double minLeft = canvas.Children.OfType<Line>().Min(i => Math.Min(i.X1, i.X2));
            double maxLeft = canvas.Children.OfType<Line>().Max(i => Math.Max(i.X1, i.X2));
            double minTop = canvas.Children.OfType<Line>().Min(i => Math.Min(i.Y1, i.Y2));
            double maxTop = canvas.Children.OfType<Line>().Max(i => Math.Max(i.Y1, i.Y2));

            // Adjust scale
            double scale = Math.Min(canvas.ActualWidth / (maxLeft - minLeft), canvas.ActualHeight / (maxTop - minTop)) * 0.95;
            if (scale > 0)
            {
                double centerX = (maxLeft - minLeft) * scale;
                double centerY = (maxTop - minTop) * scale;
                canvas.Scale = scale;

                // Adjust offset
                canvas.Offset = new System.Windows.Point(minLeft * scale - (canvas.ActualWidth - (maxLeft - minLeft) * scale) / 2, minTop * scale - (canvas.ActualHeight - ((maxTop - minTop) * scale)) / 2);
            }

        }

        private void ZoomIn()
        {
            var x = Math.Pow(2, 120 / 3.0 / Mouse.MouseWheelDeltaForOneLine);
            canvas.Scale *= x;

            var position = new Vector(canvas.ActualWidth / 2, canvas.ActualHeight / 2);
            canvas.Offset = (System.Windows.Point)((Vector)(canvas.Offset + position) * x - position);
        }

        private void ZoomOut()
        {
            var x = Math.Pow(2, -120 / 3.0 / Mouse.MouseWheelDeltaForOneLine);
            canvas.Scale *= x;

            var position = new Vector(canvas.ActualWidth / 2, canvas.ActualHeight / 2);
            canvas.Offset = (System.Windows.Point)((Vector)(canvas.Offset + position) * x - position);
        }

        #endregion

    }
}
