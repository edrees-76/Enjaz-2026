using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Enjaz.Helpers;
using Enjaz.Models;
using Enjaz.Services;
using Enjaz.Services.Repositories;

namespace Enjaz.ViewModels;

public class SampleReceptionsViewModel : BaseViewModel
{
	private readonly SampleReceptionRepository _receptionRepository;

	private readonly CertificateRepository _certificateRepository;

	private readonly ReportingService _reportingService;

	private readonly INotificationService _alertService;

	private string _newSampleNumber = string.Empty;

	private string _newSampleDescription = string.Empty;

	private int _currentPage = 1;

	private int _itemsPerPage = 10;

	private int _totalPages;

	private SampleReception? _selectedReception;

	private SampleReception? _editingReception;

	private bool _isEditing;

	private bool _isViewingDetails;

	private bool _isSelectingType;

	private string _selectedCertificateType = string.Empty;

	private string _searchCriteria = string.Empty;

	private string _selectedCertificateStatusFilter = "الكل";

	private string _searchText = string.Empty;

	private DateTime? _searchStartDate;

	private DateTime? _searchEndDate;

	private static readonly List<string> DefaultSenders = new List<string> { "مركز الرقابة على الأغذية والأدوية - بنغازي", "مركز الرقابة على الأغذية والأدوية - البطنان", "مركز الرقابة على الأغذية والأدوية - مصراتة", "مركز الرقابة على الأغذية والأدوية - الخمس", "مركز الرقابة على الأغذية والأدوية - طرابلس", "مركز الرقابة على الأغذية والأدوية - زوارة" };

	public ObservableCollection<string> SendersList { get; }

	public string NewSampleNumber
	{
		get
		{
			return _newSampleNumber;
		}
		set
		{
			SetProperty(ref _newSampleNumber, value, "NewSampleNumber");
		}
	}

	public string NewSampleDescription
	{
		get
		{
			return _newSampleDescription;
		}
		set
		{
			SetProperty(ref _newSampleDescription, value, "NewSampleDescription");
		}
	}

	public ObservableCollection<SampleReception> Receptions { get; }

	public ObservableCollection<SampleReception> PagedReceptions { get; } = new ObservableCollection<SampleReception>();

	public int CurrentPage
	{
		get
		{
			return _currentPage;
		}
		set
		{
			if (SetProperty(ref _currentPage, value, "CurrentPage"))
			{
				UpdatePagedReceptions();
			}
		}
	}

	public int ItemsPerPage
	{
		get
		{
			return _itemsPerPage;
		}
		set
		{
			if (SetProperty(ref _itemsPerPage, value, "ItemsPerPage"))
			{
				CurrentPage = 1;
				UpdatePagedReceptions();
			}
		}
	}

	public int TotalPages
	{
		get
		{
			return _totalPages;
		}
		private set
		{
			SetProperty(ref _totalPages, value, "TotalPages");
		}
	}

	public int TotalSamplesSum => Receptions.Sum((SampleReception r) => r.SampleCount);

	public int TotalConsumableSamples => Receptions.Where((SampleReception r) => r.CertificateType.Contains("استهلاكية")).Sum((SampleReception r) => r.SampleCount);

	public int TotalEnvironmentalSamples => Receptions.Where((SampleReception r) => r.CertificateType.Contains("بيئية")).Sum((SampleReception r) => r.SampleCount);

	public SampleReception? SelectedReception
	{
		get
		{
			return _selectedReception;
		}
		set
		{
			SetProperty(ref _selectedReception, value, "SelectedReception");
		}
	}

	public SampleReception? EditingReception
	{
		get
		{
			return _editingReception;
		}
		set
		{
			SetProperty(ref _editingReception, value, "EditingReception");
		}
	}

	public bool IsEditing
	{
		get
		{
			return _isEditing;
		}
		set
		{
			SetProperty(ref _isEditing, value, "IsEditing");
		}
	}

	public bool IsViewingDetails
	{
		get
		{
			return _isViewingDetails;
		}
		set
		{
			SetProperty(ref _isViewingDetails, value, "IsViewingDetails");
		}
	}

	public bool IsSelectingType
	{
		get
		{
			return _isSelectingType;
		}
		set
		{
			SetProperty(ref _isSelectingType, value, "IsSelectingType");
		}
	}

	public string SelectedCertificateType
	{
		get
		{
			return _selectedCertificateType;
		}
		set
		{
			SetProperty(ref _selectedCertificateType, value, "SelectedCertificateType");
		}
	}

	public ObservableCollection<string> SearchCriteriaList { get; }

	public string SearchCriteria
	{
		get
		{
			return _searchCriteria;
		}
		set
		{
			SetProperty(ref _searchCriteria, value, "SearchCriteria");
		}
	}

