using NetTopologySuite.Geometries;
using SkiaSharp;

namespace OpenMap
{
    public static class GeometryConverter
    {
        public static SKPath ToSKPath(Geometry geometry)
        {
            var path = new SKPath();
            if (geometry is LineString line)
            {
                if (line.NumPoints > 0)
                {
                    var start = line.GetCoordinateN(0);
                    path.MoveTo((float)start.X, (float)start.Y);
                    for (int i = 1; i < line.NumPoints; i++)
                    {
                        var coord = line.GetCoordinateN(i);
                        path.LineTo((float)coord.X, (float)coord.Y);
                    }
                }
            }
            else if (geometry is Point point)
            {
                path.AddCircle((float)point.X, (float)point.Y, 5); // Draw a small circle for the point
            }
            else if (geometry is Polygon polygon)
            {
                var exteriorRing = polygon.ExteriorRing;
                if (exteriorRing != null)
                {
                    var start = exteriorRing.GetCoordinateN(0);
                    path.MoveTo((float)start.X, (float)start.Y);
                    for (int i = 1; i < exteriorRing.NumPoints; i++)
                    {
                        var coord = exteriorRing.GetCoordinateN(i);
                        path.LineTo((float)coord.X, (float)coord.Y);
                    }
                    path.Close(); // Close the polygon
                }

                // Handle interior rings (holes)
                for (int i = 0; i < polygon.NumInteriorRings; i++)
                {
                    var interiorRing = polygon.GetInteriorRingN(i);
                    var start = interiorRing.GetCoordinateN(0);
                    path.MoveTo((float)start.X, (float)start.Y);
                    for (int j = 1; j < interiorRing.NumPoints; j++)
                    {
                        var coord = interiorRing.GetCoordinateN(j);
                        path.LineTo((float)coord.X, (float)coord.Y);
                    }
                    path.Close();
                }
            }
            else if (geometry is MultiLineString multiLine)
            {
                foreach (var lineString in multiLine.Geometries) // Renamed 'line' to 'lineString' to avoid conflict
                {
                    var linePath = ToSKPath(lineString);
                    path.AddPath(linePath);
                }
            }
            else if (geometry is MultiPolygon multiPolygon)
            {
                foreach (var multiPolygonGeometry in multiPolygon.Geometries) // Renamed 'polygon' to 'multiPolygonGeometry' to avoid conflict
                {
                    var polygonPath = ToSKPath(multiPolygonGeometry);
                    path.AddPath(polygonPath);
                }
            }
            // Handle other geometry types...
            return path;
        }
    }
}