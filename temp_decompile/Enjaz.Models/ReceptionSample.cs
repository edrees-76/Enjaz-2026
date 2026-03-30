using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Enjaz.Models;

public class ReceptionSample : INotifyPropertyChanged
{
	private int _id;

	private int _receptionId;

	private string _sampleNumber = string.Empty;

	private string _description = string.Empty;

	private string _root = string.Empty;

	public int Id
	{
		get
		{
			return _id;
		}
		set
		{
			_id = value;
			OnPropertyChanged("Id");
		}
	}

	public int ReceptionId
	{
		get
		{
			return _receptionId;
		}
		set
		{
			_receptionId = value;
			OnPropertyChanged("ReceptionId");
		}
	}

	public string SampleNumber
	{
		get
		{
			return _sampleNumber;
		}
		set
		{
			_sampleNumber = value;
			OnPropertyChanged("SampleNumber");
		}
	}

	public string Description
	{
		get
		{
			return _description;
		}
		set
		{
			_description = value;
			OnPropertyChanged("Description");
		}
	}

	public string Root
	{
		get
		{
			return _root;
		}
		set
		{
			_root = value;
			OnPropertyChanged("Root");
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
