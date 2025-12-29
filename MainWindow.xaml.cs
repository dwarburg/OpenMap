using System.Configuration;
using System.Windows;
using Microsoft.Extensions.Configuration;

namespace OpenMap
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MapViewModel _viewModel;
        private readonly IConfiguration _configuration;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MapViewModel();
            DataContext = _viewModel;

            // Build configuration to include user secrets
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddUserSecrets<MainWindow>() // Add user secrets
                .Build();

            // Fetch geometries and update the map
            LoadGeometries();
        }

        private async void LoadGeometries()
        {
            // Use IConfiguration to fetch the connection string
            var connectionString = _configuration.GetConnectionString("Postgis");
            if (string.IsNullOrEmpty(connectionString))
            {
                MessageBox.Show("Connection string for 'Postgis' is not configured.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var postgisService = new PostgisService(connectionString);
            var geometries = await postgisService.GetGeometriesAsync("SELECT geom FROM line_features_1 LIMIT 1000;");
            _viewModel.Geometries.Clear();
            foreach (var geometry in geometries)
            {
                _viewModel.Geometries.Add(geometry);
            }
            Map.UpdateGeometries(_viewModel.Geometries); // Notify MapControl to update
        }
    }
}