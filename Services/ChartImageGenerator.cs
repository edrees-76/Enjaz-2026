using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Enjaz.Controls;
using Enjaz.ViewModels;

namespace Enjaz.Services
{
    /// <summary>
    /// Generates chart images from native WPF controls for PDF/Excel export.
    /// </summary>
    public class ChartImageGenerator
    {
        public Dictionary<string, byte[]> GenerateChartImages(IEnumerable<DashboardWidgetViewModel> widgets)
        {
            var images = new Dictionary<string, byte[]>();

            const int baseWidth = 800;
            const int baseHeight = 550;

            foreach (var widget in widgets)
            {
                try
                {
                    byte[]? imageBytes = null;

                    if (widget is PieWidgetViewModel pieWidget && pieWidget.NativeSlices?.Count > 0)
                    {
                        var chart = new NativeDonutChart
                        {
                            Width = baseWidth,
                            Height = baseHeight - 50,
                            Slices = pieWidget.NativeSlices
                        };
                        imageBytes = RenderControlToImage(chart, baseWidth, baseHeight, widget.Title);
                    }
                    else if (widget is ColumnWidgetViewModel columnWidget && columnWidget.NativeBars?.Count > 0)
                    {
                        var chart = new NativeSingleBarChart
                        {
                            Width = baseWidth,
                            Height = baseHeight - 50,
                            ItemsSource = columnWidget.NativeBars,
                            MaxValue = columnWidget.NativeMaxValue
                        };
                        imageBytes = RenderControlToImage(chart, baseWidth, baseHeight, widget.Title);
                    }
                    else if (widget is RowWidgetViewModel rowWidget && rowWidget.NativeBars?.Count > 0)
                    {
                        var chart = new NativeHorizontalBarChart
                        {
                            Width = baseWidth,
                            Height = baseHeight - 50,
                            ItemsSource = rowWidget.NativeBars
                        };
                        imageBytes = RenderControlToImage(chart, baseWidth, baseHeight, widget.Title);
                    }
                    else if (widget is LineWidgetViewModel lineWidget && lineWidget.NativePoints?.Count > 0)
                    {
                        var chart = new NativeLineChart
                        {
                            Width = baseWidth,
                            Height = baseHeight - 50,
                            ItemsSource = lineWidget.NativePoints
                        };
                        imageBytes = RenderControlToImage(chart, baseWidth, baseHeight, widget.Title);
                    }

                    if (imageBytes != null)
                    {
                        images.Add(widget.Title, imageBytes);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error generating image for {widget.Title}: {ex.Message}");
                }
            }

            return images;
        }

        /// <summary>
        /// Renders a WPF control to a PNG image byte array.
        /// </summary>
        private byte[]? RenderControlToImage(FrameworkElement control, int width, int height, string title)
        {
            try
            {
                // Create a container with title and chart
                var container = new StackPanel
                {
                    Background = Brushes.White,
                    Width = width,
                    Height = height
                };

                // Title
                var titleBlock = new TextBlock
                {
                    Text = title,
                    FontFamily = new FontFamily("Tahoma"),
                    FontSize = 22,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(25, 25, 112)),
                    Margin = new Thickness(20, 15, 20, 10),
                    FlowDirection = FlowDirection.RightToLeft,
                    TextAlignment = TextAlignment.Center
                };

                container.Children.Add(titleBlock);
                container.Children.Add(control);

                // Measure and arrange
                container.Measure(new Size(width, height));
                container.Arrange(new Rect(0, 0, width, height));
                container.UpdateLayout();

                // Render to bitmap
                var renderBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                renderBitmap.Render(container);

                // Encode to PNG
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                using var ms = new MemoryStream();
                encoder.Save(ms);
                return ms.ToArray();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RenderControlToImage error: {ex.Message}");
                return null;
            }
        }
    }
}
