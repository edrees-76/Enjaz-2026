namespace Enjaz.ViewModels;

public class ColumnOption : BaseViewModel
{
	private bool _isSelected;

	public string Name { get; set; } = string.Empty;

	public string PropertyName { get; set; } = string.Empty;

	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			SetProperty(ref _isSelected, value, "IsSelected");
		}
	}
}
