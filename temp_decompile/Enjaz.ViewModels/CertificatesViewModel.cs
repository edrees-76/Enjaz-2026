using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Enjaz.Helpers;
using Enjaz.Models;
using Enjaz.Services;
using Enjaz.Services.Repositories;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;

namespace Enjaz.ViewModels;

public class CertificatesViewModel : BaseViewModel
{
	private readonly CertificateRepository _certificateRepository;

	private readonly IPdfService _pdfService;

	private readonly ExcelExportService _excelExportService;

	private readonly UserService _userService;

	private readonly SampleReceptionRepository _sampleReceptionRepository;

	private readonly INotificationService _notificationService;

	private readonly IReceptionSearchService _receptionSearchService;

	private CancellationTokenSource? _searchCancellationTokenSource;

	private ObservableCollection<Certificate> _certificates;

	private Certificate? _selectedCertificate;

	private ObservableCollection<Sample> _samples;

	private Sample? _selectedSample;

	private string _searchText = string.Empty;

	private string _searchCriteria = "ط§ظ„ظƒظ„";

	private DateTime? _searchStartDate;

	private DateTime? _searchEndDate;

	private bool _isEditing;

	private int _currentPage = 1;

	private int _pageSize = 50;

	private int _totalRecords = 0;

	private int _totalPages = 0;

	private ObservableCollection<AuditLog> _selectedCertificateHistory;

	private string _recipientName = string.Empty;

	private string _certificateType = string.Empty;

	private string _description = string.Empty;

	private DateTime _issueDate = DateTime.Now;

	private DateTime? _expiryDate;

	private string _issuingAuthority = string.Empty;

	private string _analysisType = string.Empty;

	private string _sender = string.Empty;

	private string _supplier = string.Empty;

	private string _origin = string.Empty;

	private string _declarationNumber = string.Empty;

	private string _policyNumber = string.Empty;

	private string _notificationNumber = string.Empty;

	private string _financialReceiptNumber = string.Empty;

	private string _specialistName = string.Empty;

	private string _sectionHeadName = string.Empty;

	private string _managerName = string.Empty;

	private string _notes = string.Empty;

	private int _totalConsumableCertificates = 0;

	private int _totalEnvironmentalCertificates = 0;

	private int _totalSamplesSum = 0;

	private bool _isInternalClear = false;

	private int? _linkedReceptionId = null;

	private bool _isViewingDetails;

	private SampleReception? _selectedPendingReception;

	public string SectionHeadName
	{
		get
		{
			return _sectionHeadName;
		}
		set
		{
			SetProperty(ref _sectionHeadName, value, "SectionHeadName");
		}
	}

	public string ManagerName
	{
		get
		{
			return _managerName;
		}
		set
		{
			SetProperty(ref _managerName, value, "ManagerName");
		}
	}

	public ObservableCollection<Certificate> Certificates
	{
		get
		{
			return _certificates;
		}
		set
		{
			SetProperty(ref _certificates, value, "Certificates");
		}
	}

	public Certificate? SelectedCertificate
	{
		get
		{
			return _selectedCertificate;
		}
		set
		{
			SetProperty(ref _selectedCertificate, value, "SelectedCertificate");
			OnPropertyChanged("IsSelectedCertificateEnvironmental");
			CommandManager.InvalidateRequerySuggested();
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
			if (!SetProperty(ref _searchText, value, "SearchText") || _isInternalClear)
			{
				return;
			}
			_searchCancellationTokenSource?.Cancel();
			_searchCancellationTokenSource = new CancellationTokenSource();
			CancellationToken token = _searchCancellationTokenSource.Token;
			Task.Delay(400, token).ContinueWith(delegate(Task t)
			{
				if (!t.IsCanceled)
				{
					((DispatcherObject)Application.Current).Dispatcher.Invoke<Task>((Func<Task>)async delegate
					{
						await SearchCertificatesAsync();
					});
				}
			}, token);
		}
	}

	public string SearchCriteria
	{
		get
		{
			return _searchCriteria;
		}
		set
		{
			if (SetProperty(ref _searchCriteria, value, "SearchCriteria"))
			{
				SearchCertificatesAsync();
			}
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
			if (SetProperty(ref _searchStartDate, value, "SearchStartDate") && SearchCriteria == "التاريخ")
			{
				SearchCertificatesAsync();
			}
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
			if (SetProperty(ref _searchEndDate, value, "SearchEndDate") && SearchCriteria == "التاريخ")
			{
				SearchCertificatesAsync();
			}
		}
	}

	public ObservableCollection<AuditLog> SelectedCertificateHistory
	{
		get
		{
			return _selectedCertificateHistory;
		}
		set
		{
			SetProperty(ref _selectedCertificateHistory, value, "SelectedCertificateHistory");
		}
	}

	public ISnackbarMessageQueue MessageQueue => _notificationService.MessageQueue;

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

	public string RecipientName
	{
		get
		{
			return _recipientName;
		}
		set
		{
			SetProperty(ref _recipientName, value, "RecipientName");
			CommandManager.InvalidateRequerySuggested();
		}
	}

	public string CertificateType
	{
		get
		{
			return _certificateType;
		}
		set
		{
			if (SetProperty(ref _certificateType, value, "CertificateType"))
			{
				OnPropertyChanged("IsSampleCertificate");
				OnPropertyChanged("IsAnalysisTypeVisible");
				OnPropertyChanged("IsEnvironmentalCertificate");
				if (IsEnvironmentalCertificate && string.IsNullOrEmpty(AnalysisType))
				{
					AnalysisType = "تحليل مبدئي (دون الوصول لحالة الاتزان)";
				}
				CommandManager.InvalidateRequerySuggested();
			}
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
			SetProperty(ref _description, value, "Description");
		}
	}

	public DateTime IssueDate
	{
		get
		{
			return _issueDate;
		}
		set
		{
			SetProperty(ref _issueDate, value, "IssueDate");
		}
	}

