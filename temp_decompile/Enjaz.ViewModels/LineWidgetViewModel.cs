using System;

namespace Enjaz.ViewModels;

public class LineWidgetViewModel : DashboardWidgetViewModel
{
	public string[] Labels { get; set; } = Array.Empty<string>();

	public Func<double, string> Formatter { get; set; } = (double x) => x.ToString("N0");
}
