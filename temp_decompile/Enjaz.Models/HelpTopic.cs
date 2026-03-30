using MaterialDesignThemes.Wpf;

namespace Enjaz.Models;

public class HelpTopic
{
	public string Title { get; set; } = string.Empty;

	public string Content { get; set; } = string.Empty;

	public PackIconKind Icon { get; set; }

	public string Category { get; set; } = string.Empty;
}