	public DateTime? ExpiryDate
	{
		get
		{
			return _expiryDate;
		}
		set
		{
			SetProperty(ref _expiryDate, value, "ExpiryDate");
		}
	}

	public string IssuingAuthority
	{
		get
		{
			return _issuingAuthority;
		}
		set
		{
			SetProperty(ref _issuingAuthority, value, "IssuingAuthority");
			CommandManager.InvalidateRequerySuggested();
		}
	}

	public string AnalysisType
	{
		get
		{
			return _analysisType;
		}
		set
		{
			SetProperty(ref _analysisType, value, "AnalysisType");
		}
	}

	public string Sender
	{
		get
		{
			return _sender;
		}
		set
		{
			SetProperty(ref _sender, value, "Sender");
		}
	}

	public string Supplier
	{
		get
		{
			return _supplier;
		}
		set
		{
			SetProperty(ref _supplier, value, "Supplier");
		}
	}

	public string Origin
	{
		get
		{
			return _origin;
		}
		set
		{
			SetProperty(ref _origin, value, "Origin");
		}
	}

	public string DeclarationNumber
	{
		get
		{
			return _declarationNumber;
		}
		set
		{
			SetProperty(ref _declarationNumber, value, "DeclarationNumber");
		}
	}

	public string PolicyNumber
	{
		get
		{
			return _policyNumber;
		}
		set
		{
			SetProperty(ref _policyNumber, value, "PolicyNumber");
		}
	}

	public string NotificationNumber
	{
		get
		{
			return _notificationNumber;
		}
		set
		{
			SetProperty(ref _notificationNumber, value, "NotificationNumber");
		}
	}

	public string FinancialReceiptNumber
	{
		get
		{
			return _financialReceiptNumber;
		}
		set
		{
			SetProperty(ref _financialReceiptNumber, value, "FinancialReceiptNumber");
		}
	}

	public string SpecialistName
	{
		get
		{
			return _specialistName;
		}
		set
		{
			SetProperty(ref _specialistName, value, "SpecialistName");
		}
	}

	public string Notes
	{
		get
		{
			return _notes;
		}
		set
		{
			SetProperty(ref _notes, value, "Notes");
		}
	}

	public bool IsEnvironmentalCertificate => CertificateType != null && (CertificateType.Contains("بيئية") || CertificateType.Contains("بيئيه") || CertificateType.Contains("بيئيه"));

	public bool IsSampleCertificate => CertificateType != null && (CertificateType.Contains("بيئة") || CertificateType.Contains("تحليل") || CertificateType.Contains("عينة") || CertificateType.Contains("عينات"));

	public bool IsAnalysisTypeVisible => CertificateType != null && !CertificateType.Contains("استهلاكية");

	public ObservableCollection<string> SearchCriteriaList { get; } = new ObservableCollection<string>
	{
		"الكل", "رقم العينة", "رقم الشهادة", "رقم الاخطار", "رقم الاقرار الجمركى", "الجهة المرسلة", "المورد", "رقم الايصال المالى", "التاريخ", "رقم البوليصة",
		"اسم المستخدم"
	};

	public ObservableCollection<string> AvailableSenders { get; } = new ObservableCollection<string> { "مركز الرقابة على الاغذية و الادوية - طرابلس", "مركز الرقابة على الاغذية و الادوية - ازوارة", "مركز الرقابة على الاغذية و الادوية - الخمس", "مركز الرقابة على الاغذية و الادوية - مصراتة", "مركز الرقابة على الاغذية والأدوية - بنغازي", "مركز الرقابة على الاغذية و الادوية - البطنان" };

	public ObservableCollection<string> AvailableResults { get; } = new ObservableCollection<string> { "خالية من العناصر المشعة المصنعة" };

	public ObservableCollection<string> AvailableAnalysisTypes { get; } = new ObservableCollection<string>();

	public ObservableCollection<string> AvailableRecipientNames { get; } = new ObservableCollection<string>();

	public ObservableCollection<string> AvailableSuppliers { get; } = new ObservableCollection<string>();

	public ObservableCollection<string> AvailableOrigins { get; } = new ObservableCollection<string>();

	public ObservableCollection<string> AvailableSpecialistNames { get; } = new ObservableCollection<string>();

	public ObservableCollection<string> AvailableSectionHeadNames { get; } = new ObservableCollection<string>();

	public ObservableCollection<string> AvailableManagerNames { get; } = new ObservableCollection<string>();

	public ObservableCollection<SampleReception> PendingReceptions { get; }

	public SampleReception? SelectedPendingReception
	{
		get
		{
			return _selectedPendingReception;
		}
		set
		{
			if (SetProperty(ref _selectedPendingReception, value, "SelectedPendingReception") && value != null)
			{
				LoadFromReception(value);
			}
		}
	}

	public ObservableCollection<Sample> Samples
	{
		get
		{
			return _samples;
		}
		set
		{
			SetProperty(ref _samples, value, "Samples");
		}
	}

	public Sample? SelectedSample
	{
		get
		{
			return _selectedSample;
		}
		set
		{
			SetProperty(ref _selectedSample, value, "SelectedSample");
			CommandManager.InvalidateRequerySuggested();
		}
	}

	public int TotalConsumableCertificates
	{
		get
		{
			return _totalConsumableCertificates;
		}
		set
		{
			SetProperty(ref _totalConsumableCertificates, value, "TotalConsumableCertificates");
		}
	}

	public int TotalEnvironmentalCertificates
	{
		get
		{
			return _totalEnvironmentalCertificates;
		}
		set
		{
			SetProperty(ref _totalEnvironmentalCertificates, value, "TotalEnvironmentalCertificates");
		}
	}

	public int TotalSamplesSum
	{
		get
		{
			return _totalSamplesSum;
		}
		set
		{
			SetProperty(ref _totalSamplesSum, value, "TotalSamplesSum");
		}
	}