	public ObservableCollection<string> CertificateStatusFilters { get; } = new ObservableCollection<string> { "الكل", "تم إصدار شهادة", "لم يتم إصدار شهادة" };

	public string SelectedCertificateStatusFilter
	{
		get
		{
			return _selectedCertificateStatusFilter;
		}
		set
		{
			if (SetProperty(ref _selectedCertificateStatusFilter, value, "SelectedCertificateStatusFilter"))
			{
				CurrentPage = 1;
				UpdatePagedReceptions();
			}
		}
	}

	public string SearchText
	{
		get
		{
			return _searchText;
		}
		set
		{
			SetProperty(ref _searchText, value, "SearchText");
		}
	}

	public DateTime? SearchStartDate
	{
		get
		{
			return _searchStartDate;
		}
		set
		{
			SetProperty(ref _searchStartDate, value, "SearchStartDate");
		}
	}

	public DateTime? SearchEndDate
	{
		get
		{
			return _searchEndDate;
		}
		set
		{
			SetProperty(ref _searchEndDate, value, "SearchEndDate");
		}
	}

	public ICommand AddReceptionCommand { get; }

	public ICommand ConfirmTypeCommand { get; }

	public ICommand EditReceptionCommand { get; }

	public ICommand SaveReceptionCommand { get; }

	public ICommand CancelEditCommand { get; }

	public ICommand DeleteReceptionCommand { get; }

	public ICommand SearchCommand { get; }

	public ICommand ClearSearchCommand { get; }

	public ICommand ViewDetailsCommand { get; }

	public ICommand CloseDetailsCommand { get; }

	public ICommand AddSampleCommand { get; }

	public ICommand RemoveSampleCommand { get; }

	public ICommand NextPageCommand { get; }

	public ICommand PreviousPageCommand { get; }

	public event Action<NavigationDestination>? RequestNavigation;

	public SampleReceptionsViewModel(SampleReceptionRepository receptionRepository, CertificateRepository certificateRepository, ReportingService reportingService, INotificationService alertService)
	{
		_receptionRepository = receptionRepository;
		_certificateRepository = certificateRepository;
		_reportingService = reportingService;
		_alertService = alertService;
		Receptions = new ObservableCollection<SampleReception>();
		SendersList = new ObservableCollection<string>();
		SearchCriteriaList = new ObservableCollection<string> { "رقم طلب التحليل", "الجهة المرسلة", "المورد", "التاريخ" };
		SearchCriteria = "رقم طلب التحليل";
		AddReceptionCommand = new RelayCommand(delegate
		{
			AddReception();
		});
		ConfirmTypeCommand = new RelayCommand(delegate
		{
			ConfirmType();
		}, (object? _) => !string.IsNullOrEmpty(SelectedCertificateType));
		EditReceptionCommand = new RelayCommand(delegate
		{
			EditReception();
		}, (object? _) => SelectedReception != null);
		SaveReceptionCommand = new AsyncRelayCommand((Func<object?, Task>)async delegate
		{
			await SaveReceptionAsync();
		}, (Predicate<object?>?)null, (AsyncRelayCommand.IErrorHandler?)null);
		CancelEditCommand = new RelayCommand(delegate
		{
			CancelEdit();
		});
		DeleteReceptionCommand = new AsyncRelayCommand((Func<object?, Task>)async delegate
		{
			await DeleteReceptionAsync();
		}, (Predicate<object?>?)((object? _) => SelectedReception != null), (AsyncRelayCommand.IErrorHandler?)null);
		SearchCommand = new AsyncRelayCommand((Func<object?, Task>)async delegate
		{
			await SearchAsync();
		}, (Predicate<object?>?)null, (AsyncRelayCommand.IErrorHandler?)null);
		ClearSearchCommand = new AsyncRelayCommand((Func<object?, Task>)async delegate
		{
			await LoadReceptionsAsync();
		}, (Predicate<object?>?)null, (AsyncRelayCommand.IErrorHandler?)null);
		ViewDetailsCommand = new AsyncRelayCommand((Func<object?, Task>)async delegate
		{
			await ViewDetailsAsync();
		}, (Predicate<object?>?)((object? _) => SelectedReception != null), (AsyncRelayCommand.IErrorHandler?)null);
		CloseDetailsCommand = new RelayCommand(delegate
		{
			CloseDetails();
		});
		AddSampleCommand = new RelayCommand(delegate
		{
			AddSample();
		});
		RemoveSampleCommand = new RelayCommand(RemoveSample);
		NextPageCommand = new RelayCommand(delegate
		{
			if (CurrentPage < TotalPages)
			{
				CurrentPage++;
			}
		});
		PreviousPageCommand = new RelayCommand(delegate
		{
			if (CurrentPage > 1)
			{
				CurrentPage--;
			}
		});
	}

