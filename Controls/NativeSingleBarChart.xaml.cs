using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Enjaz.Models;

namespace Enjaz.Controls
{
    public partial class NativeSingleBarChart : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(List<SingleBarItem>), typeof(NativeSingleBarChart),
                new PropertyMetadata(null, OnItemsSourceChanged));

        public static readonly DependencyProperty MaxValueProperty =
            DependencyProperty.Register("MaxValue", typeof(int), typeof(NativeSingleBarChart),
                new PropertyMetadata(0, OnMaxValueChanged));

        public List<SingleBarItem> ItemsSource
        {
            get => (List<SingleBarItem>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public int MaxValue
        {
            get => (int)GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }

        public NativeSingleBarChart()
        {
            InitializeComponent();
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NativeSingleBarChart chart)
            {
                chart.BarsItemsControl.ItemsSource = chart.ItemsSource;
                chart.LabelsItemsControl.ItemsSource = chart.ItemsSource;
            }
        }

        private static void OnMaxValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NativeSingleBarChart chart)
            {
                chart.MaxValueLabel.Text = chart.MaxValue.ToString("N0");
            }
        }
    }

    /// <summary>
    /// Converts height percentage and container height to actual pixel height for single bar charts.
    /// </summary>
    public class SingleBarHeightConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is double percentage && values[1] is double containerHeight)
            {
                return Math.Max((percentage / 100.0) * containerHeight * 0.85, 4);
            }
            return 4.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
