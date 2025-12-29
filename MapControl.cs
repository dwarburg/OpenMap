using System;
using System.Windows.Input;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System.Windows.Controls;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace OpenMap
{
    public class MapControl : SKElement
    {
        private SKMatrix _viewMatrix = SKMatrix.CreateIdentity(); // Pan/zoom matrix
        private float _scale = 1.0f; // Zoom level
        private SKPoint _translate = new SKPoint(0, 0); // Pan offset
        private SKPath _geometryPath = new SKPath(); // Geometry to render

        public MapControl()
        {
            // Attach event handlers for mouse interactions
            this.MouseWheel += OnMouseWheel;
            this.MouseMove += OnMouseMove;
            this.MouseDown += OnMouseDown;
        }

        // Mouse interaction state
        private bool _isPanning = false;
        private SKPoint _lastMousePosition;

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            base.OnPaintSurface(e);

            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.White);

            // Apply the pan/zoom matrix
            canvas.SetMatrix(_viewMatrix);

            // Draw the geometry
            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Blue,
                StrokeWidth = 2
            };
            canvas.DrawPath(_geometryPath, paint);
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Zoom in/out based on mouse wheel
            var zoomFactor = e.Delta > 0 ? 1.1f : 0.9f;
            _scale *= zoomFactor;

            // Update the view matrix
            _viewMatrix = SKMatrix.CreateScale(_scale, _scale, _translate.X, _translate.Y);
            InvalidateVisual(); // Redraw
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentMousePosition = e.GetPosition(this).ToSKPoint();
                var delta = currentMousePosition - _lastMousePosition;

                // Update translation
                _translate += delta;
                _lastMousePosition = currentMousePosition;

                // Update the view matrix
                _viewMatrix = SKMatrix.CreateTranslation(_translate.X, _translate.Y);
                InvalidateVisual(); // Redraw
            }
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isPanning = true;
                _lastMousePosition = e.GetPosition(this).ToSKPoint();
            }
        }

        public void UpdateGeometries(IEnumerable<Geometry> geometries)
        {
            _geometryPath.Reset(); // Clear existing paths
            foreach (var geometry in geometries)
            {
                var path = GeometryConverter.ToSKPath(geometry);
                _geometryPath.AddPath(path);
            }
            InvalidateVisual(); // Trigger redraw
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
