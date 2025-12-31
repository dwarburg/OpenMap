using System;
using System.Windows.Input;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Linq;
using NetTopologySuite.Geometries;
using System.Diagnostics;

namespace OpenMap
{
    public class MapControl : SKElement
    {
        private SKMatrix _viewMatrix = SKMatrix.CreateIdentity(); // Pan/zoom matrix
        private float _scale = 1.0f; // Zoom level
        private SKPoint _translate = new(0, 0); // Pan offset
        private IEnumerable<Geometry>? _geometries; // Store geometries

        public MapControl()
        {
            // Attach event handlers for mouse interactions
            this.MouseWheel += OnMouseWheel;
            this.MouseMove += OnMouseMove;
            this.MouseLeftButtonDown += OnMouseLeftButtonDown;
            this.MouseRightButtonDown += OnMouseRightButtonDown;
            this.MouseLeftButtonUp += OnMouseLeftButtonUp;
            this.MouseRightButtonUp += OnMouseRightButtonUp;
            this.MouseLeave += OnMouseLeave;
        }

        // Mouse interaction state
        private bool _isPanning = false;
        private SKPoint _lastMousePosition;

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            base.OnPaintSurface(e);

            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.White);

            float width = (float)Width;
            float height = (float)Height;

            // Ensure valid dimensions
            if (width <= 0 || height <= 0 || float.IsNaN(width) || float.IsNaN(height))
            {
                width = e.Info.Width;
                height = e.Info.Height;
                if (width <= 0 || height <= 0 || float.IsNaN(width) || float.IsNaN(height))
                {
                    width = 800;  // Default fallback width
                    height = 600; // Default fallback height
                }
            }

            // Debug: Draw a border around the control
            using (var borderPaint = new SKPaint { Color = SKColors.LightGray, Style = SKPaintStyle.Stroke, StrokeWidth = 1 })
            {
                canvas.DrawRect(new SKRect(0, 0, width - 1, height - 1), borderPaint);
            }

            if (_geometries == null || !_geometries.Any())
            {
                DrawDebugText(canvas, _geometries == null ? "No geometries loaded (null)" : "No geometries to display (empty collection)");
                return;
            }

            Debug.WriteLine($"Rendering {_geometries.Count()} geometries...");

            // Calculate the bounding box of all geometries
            var envelope = new Envelope();
            int validGeometries = 0;
            
            foreach (var geometry in _geometries.Where(g => g != null && !g.IsEmpty))
            {
                if (geometry.EnvelopeInternal != null && !geometry.EnvelopeInternal.IsNull)
                {
                    envelope.ExpandToInclude(geometry.EnvelopeInternal);
                    validGeometries++;
                    Debug.WriteLine($"Geometry: Type={geometry.GeometryType}, SRID={geometry.SRID}, Points={geometry.NumPoints}, Bounds={geometry.EnvelopeInternal}");
                }
            }

            if (validGeometries == 0)
            {
                DrawDebugText(canvas, "No valid geometries to display (all empty or null)");
                return;
            }

            // Ensure envelope has valid dimensions
            if (envelope.Width <= 0 || envelope.Height <= 0 || double.IsNaN(envelope.Width) || double.IsNaN(envelope.Height))
            {
                // If envelope is invalid, use a default view
                envelope = new Envelope(-180, 180, -90, 90); // Default world bounds
                Debug.WriteLine("Using default envelope due to invalid geometry bounds");
            }

            // Use actual surface size
            var controlWidth = (float)e.Info.Width;
            var controlHeight = (float)e.Info.Height;

            // Calculate the transformation matrix;
            var transformMatrix = CalculateTransform(envelope, controlWidth, controlHeight);
            var finalMatrix = transformMatrix.PreConcat(_viewMatrix);
            canvas.SetMatrix(finalMatrix);

            // Draw the geometries with different colors based on type
            int drawnCount = 0;
            foreach (var geometry in _geometries.Where(g => g != null && !g.IsEmpty))
            {
                using var paint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = geometry switch
                    {
                        Point _ => SKColors.Red,
                        LineString _ => SKColors.Blue,
                        Polygon _ => SKColors.Green,
                        _ => SKColors.Purple
                    },
                    StrokeWidth = 2,
                    IsAntialias = true
                };

                try
                {
                    var path = GeometryConverter.ToSKPath(geometry);
                    if (path != null && !path.IsEmpty)
                    {
                        canvas.DrawPath(path, paint);
                        drawnCount++;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error rendering geometry: {ex.Message}");
                }
            }

            // Reset matrix for debug text
            canvas.ResetMatrix();

