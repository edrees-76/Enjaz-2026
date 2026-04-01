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

            // Chart dimensions - High resolution for print
            // 96 DPI * 3.125 = 300 DPI
            const double dpi = 300d;
            const double scale = dpi / 96d;
            
            const int baseWidth = 800;
            const int baseHeight = 500;
            
            int renderWidth = (int)(baseWidth * scale);
            int renderHeight = (int)((baseHeight + 50) * scale);

            var chartForeground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1B4F72"));
            var axisForeground = Brushes.Black;

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
                            Width = baseWidth,
                            Height = baseHeight,
                            Series = pieWidget.Series,
                            LegendLocation = LegendLocation.Right,
                            DisableAnimations = true,
                            Hoverable = false,
                            DataTooltip = null,
                            Background = Brushes.White,
                            InnerRadius = 50,
                            Foreground = axisForeground
                        };
                        
                        pieChart.ChartLegend = new DefaultLegend 
                        { 
                            Foreground = axisForeground,
                            FontSize = 14,
                            FontWeight = FontWeights.Medium
                        };

                        chartElement = pieChart;
                    }
                    else if (widget is ColumnWidgetViewModel columnWidget)
                    {
                        var columnChart = new CartesianChart
                        {
                            Width = baseWidth,
                            Height = baseHeight,
                            Series = columnWidget.Series,
                            DisableAnimations = true,
                            Hoverable = false,
                            DataTooltip = null,
                            Background = Brushes.White,
                            AxisX = new AxesCollection { 
                                new Axis { 
                                    Labels = columnWidget.Labels, 
                                    LabelsRotation = 15,
                                    Foreground = axisForeground,
                                    FontSize = 12,
                                    Separator = new LiveCharts.Wpf.Separator { StrokeThickness = 0.5, Stroke = Brushes.LightGray }
                                } 
                            },
                            AxisY = new AxesCollection { 
                                new Axis { 
                                    LabelFormatter = columnWidget.Formatter,
                                    Foreground = axisForeground,
                                    FontSize = 12,
                                    Separator = new LiveCharts.Wpf.Separator { StrokeThickness = 0.5, Stroke = Brushes.LightGray }
                                } 
                            },
                            LegendLocation = LegendLocation.Bottom
                        };
                        
                        columnChart.ChartLegend = new DefaultLegend { Foreground = axisForeground, FontSize = 14 };
                        chartElement = columnChart;
                    }
                    else if (widget is RowWidgetViewModel rowWidget)
                    {
                        var rowChart = new CartesianChart
                        {
                            Width = baseWidth,
                            Height = baseHeight,
                            Series = rowWidget.Series,
                            DisableAnimations = true,
                            Hoverable = false,
                            DataTooltip = null,
                            Background = Brushes.White,
                            AxisX = new AxesCollection { 
                                new Axis { 
                                    LabelFormatter = rowWidget.Formatter,
                                    Foreground = axisForeground,
                                    FontSize = 12,
                                    Separator = new LiveCharts.Wpf.Separator { StrokeThickness = 0.5, Stroke = Brushes.LightGray }
                                } 
                            },
                            AxisY = new AxesCollection { 
                                new Axis { 
                                    Labels = rowWidget.Labels,
                                    Foreground = axisForeground,
                                    FontSize = 12,
                                    Separator = new LiveCharts.Wpf.Separator { StrokeThickness = 0, Stroke = Brushes.Transparent }
                                } 
                            },
                            LegendLocation = LegendLocation.Bottom
                        };
                        
                        rowChart.ChartLegend = new DefaultLegend { Foreground = axisForeground, FontSize = 14 };
                        chartElement = rowChart;
                    }
                    else
                    {
                        continue;
                    }

                    // Add title above chart
                    var container = new StackPanel
                    {
                        Width = baseWidth,
                        Height = baseHeight + 50,
                        Background = Brushes.White,
                        Orientation = Orientation.Vertical
                    };
                    
                    var title = new TextBlock
                    {
                        Text = widget.Title,
                        FontSize = 20,
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 10, 0, 10),
                        Foreground = chartForeground
                    };
                    
                    container.Children.Add(title);
                    container.Children.Add(chartElement);

                    // Measure and arrange at the BASE logical size (96 DPI scale)
                    // The RenderTargetBitmap(..., 300, 300) will handle the 3.125x scaling during Render
                    var baseSize = new Size(baseWidth, baseHeight + 50);
                    container.Measure(baseSize);
                    container.Arrange(new Rect(baseSize));
                    container.UpdateLayout();

                    // Force chart update before final render
                    if (chartElement is PieChart pc) pc.Update(true, true);
                    else if (chartElement is CartesianChart cc) cc.Update(true, true);

                    container.UpdateLayout();

                    // Render directly to bitmap (using high DPI for pixels and metadata)
                    var renderBitmap = new RenderTargetBitmap(renderWidth, renderHeight, dpi, dpi, PixelFormats.Pbgra32);
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
