using System.IO;
using System.Windows;

namespace DxfEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnClickOpen(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".dxf";
            dlg.InitialDirectory = Path.Combine(Directory.GetCurrentDirectory(), "DXF");
            dlg.Filter = "Autocad DXF Files (.dxf)|*.dxf";

            // Show open file dialog box 
            bool? result = dlg.ShowDialog();

            if (result.HasValue && result.Value)
            {
                viewer.FileName = dlg.FileName;
            }
        }

        private void OnClickZoomExtents(object sender, RoutedEventArgs e)
        {
            viewer.ZoomExtents();
        }

        private void OnClickZoomIn(object sender, RoutedEventArgs e)
        {
            viewer.ZoomIn();
        }

        private void OnClickZoomOut(object sender, RoutedEventArgs e)
        {
            viewer.ZoomOut();
        }
    }
}
