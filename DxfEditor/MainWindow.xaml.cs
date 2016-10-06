using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DxfEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private float scaleX = 0.05f;
        private float scaleY = 0.05f;
        private double strokeWeight = 40;

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
                        ConvertDxfEntityToShapes(doc, entity, shapes, System.Windows.Media.Brushes.SteelBlue);                        

                    System.Diagnostics.Debug.WriteLine("Created shapes in {0}ms", DateTime.UtcNow.Subtract(start).TotalMilliseconds);

                    return shapes;
                }
            }

            return null;
        }

        private void ConvertDxfEntityToShapes(DXFLib.DXFDocument doc, DXFLib.DXFEntity entity, IList<Shape> shapes, System.Windows.Media.Brush stroke)
        {
            if (entity is DXFLib.DXFLine)
            {
                DXFLib.DXFLine line = (DXFLib.DXFLine)entity;
                Point start = canvas.LayoutTransform.Transform(new Point((float)line.Start.X, (float)-line.Start.Y));
                Point end = canvas.LayoutTransform.Transform(new Point((float)line.End.X, (float)-line.End.Y));

                Line drawLine = new Line();
                drawLine.Stroke = stroke;
                drawLine.X1 = end.X * scaleX;
                drawLine.X2 = start.X * scaleX;
                drawLine.Y1 = end.Y * scaleY;
                drawLine.Y2 = start.Y * scaleY;
                drawLine.StrokeThickness = strokeWeight;
                drawLine.IsHitTestVisible = false;

                shapes.Add(drawLine);
            }
            else if (entity is DXFLib.DXFCircle)
            {

                DXFLib.DXFCircle circle = (DXFLib.DXFCircle)entity;
                Ellipse drawCircle = new Ellipse();
                drawCircle.StrokeThickness = strokeWeight;
                drawCircle.Stroke = stroke;
                drawCircle.Width = circle.Radius * 2 * scaleX;
                drawCircle.Height = circle.Radius * 2 * scaleY;
                drawCircle.IsHitTestVisible = false;

                Point center = canvas.LayoutTransform.Transform(new Point((double)circle.Center.X, (double)circle.Center.Y));

                Canvas.SetLeft(drawCircle, (center.X - circle.Radius) * scaleX);
                Canvas.SetTop(drawCircle, (-center.Y - circle.Radius) * scaleY);

                shapes.Add(drawCircle);

            }
            else if (entity is DXFLib.DXFArc)
            {
                DXFLib.DXFArc arc = (DXFLib.DXFArc)entity;

                System.Windows.Shapes.Path path = new System.Windows.Shapes.Path();
                path.Stroke = stroke;
                path.StrokeThickness = strokeWeight;

                Point endPoint = canvas.LayoutTransform.Transform(new Point(
                    (arc.Center.X.Value + Math.Cos(arc.EndAngle * Math.PI / 180) * arc.Radius) * scaleX,
                    (-arc.Center.Y.Value - Math.Sin(arc.EndAngle * Math.PI / 180) * arc.Radius) * scaleY));

                Point startPoint = canvas.LayoutTransform.Transform(new Point(
                    (arc.Center.X.Value + Math.Cos(arc.StartAngle * Math.PI / 180) * arc.Radius) * scaleX,
                    (-arc.Center.Y.Value - Math.Sin(arc.StartAngle * Math.PI / 180) * arc.Radius) * scaleY));

                ArcSegment arcSegment = new ArcSegment();
                double sweep = 0.0;
                if (arc.EndAngle < arc.StartAngle)
                    sweep = (360 + arc.EndAngle) - arc.StartAngle;
                else sweep = Math.Abs(arc.EndAngle - arc.StartAngle);

                arcSegment.IsLargeArc = sweep >= 180;
                arcSegment.Point = endPoint;
                arcSegment.Size = new System.Windows.Size(arc.Radius * scaleX, arc.Radius * scaleY);
                arcSegment.SweepDirection = arc.ExtrusionDirection.Z >= 0 ? SweepDirection.Counterclockwise : SweepDirection.Clockwise;

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

                    Point start = canvas.LayoutTransform.Transform(new Point((float)vertex1.Location.X, (float)-vertex1.Location.Y));
                    Point end = canvas.LayoutTransform.Transform(new Point((float)vertex2.Location.X, (float)-vertex2.Location.Y));

                    // TODO: Handle Vertex.Buldge http://www.afralisp.net/archive/lisp/Bulges1.htm

                    Line drawLine = new Line();
                    drawLine.Stroke = stroke;
                    drawLine.X1 = end.X * scaleX;
                    drawLine.X2 = start.X * scaleX;
                    drawLine.Y1 = end.Y * scaleY;
                    drawLine.Y2 = start.Y * scaleY;
                    drawLine.StrokeThickness = strokeWeight;
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

                    Point start = canvas.LayoutTransform.Transform(new Point((float)vertex1.X, (float)-vertex1.Y));
                    Point end = canvas.LayoutTransform.Transform(new Point((float)vertex2.X, (float)-vertex2.Y));

                    Line drawLine = new Line();
                    drawLine.Stroke = stroke;
                    drawLine.X1 = end.X * scaleX;
                    drawLine.X2 = start.X * scaleX;
                    drawLine.Y1 = end.Y * scaleY;
                    drawLine.Y2 = start.Y * scaleY;
                    drawLine.StrokeThickness = strokeWeight;
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
                return;

                //// THIS FUNCTION STILL HAVE SOME PROBLEMS
                //DXFLib.DXFInsert insert = (DXFLib.DXFInsert)entity;
                //DXFLib.DXFBlock block = doc.Blocks.FirstOrDefault(x => x.BlockName == insert.BlockName);
                //if (block != null && block.HasChildren && !block.IsInvisible)
                //{
                //    IList<Shape> blockEntities = new List<Shape>();
                //    foreach (DXFLib.DXFEntity blockEntity in block.Children)
                //        ConvertDxfEntityToShapes(doc, blockEntity, blockEntities, System.Windows.Media.Brushes.Red);

                //    double centerX = insert.InsertionPoint.X.Value * scaleX;
                //    double centerY = -insert.InsertionPoint.Y.Value * scaleY;

                //    foreach (Shape shape in blockEntities)
                //    {
                //        TranslateTransform translateTransform1 = new TranslateTransform(centerX, centerY);
                //        RotateTransform rotateTransform1 = new RotateTransform(insert.RotationAngle.HasValue ? insert.RotationAngle.Value : 0, centerX, centerY);
                //        ScaleTransform scaleTransform1 = null;

                //        if ((insert.Scaling.X.HasValue && insert.Scaling.X.Value < 100) || (insert.Scaling.Y.HasValue && insert.Scaling.Y.Value < 100))
                //            scaleTransform1 = new ScaleTransform(insert.Scaling.X.HasValue ? insert.Scaling.X.Value : 1, insert.Scaling.Y.HasValue ? insert.Scaling.Y.Value : 1, centerX, centerY);
                //        //else if (System.Diagnostics.Debugger.IsAttached)
                //        //    System.Diagnostics.Debugger.Break();

                //        // Create a TransformGroup to contain the transforms 
                //        TransformGroup myTransformGroup = new TransformGroup();
                //        myTransformGroup.Children.Add(translateTransform1);
                //        myTransformGroup.Children.Add(rotateTransform1);

                //        if (scaleTransform1 != null)
                //            myTransformGroup.Children.Add(scaleTransform1);

                //        shape.RenderTransform = myTransformGroup;
                //        shapes.Add(shape);
                //    }
                //}
            }
            else if (entity is DXFLib.DXFSolid)
            {
            }
            else if (entity is DXFLib.DXFText)
            {
                DXFLib.DXFText dxfText = (DXFLib.DXFText)entity;
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
            }
        }

        private void Autofill()
        {
            if (canvas.Children.Count == 0) return;

            double minLeft = canvas.Children.OfType<Line>().Min(i => Math.Min(i.X1, i.X2));
            double maxLeft = canvas.Children.OfType<Line>().Max(i => Math.Max(i.X1, i.X2));
            double minTop = canvas.Children.OfType<Line>().Min(i => Math.Min(i.Y1, i.Y2));
            double maxTop = canvas.Children.OfType<Line>().Max(i => Math.Max(i.Y1, i.Y2));

            // Calculate scale
            double scale = Math.Min(canvas.ActualWidth / (maxLeft - minLeft), canvas.ActualHeight / (maxTop - minTop)) * 0.95;
            if (scale > 0)
            {
                // Adjust scale
                canvas.Scale = scale;

                // Adjust offset
                Point generalOffset = new Point(minLeft * scale - (canvas.ActualWidth - (maxLeft - minLeft) * scale) / 2,
                    minTop * scale - (canvas.ActualHeight - ((maxTop - minTop) * scale)) / 2);

                canvas.Offset = canvas.LayoutTransform.Transform(generalOffset);
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
