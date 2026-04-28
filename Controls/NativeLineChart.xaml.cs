using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Enjaz.Models;

namespace Enjaz.Controls
{
    public partial class NativeLineChart : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(List<LineChartPoint>), typeof(NativeLineChart),
                new PropertyMetadata(null, OnItemsSourceChanged));

        public static readonly DependencyProperty LineColorProperty =
            DependencyProperty.Register("LineColor", typeof(Brush), typeof(NativeLineChart),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(244, 162, 97))));

        public List<LineChartPoint> ItemsSource
        {
            get => (List<LineChartPoint>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public Brush LineColor
        {
            get => (Brush)GetValue(LineColorProperty);
            set => SetValue(LineColorProperty, value);
        }

        public NativeLineChart()
        {
            InitializeComponent();
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NativeLineChart chart)
            {
                chart.LabelsControl.ItemsSource = chart.ItemsSource;
                chart.DrawChart();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e) => DrawChart();
        private void OnSizeChanged(object sender, SizeChangedEventArgs e) => DrawChart();

        private void DrawChart()
        {
            ChartCanvas.Children.Clear();
            if (ItemsSource == null || ItemsSource.Count == 0) return;

            var width = ChartCanvas.ActualWidth;
            var height = ChartCanvas.ActualHeight;
            if (width <= 0 || height <= 0) return;

            var items = ItemsSource;
            int count = items.Count;
            int maxVal = items.Max(i => i.Value);
            if (maxVal == 0) maxVal = 1;

            double padding = 20;
            double chartWidth = width - padding * 2;
            double chartHeight = height - padding * 2;

            // Draw horizontal grid lines
            for (int i = 0; i <= 4; i++)
            {
                double y = padding + (chartHeight / 4.0) * i;
                var gridLine = new Line
                {
                    X1 = padding, Y1 = y,
                    X2 = width - padding, Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromArgb(40, 128, 128, 128)),
                    StrokeThickness = 1
                };
                ChartCanvas.Children.Add(gridLine);

                // Y-axis labels
                int val = maxVal - (int)(maxVal * i / 4.0);
                var label = new TextBlock
                {
                    Text = val.ToString("N0"),
                    FontFamily = new FontFamily("Tahoma"),
                    FontSize = 10,
                    Foreground = (Brush)FindResource("SecondaryTextBrush")
                };
                Canvas.SetLeft(label, 0);
                Canvas.SetTop(label, y - 8);
                ChartCanvas.Children.Add(label);
            }

            // Calculate points
            var points = new List<Point>();
            for (int i = 0; i < count; i++)
            {
                double x = padding + (chartWidth / Math.Max(count - 1, 1)) * i;
                double y = padding + chartHeight - (chartHeight * items[i].Value / maxVal);
                points.Add(new Point(x, y));
            }

            // Draw gradient fill under line
            if (points.Count > 1)
            {
                var fillPoints = new PointCollection(points);
                fillPoints.Add(new Point(points.Last().X, padding + chartHeight));
                fillPoints.Add(new Point(points.First().X, padding + chartHeight));

                var fillColor = (LineColor as SolidColorBrush)?.Color ?? Color.FromRgb(244, 162, 97);
                var gradientBrush = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb(60, fillColor.R, fillColor.G, fillColor.B), 0),
                        new GradientStop(Color.FromArgb(5, fillColor.R, fillColor.G, fillColor.B), 1)
                    }
                };

                var fillPolygon = new Polygon
                {
                    Points = fillPoints,
                    Fill = gradientBrush
                };
                ChartCanvas.Children.Add(fillPolygon);
            }

            // Draw line
            if (points.Count > 1)
            {
                var polyline = new Polyline
                {
                    Points = new PointCollection(points),
                    Stroke = LineColor,
                    StrokeThickness = 2.5,
                    StrokeLineJoin = PenLineJoin.Round
                };
                ChartCanvas.Children.Add(polyline);
            }

            // Draw data points with tooltips
            for (int i = 0; i < points.Count; i++)
            {
                var item = items[i];
                var dot = new Ellipse
                {
                    Width = 10, Height = 10,
                    Fill = LineColor,
                    Stroke = Brushes.White,
                    StrokeThickness = 2,
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                Canvas.SetLeft(dot, points[i].X - 5);
                Canvas.SetTop(dot, points[i].Y - 5);

                // Tooltip
                var tooltip = new ToolTip
                {
                    HasDropShadow = true,
                    Content = new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(230, 15, 23, 42)),
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(12, 8, 12, 8),
                        Child = new StackPanel
                        {
                            Children =
                            {
                                new TextBlock
                                {
                                    Text = item.Label,
                                    FontFamily = new FontFamily("Tahoma"),
                                    FontSize = 13, FontWeight = FontWeights.Bold,
                                    Foreground = Brushes.White,
                                    FlowDirection = FlowDirection.RightToLeft,
                                    Margin = new Thickness(0, 0, 0, 4)
                                },
                                new TextBlock
                                {
                                    FontFamily = new FontFamily("Tahoma"),
                                    FontSize = 14,
                                    Foreground = new SolidColorBrush(Color.FromRgb(244, 162, 97)),
                                    FlowDirection = FlowDirection.RightToLeft,
                                    Text = $"القيمة: {item.Value}"
                                }
                            }
                        }
                    }
                };
                ToolTipService.SetToolTip(dot, tooltip);
                ToolTipService.SetInitialShowDelay(dot, 100);
                ToolTipService.SetBetweenShowDelay(dot, 0);

                // Hover animation
                var originalSize = 10.0;
                dot.MouseEnter += (s, e) => { dot.Width = 14; dot.Height = 14; Canvas.SetLeft(dot, points[i].X - 7); Canvas.SetTop(dot, points[i].Y - 7); };
                var idx = i; // capture
                dot.MouseLeave += (s, e) => { dot.Width = originalSize; dot.Height = originalSize; Canvas.SetLeft(dot, points[idx].X - 5); Canvas.SetTop(dot, points[idx].Y - 5); };

                // Value label above point
                var valueLabel = new TextBlock
                {
                    Text = item.Value.ToString(),
                    FontFamily = new FontFamily("Tahoma"),
                    FontSize = 10, FontWeight = FontWeights.Bold,
                    Foreground = (Brush)FindResource("PrimaryTextBrush"),
                    TextAlignment = TextAlignment.Center
                };
                valueLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(valueLabel, points[i].X - valueLabel.DesiredSize.Width / 2);
                Canvas.SetTop(valueLabel, points[i].Y - 20);

                ChartCanvas.Children.Add(dot);
                ChartCanvas.Children.Add(valueLabel);
            }
        }
    }
}