	public async Task LoadReceptionsAsync()
	{
		try
		{
			List<SampleReception> list = await _receptionRepository.GetAllReceptionsAsync();
			Receptions.Clear();
			foreach (SampleReception r in list.OrderByDescending((SampleReception x) => x.CreatedAt))
			{
				Receptions.Add(r);
			}
			CurrentPage = 1;
			UpdatePagedReceptions();
			UpdateTotals();
			await LoadSuggestionsAsync();
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			_alertService.ShowError("حدث خطأ أثناء تحميل الاستلامات: " + ex2.Message);
		}
	}

	public async Task LoadSuggestionsAsync()
	{
		try
		{
			SendersList.Clear();
			foreach (string s in DefaultSenders)
			{
				SendersList.Add(s);
			}
			foreach (string s2 in await _reportingService.GetUniqueSendersAsync())
			{
				if (!string.IsNullOrWhiteSpace(s2) && !SendersList.Contains(s2))
				{
					SendersList.Add(s2);
				}
			}
		}
		catch (Exception)
		{
			if (SendersList.Count != 0)
			{
				return;
			}
			foreach (string s3 in DefaultSenders)
			{
				SendersList.Add(s3);
			}
		}
	}

	private void AddReception()
	{
		SelectedCertificateType = string.Empty;
		IsSelectingType = true;
	}

	private void ConfirmType()
	{
		string selectedCertificateType = SelectedCertificateType;
		EditingReception = new SampleReception
		{
			Date = DateTime.Now,
			CertificateType = selectedCertificateType,
			Status = "في انتظار إصدار شهادة"
		};
		IsSelectingType = false;
		IsEditing = true;
		this.RequestNavigation?.Invoke(NavigationDestination.SampleReceptionForm);
	}

	private void EditReception()
	{
		if (SelectedReception != null)
		{
			EditingReception = new SampleReception
			{
				Id = SelectedReception.Id,
				AnalysisRequestNumber = SelectedReception.AnalysisRequestNumber,
				NotificationNumber = SelectedReception.NotificationNumber,
				DeclarationNumber = SelectedReception.DeclarationNumber,
				Sender = SelectedReception.Sender,
				Supplier = SelectedReception.Supplier,
				Origin = SelectedReception.Origin,
				PolicyNumber = SelectedReception.PolicyNumber,
				FinancialReceiptNumber = SelectedReception.FinancialReceiptNumber,
				CertificateType = SelectedReception.CertificateType,
				Date = SelectedReception.Date,
				Status = SelectedReception.Status,
				CreatedAt = SelectedReception.CreatedAt,
				Samples = new ObservableCollection<ReceptionSample>(SelectedReception.Samples.Select((ReceptionSample s) => new ReceptionSample
				{
					Id = s.Id,
					ReceptionId = s.ReceptionId,
					Root = s.Root,
					SampleNumber = s.SampleNumber,
					Description = s.Description
				}))
			};
			IsEditing = true;
			this.RequestNavigation?.Invoke(NavigationDestination.SampleReceptionForm);
		}
	}

	private async Task SaveReceptionAsync()
	{
		if (EditingReception == null)
		{
			return;
		}
		if (string.IsNullOrWhiteSpace(EditingReception.AnalysisRequestNumber))
		{
			_alertService.ShowError("يجب إدخال رقم طلب التحليل.");
			return;
		}
		try
		{
			if (EditingReception.Id == 0)
			{
				EditingReception.CreatedAt = DateTime.Now;
				await _receptionRepository.AddSampleReceptionAsync(EditingReception);
				_alertService.ShowSuccess("تمت إضافة الاستلام بنجاح.");
			}
			else
			{
				await _receptionRepository.UpdateSampleReceptionAsync(EditingReception);
				try
				{
					Certificate associatedCert = await _certificateRepository.GetCertificateByReceptionIdAsync(EditingReception.Id);
					if (associatedCert != null)
					{
						associatedCert.Sender = EditingReception.Sender;
						associatedCert.Supplier = EditingReception.Supplier;
						associatedCert.Origin = EditingReception.Origin;
						associatedCert.DeclarationNumber = EditingReception.DeclarationNumber;
						associatedCert.NotificationNumber = EditingReception.NotificationNumber;
						associatedCert.FinancialReceiptNumber = EditingReception.FinancialReceiptNumber;
						associatedCert.PolicyNumber = EditingReception.PolicyNumber;
						await _certificateRepository.UpdateCertificateAsync(associatedCert);
					}
				}
				catch (Exception ex)
				{
					Exception ex2 = ex;
					LoggerService.LogWarning("Failed to sync reception changes to certificate: " + ex2.Message);
				}
				_alertService.ShowSuccess("تم تحديث الاستلام بنجاح.");
			}
			IsEditing = false;
			EditingReception = null;
			this.RequestNavigation?.Invoke(NavigationDestination.SampleReceptions);
			await LoadReceptionsAsync();
		}
		catch (Exception ex3)
		{
			_alertService.ShowError("حدث خطأ أثناء الحفظ: " + ex3.Message);
		}
	}

