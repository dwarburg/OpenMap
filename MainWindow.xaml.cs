using System.Configuration;
using System.Windows;

namespace OpenMap
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MapViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MapViewModel();
            DataContext = _viewModel;

            // Fetch geometries and update the map
            LoadGeometries();
        }

        private async void LoadGeometries()
        {
            // Use ConfigurationManager to fetch the connection string
            var connectionString = ConfigurationManager.ConnectionStrings["Postgis"]?.ConnectionString;
            if (string.IsNullOrEmpty(connectionString))
            {
                MessageBox.Show("Connection string for 'Postgis' is not configured.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var postgisService = new PostgisService(connectionString);
            var geometries = await postgisService.GetGeometriesAsync("SELECT geom FROM your_table LIMIT 1000;");
            _viewModel.Geometries.Clear();
            foreach (var geometry in geometries)
            {
                _viewModel.Geometries.Add(geometry);
            }
            Map.UpdateGeometries(_viewModel.Geometries); // Notify MapControl to update
        }
    }
}