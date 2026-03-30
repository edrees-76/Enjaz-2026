using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Enjaz.Models;

public class Sample : INotifyPropertyChanged
{
	private int _id;

	private int _certificateId;

	private int _root;

	private string _sampleNumber = string.Empty;

	private string _description = string.Empty;

	private DateTime _measurementDate = DateTime.Now;

	private string _result = string.Empty;

	private int? _receptionId;

	private string _isotopeK40 = string.Empty;

	private string _isotopeRa226 = string.Empty;

	private string _isotopeTh232 = string.Empty;

	private string _isotopeRa = string.Empty;

	private string _isotopeCs137 = string.Empty;

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

	public int CertificateId
	{
		get
		{
			return _certificateId;
		}
		set
		{
			_certificateId = value;
			OnPropertyChanged("CertificateId");
		}
	}

	public int? ReceptionId
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

	public int Root
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

	public DateTime MeasurementDate
	{
		get
		{
			return _measurementDate;
		}
		set
		{
			_measurementDate = value;
			OnPropertyChanged("MeasurementDate");
		}
	}

	public string Result
	{
		get
		{
			return _result;
		}
		set
		{
			_result = value;
			OnPropertyChanged("Result");
		}
	}

	public string IsotopeK40
	{
		get
		{
			return _isotopeK40;
		}
		set
		{
			_isotopeK40 = value;
			OnPropertyChanged("IsotopeK40");
		}
	}

	public string IsotopeRa226
	{
		get
		{
			return _isotopeRa226;
		}
		set
		{
			_isotopeRa226 = value;
			OnPropertyChanged("IsotopeRa226");
		}
	}

	public string IsotopeTh232
	{
		get
		{
			return _isotopeTh232;
		}
		set
		{
			_isotopeTh232 = value;
			OnPropertyChanged("IsotopeTh232");
		}
	}

	public string IsotopeRa
	{
		get
		{
			return _isotopeRa;
		}
		set
		{
			_isotopeRa = value;
			OnPropertyChanged("IsotopeRa");
		}
	}

	public string IsotopeCs137
	{
		get
		{
			return _isotopeCs137;
		}
		set
		{
			_isotopeCs137 = value;
			OnPropertyChanged("IsotopeCs137");
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
