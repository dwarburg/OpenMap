using NetTopologySuite.Geometries;
using SkiaSharp;

namespace OpenMap
{
    public static class GeometryConverter
    {
        public static SKPath ToSKPath(Geometry geometry)
        {
            // Convert NetTopologySuite Geometry to SkiaSharp SKPath
            // Implement for Point, LineString, Polygon, etc.
            return new SKPath();
        }
    }
}