using System;
using MaterialDesignThemes.Wpf;

namespace Enjaz.Models;

public class HelpContent
{
	public int Id { get; set; }

	public string Title { get; set; } = string.Empty;

	public string Abstract { get; set; } = string.Empty;

	public string ContentSimple { get; set; } = string.Empty;

	public string ContentAdvanced { get; set; } = string.Empty;

	public string Category { get; set; } = string.Empty;

	public string IconKind { get; set; } = "HelpCircleOutline";

	public string Keywords { get; set; } = string.Empty;

	public string RelatedView { get; set; } = string.Empty;

	public string VideoUrl { get; set; } = string.Empty;

	public PackIconKind Icon
	{
		get
		{
			PackIconKind result;
			return Enum.TryParse<PackIconKind>(IconKind, out result) ? result : PackIconKind.HelpCircleOutline;
		}
	}

	public bool HasVideo => !string.IsNullOrEmpty(VideoUrl);

	public string GetContent(bool isAdvanced)
	{
		return isAdvanced ? ContentAdvanced : ContentSimple;
	}
}
