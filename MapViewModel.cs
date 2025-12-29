using System.Collections.ObjectModel;
using NetTopologySuite.Geometries;

namespace OpenMap
{
    public class MapViewModel
    {
        public ObservableCollection<Geometry> Geometries { get; set; } = new();
        // Add pan/zoom state here
    }
}