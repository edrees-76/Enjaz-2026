using LiveCharts;

namespace Enjaz.ViewModels;

public abstract class DashboardWidgetViewModel : BaseViewModel
{
	public string Title { get; set; } = string.Empty;

	public SeriesCollection Series { get; set; } = new SeriesCollection();

	public int ColumnSpan { get; set; } = 1;
}
