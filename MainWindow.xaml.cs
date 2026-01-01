using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace OpenMap
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MapViewModel _viewModel;
        private readonly IConfiguration _configuration;
        private MapControl? mapControl;

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

            // Initialize MapControl with connection string
            var connectionString = _configuration.GetConnectionString("Postgis");
            if (string.IsNullOrEmpty(connectionString))
            {
                var errorMsg = "Connection string for 'Postgis' is not configured in appsettings.json or user secrets.";
                Debug.WriteLine(errorMsg);
                MessageBox.Show(errorMsg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            // Create new MapControl instance with connection string
            mapControl = new MapControl(connectionString);
            MainGrid.Children.Add(mapControl);
            
            // Fetch geometries and update the map
            LoadGeometries();
        }

        private async void LoadGeometries()
        {
            try
            {
                // Use IConfiguration to fetch the connection string
                var connectionString = _configuration.GetConnectionString("Postgis");
                if (string.IsNullOrEmpty(connectionString))
                {
                    var errorMsg = "Connection string for 'Postgis' is not configured in appsettings.json or user secrets.";
                    Debug.WriteLine(errorMsg);
                    MessageBox.Show(errorMsg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                Debug.WriteLine($"Successfully retrieved connection string: {connectionString.Substring(0, Math.Min(30, connectionString.Length))}...");

                var postgisService = new PostgisService(connectionString);
                string query = "SELECT geom FROM line_features_1 LIMIT 1000;";
                Debug.WriteLine($"Executing query: {query}");
                
                var geometries = await postgisService.GetGeometriesAsync(query);
                Debug.WriteLine($"Retrieved {geometries.Count} geometries from the database.");
                
                if (geometries.Count == 0)
                {
                    Debug.WriteLine("Warning: No geometries were returned from the database.");
                    MessageBox.Show("No geometries were found in the database table 'line_features_1'.", "Warning", 
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Log some information about the first few geometries
                for (int i = 0; i < Math.Min(3, geometries.Count); i++)
                {
                    var geom = geometries[i];
                    Debug.WriteLine($"Geometry {i + 1}: Type={geom.GeometryType}, SRID={geom.SRID}, " +
                                    $"Points={geom.NumPoints}, Bounds={geom.EnvelopeInternal}");
                }

                _viewModel.Geometries.Clear();
                foreach (var geometry in geometries)
                {
                    _viewModel.Geometries.Add(geometry);
                }
                Debug.WriteLine($"Added {_viewModel.Geometries.Count} geometries to the view model.");
                
                if (mapControl != null)
                {
                    mapControl.UpdateGeometries(_viewModel.Geometries); // Notify MapControl to update
                }
                Debug.WriteLine("Map control update triggered.");
            }
            catch (Exception ex)
            {
                string errorMsg = $"Error loading geometries: {ex.Message}\n\n{ex.StackTrace}";
                Debug.WriteLine(errorMsg);
                MessageBox.Show($"An error occurred while loading geometries: {ex.Message}", 
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}