using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Enjaz.ViewModels;
using LiveCharts;
using LiveCharts.Wpf;

namespace Enjaz.Services
{
    public class ChartImageGenerator
    {
        public Dictionary<string, byte[]> GenerateChartImages(IEnumerable<DashboardWidgetViewModel> widgets)
        {
            var images = new Dictionary<string, byte[]>();
            
            // Ensure we are on UI thread
            if (!System.Windows.Application.Current.Dispatcher.CheckAccess())
            {
                return System.Windows.Application.Current.Dispatcher.Invoke(() => GenerateChartImages(widgets));
            }

            // Chart dimensions - use consistent size for PDF
            // A modest size (e.g. 800x500) allows good resolution without excessive height
            const int chartWidth = 800;
            const int chartHeight = 500;

            foreach (var widget in widgets)
            {
                try
                {
                    FrameworkElement chartElement;

                    // Create appropriate chart based on widget type
                    if (widget is PieWidgetViewModel pieWidget)
                    {
                        var pieChart = new PieChart
                        {
                            Width = chartWidth,
                            Height = chartHeight,
                            Series = pieWidget.Series,
                            LegendLocation = LegendLocation.Right,
                            DisableAnimations = true,
                            Hoverable = false,
                            DataTooltip = null,
                            Background = Brushes.White,
                            InnerRadius = 50
                        };
                        chartElement = pieChart;
                    }
                    else if (widget is ColumnWidgetViewModel columnWidget)
                    {
                        var columnChart = new CartesianChart
                        {
                            Width = chartWidth,
                            Height = chartHeight,
                            Series = columnWidget.Series,
                            DisableAnimations = true,
                            Hoverable = false,
                            DataTooltip = null,
                            Background = Brushes.White,
                            AxisX = new AxesCollection { new Axis { Labels = columnWidget.Labels, LabelsRotation = 15 } },
                            AxisY = new AxesCollection { new Axis { LabelFormatter = columnWidget.Formatter } }
                        };
                        chartElement = columnChart;
                    }
                    else if (widget is RowWidgetViewModel rowWidget)
                    {
                        var rowChart = new CartesianChart
                        {
                            Width = chartWidth,
                            Height = chartHeight,
                            Series = rowWidget.Series,
                            DisableAnimations = true,
                            Hoverable = false,
                            DataTooltip = null,
                            Background = Brushes.White,
                            AxisX = new AxesCollection { new Axis { LabelFormatter = rowWidget.Formatter } },
                            AxisY = new AxesCollection { new Axis { Labels = rowWidget.Labels } }
                        };
                        chartElement = rowChart;
                    }
                    else
                    {
                        continue; // Skip unknown widget types
                    }

                    // Add title above chart
                    var container = new StackPanel
                    {
                        Width = chartWidth,
                        Height = chartHeight + 30,
                        Background = Brushes.White,
                        Orientation = Orientation.Vertical
                    };
                    
                    var title = new TextBlock
                    {
                        Text = widget.Title,
                        FontSize = 14,
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 5, 0, 5),
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1B4F72"))
                    };
                    
                    container.Children.Add(title);
                    container.Children.Add(chartElement);

                    // Measure and arrange
                    var size = new Size(chartWidth, chartHeight + 30);
                    container.Measure(size);
                    container.Arrange(new Rect(size));
                    container.UpdateLayout();

                    // Force chart update
                    if (chartElement is PieChart pc)
                        pc.Update(true, true);
                    else if (chartElement is CartesianChart cc)
                        cc.Update(true, true);

                    container.UpdateLayout();

                    // Render to bitmap
                    var renderBitmap = new RenderTargetBitmap(
                        chartWidth, 
                        chartHeight + 30, 
                        96d, 
                        96d, 
                        PixelFormats.Pbgra32);
                    
                    renderBitmap.Render(container);

                    // Encode to PNG
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                    using (var stream = new MemoryStream())
                    {
                        encoder.Save(stream);
                        images.Add(widget.Title, stream.ToArray());
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error generating image for {widget.Title}: {ex.Message}");
                }
            }

            return images;
        }

        private FrameworkElement? FindChart(DependencyObject parent)
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is PieChart || child is CartesianChart)
                {
                    return child as FrameworkElement;
                }
                
                var result = FindChart(child);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }
    }
}
