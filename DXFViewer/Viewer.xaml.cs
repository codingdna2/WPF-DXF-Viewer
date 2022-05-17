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
            if (dxf.Document != null)
            {
                drawingExtendsUpperRight = dxf.Document.Header.DrawingExtendsUpperRight;
                drawingExtendsLowerRight = dxf.Document.Header.DrawingExtendsLowerRight;
                canvas.Children.Add(dxf.Canvas);
                ZoomExtents();
            }
        }

        private void LoadBackgroundGeometries()
        {
            canvas.Children.Clear();
            var dxf = this.ConvertDxfToGeometries(FileName);
            if (dxf.Document != null)
            {
                drawingExtendsUpperRight = dxf.Document.Header.DrawingExtendsUpperRight;
                drawingExtendsLowerRight = dxf.Document.Header.DrawingExtendsLowerRight;
                canvas.Children.Add(dxf.Canvas);
                ZoomExtents();
            }
        }

        private (DXFDocument Document, Canvas Canvas) ConvertDxfToGeometries(string fileName)
        {
            Canvas childCanvas = new Canvas();
            var (Document, Path) = DxfConversionHelpers.LoadDxfGeometries(fileName);

            //Path.StrokeThickness = 20;

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
            if (canvas.Children.Count == 0 || drawingExtendsLowerRight == null || drawingExtendsUpperRight == null) return;

            double xMin = Math.Min(drawingExtendsLowerRight.X.Value * ScaleX, drawingExtendsUpperRight.X.Value * ScaleX);  
            double xMax = Math.Max(drawingExtendsLowerRight.X.Value * ScaleX, drawingExtendsUpperRight.X.Value * ScaleX);  
            double yMin = Math.Min(drawingExtendsUpperRight.Y.Value * -ScaleY, drawingExtendsLowerRight.Y.Value * -ScaleY); 
            double yMax = Math.Max(drawingExtendsUpperRight.Y.Value * -ScaleY, drawingExtendsLowerRight.Y.Value * -ScaleY);

            // Calculate scale
            double scale = Math.Min(canvas.ActualWidth / (xMax - xMin), canvas.ActualHeight / (yMax - yMin)) * zoomExtentsFactor;
            if (scale > 0 && !double.IsInfinity(scale))
            {
                // Adjust scale
                canvas.Scale = scale;

                // Adjust offset
                Point generalOffset = new Point(xMin * scale - (canvas.ActualWidth - (xMax - xMin) * scale) / 2,
                    yMin * scale - (canvas.ActualHeight - ((yMax - yMin) * scale)) / 2);

                canvas.Offset = canvas.LayoutTransform.Transform(generalOffset);
                NotifyPropertyChanged(nameof(Offset));

                StrokeThickness = 1 / canvas.Scale;
            }
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
