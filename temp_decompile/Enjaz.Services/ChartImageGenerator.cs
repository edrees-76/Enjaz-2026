#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Enjaz.ViewModels;
using LiveCharts;
using LiveCharts.Wpf;

namespace Enjaz.Services;

public class ChartImageGenerator
{
	public Dictionary<string, byte[]> GenerateChartImages(IEnumerable<DashboardWidgetViewModel> widgets)
	{
		//IL_039a: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a6: Unknown result type (might be due to invalid IL or missing references)
		Dictionary<string, byte[]> dictionary = new Dictionary<string, byte[]>();
		if (!((DispatcherObject)Application.Current).Dispatcher.CheckAccess())
		{
			return ((DispatcherObject)Application.Current).Dispatcher.Invoke<Dictionary<string, byte[]>>((Func<Dictionary<string, byte[]>>)(() => GenerateChartImages(widgets)));
		}
		Size val = default(Size);
		foreach (DashboardWidgetViewModel widget in widgets)
		{
			try
			{
				FrameworkElement frameworkElement;
				if (widget is PieWidgetViewModel pieWidgetViewModel)
				{
					PieChart pieChart = new PieChart
					{
						Width = 800.0,
						Height = 500.0,
						Series = pieWidgetViewModel.Series,
						LegendLocation = LegendLocation.Right,
						DisableAnimations = true,
						Hoverable = false,
						DataTooltip = null,
						Background = Brushes.White,
						InnerRadius = 50.0
					};
					frameworkElement = pieChart;
					goto IL_02a2;
				}
				if (widget is ColumnWidgetViewModel columnWidgetViewModel)
				{
					CartesianChart cartesianChart = new CartesianChart
					{
						Width = 800.0,
						Height = 500.0,
						Series = columnWidgetViewModel.Series,
						DisableAnimations = true,
						Hoverable = false,
						DataTooltip = null,
						Background = Brushes.White,
						AxisX = new AxesCollection
						{
							new Axis
							{
								Labels = columnWidgetViewModel.Labels,
								LabelsRotation = 15.0
							}
						},
						AxisY = new AxesCollection
						{
							new Axis
							{
								LabelFormatter = columnWidgetViewModel.Formatter
							}
						}
					};
					frameworkElement = cartesianChart;
					goto IL_02a2;
				}
				if (widget is RowWidgetViewModel rowWidgetViewModel)
				{
					CartesianChart cartesianChart2 = new CartesianChart
					{
						Width = 800.0,
						Height = 500.0,
						Series = rowWidgetViewModel.Series,
						DisableAnimations = true,
						Hoverable = false,
						DataTooltip = null,
						Background = Brushes.White,
						AxisX = new AxesCollection
						{
							new Axis
							{
								LabelFormatter = rowWidgetViewModel.Formatter
							}
						},
						AxisY = new AxesCollection
						{
							new Axis
							{
								Labels = rowWidgetViewModel.Labels
							}
						}
					};
					frameworkElement = cartesianChart2;
					goto IL_02a2;
				}
				goto end_IL_0070;
				IL_02a2:
				StackPanel stackPanel = new StackPanel
				{
					Width = 800.0,
					Height = 530.0,
					Background = Brushes.White,
					Orientation = Orientation.Vertical
				};
				TextBlock element = new TextBlock
				{
					Text = widget.Title,
					FontSize = 14.0,
					FontWeight = FontWeights.Bold,
					HorizontalAlignment = HorizontalAlignment.Center,
					Margin = new Thickness(0.0, 5.0, 0.0, 5.0),
					Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1B4F72"))
				};
				stackPanel.Children.Add(element);
				stackPanel.Children.Add(frameworkElement);
				((Size)(ref val))._002Ector(800.0, 530.0);
				stackPanel.Measure(val);
				stackPanel.Arrange(new Rect(val));
				stackPanel.UpdateLayout();
				if (frameworkElement is PieChart pieChart2)
				{
					pieChart2.Update(restartView: true, force: true);
				}
				else if (frameworkElement is CartesianChart cartesianChart3)
				{
					cartesianChart3.Update(restartView: true, force: true);
				}
				stackPanel.UpdateLayout();
				RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(800, 530, 96.0, 96.0, PixelFormats.Pbgra32);
				renderTargetBitmap.Render(stackPanel);
				PngBitmapEncoder pngBitmapEncoder = new PngBitmapEncoder();
				pngBitmapEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
				using (MemoryStream memoryStream = new MemoryStream())
				{
					pngBitmapEncoder.Save(memoryStream);
					dictionary.Add(widget.Title, memoryStream.ToArray());
				}
				end_IL_0070:;
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Error generating image for " + widget.Title + ": " + ex.Message);
			}
		}
		return dictionary;
	}

	private FrameworkElement? FindChart(DependencyObject parent)
	{
		if (parent == null)
		{
			return null;
		}
		for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
		{
			DependencyObject child = VisualTreeHelper.GetChild(parent, i);
			if (child is PieChart || child is CartesianChart)
			{
				return child as FrameworkElement;
			}
			FrameworkElement frameworkElement = FindChart(child);
			if (frameworkElement != null)
			{
				return frameworkElement;
			}
		}
		return null;
	}
}
