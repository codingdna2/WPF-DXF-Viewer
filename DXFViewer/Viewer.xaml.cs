using DXFLib;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DXFViewer
{
    public partial class Viewer : UserControl, INotifyPropertyChanged
    {
        #region Fields

        public static readonly DependencyProperty FileNameProperty = DependencyProperty.Register(nameof(FileName), typeof(string), typeof(Viewer), new PropertyMetadata(null, new PropertyChangedCallback(OnFilenameChanged)));

        public static readonly DependencyProperty PanEnabledProperty = DependencyProperty.Register(nameof(PanEnabled), typeof(bool), typeof(Viewer), new PropertyMetadata(false, new PropertyChangedCallback(OnPanEnabledChanged)));

        public static readonly DependencyProperty ScaleXProperty = DependencyProperty.Register(nameof(ScaleX), typeof(double), typeof(Viewer), new PropertyMetadata(1.0));

        public static readonly DependencyProperty ScaleYProperty = DependencyProperty.Register(nameof(ScaleY), typeof(double), typeof(Viewer), new PropertyMetadata(1.0));

        private const double zoomExtentsFactor = 0.95;

        // Mouse position
        private Point mousePosition;

        private Point canvasPosition;

        // Bounding box
        private DXFPoint drawingExtendsUpperRight;

        private DXFPoint drawingExtendsLowerRight;

        // Default stroke weight
        private double strokeThickness = 40;

        // Set when ZoomExtents is requested before the canvas has a viewport size;
        // the pending fit is applied on the next layout pass.
        private bool zoomExtentsPending;

        #endregion Fields

        #region Constructors

        public Viewer()
        {
            InitializeComponent();
        }

        #endregion Constructors

        #region Properties

        public bool PanEnabled
        {
            get => (bool)GetValue(PanEnabledProperty);
            set => SetValue(PanEnabledProperty, value);
        }

        public string FileName
        {
            get => (string)GetValue(FileNameProperty);
            set => SetValue(FileNameProperty, value);
        }

        public Point MousePosition { get => mousePosition; set { mousePosition = value; NotifyPropertyChanged(); } }

        public Point CanvasPosition { get => canvasPosition; set { canvasPosition = value; NotifyPropertyChanged(); } }

        public Point? Offset => canvas?.Offset;

        public double StrokeThickness { get => strokeThickness; set { strokeThickness = value; NotifyPropertyChanged(); } }

        public double ScaleX        
        {
            get => (double)GetValue(ScaleXProperty);
            set => SetValue(ScaleXProperty, value);
        }

        public double ScaleY
        {
            get => (double)GetValue(ScaleYProperty);
            set => SetValue(ScaleYProperty, value);
        }

        #endregion Properties

        #region Methods

        private static void OnPanEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = (d as Viewer);
            instance.PanEnabled = (bool)e.NewValue;
        }

        private static void OnFilenameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = (d as Viewer);
            instance.LoadBackgroundGeometries();
            //instance.LoadBackground();
        }

        private void LoadBackground()
        {
            canvas.Children.Clear();
            var dxf = this.ConvertDxf(FileName);
            if (dxf.Document != null && dxf.Canvas != null)
            {
                drawingExtendsUpperRight = dxf.Document.Header.DrawingExtendsUpperRight;
                drawingExtendsLowerRight = dxf.Document.Header.DrawingExtendsLowerRight;
                canvas.Children.Add(dxf.Canvas);
                ZoomExtentsWhenReady();
            }
        }

        private void LoadBackgroundGeometries()
        {
            canvas.Children.Clear();
            var dxf = this.ConvertDxfToGeometries(FileName);
            if (dxf.Document != null && dxf.Canvas != null)
            {
                drawingExtendsUpperRight = dxf.Document.Header.DrawingExtendsUpperRight;
                drawingExtendsLowerRight = dxf.Document.Header.DrawingExtendsLowerRight;
                canvas.Children.Add(dxf.Canvas);
                ZoomExtentsWhenReady();
            }
        }

        /// <summary>
        /// Centres the newly loaded drawing. Loading runs before the canvas has been laid out,
        /// so the fit is deferred to the next layout pass when that is the case.
        /// </summary>
        private void ZoomExtentsWhenReady()
        {
            if (HasViewport())
            {
                ZoomExtents();
                return;
            }

            zoomExtentsPending = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (zoomExtentsPending) ZoomExtents();
            }), DispatcherPriority.Loaded);
        }

        private (DXFDocument Document, Canvas Canvas) ConvertDxfToGeometries(string fileName)
        {
            Canvas childCanvas = new Canvas();
            var (Document, Path) = DxfConversionHelpers.LoadDxfGeometries(fileName);

            if (Path == null) return (null, null);

            Binding bindingStrokeThickness = new Binding(nameof(StrokeThickness)) { Source = this, Mode = BindingMode.OneWay, ValidatesOnDataErrors = false, ValidatesOnExceptions = false, ValidatesOnNotifyDataErrors = false };
            Path.DataContext = this;
            Path.SetBinding(Shape.StrokeThicknessProperty, bindingStrokeThickness);

            childCanvas.Children.Add(Path);
            childCanvas.RenderTransform = new ScaleTransform(ScaleX, ScaleY);
            childCanvas.RenderTransformOrigin = new Point(0.5, 0.5);

            return (Document, childCanvas);
        }


        private (DXFDocument Document, Canvas Canvas) ConvertDxf(string fileName)
        {
            Canvas childCanvas = new Canvas();
            var (Document, Shapes) = DxfConversionHelpers.LoadDxf(fileName);

            // Add it to canvas                    
            foreach (Shape shape in Shapes)
            {
                //shape.StrokeThickness = strokeThickness;

                Binding bindingStrokeThickness = new Binding(nameof(StrokeThickness)) { Source = this, Mode = BindingMode.OneWay, ValidatesOnDataErrors = false, ValidatesOnExceptions = false, ValidatesOnNotifyDataErrors = false };
                shape.DataContext = this;
                shape.SetBinding(Shape.StrokeThicknessProperty, bindingStrokeThickness);

                childCanvas.Children.Add(shape);
            }

            childCanvas.RenderTransform = new ScaleTransform(ScaleX, ScaleY);
            childCanvas.RenderTransformOrigin = new Point(0.5, 0.5);

            return (Document, childCanvas);
        }

        #endregion Methods

        #region Zoomable Canvas

        public void ZoomExtents()
        {
            if (canvas == null || canvas.Children.Count == 0) return;

            // The canvas has not been laid out yet, so there is no viewport to fit into and
            // the geometry has no rendered bounds either. Defer until layout gives us a size;
            // OnRenderSizeChanged calls us again once it does.
            if (!HasViewport()) { zoomExtentsPending = true; return; }

            double xMin, xMax, yMin, yMax;

            if (!TryGetHeaderExtents(out xMin, out xMax, out yMin, out yMax) &&
                !TryGetContentExtents(out xMin, out xMax, out yMin, out yMax))
                return;

            ApplyZoomExtents(xMin, xMax, yMin, yMax);
        }

        /// <summary>
        /// Returns whether the canvas has a usable viewport size.
        /// </summary>
        private bool HasViewport()
        {
            return canvas.ActualWidth > 0 && canvas.ActualHeight > 0 &&
                   !double.IsInfinity(canvas.ActualWidth) && !double.IsInfinity(canvas.ActualHeight);
        }

        /// <summary>
        /// Fits the given extents into the canvas and centres them.
        /// </summary>
        private void ApplyZoomExtents(double xMin, double xMax, double yMin, double yMax)
        {
            // Guard against a degenerate extent (e.g. a single horizontal line): a zero span
            // would divide by zero and produce an infinite or NaN scale.
            if (xMax <= xMin) { xMin -= 0.5; xMax += 0.5; }
            if (yMax <= yMin) { yMin -= 0.5; yMax += 0.5; }

            double width = xMax - xMin;
            double height = yMax - yMin;

            // Calculate scale
            double scale = Math.Min(canvas.ActualWidth / width, canvas.ActualHeight / height) * zoomExtentsFactor;
            if (!(scale > 0) || double.IsInfinity(scale)) return;

            // Adjust scale
            canvas.Scale = scale;

            // Adjust offset so the extents are centred on the canvas
            Point generalOffset = new Point(xMin * scale - (canvas.ActualWidth - width * scale) / 2,
                yMin * scale - (canvas.ActualHeight - height * scale) / 2);

            canvas.Offset = canvas.LayoutTransform.Transform(generalOffset);
            NotifyPropertyChanged(nameof(Offset));

            StrokeThickness = 1 / canvas.Scale;

            zoomExtentsPending = false;
        }

        /// <summary>
        /// Gets the drawing extents from the DXF header. Returns false when the header
        /// has no usable extents.
        /// </summary>
        private bool TryGetHeaderExtents(out double xMin, out double xMax, out double yMin, out double yMax)
        {
            xMin = xMax = yMin = yMax = 0;

            if (drawingExtendsLowerRight == null || drawingExtendsUpperRight == null) return false;

            double? lowerX = drawingExtendsLowerRight.X;
            double? lowerY = drawingExtendsLowerRight.Y;
            double? upperX = drawingExtendsUpperRight.X;
            double? upperY = drawingExtendsUpperRight.Y;

            if (!lowerX.HasValue || !lowerY.HasValue || !upperX.HasValue || !upperY.HasValue) return false;

            if (!IsUsableExtent(lowerX.Value) || !IsUsableExtent(lowerY.Value) ||
                !IsUsableExtent(upperX.Value) || !IsUsableExtent(upperY.Value))
                return false;

            xMin = Math.Min(lowerX.Value * ScaleX, upperX.Value * ScaleX);
            xMax = Math.Max(lowerX.Value * ScaleX, upperX.Value * ScaleX);
            yMin = Math.Min(upperY.Value * -ScaleY, lowerY.Value * -ScaleY);
            yMax = Math.Max(upperY.Value * -ScaleY, lowerY.Value * -ScaleY);

            return xMax > xMin && yMax > yMin;
        }

        /// <summary>
        /// Gets the drawing extents from the geometry currently drawn on the canvas.
        /// Returns false when nothing has been drawn.
        /// </summary>
        private bool TryGetContentExtents(out double xMin, out double xMax, out double yMin, out double yMax)
        {
            xMin = xMax = yMin = yMax = 0;
            Rect contentBounds = Rect.Empty;

            foreach (UIElement child in canvas.Children)
            {
                Canvas childCanvas = child as Canvas;
                if (childCanvas == null) continue;

                foreach (UIElement grandChild in childCanvas.Children)
                {
                    Shape shape = grandChild as Shape;
                    if (shape == null) continue;

                    // RenderedGeometry is only populated once the shape has been measured, so
                    // fall back to the path data. That lets extents be computed before the
                    // first layout pass, which is when a load happens.
                    Geometry geometry = shape.RenderedGeometry;
                    if (geometry == null || geometry.Bounds.IsEmpty)
                        geometry = GetShapeGeometry(shape);
                    if (geometry == null) continue;

                    Rect shapeBounds = geometry.Bounds;
                    if (shapeBounds.IsEmpty) continue;

                    Transform transform = shape.RenderTransform;
                    if (transform != null && !transform.Value.IsIdentity)
                        shapeBounds = transform.TransformBounds(shapeBounds);

                    contentBounds.Union(shapeBounds);
                }
            }

            if (contentBounds.IsEmpty) return false;

            xMin = contentBounds.Left;
            xMax = contentBounds.Right;
            yMin = contentBounds.Top;
            yMax = contentBounds.Bottom;

            // Flat geometry has no extent on one axis; give it a unit of padding.
            if (xMax <= xMin) { xMin -= 1; xMax += 1; }
            if (yMax <= yMin) { yMin -= 1; yMax += 1; }

            return true;
        }

        /// <summary>
        /// Returns the geometry of a shape without relying on it having been measured yet.
        /// </summary>
        private static Geometry GetShapeGeometry(Shape shape)
        {
            Path path = shape as Path;
            if (path != null) return path.Data;

            // Any other Shape type has no data we can read before layout.
            return null;
        }

        /// <summary>
        /// Returns whether an extent value is a real coordinate rather than a sentinel.
        /// </summary>
        private static bool IsUsableExtent(double extent)
        {
            const double maxPlausibleExtent = 1.0E+15;

            return !double.IsNaN(extent) && !double.IsInfinity(extent) && Math.Abs(extent) < maxPlausibleExtent;
        }

        public void ZoomIn()
        {
            var x = Math.Pow(2, 120 / 3.0 / Mouse.MouseWheelDeltaForOneLine);
            canvas.Scale *= x;

            var position = new Vector(canvas.ActualWidth / 2, canvas.ActualHeight / 2);
            canvas.Offset = (Point)((Vector)(canvas.Offset + position) * x - position);
            NotifyPropertyChanged(nameof(Offset));

            StrokeThickness = 1 / canvas.Scale;
        }

        public void ZoomOut()
        {
            var x = Math.Pow(2, -120 / 3.0 / Mouse.MouseWheelDeltaForOneLine);
            canvas.Scale *= x;

            var position = new Vector(canvas.ActualWidth / 2, canvas.ActualHeight / 2);
            canvas.Offset = (Point)((Vector)(canvas.Offset + position) * x - position);
            NotifyPropertyChanged(nameof(Offset));

            StrokeThickness = 1 / canvas.Scale;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            var position = e.GetPosition(this);

            // Handle Pan
            if (e.MiddleButton == MouseButtonState.Pressed || (PanEnabled && e.LeftButton == MouseButtonState.Pressed))
            {
                this.Cursor = Cursors.Hand;
                canvas.Offset -= position - MousePosition;
                NotifyPropertyChanged(nameof(Offset));

            }
            else this.Cursor = Cursors.Arrow;

            MousePosition = position;
            CanvasPosition = canvas.GetCanvasPoint(position);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            var x = Math.Pow(2, e.Delta / 3.0 / Mouse.MouseWheelDeltaForOneLine);
            canvas.Scale *= x;

            // Adjust the offset to make the point under the mouse stay still.
            var position = (Vector)MousePosition;
            canvas.Offset = (Point)((Vector)(canvas.Offset + position) * x - position);
            NotifyPropertyChanged(nameof(Offset));

            StrokeThickness = 1 / canvas.Scale;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            ZoomExtents();
        }

        #endregion Zoomable Canvas

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

    }
}
