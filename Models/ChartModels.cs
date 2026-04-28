using System.Windows.Media;

namespace Enjaz.Models
{
    /// <summary>
    /// Represents a single bar in a monthly bar chart.
    /// </summary>
    public class MonthlyBarItem
    {
        public string Month { get; set; } = "";
        public int Value1 { get; set; }
        public int Value2 { get; set; }
        public string Label1 { get; set; } = "";
        public string Label2 { get; set; } = "";
        public Brush Color1 { get; set; } = Brushes.Green;
        public Brush Color2 { get; set; } = Brushes.Gray;
        public int MaxValue { get; set; } = 1;

        // Calculated heights (percentage of max)
        public double Height1 => MaxValue > 0 ? (double)Value1 / MaxValue * 100.0 : 0;
        public double Height2 => MaxValue > 0 ? (double)Value2 / MaxValue * 100.0 : 0;
    }

    /// <summary>
    /// Represents a slice in a donut/pie chart.
    /// </summary>
    public class DonutSlice
    {
        public string Label { get; set; } = "";
        public int Value { get; set; }
        public double Percentage { get; set; }
        public Brush Fill { get; set; } = Brushes.Green;
        public string DisplayText => $"{Value}";
        public string PercentageText => $"%{Percentage:F0}";
    }

    /// <summary>
    /// Represents a single horizontal bar item for row/horizontal bar charts.
    /// </summary>
    public class HorizontalBarItem
    {
        public string Label { get; set; } = "";
        public int Value { get; set; }
        public int MaxValue { get; set; } = 1;
        public Brush Fill { get; set; } = Brushes.Teal;
        public double WidthPercentage => MaxValue > 0 ? (double)Value / MaxValue * 100.0 : 0;
    }

    /// <summary>
    /// Represents a single data point in a line chart.
    /// </summary>
    public class LineChartPoint
    {
        public string Label { get; set; } = "";
        public int Value { get; set; }
        public int MaxValue { get; set; } = 1;
        public double HeightPercentage => MaxValue > 0 ? (double)Value / MaxValue * 100.0 : 0;
    }

    /// <summary>
    /// Represents a single bar item for simple column charts (single series).
    /// </summary>
    public class SingleBarItem
    {
        public string Label { get; set; } = "";
        public int Value { get; set; }
        public int MaxValue { get; set; } = 1;
        public Brush Fill { get; set; } = Brushes.SteelBlue;
        public double HeightPercentage => MaxValue > 0 ? (double)Value / MaxValue * 100.0 : 0;
    }
}