            // Draw debug info
            string debugInfo = DebugInfo(validGeometries, drawnCount, width, height, envelope.Width, envelope.Height, _translate.X, _translate.Y, _scale);
            DrawDebugText(canvas, debugInfo);
            Debug.WriteLine($"Rendered {drawnCount} geometries successfully");
        }

        private double MaxCoordinate(int _position, Geometry _geom)
        {
            double max = 0;
            foreach (var vertex in _geom.Coordinates)
            {
                if (vertex != null && vertex[_position] >= max)
                {
                    max = vertex[_position];
                }
            }
            return max;
        }

        private double MinCoordinate(int _position, Geometry _geom)
        {
            double min = 0;
            foreach (var vertex in _geom.Coordinates)
            {
                if (vertex != null && vertex[_position] <= min)
                {
                    min = vertex[_position];
                }
            }
            return min;
        }

        private string DebugInfo(int _validGeometries, int _drawnCount, double _Width, double _Height, double _envelopeWidth, 
            double _envelopeHeight, float _translateX, float _translateY, float _scale)
        {
            double mapUnitsPerPixel;

            mapUnitsPerPixel = _scale * _Width / _envelopeWidth;

            string _debugInfo = $"Geometries: {_validGeometries} valid, {_drawnCount} drawn\n" +
                             $"Geometry Envelope: {_envelopeWidth:F2}x{_envelopeHeight:F2} SR units\n" +
                             $"Viewport: {_Width}x{_Height} pixels\n" +
                             $"_translate.X: {_translateX}\n" +
                             $"_translate.Y: {_translateY}\n" +
                             $"_scale: {_scale} \n" +
                             $"mapUnitsPerPixel : {mapUnitsPerPixel}";
            return _debugInfo;
        }

        private static void DrawDebugText(SKCanvas canvas, string text)
        {
            // Create a font instead of using TextSize on SKPaint
            using var font = new SKFont
            {
                Size = 14,
                Typeface = SKTypeface.Default
            };

            using var textPaint = new SKPaint
            {
                Color = SKColors.Black,
                IsAntialias = true
            };

            // Draw a semi-transparent background for the text
            using var bgPaint = new SKPaint
            {
                Color = new SKColor(255, 255, 255, 200),
                Style = SKPaintStyle.Fill
            };

            // Split text into lines
            var lines = text.Split('\n');
            float lineHeight = font.Size * 1.2f;
            float y = 10 + lineHeight;

            foreach (var line in lines)
            {
                // Get text bounds without using ref
                float textWidth = font.MeasureText(line, textPaint);
        
                // Draw background
                canvas.DrawRect(5, y - font.Size, textWidth + 10, lineHeight, bgPaint);

                // Draw text using the font
                canvas.DrawText(line, 10, y, font, textPaint);
                y += lineHeight;
            }
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var zoomFactor = e.Delta > 0 ? 1.1f : 0.9f;
            _scale *= zoomFactor;
            _viewMatrix = SKMatrix.CreateScale(_scale, _scale, _translate.X, _translate.Y);
            InvalidateVisual();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning && (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed) )
            {
                var currentMousePosition = e.GetPosition(this).ToSKPoint();
                var delta = currentMousePosition - _lastMousePosition;
                _translate += delta;
                _lastMousePosition = currentMousePosition;
                _viewMatrix = SKMatrix.CreateScaleTranslation(_scale, _scale, _translate.X, _translate.Y);
                InvalidateVisual();
            }
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isPanning = true;
                _lastMousePosition = e.GetPosition(this).ToSKPoint();
                Debug.WriteLine("Left Button Pressed");
                Debug.WriteLine(_isPanning);
            }
        }

        private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                _isPanning = true;
                _lastMousePosition = e.GetPosition(this).ToSKPoint();
                Debug.WriteLine("Right Button Pressed");
                Debug.WriteLine(_isPanning);
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {
                _isPanning = false;
                Debug.WriteLine("Left Button Released");
                Debug.WriteLine(_isPanning);
            }
        }

        private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Released)
            {
                _isPanning = false;
                Debug.WriteLine("Right Button Released");
                Debug.WriteLine(_isPanning);
            }
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            _isPanning = false;
        }

        public void UpdateGeometries(IEnumerable<Geometry> geometries)
        {
            _geometries = geometries;
            InvalidateVisual();
        }

        public static SKMatrix CalculateTransform(Envelope envelope, float controlWidth, float controlHeight)
        {
            if (controlWidth <= 0 || controlHeight <= 0)
            {
                return SKMatrix.CreateIdentity();
            }

            float scaleX = controlWidth / (float)envelope.Width;
            float scaleY = controlHeight / (float)envelope.Height;
            Debug.WriteLine($"scaleX:{scaleX}");
            Debug.WriteLine($"scaleX:{scaleY}");
            float scale = Math.Min(scaleX, scaleY);

            float offsetX = (float)-envelope.MinX;
            float offsetY = (float)-envelope.MinY;
            Debug.WriteLine($"envelope.MinX: {envelope.MinX}");
            Debug.WriteLine($"envelope.MinY: {envelope.MinY}");
            Debug.WriteLine($"offsetx: {offsetX}");
            Debug.WriteLine($"offsety: {offsetY}");

            return SKMatrix.CreateScale(scale, scale)
                   .PostConcat(SKMatrix.CreateTranslation(offsetX * scale, offsetY * scale));
        }
    }

    public static class Extensions
    {
        public static SKPoint ToSKPoint(this System.Windows.Point point)
        {
            return new SKPoint((float)point.X, (float)point.Y);
        }
    }
}
