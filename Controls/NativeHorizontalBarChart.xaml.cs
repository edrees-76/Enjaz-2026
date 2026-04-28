using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Enjaz.Models;

namespace Enjaz.Controls
{
    public partial class NativeHorizontalBarChart : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(List<HorizontalBarItem>), typeof(NativeHorizontalBarChart),
                new PropertyMetadata(null, OnItemsSourceChanged));

        public List<HorizontalBarItem> ItemsSource
        {
            get => (List<HorizontalBarItem>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public NativeHorizontalBarChart()
        {
            InitializeComponent();
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NativeHorizontalBarChart chart)
            {
                chart.BarsItemsControl.ItemsSource = chart.ItemsSource;
            }
        }
    }

    /// <summary>
    /// Converts a percentage (0-100) and container width to actual pixel width.
    /// </summary>
    public class WidthPercentageConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is double percentage && values[1] is double containerWidth)
            {
                return Math.Max((percentage / 100.0) * containerWidth, 4);
            }
            return 4.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
