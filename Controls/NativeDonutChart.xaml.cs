using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Enjaz.Models;

namespace Enjaz.Controls
{
    public partial class NativeDonutChart : UserControl
    {
        public static readonly DependencyProperty SlicesProperty =
            DependencyProperty.Register("Slices", typeof(List<DonutSlice>), typeof(NativeDonutChart),
                new PropertyMetadata(null, OnSlicesChanged));

        public static readonly DependencyProperty CenterValueProperty =
            DependencyProperty.Register("CenterValue", typeof(string), typeof(NativeDonutChart),
                new PropertyMetadata("", OnCenterChanged));

        public static readonly DependencyProperty CenterLabelProperty =
            DependencyProperty.Register("CenterLabel", typeof(string), typeof(NativeDonutChart),
                new PropertyMetadata("", OnCenterChanged));

        public List<DonutSlice> Slices
        {
            get => (List<DonutSlice>)GetValue(SlicesProperty);
            set => SetValue(SlicesProperty, value);
        }

        public string CenterValue
        {
            get => (string)GetValue(CenterValueProperty);
            set => SetValue(CenterValueProperty, value);
        }

        public string CenterLabel
        {
            get => (string)GetValue(CenterLabelProperty);
            set => SetValue(CenterLabelProperty, value);
        }

        public NativeDonutChart()
        {
            InitializeComponent();
        }

        private static void OnSlicesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NativeDonutChart chart) chart.DrawDonut();
        }

        private static void OnCenterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NativeDonutChart chart)
            {
                chart.CenterValueText.Text = chart.CenterValue;
                chart.CenterLabelText.Text = chart.CenterLabel;
            }
        }

        private void DrawDonut()
        {
            DonutCanvas.Children.Clear();
            if (Slices == null || Slices.Count == 0) return;

            double centerX = 80, centerY = 80;
            double outerRadius = 72, innerRadius = 47;
            double total = 0;
            foreach (var s in Slices) total += s.Value;
            if (total == 0) return;

            double startAngle = -90; // Start from top
            double gapDegrees = 2;   // Gap between slices

            for (int i = 0; i < Slices.Count; i++)
            {
                var slice = Slices[i];
                double sweepAngle = (slice.Value / total) * 360.0;

                // Apply gap
                double actualStart = startAngle + (gapDegrees / 2);
                double actualSweep = sweepAngle - gapDegrees;
                if (actualSweep < 0.5) actualSweep = 0.5;

                var path = CreateArcPath(centerX, centerY, outerRadius, innerRadius, actualStart, actualSweep, slice.Fill);
                
                // Add hover tooltip - professional styled
                var tooltipBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(230, 15, 23, 42)), // Slate-900
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 8, 12, 8),
                    Child = new TextBlock
                    {
                        Text = $"{slice.Label}: {slice.Value}",
                        FontFamily = new FontFamily("Tahoma"),
                        FontSize = 14,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = Brushes.White,
                        FlowDirection = FlowDirection.RightToLeft
                    }
                };
                var tooltip = new ToolTip
                {
                    Content = tooltipBorder,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(0),
                    HasDropShadow = true
                };
                ToolTipService.SetToolTip(path, tooltip);
                ToolTipService.SetInitialShowDelay(path, 100);
                ToolTipService.SetBetweenShowDelay(path, 0);
                
                // Add hover animation
                path.MouseEnter += (s, e) =>
                {
                    path.RenderTransform = new ScaleTransform(1.05, 1.05, centerX, centerY);
                    path.Opacity = 0.9;
                };
                path.MouseLeave += (s, e) =>
                {
                    path.RenderTransform = null;
                    path.Opacity = 1.0;
                };

                DonutCanvas.Children.Add(path);
                startAngle += sweepAngle;
            }

            // Update center text
            CenterValueText.Text = CenterValue;
            CenterLabelText.Text = CenterLabel;
        }

        private Path CreateArcPath(double cx, double cy, double outerR, double innerR, double startDeg, double sweepDeg, Brush fill)
        {
            double startRad = startDeg * Math.PI / 180.0;
            double endRad = (startDeg + sweepDeg) * Math.PI / 180.0;

            bool isLargeArc = sweepDeg > 180;

            // Outer arc points
            Point outerStart = new Point(cx + outerR * Math.Cos(startRad), cy + outerR * Math.Sin(startRad));
            Point outerEnd = new Point(cx + outerR * Math.Cos(endRad), cy + outerR * Math.Sin(endRad));

            // Inner arc points (reversed)
            Point innerStart = new Point(cx + innerR * Math.Cos(endRad), cy + innerR * Math.Sin(endRad));
            Point innerEnd = new Point(cx + innerR * Math.Cos(startRad), cy + innerR * Math.Sin(startRad));

            var figure = new PathFigure { StartPoint = outerStart, IsClosed = true };

            // Outer arc
            figure.Segments.Add(new ArcSegment
            {
                Point = outerEnd,
                Size = new Size(outerR, outerR),
                IsLargeArc = isLargeArc,
                SweepDirection = SweepDirection.Clockwise
            });

            // Line to inner arc
            figure.Segments.Add(new LineSegment { Point = innerStart });

            // Inner arc (counter-clockwise)
            figure.Segments.Add(new ArcSegment
            {
                Point = innerEnd,
                Size = new Size(innerR, innerR),
                IsLargeArc = isLargeArc,
                SweepDirection = SweepDirection.Counterclockwise
            });

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);

            return new Path
            {
                Data = geometry,
                Fill = fill,
                Cursor = System.Windows.Input.Cursors.Hand
            };
        }
    }
}