	private void CancelEdit()
	{
		IsEditing = false;
		IsSelectingType = false;
		EditingReception = null;
		this.RequestNavigation?.Invoke(NavigationDestination.SampleReceptions);
	}

	private Task DeleteReceptionAsync()
	{
		if (SelectedReception == null)
		{
			return Task.CompletedTask;
		}
		RaiseConfirmation("تأكيد الحذف", "هل أنت متأكد من حذف استلام العينات هذا؟", NotificationType.Warning, async delegate(bool confirmed)
		{
			if (confirmed)
			{
				try
				{
					await _receptionRepository.DeleteSampleReceptionAsync(SelectedReception.Id);
					_alertService.ShowSuccess("تم الحذف بنجاح.");
					await LoadReceptionsAsync();
				}
				catch (Exception ex)
				{
					_alertService.ShowError("حدث خطأ أثناء الحذف: " + ex.Message);
				}
			}
		});
		return Task.CompletedTask;
	}

	private async Task SearchAsync()
	{
		try
		{
			List<SampleReception> results = await _receptionRepository.SearchSampleReceptionsAsync((SearchCriteria != "التاريخ") ? SearchText : "");
			if (SearchCriteria == "التاريخ" && SearchStartDate.HasValue && SearchEndDate.HasValue)
			{
				results = results.Where((SampleReception sampleReception) => sampleReception.Date >= SearchStartDate.Value && sampleReception.Date <= SearchEndDate.Value).ToList();
			}
			Receptions.Clear();
			foreach (SampleReception r in results.OrderByDescending((SampleReception x) => x.CreatedAt))
			{
				Receptions.Add(r);
			}
			CurrentPage = 1;
			UpdatePagedReceptions();
			UpdateTotals();
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			_alertService.ShowError("خطأ في البحث: " + ex2.Message);
		}
	}

	private string GetSearchCriteriaMap()
	{
		string searchCriteria = SearchCriteria;
		if (1 == 0)
		{
		}
		string result = searchCriteria switch
		{
			"رقم طلب التحليل" => "AnalysisRequestNumber", 
			"الجهة المرسلة" => "Sender", 
			"المورد" => "Supplier", 
			_ => "All", 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private async Task ViewDetailsAsync()
	{
		if (SelectedReception != null)
		{
			IsViewingDetails = true;
			await Task.CompletedTask;
		}
	}

	private void CloseDetails()
	{
		IsViewingDetails = false;
	}

	private void AddSample()
	{
		if (EditingReception != null)
		{
			string root = (EditingReception.Samples.Count + 1).ToString();
			EditingReception.Samples.Add(new ReceptionSample
			{
				Root = root,
				SampleNumber = NewSampleNumber,
				Description = NewSampleDescription
			});
			NewSampleNumber = "";
			NewSampleDescription = "";
		}
	}

	private void RemoveSample(object? obj)
	{
		if (EditingReception != null && obj is ReceptionSample item)
		{
			EditingReception.Samples.Remove(item);
			for (int i = 0; i < EditingReception.Samples.Count; i++)
			{
				EditingReception.Samples[i].Root = (i + 1).ToString();
			}
		}
	}

	private void UpdatePagedReceptions()
	{
		IEnumerable<SampleReception> source = Receptions.AsEnumerable();
		if (SelectedCertificateStatusFilter == "تم إصدار شهادة")
		{
			source = source.Where((SampleReception r) => r.Status != "في انتظار إصدار شهادة" && r.Status != "ملغية");
		}
		else if (SelectedCertificateStatusFilter == "لم يتم إصدار شهادة")
		{
			source = source.Where((SampleReception r) => r.Status == "في انتظار إصدار شهادة");
		}
		int num = source.Count();
		TotalPages = (int)Math.Ceiling((double)num / (double)ItemsPerPage);
		if (TotalPages == 0)
		{
			TotalPages = 1;
		}
		if (CurrentPage > TotalPages)
		{
			CurrentPage = TotalPages;
		}
		List<SampleReception> list = source.Skip((CurrentPage - 1) * ItemsPerPage).Take(ItemsPerPage).ToList();
		PagedReceptions.Clear();
		for (int num2 = 0; num2 < list.Count; num2++)
		{
			list[num2].Sequence = (CurrentPage - 1) * ItemsPerPage + num2 + 1;
			PagedReceptions.Add(list[num2]);
		}
		OnPropertyChanged("TotalPages");
	}

	private void UpdateTotals()
	{
		OnPropertyChanged("TotalSamplesSum");
		OnPropertyChanged("TotalConsumableSamples");
		OnPropertyChanged("TotalEnvironmentalSamples");
	}
}