	public bool IsSelectedCertificateEnvironmental
	{
		get
		{
			if (SelectedCertificate == null)
			{
				return false;
			}
			return SelectedCertificate.CertificateType != null && SelectedCertificate.CertificateType.Contains("ط\u00a8ظٹط¦ظٹط©");
		}
	}

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
				OnPropertyChanged("IsFirstPage");
				OnPropertyChanged("IsLastPage");
				OnPropertyChanged("PageSummary");
			}
		}
	}

	public int PageSize
	{
		get
		{
			return _pageSize;
		}
		set
		{
			if (SetProperty(ref _pageSize, value, "PageSize"))
			{
				CurrentPage = 1;
				LoadCertificatesAsync();
			}
		}
	}

	public int TotalRecords
	{
		get
		{
			return _totalRecords;
		}
		set
		{
			if (SetProperty(ref _totalRecords, value, "TotalRecords"))
			{
				OnPropertyChanged("PageSummary");
			}
		}
	}

	public int TotalPages
	{
		get
		{
			return _totalPages;
		}
		set
		{
			if (SetProperty(ref _totalPages, value, "TotalPages"))
			{
				OnPropertyChanged("IsLastPage");
				OnPropertyChanged("PageSummary");
			}
		}
	}

	public bool IsFirstPage => CurrentPage > 1;

	public bool IsLastPage => CurrentPage < TotalPages;

	public string PageSummary => $"طµظپط\u00adط© {CurrentPage} ظ…ظ† {TotalPages} (ط§ظ„ط¹ط\u00afط\u00af ط§ظ„ظƒظ„ظٹ: {TotalRecords})";

	public ICommand AddCertificateCommand { get; private set; } = null;

	public ICommand EditCertificateCommand { get; private set; } = null;

	public ICommand SaveCertificateCommand { get; private set; } = null;

	public ICommand CancelEditCommand { get; private set; } = null;

	public ICommand AddSampleCommand { get; private set; } = null;

	public ICommand RemoveSampleCommand { get; private set; } = null;

	public ICommand ViewCertificateCommand { get; private set; } = null;

	public ICommand CloseDetailsCommand { get; private set; } = null;

	public ICommand GeneratePdfCommand { get; private set; } = null;

	public ICommand SavePdfCommand { get; private set; } = null;

	public ICommand PrintCertificateCommand { get; private set; } = null;

	public ICommand SearchCommand { get; private set; } = null;

	public ICommand LoadCertificatesCommand { get; private set; } = null;

	public ICommand ExportToExcelCommand { get; private set; } = null;

	public ICommand NextPageCommand { get; private set; } = null;

	public ICommand PreviousPageCommand { get; private set; } = null;

	public event Action<NavigationDestination>? RequestNavigation;

	public event Action? CertificateSaved;

	public async Task LoadCertificatesAsync()
	{
		try
		{
			base.IsBusy = true;
			base.StatusMessage = "جاري تحميل الشهادات...";
			TotalRecords = await _certificateRepository.GetTotalCertificatesCountAsync();
			TotalPages = (int)Math.Ceiling((double)TotalRecords / (double)PageSize);
			if (TotalPages == 0)
			{
				TotalPages = 1;
			}
			if (CurrentPage > TotalPages)
			{
				CurrentPage = TotalPages;
			}
			if (CurrentPage < 1)
			{
				CurrentPage = 1;
			}
			List<Certificate> result = await _certificateRepository.GetCertificatesPaginatedAsync(CurrentPage, PageSize);
			Certificates.Clear();
			for (int i = 0; i < result.Count; i++)
			{
				result[i].Sequence = (CurrentPage - 1) * PageSize + i + 1;
				Certificates.Add(result[i]);
			}
			(int total, int consumable, int environmental, int totalSamples) counts = await _certificateRepository.GetCertificateCountsByTypeAsync();
			TotalConsumableCertificates = counts.consumable;
			TotalEnvironmentalCertificates = counts.environmental;
			TotalSamplesSum = counts.totalSamples;
			base.StatusMessage = $"تم تحميل {Certificates.Count} شهادة (صفحة {CurrentPage})";
			CommandManager.InvalidateRequerySuggested();
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			base.StatusMessage = "خطأ في تحميل الشهادات: " + ex2.Message;
			_notificationService.ShowError("فشل تحميل الشهادات");
		}
		finally
		{
			base.IsBusy = false;
		}
	}

	public async Task SearchCertificatesAsync(bool isManualSearch = false)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(SearchText) && SearchCriteria != "التاريخ")
			{
				await LoadCertificatesAsync();
				return;
			}
			base.IsBusy = true;
			base.StatusMessage = "جاري البحث عن الشهادات...";
			List<Certificate> result = await _certificateRepository.SearchCertificatesAsync(SearchText, SearchCriteria, CurrentPage, PageSize, SearchStartDate, SearchEndDate);
			Certificates.Clear();
			for (int i = 0; i < result.Count; i++)
			{
				result[i].Sequence = (CurrentPage - 1) * PageSize + i + 1;
				Certificates.Add(result[i]);
			}
			TotalRecords = await _certificateRepository.GetSearchCertificatesCountAsync(SearchText, SearchCriteria, SearchStartDate, SearchEndDate);
			TotalPages = (int)Math.Ceiling((double)TotalRecords / (double)PageSize);
			if (TotalPages == 0)
			{
				TotalPages = 1;
			}
			if (TotalRecords == 0 && isManualSearch)
			{
				SetNotification("لا توجد نتائج", "لا يوجد شهادة بهذه البيانات");
			}
			base.StatusMessage = $"تم العثور على {TotalRecords} نتيجة";
			CommandManager.InvalidateRequerySuggested();
			(int total, int consumable, int environmental, int totalSamples) counts = await _certificateRepository.GetCertificateCountsByTypeAsync();
			TotalConsumableCertificates = counts.consumable;
			TotalEnvironmentalCertificates = counts.environmental;
			TotalSamplesSum = counts.totalSamples;
			if (isManualSearch && !string.IsNullOrWhiteSpace(SearchText))
			{
				_isInternalClear = true;
				SearchText = string.Empty;
				_isInternalClear = false;
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			base.StatusMessage = "خطأ في البحث: " + ex2.Message;
			_notificationService.ShowError("فشل عملية البحث");
		}
		finally
		{
			base.IsBusy = false;
		}
	}

	private async Task NextPageAsync()
	{
		if (IsLastPage)
		{
			CurrentPage++;
			await LoadCertificatesAsync();
		}
	}

	private async Task PreviousPageAsync()
	{
		if (IsFirstPage)
		{
			CurrentPage--;
			await LoadCertificatesAsync();
		}
	}

	private void StartAddCertificate()
	{
		ClearCertificateForm();
		SelectedCertificate = null;
		SampleReception sampleReception = _receptionSearchService.ShowSearchDialog();
		if (sampleReception != null)
		{
			_linkedReceptionId = sampleReception.Id;
			LoadFromReception(sampleReception);
			IsEditing = true;
			this.RequestNavigation?.Invoke(NavigationDestination.CertificateForm);
		}
	}

	private async Task StartEditCertificate()
	{
		if (SelectedCertificate != null)
		{
			await LoadCertificateForEditAsync(SelectedCertificate);
			IsEditing = true;
			this.RequestNavigation?.Invoke(NavigationDestination.CertificateForm);
		}
	}

	private void ClearCertificateForm()
	{
		RecipientName = string.Empty;
		CertificateType = "شهادة خلو من الاشعاع";
		Description = string.Empty;
		IssueDate = DateTime.Now;
		ExpiryDate = null;
		IssuingAuthority = "ادارة الرقابة";
		AnalysisType = string.Empty;
		Sender = string.Empty;
		Supplier = string.Empty;
		Origin = string.Empty;
		DeclarationNumber = string.Empty;
		PolicyNumber = string.Empty;
		NotificationNumber = string.Empty;
		FinancialReceiptNumber = string.Empty;
		SpecialistName = string.Empty;
		SectionHeadName = string.Empty;
		ManagerName = string.Empty;
		Notes = string.Empty;
		Samples = new ObservableCollection<Sample>();
		SelectedPendingReception = null;
		_linkedReceptionId = null;
	}

	private async Task LoadPendingReceptionsAsync()
	{
		try
		{
			List<SampleReception> dict = await _sampleReceptionRepository.GetPendingReceptionsAsync();
			PendingReceptions.Clear();
			foreach (SampleReception rec in dict)
			{
				PendingReceptions.Add(rec);
			}
		}
		catch (Exception)
		{
			_notificationService.ShowError("خطأ في جلب بيانات استلام العينات.");
		}
	}

	private void LoadFromReception(SampleReception rec)
	{
		CertificateType = rec.CertificateType;
		Sender = rec.Sender ?? string.Empty;
		Supplier = rec.Supplier ?? string.Empty;
		Origin = rec.Origin ?? string.Empty;
		DeclarationNumber = rec.DeclarationNumber ?? string.Empty;
		NotificationNumber = rec.NotificationNumber ?? string.Empty;
		PolicyNumber = rec.PolicyNumber ?? string.Empty;
		FinancialReceiptNumber = rec.FinancialReceiptNumber ?? string.Empty;
		Samples.Clear();
		if (rec.Samples == null)
		{
			return;
		}
		foreach (ReceptionSample sample in rec.Samples)
		{
			Samples.Add(new Sample
			{
				Root = (int.TryParse(sample.Root, out var result) ? result : 0),
				SampleNumber = (sample.SampleNumber ?? string.Empty),
				Description = (sample.Description ?? string.Empty),
				MeasurementDate = DateTime.Now
			});
		}
	}

	private async Task LoadCertificateForEditAsync(Certificate certificate)
	{
		RecipientName = certificate.RecipientName;
		CertificateType = certificate.CertificateType;
		Description = certificate.Description;
		IssueDate = certificate.IssueDate;
		ExpiryDate = certificate.ExpiryDate;
		IssuingAuthority = certificate.IssuingAuthority;
		AnalysisType = certificate.AnalysisType ?? string.Empty;
		Sender = certificate.Sender ?? string.Empty;
		Supplier = certificate.Supplier ?? string.Empty;
		Origin = certificate.Origin ?? string.Empty;
		DeclarationNumber = certificate.DeclarationNumber ?? string.Empty;
		PolicyNumber = certificate.PolicyNumber ?? string.Empty;
		NotificationNumber = certificate.NotificationNumber ?? string.Empty;
		FinancialReceiptNumber = certificate.FinancialReceiptNumber ?? string.Empty;
		SpecialistName = certificate.SpecialistName ?? string.Empty;
		SectionHeadName = certificate.SectionHeadName ?? string.Empty;
		ManagerName = certificate.ManagerName ?? string.Empty;
		Notes = certificate.Notes ?? string.Empty;
		base.IsBusy = true;
		base.BusyMessage = "جاري تحميل بيانات العينات...";
		try
		{
			Samples = new ObservableCollection<Sample>(await _certificateRepository.GetSamplesByCertificateIdAsync(certificate.Id));
		}
		finally
		{
			base.IsBusy = false;
		}
	}

	private bool CanSaveCertificate()
	{
		return true;
	}

	private bool ValidateRequiredFields()
	{
		bool result = true;
		List<string> list = new List<string>();
		if (string.IsNullOrWhiteSpace(CertificateType))
		{
			list.Add("نوع الشهادة");
		}
		if (string.IsNullOrWhiteSpace(IssuingAuthority))
		{
			IssuingAuthority = "ادارة الرقابة";
		}
		if (string.IsNullOrWhiteSpace(RecipientName))
		{
			RecipientName = "عام";
		}
		if (IsSampleCertificate)
		{
			if (string.IsNullOrWhiteSpace(Sender))
			{
				list.Add("الجهة المرسلة");
			}
			if (string.IsNullOrWhiteSpace(Supplier))
			{
				list.Add("المورد");
			}
			if (string.IsNullOrWhiteSpace(Origin))
			{
				list.Add("بلد المنشأ");
			}
			if (string.IsNullOrWhiteSpace(DeclarationNumber))
			{
				list.Add("رقم الإقرار الجمركي");
			}
			if (string.IsNullOrWhiteSpace(NotificationNumber))
			{
				list.Add("رقم الإخطار");
			}
			if (string.IsNullOrWhiteSpace(PolicyNumber))
			{
				list.Add("رقم البوليصة");
			}
			if (string.IsNullOrWhiteSpace(FinancialReceiptNumber))
			{
				list.Add("رقم الإيصال المالي");
			}
			if (Samples == null || Samples.Count == 0)
			{
				list.Add("إضافة عينة واحدة على الأقل للجدول");
			}
			else if (IsEnvironmentalCertificate)
			{
				bool flag = true;
				foreach (Sample sample in Samples)
				{
					if (string.IsNullOrWhiteSpace(sample.SampleNumber) || string.IsNullOrWhiteSpace(sample.Description) || string.IsNullOrWhiteSpace(sample.IsotopeK40) || string.IsNullOrWhiteSpace(sample.IsotopeCs137) || string.IsNullOrWhiteSpace(sample.IsotopeRa) || string.IsNullOrWhiteSpace(sample.IsotopeTh232) || string.IsNullOrWhiteSpace(sample.IsotopeRa226))
					{
						flag = false;
						break;
					}
				}
				if (!flag)
				{
					list.Add("بيانات العينة (الرقم، الوصف) ونتائج النظائر الخمسة (K40, Cs-137, Raeq, Th232, Ra226) لكافة العينات");
				}
			}
			else
			{
				bool flag2 = true;
				foreach (Sample sample2 in Samples)
				{
					if (string.IsNullOrWhiteSpace(sample2.Description) || string.IsNullOrWhiteSpace(sample2.Result))
					{
						flag2 = false;
						break;
					}
				}
				if (!flag2)
				{
					list.Add("بيانات العينة (الوصف) ونتيجة التحليل لكافة العينات");
				}
			}
		}
		if (list.Count > 0)
		{
			SetNotification("بيانات ناقصة", "لا يمكن حفظ الشهادة. يرجى إكمال الحقول والمتطلبات التالية:\n\n• " + string.Join("\n• ", list), NotificationType.Error);
			base.StatusMessage = "⚠\ufe0f البيانات غير مكتملة - يرجى مراجعة التنبيه";
			result = false;
		}
		return result;
	}

	private async Task<bool> SaveCertificateAsync(bool closeAfterSave = true)
	{
		try
		{
			if (!ValidateRequiredFields())
			{
				return false;
			}
			if (!string.IsNullOrWhiteSpace(FinancialReceiptNumber))
			{
				base.IsBusy = true;
				base.StatusMessage = "جاري التحقق من رقم الإيصال المالي...";
				bool isDuplicate = await _certificateRepository.IsFinancialReceiptDuplicateAsync(FinancialReceiptNumber, SelectedCertificate?.Id);
				base.IsBusy = false;
				if (isDuplicate)
				{
					SetNotification("تكرار رقم الإيصال", "إن رقم الإيصال المالي (" + FinancialReceiptNumber + ") مستخدم من قبل في شهادة أخرى.\nيرجى التأكد من الرقم والمحاولة مرة أخرى.", NotificationType.Warning);
					base.StatusMessage = "⚠\ufe0f رقم إيصال مكرر - يرجى مراجعة التنبيه";
					return false;
				}
			}
			base.IsBusy = true;
			base.BusyMessage = "جاري التحقق من البيانات...";
			if (SelectedCertificate == null)
			{
				base.BusyMessage = "جاري حفظ الشهادة الجديدة...";
				Certificate newCertificate = new Certificate
				{
					CertificateNumber = "AUTO",
					RecipientName = RecipientName,
					CertificateType = CertificateType,
					Description = Description,
					IssueDate = IssueDate,
					ExpiryDate = ExpiryDate,
					IssuingAuthority = IssuingAuthority,
					AnalysisType = AnalysisType,
					Sender = Sender,
					Supplier = Supplier,
					Origin = Origin,
					DeclarationNumber = DeclarationNumber,
					PolicyNumber = PolicyNumber,
					NotificationNumber = NotificationNumber,
					FinancialReceiptNumber = FinancialReceiptNumber,
					SpecialistName = SpecialistName,
					SectionHeadName = SectionHeadName,
					ManagerName = ManagerName,
					Notes = Notes,
					ReceptionId = _linkedReceptionId,
					Samples = new ObservableCollection<Sample>(Samples),
					CreatedBy = (_userService.CurrentUser?.Id ?? 1),
					CreatedByName = (_userService.CurrentUser?.FullName ?? "مدير النظام")
				};
				if (await _certificateRepository.AddCertificateAsync(newCertificate) > 0)
				{
					if (_linkedReceptionId.HasValue)
					{
						SampleReception linkedReception = await _sampleReceptionRepository.GetReceptionByIdAsync(_linkedReceptionId.Value);
						if (linkedReception != null)
						{
							linkedReception.FinancialReceiptNumber = newCertificate.FinancialReceiptNumber;
							linkedReception.Status = "تم إصدار شهادة";
							await _sampleReceptionRepository.UpdateSampleReceptionAsync(linkedReception);
						}
						_linkedReceptionId = null;
					}
					base.StatusMessage = "تم إضافة الشهادة بنجاح";
					this.CertificateSaved?.Invoke();
					await LoadCertificatesAsync();
					LoadSuggestionsAsync();
					try
					{
						base.StatusMessage = "جاري إنشاء وفتح الشهادة بصيغة PDF...";
						await Task.Run(() => _pdfService.GenerateAndOpenCertificatePdf(newCertificate, newCertificate.Samples));
					}
					catch (Exception)
					{
					}
					if (closeAfterSave)
					{
						CancelEdit();
					}
					base.IsBusy = false;
					SetNotification("تم بنجاح", "تم إضافة وقبول الشهادة الجديدة وحفظها في قاعدة البيانات بنجاح.", NotificationType.Success);
					return true;
				}
				base.IsBusy = false;
				SetNotification("فشل الحفظ", "حدث خطأ غير متوقع أثناء محاولة حفظ الشهادة. يرجى المحاولة مرة أخرى أو الاتصال بالدعم الفني.", NotificationType.Error);
				base.StatusMessage = "فشل في إضافة الشهادة. راجع السجلات.";
				return false;
			}
			base.BusyMessage = "جاري تحديث بيانات الشهادة...";
			SelectedCertificate.RecipientName = RecipientName;
			SelectedCertificate.CertificateType = CertificateType;
			SelectedCertificate.Description = Description;
			SelectedCertificate.IssueDate = IssueDate;
			SelectedCertificate.ExpiryDate = ExpiryDate;
			SelectedCertificate.IssuingAuthority = IssuingAuthority;
			SelectedCertificate.AnalysisType = AnalysisType;
			SelectedCertificate.Sender = Sender;
			SelectedCertificate.Supplier = Supplier;
			SelectedCertificate.Origin = Origin;
			SelectedCertificate.DeclarationNumber = DeclarationNumber;
			SelectedCertificate.PolicyNumber = PolicyNumber;
			SelectedCertificate.NotificationNumber = NotificationNumber;
			SelectedCertificate.FinancialReceiptNumber = FinancialReceiptNumber;
			SelectedCertificate.SpecialistName = SpecialistName;
			SelectedCertificate.SectionHeadName = SectionHeadName;
			SelectedCertificate.ManagerName = ManagerName;
			SelectedCertificate.Notes = Notes;
			SelectedCertificate.Samples = new ObservableCollection<Sample>(Samples);
			SelectedCertificate.UpdatedBy = _userService.CurrentUser?.Id;
			SelectedCertificate.UpdatedByName = _userService.CurrentUser?.FullName;
			SelectedCertificate.UpdatedAt = DateTime.Now;
			if (await _certificateRepository.UpdateCertificateAsync(SelectedCertificate))
			{
				if (SelectedCertificate.ReceptionId.HasValue)
				{
					SampleReception linkedReception2 = await _sampleReceptionRepository.GetReceptionByIdAsync(SelectedCertificate.ReceptionId.Value);
					if (linkedReception2 != null)
					{
						linkedReception2.Sender = SelectedCertificate.Sender;
						linkedReception2.Supplier = SelectedCertificate.Supplier;
						linkedReception2.Origin = SelectedCertificate.Origin;
						linkedReception2.DeclarationNumber = SelectedCertificate.DeclarationNumber;
						linkedReception2.PolicyNumber = SelectedCertificate.PolicyNumber;
						linkedReception2.NotificationNumber = SelectedCertificate.NotificationNumber;
						linkedReception2.FinancialReceiptNumber = SelectedCertificate.FinancialReceiptNumber;
						linkedReception2.Status = "تم إصدار شهادة";
						await _sampleReceptionRepository.UpdateSampleReceptionAsync(linkedReception2);
					}
				}
				int certId = SelectedCertificate.Id;
				base.StatusMessage = "تم تحديث الشهادة بنجاح";
				this.CertificateSaved?.Invoke();
				await LoadCertificatesAsync();
				LoadSuggestionsAsync();
				SelectedCertificateHistory = new ObservableCollection<AuditLog>(await _certificateRepository.GetCertificateHistoryAsync(certId));
				if (closeAfterSave)
				{
					CancelEdit();
				}
				base.IsBusy = false;
				SetNotification("تم التحديث", "تم تحديث كافة بيانات الشهادة وحفظ التغييرات بنجاح.", NotificationType.Success);
				return true;
			}
			base.IsBusy = false;
			SetNotification("فشل التحديث", "حدث خطأ أثناء محاولة تحديث بيانات الشهادة. يرجى المحاولة مرة أخرى.", NotificationType.Error);
			base.StatusMessage = "فشل في تحديث الشهادة";
			return false;
		}
		catch (Exception ex2)
		{
			Exception ex3 = ex2;
			base.IsBusy = false;
			base.StatusMessage = "خطأ: " + ex3.Message;
			_notificationService.ShowError("حدث خطأ غير متوقع: " + ex3.Message);
			return false;
		}
	}

	private void AddSample()
	{
		int root = 1;
		if (Samples.Any())
		{
			root = Samples.Max((Sample s) => s.Root) + 1;
		}
		Sample item = new Sample
		{
			Root = root,
			SampleNumber = string.Empty,
			Description = string.Empty,
			MeasurementDate = DateTime.Now,
			Result = string.Empty
		};
		Samples.Add(item);
	}

	private void RemoveSample()
	{
		if (SelectedSample != null)
		{
			Samples.Remove(SelectedSample);
		}
	}

	private void CancelEdit()
	{
		this.RequestNavigation?.Invoke(NavigationDestination.Certificates);
		IsEditing = false;
		IsViewingDetails = false;
		ClearCertificateForm();
	}

	public CertificatesViewModel(CertificateRepository certificateRepository, IPdfService pdfService, INotificationService notificationService, ExcelExportService excelExportService, UserService userService, SampleReceptionRepository sampleReceptionRepository, IReceptionSearchService receptionSearchService)
	{
		_certificateRepository = certificateRepository;
		_pdfService = pdfService;
		_notificationService = notificationService;
		_excelExportService = excelExportService;
		_userService = userService;
		_sampleReceptionRepository = sampleReceptionRepository;
		_receptionSearchService = receptionSearchService;
		_certificates = new ObservableCollection<Certificate>();
		_samples = new ObservableCollection<Sample>();
		_selectedCertificateHistory = new ObservableCollection<AuditLog>();
		PendingReceptions = new ObservableCollection<SampleReception>();
		InitializeCommands();
		LoadSuggestionsAsync();
	}

	private void InitializeCommands()
	{
		AddCertificateCommand = new RelayCommand(delegate
		{
			StartAddCertificate();
		});
		EditCertificateCommand = new AsyncRelayCommand((object? _) => StartEditCertificate(), (object? _) => SelectedCertificate != null && (_userService.CurrentUser?.CanEditCertificates ?? false));
		SaveCertificateCommand = new AsyncRelayCommand((Func<object?, Task>)async delegate
		{
			await SaveCertificateAsync();
		}, (Predicate<object?>?)((object? _) => CanSaveCertificate()), (AsyncRelayCommand.IErrorHandler?)null);
		CancelEditCommand = new RelayCommand(delegate
		{
			CancelEdit();
		});
		AddSampleCommand = new RelayCommand(delegate
		{
			AddSample();
		});
		RemoveSampleCommand = new RelayCommand(delegate
		{
			RemoveSample();
		}, (object? _) => SelectedSample != null);
		ViewCertificateCommand = new AsyncRelayCommand((Func<object?, Task>)async delegate
		{
			await ViewCertificateAsync();
		}, (Predicate<object?>?)((object? _) => SelectedCertificate != null), (AsyncRelayCommand.IErrorHandler?)null);
		CloseDetailsCommand = new RelayCommand(delegate
		{
			CloseDetails();
		});
		GeneratePdfCommand = new AsyncRelayCommand((Func<object?, Task>)async delegate
		{
			await GeneratePdfAsync();
		}, (Predicate<object?>?)((object? _) => SelectedCertificate != null), (AsyncRelayCommand.IErrorHandler?)null);
		SavePdfCommand = new AsyncRelayCommand((Func<object?, Task>)async delegate
		{
			await SavePdfAsync();
		}, (Predicate<object?>?)((object? _) => SelectedCertificate != null), (AsyncRelayCommand.IErrorHandler?)null);
		PrintCertificateCommand = new AsyncRelayCommand((Func<object?, Task>)async delegate
		{
			await PrintCertificateAsync();
		}, (Predicate<object?>?)((object? _) => SelectedCertificate != null), (AsyncRelayCommand.IErrorHandler?)null);
		SearchCommand = new AsyncRelayCommand((Func<object?, Task>)async delegate
		{
			await SearchCertificatesAsync(isManualSearch: true);
		}, (Predicate<object?>?)null, (AsyncRelayCommand.IErrorHandler?)null);
		LoadCertificatesCommand = new AsyncRelayCommand((Func<object?, Task>)async delegate
		{
			CurrentPage = 1;
			await LoadCertificatesAsync();
		}, (Predicate<object?>?)null, (AsyncRelayCommand.IErrorHandler?)null);
		ExportToExcelCommand = new RelayCommand(delegate
		{
			ExportToExcel();
		}, (object? _) => Certificates.Count > 0);
		NextPageCommand = new AsyncRelayCommand((Func<object?, Task>)async delegate
		{
			await NextPageAsync();
		}, (Predicate<object?>?)((object? _) => IsLastPage), (AsyncRelayCommand.IErrorHandler?)null);
		PreviousPageCommand = new AsyncRelayCommand((Func<object?, Task>)async delegate
		{
			await PreviousPageAsync();
		}, (Predicate<object?>?)((object? _) => IsFirstPage), (AsyncRelayCommand.IErrorHandler?)null);
	}

	private void ExportToExcel()
	{
		try
		{
			SaveFileDialog saveFileDialog = new SaveFileDialog
			{
				Filter = "CSV Files (*.csv)|*.csv",
				DefaultExt = ".csv",
				FileName = $"Certificates_Export_{DateTime.Now:yyyyMMdd}"
			};
			if (saveFileDialog.ShowDialog() == true)
			{
				if (_excelExportService.ExportCertificatesToCsv(Certificates, saveFileDialog.FileName))
				{
					base.StatusMessage = "طھظ… طھطµط\u00afظٹط± ط§ظ„ط\u00a8ظٹط§ظ†ط§طھ ط\u00a8ظ†ط¬ط§ط\u00ad";
					_notificationService.ShowSuccess($"طھظ… طھطµط\u00afظٹط± {Certificates.Count} ط\u00b4ظ‡ط§ط\u00afط©");
				}
				else
				{
					base.StatusMessage = "ظپط\u00b4ظ„ ظپظٹ طھطµط\u00afظٹط± ط§ظ„ط\u00a8ظٹط§ظ†ط§طھ";
					_notificationService.ShowError("ظپط\u00b4ظ„ ط§ظ„طھطµط\u00afظٹط±");
				}
			}
		}
		catch (Exception ex)
		{
			base.StatusMessage = "ط®ط·ط£: " + ex.Message;
			_notificationService.ShowError("ط®ط·ط£ ظپظٹ ط§ظ„طھطµط\u00afظٹط±");
		}
	}

	private async Task GeneratePdfAsync()
	{
		if (SelectedCertificate == null)
		{
			return;
		}
		try
		{
			base.IsBusy = true;
			base.BusyMessage = "جاري تحضير ملف PDF...";
			List<Sample> samples = await _certificateRepository.GetSamplesByCertificateIdAsync(SelectedCertificate.Id);
			if (await Task.Run(() => _pdfService.GenerateAndOpenCertificatePdf(SelectedCertificate, samples)))
			{
				base.StatusMessage = "تم إنشاء PDF بنجاح";
				_notificationService.ShowSuccess("تم إنشاء الشهادة");
			}
			else
			{
				base.StatusMessage = "فشل في إنشاء PDF";
				_notificationService.ShowError("فشل إنشاء الشهادة");
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			base.StatusMessage = "خطأ: " + ex2.Message;
			_notificationService.ShowError("حدث خطأ أثناء إنشاء PDF");
		}
		finally
		{
			base.IsBusy = false;
		}
	}

	private async Task SavePdfAsync()
	{
		if (SelectedCertificate == null)
		{
			return;
		}
		try
		{
			SaveFileDialog saveDialog = new SaveFileDialog
			{
				Filter = "PDF Files (*.pdf)|*.pdf",
				FileName = $"Certificate_{SelectedCertificate.CertificateNumber}_{DateTime.Now:yyyyMMdd}.pdf"
			};
			if (saveDialog.ShowDialog() == true)
			{
				base.IsBusy = true;
				base.BusyMessage = "جاري حفظ ملف PDF...";
				List<Sample> samples = await _certificateRepository.GetSamplesByCertificateIdAsync(SelectedCertificate.Id);
				if (await Task.Run(() => _pdfService.SaveCertificatePdf(SelectedCertificate, samples, saveDialog.FileName)))
				{
					base.StatusMessage = "تم حفظ PDF بنجاح";
					_notificationService.ShowSuccess("تم حفظ الشهادة في: " + saveDialog.FileName);
				}
				else
				{
					base.StatusMessage = "فشل في حفظ PDF";
					_notificationService.ShowError("فشل حفظ الشهادة");
				}
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			base.StatusMessage = "خطأ: " + ex2.Message;
			_notificationService.ShowError("حدث خطأ أثناء حفظ PDF");
		}
		finally
		{
			base.IsBusy = false;
		}
	}

	private async Task PrintCertificateAsync()
	{
		if (SelectedCertificate == null)
		{
			return;
		}
		try
		{
			if (!IsEditing || await SaveCertificateAsync(closeAfterSave: false))
			{
				base.IsBusy = true;
				base.BusyMessage = "جاري إرسال الشهادة للطباعة...";
				List<Sample> samples = await _certificateRepository.GetSamplesByCertificateIdAsync(SelectedCertificate.Id);
				if (await Task.Run(() => _pdfService.PrintCertificatePdf(SelectedCertificate, samples)))
				{
					base.StatusMessage = "تم إرسال الشهادة للطباعة";
					_notificationService.ShowSuccess("تم إرسال الشهادة للطباعة");
				}
				else
				{
					base.StatusMessage = "فشل في طباعة الشهادة";
					_notificationService.ShowError("فشل في طباعة الشهادة");
				}
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			base.StatusMessage = "خطأ: " + ex2.Message;
			_notificationService.ShowError("حدث خطأ أثناء الطباعة");
		}
		finally
		{
			base.IsBusy = false;
		}
	}

	private async Task ViewCertificateAsync()
	{
		if (SelectedCertificate == null)
		{
			return;
		}
		try
		{
			base.IsBusy = true;
			base.BusyMessage = "جاري تحميل تفاصيل الشهادة...";
			Samples = new ObservableCollection<Sample>(await _certificateRepository.GetSamplesByCertificateIdAsync(SelectedCertificate.Id));
			SelectedCertificateHistory = new ObservableCollection<AuditLog>(await _certificateRepository.GetCertificateHistoryAsync(SelectedCertificate.Id));
			IsViewingDetails = true;
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			base.StatusMessage = "خطأ في تحميل تفاصيل الشهادة: " + ex2.Message;
		}
		finally
		{
			base.IsBusy = false;
		}
	}

	public void CloseDetails()
	{
		IsViewingDetails = false;
	}

	public async Task LoadSuggestionsAsync()
	{
		try
		{
			foreach (string s in await _certificateRepository.GetDistinctFieldValuesAsync("Sender"))
			{
				if (!AvailableSenders.Contains(s))
				{
					AvailableSenders.Add(s);
				}
			}
			List<string> analysisTypes = await _certificateRepository.GetDistinctFieldValuesAsync("AnalysisType");
			if (!analysisTypes.Contains("تحليل مبدئي (دون الوصول لحالة الاتزان)"))
			{
				analysisTypes.Insert(0, "تحليل مبدئي (دون الوصول لحالة الاتزان)");
			}
			UpdateCollection(AvailableAnalysisTypes, analysisTypes);
			List<string> results = await _certificateRepository.GetDistinctFieldValuesAsync("Result");
			if (!results.Contains("خالية من العناصر المشعة المصنعة"))
			{
				results.Add("خالية من العناصر المشعة المصنعة");
			}
			UpdateCollection(AvailableResults, results);
			UpdateCollection(newValues: await _certificateRepository.GetDistinctFieldValuesAsync("RecipientName"), collection: AvailableRecipientNames);
			UpdateCollection(newValues: await _certificateRepository.GetDistinctFieldValuesAsync("Supplier"), collection: AvailableSuppliers);
			UpdateCollection(newValues: await _certificateRepository.GetDistinctFieldValuesAsync("Origin"), collection: AvailableOrigins);
			UpdateCollection(newValues: await _certificateRepository.GetDistinctFieldValuesAsync("SpecialistName"), collection: AvailableSpecialistNames);
			UpdateCollection(newValues: await _certificateRepository.GetDistinctFieldValuesAsync("SectionHeadName"), collection: AvailableSectionHeadNames);
			UpdateCollection(newValues: await _certificateRepository.GetDistinctFieldValuesAsync("ManagerName"), collection: AvailableManagerNames);
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			LoggerService.LogError("Failed to load AutoComplete suggestions", ex2);
		}
	}

	private void UpdateCollection(ObservableCollection<string> collection, List<string> newValues)
	{
		collection.Clear();
		foreach (string newValue in newValues)
		{
			if (!string.IsNullOrWhiteSpace(newValue))
			{
				collection.Add(newValue);
			}
		}
	}
}
