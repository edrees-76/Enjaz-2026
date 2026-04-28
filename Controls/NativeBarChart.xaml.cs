using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Enjaz.Models;

namespace Enjaz.Controls
{
    public partial class NativeBarChart : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(List<MonthlyBarItem>), typeof(NativeBarChart),
                new PropertyMetadata(null, OnItemsSourceChanged));

        public static readonly DependencyProperty MaxValueProperty =
            DependencyProperty.Register("MaxValue", typeof(int), typeof(NativeBarChart),
                new PropertyMetadata(0, OnMaxValueChanged));

        public List<MonthlyBarItem> ItemsSource
        {
            get => (List<MonthlyBarItem>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public int MaxValue
        {
            get => (int)GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }

        public NativeBarChart()
        {
            Resources.Add("PercentageToHeightConverter", new PercentageToHeightConverter());
            InitializeComponent();
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NativeBarChart chart)
            {
                chart.BarsItemsControl.ItemsSource = chart.ItemsSource;
                chart.LabelsItemsControl.ItemsSource = chart.ItemsSource;
                chart.ApplyTooltips();
            }
        }

        private static void OnMaxValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NativeBarChart chart)
            {
                chart.MaxValueLabel.Text = chart.MaxValue.ToString("N0");
            }
        }

        private void ApplyTooltips()
        {
            // Tooltips are applied after ItemsControl renders
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (ItemsSource == null) return;
                // Force layout update
                BarsItemsControl.UpdateLayout();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }

    public class PercentageToHeightConverter : IMultiValueConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double percentage)
            {
                return Math.Max(percentage * 2.0, 0); // Fallback simple scaling
            }
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is double percentage && values[1] is double containerHeight)
            {
                return Math.Max((percentage / 100.0) * containerHeight, 0);
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    /// <summary>
    /// Converter to create styled tooltips for bar chart items
    /// </summary>
    public class BarTooltipConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is string label && values[1] is int val)
            {
                var border = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(230, 15, 23, 42)),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 8, 12, 8),
                    Child = new TextBlock
                    {
                        Text = $"{label}: {val}",
                        FontFamily = new FontFamily("Tahoma"),
                        FontSize = 14,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = Brushes.White,
                        FlowDirection = FlowDirection.RightToLeft
                    }
                };
                return border;
            }
            return DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
