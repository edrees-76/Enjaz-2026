using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Enjaz.Helpers;
using Enjaz.Models;
using Enjaz.Services;
using Enjaz.Services.Repositories;

namespace Enjaz.ViewModels
{
    public partial class CertificatesViewModel : BaseViewModel
    {
        private readonly CertificateRepository _certificateRepository;
        private readonly ISampleRepository _sampleRepository;
        private readonly IPdfService _pdfService;
        private readonly ExcelExportService _excelExportService;
        private readonly UserService _userService;
        private readonly SampleReceptionRepository _sampleReceptionRepository;
        private readonly INotificationService _notificationService;
        private readonly IReceptionSearchService _receptionSearchService;
        
        private System.Threading.CancellationTokenSource? _searchCancellationTokenSource;

        // --- Sample Uniqueness Validation Fields ---
        private bool _isLoadingCertificate = false;
        private readonly HashSet<string> _notifiedDeletedSamples = new();
        private CancellationTokenSource? _sampleValidationCts;

        public event Action<NavigationDestination>? RequestNavigation;
        public event Action? CertificateSaved;


        private ObservableCollection<Certificate> _certificates;
        private Certificate? _selectedCertificate;
        private ObservableCollection<Sample> _samples;
        private Sample? _selectedSample;
        private string _searchText = string.Empty;
        private string _searchCriteria = "الكل";
        private DateTime? _searchStartDate;
        private DateTime? _searchEndDate;
        private bool _isEditing;
        private int _currentPage = 1;
        private int _pageSize = 50;
        private int _totalRecords = 0;
        private int _totalPages = 0;
        private ObservableCollection<AuditLog> _selectedCertificateHistory;

        // Certificate Form Properties
        private string _recipientName = string.Empty;
        private string _certificateType = string.Empty;
        private string _description = string.Empty;
        private DateTime _issueDate = DateTime.Now;
        private DateTime? _expiryDate;
        private string _issuingAuthority = string.Empty;

        // Extended Properties
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

        public CertificatesViewModel(CertificateRepository certificateRepository, ISampleRepository sampleRepository, IPdfService pdfService, INotificationService notificationService, ExcelExportService excelExportService, UserService userService, SampleReceptionRepository sampleReceptionRepository, IReceptionSearchService receptionSearchService)
        {
            _certificateRepository = certificateRepository;
            _sampleRepository = sampleRepository;
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
            _ = LoadSuggestionsAsync();
        }

        #region Properties

        public string SectionHeadName
        {
            get => _sectionHeadName;
            set => SetProperty(ref _sectionHeadName, value);
        }

        public string ManagerName
        {
            get => _managerName;
            set => SetProperty(ref _managerName, value);
        }



        public ObservableCollection<Certificate> Certificates
        {
            get => _certificates;
            set => SetProperty(ref _certificates, value);
        }

        public Certificate? SelectedCertificate
        {
            get => _selectedCertificate;
            set
            {
                SetProperty(ref _selectedCertificate, value);
                OnPropertyChanged(nameof(IsSelectedCertificateEnvironmental));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value) && !_isInternalClear)
                {
                    _searchCancellationTokenSource?.Cancel();
                    _searchCancellationTokenSource = new System.Threading.CancellationTokenSource();
                    var token = _searchCancellationTokenSource.Token;

                    System.Threading.Tasks.Task.Delay(400, token).ContinueWith(t =>
                    {
                        if (!t.IsCanceled)
                        {
                            Application.Current.Dispatcher.Invoke(async () => await SearchCertificatesAsync(false));
                        }
                    }, token);
                }
            }
        }

        public string SearchCriteria
        {
            get => _searchCriteria;
            set
            {
                if (SetProperty(ref _searchCriteria, value))
                {
                    _ = SearchCertificatesAsync(false);
                }
            }
        }

        public DateTime? SearchStartDate
        {
            get => _searchStartDate;
            set
            {
                if (SetProperty(ref _searchStartDate, value))
                {
                    if (SearchCriteria == "التاريخ") _ = SearchCertificatesAsync(false);
                }
            }
        }

        public DateTime? SearchEndDate
        {
            get => _searchEndDate;
            set
            {
                if (SetProperty(ref _searchEndDate, value))
                {
                    if (SearchCriteria == "التاريخ") _ = SearchCertificatesAsync(false);
                }
            }
        }


        public ObservableCollection<AuditLog> SelectedCertificateHistory
        {
            get => _selectedCertificateHistory;
            set => SetProperty(ref _selectedCertificateHistory, value);
        }

        public MaterialDesignThemes.Wpf.ISnackbarMessageQueue MessageQueue => _notificationService.MessageQueue;

        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        private bool _isViewingDetails;
        public bool IsViewingDetails
        {
            get => _isViewingDetails;
            set => SetProperty(ref _isViewingDetails, value);
        }

        private bool _isDeletedRecordsView;
        public bool IsDeletedRecordsView
        {
            get => _isDeletedRecordsView;
            set => SetProperty(ref _isDeletedRecordsView, value);
        }

        // Form Properties
        public string RecipientName 
        { 
            get => _recipientName; 
            set 
            {
                SetProperty(ref _recipientName, value);
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string CertificateType 
        { 
            get => _certificateType;
            set 
            {
                if (SetProperty(ref _certificateType, value))
                {
                    OnPropertyChanged(nameof(IsSampleCertificate));
                    OnPropertyChanged(nameof(IsAnalysisTypeVisible));
                    OnPropertyChanged(nameof(IsEnvironmentalCertificate));

                    if (IsEnvironmentalCertificate && string.IsNullOrEmpty(AnalysisType))
                    {
                        AnalysisType = "تحليل مبدئي (دون الوصول لحالة الاتزان)";
                    }
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string Description { get => _description; set => SetProperty(ref _description, value); }
        public DateTime IssueDate
        {
            get => _issueDate;
            set
            {
                int oldYear = _issueDate.Year;
                if (SetProperty(ref _issueDate, value))
                {
                    if (!_isLoadingCertificate && oldYear != _issueDate.Year)
                    {
                        _ = RevalidateAllSamplesAsync();
                    }
                }
            }
        }
        public DateTime? ExpiryDate { get => _expiryDate; set => SetProperty(ref _expiryDate, value); }
        public string IssuingAuthority 
        { 
            get => _issuingAuthority; 
            set 
            {
                SetProperty(ref _issuingAuthority, value);
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string AnalysisType { get => _analysisType; set => SetProperty(ref _analysisType, value); }
        public string Sender
        {
            get => _sender;
            set
            {
                if (SetProperty(ref _sender, value))
                {
                    if (!_isLoadingCertificate)
                    {
                        _ = RevalidateAllSamplesAsync();
                    }
                }
            }
        }
        public string Supplier { get => _supplier; set => SetProperty(ref _supplier, value); }
        public string Origin { get => _origin; set => SetProperty(ref _origin, value); }
        public string DeclarationNumber { get => _declarationNumber; set => SetProperty(ref _declarationNumber, value); }
        public string PolicyNumber { get => _policyNumber; set => SetProperty(ref _policyNumber, value); }
        public string NotificationNumber { get => _notificationNumber; set => SetProperty(ref _notificationNumber, value); }
        public string FinancialReceiptNumber { get => _financialReceiptNumber; set => SetProperty(ref _financialReceiptNumber, value); }
        public string SpecialistName { get => _specialistName; set => SetProperty(ref _specialistName, value); }
        public string Notes { get => _notes; set => SetProperty(ref _notes, value); }

        public bool IsEnvironmentalCertificate => CertificateType != null && (CertificateType.Contains("بيئية") || CertificateType.Contains("بيئيه") || CertificateType.Contains("بيئيه"));
        public bool IsSampleCertificate => CertificateType != null && (CertificateType.Contains("بيئة") || CertificateType.Contains("تحليل") || CertificateType.Contains("عينة") || CertificateType.Contains("عينات"));
        public bool IsAnalysisTypeVisible => CertificateType != null && !CertificateType.Contains("استهلاكية");

        public ObservableCollection<string> SearchCriteriaList { get; } = new ObservableCollection<string>
        {
            "الكل", "رقم العينة", "رقم الشهادة", "رقم الاخطار", "رقم الاقرار الجمركى", "الجهة المرسلة", "المورد", "رقم الايصال المالى", "التاريخ", "رقم البوليصة", "اسم المستخدم"
        };
        
        public ObservableCollection<string> AvailableSenders { get; } = new ObservableCollection<string>
        {
            "مركز الرقابة على الأغذية والأدوية - طرابلس",
            "مركز الرقابة على الأغذية والأدوية - بنغازي",
            "مركز الرقابة على الأغذية والأدوية - مصراتة",
            "مركز الرقابة على الأغذية والأدوية - الخمس",
            "مركز الرقابة على الأغذية والأدوية - زوارة",
            "مركز الرقابة على الأغذية والأدوية - البطنان"
        };
        public ObservableCollection<string> AvailableResults { get; } = new ObservableCollection<string>
        {
            "خالية من العناصر المشعة المصنعة"
        };

        public ObservableCollection<string> AvailableAnalysisTypes { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> AvailableRecipientNames { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> AvailableSuppliers { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> AvailableOrigins { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> AvailableSpecialistNames { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> AvailableSectionHeadNames { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> AvailableManagerNames { get; } = new ObservableCollection<string>();

        public ObservableCollection<SampleReception> PendingReceptions { get; }
        
        private SampleReception? _selectedPendingReception;
        public SampleReception? SelectedPendingReception
        {
            get => _selectedPendingReception;
            set
            {
                if (SetProperty(ref _selectedPendingReception, value) && value != null)
                {
                    LoadFromReception(value);
                }
            }
        }

        public ObservableCollection<Sample> Samples
        {
            get => _samples;
            set => SetProperty(ref _samples, value);
        }

        public Sample? SelectedSample
        {
            get => _selectedSample;
            set
            {
                SetProperty(ref _selectedSample, value);
                CommandManager.InvalidateRequerySuggested();
            }
        }

        /// <summary>
        /// تحديد نوع الشهادة المختارة للعرض
        /// Determine if selected certificate is environmental
        /// </summary>
        public int TotalConsumableCertificates
        {
            get => _totalConsumableCertificates;
            set => SetProperty(ref _totalConsumableCertificates, value);
        }

        public int TotalEnvironmentalCertificates
        {
            get => _totalEnvironmentalCertificates;
            set => SetProperty(ref _totalEnvironmentalCertificates, value);
        }

        public int TotalSamplesSum
        {
            get => _totalSamplesSum;
            set => SetProperty(ref _totalSamplesSum, value);
        }

        public bool IsSelectedCertificateEnvironmental
        {
            get
            {
                if (SelectedCertificate == null) return false;
                return SelectedCertificate.CertificateType != null && 
                       (SelectedCertificate.CertificateType.Contains("بيئية") || 
                        SelectedCertificate.CertificateType.Contains("بيئيه") || 
                        SelectedCertificate.CertificateType.Contains("بيييه"));
            }
        }

        #region Pagination Properties

        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    OnPropertyChanged(nameof(IsFirstPage));
                    OnPropertyChanged(nameof(IsLastPage));
                    OnPropertyChanged(nameof(PageSummary));
                }
            }
        }

        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (SetProperty(ref _pageSize, value))
                {
                     CurrentPage = 1; 
                     _ = LoadCertificatesAsync();
                }
            }
        }

        public int TotalRecords
        {
            get => _totalRecords;
            set
            {
                if (SetProperty(ref _totalRecords, value))
                {
                     OnPropertyChanged(nameof(PageSummary));
                }
            }
        }

        public int TotalPages
        {
            get => _totalPages;
            set
            {
                if (SetProperty(ref _totalPages, value))
                {
                    OnPropertyChanged(nameof(IsLastPage));
                    OnPropertyChanged(nameof(PageSummary));
                }
            }
        }

        public bool IsFirstPage => CurrentPage > 1;
        public bool IsLastPage => CurrentPage < TotalPages;

        public string PageSummary => $"صفحة {CurrentPage} من {TotalPages} (العدد الكلي: {TotalRecords})";

        #endregion

        #endregion

        #region Commands

        public ICommand AddCertificateCommand { get; private set; } = null!;
        public ICommand EditCertificateCommand { get; private set; } = null!;
        public ICommand SaveCertificateCommand { get; private set; } = null!;
        public ICommand CancelEditCommand { get; private set; } = null!;
        public ICommand AddSampleCommand { get; private set; } = null!;
        public ICommand RemoveSampleCommand { get; private set; } = null!;
        public ICommand ViewCertificateCommand { get; private set; } = null!;
        public ICommand CloseDetailsCommand { get; private set; } = null!;


        public ICommand GeneratePdfCommand { get; private set; } = null!;
        public ICommand SavePdfCommand { get; private set; } = null!;
        public ICommand PrintCertificateCommand { get; private set; } = null!;
        public ICommand SearchCommand { get; private set; } = null!;
        public ICommand LoadCertificatesCommand { get; private set; } = null!;
        public ICommand ExportToExcelCommand { get; private set; } = null!;
        public ICommand NextPageCommand { get; private set; } = null!;
        public ICommand PreviousPageCommand { get; private set; } = null!;


        private void InitializeCommands()
        {
            AddCertificateCommand = new RelayCommand(_ => StartAddCertificate());
            EditCertificateCommand = new AsyncRelayCommand(_ => StartEditCertificate(), _ => SelectedCertificate != null && (_userService.CurrentUser?.CanEditCertificates == true));
            SaveCertificateCommand = new AsyncRelayCommand(async _ => await SaveCertificateAsync(true), _ => CanSaveCertificate());
            CancelEditCommand = new RelayCommand(_ => CancelEdit());
            
            AddSampleCommand = new RelayCommand(_ => AddSample());
            RemoveSampleCommand = new RelayCommand(_ => RemoveSample(), _ => SelectedSample != null);

            ViewCertificateCommand = new AsyncRelayCommand(async _ => await ViewCertificateAsync(), _ => SelectedCertificate != null);
            CloseDetailsCommand = new RelayCommand(_ => CloseDetails());



            GeneratePdfCommand = new AsyncRelayCommand(async _ => await GeneratePdfAsync(), _ => SelectedCertificate != null);
            SavePdfCommand = new AsyncRelayCommand(async _ => await SavePdfAsync(), _ => SelectedCertificate != null);
            PrintCertificateCommand = new AsyncRelayCommand(async _ => await PrintCertificateAsync(), _ => SelectedCertificate != null);
            
            SearchCommand = new AsyncRelayCommand(async _ => await SearchCertificatesAsync(true));
            LoadCertificatesCommand = new AsyncRelayCommand(async _ => { CurrentPage = 1; await LoadCertificatesAsync(); });
            ExportToExcelCommand = new RelayCommand(_ => ExportToExcel(), _ => Certificates.Count > 0);
            
            NextPageCommand = new AsyncRelayCommand(async _ => await NextPageAsync(), _ => IsLastPage);
            PreviousPageCommand = new AsyncRelayCommand(async _ => await PreviousPageAsync(), _ => IsFirstPage);
        }

        // Removed redundant CloseNotificationCommand - now in BaseViewModel

        /// <summary>
        /// تصدير الشهادات إلى Excel
        /// </summary>
        private void ExportToExcel()
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv",
                    DefaultExt = ".csv",
                    FileName = $"Certificates_Export_{DateTime.Now:yyyyMMdd}"
                };

                if (dialog.ShowDialog() == true)
                {
                    if (_excelExportService.ExportCertificatesToCsv(Certificates, dialog.FileName))
                    {
                        StatusMessage = "تم تصدير البيانات بنجاح";
                        _notificationService.ShowSuccess($"تم تصدير {Certificates.Count} شهادة");
                    }
                    else
                    {
                        StatusMessage = "فشل في تصدير البيانات";
                        _notificationService.ShowError("فشل التصدير");
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطأ: {ex.Message}";
                _notificationService.ShowError("خطأ في التصدير");
            }
        }

        #endregion

        #region Sample Uniqueness Validation — فحص تفرد رقم العينة

        private void Samples_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (Sample s in e.NewItems)
                {
                    s.PropertyChanged -= Sample_PropertyChanged;
                    s.PropertyChanged += Sample_PropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (Sample s in e.OldItems)
                {
                    s.PropertyChanged -= Sample_PropertyChanged;
                }
            }
        }

        private async void Sample_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_isLoadingCertificate) return;
            if (sender is not Sample sample) return;

            if (e.PropertyName == nameof(Sample.SampleNumber))
            {
                // إلغاء أي فحص سابق لمنع Race Condition
                _sampleValidationCts?.Cancel();
                var cts = new CancellationTokenSource();
                _sampleValidationCts = cts;

                try
                {
                    await ValidateSingleSampleAsync(sample, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // تم إلغاء الفحص — فحص أحدث بدأ
                }
            }
        }

        private async Task<bool> ValidateSingleSampleAsync(Sample sample, CancellationToken cancellationToken = default)
        {
            if (_isLoadingCertificate) return true;
            if (string.IsNullOrWhiteSpace(sample.SampleNumber)) return true;

            // حفظ القيمة قبل أي await لمنع مسح قيمة تغيرت أثناء الفحص
            string capturedValue = sample.SampleNumber;
            string norm = SampleValidationHelper.NormalizeSampleNumber(capturedValue);
            if (string.IsNullOrEmpty(norm)) return true;

            // 1. فحص التكرار الداخلي (ضمن جدول الشهادة الحالية) — بالرقم المطبّع
            int internalMatches = Samples.Count(s =>
                !string.IsNullOrWhiteSpace(s.SampleNumber) &&
                SampleValidationHelper.NormalizeSampleNumber(s.SampleNumber) == norm);

            if (internalMatches > 1)
            {
                // التحقق أن القيمة لم تتغير بعد الفحص
                if (sample.SampleNumber != capturedValue) return true;

                sample.SampleNumber = string.Empty;
                SetNotification("تكرار رقم العينة",
                    $"رقم العينة ({norm}) مكرر في نفس جدول الشهادة الحالية.\nيرجى كتابة رقم عينة غير مكرر.",
                    NotificationType.Error, "AlertCircleOutline");
                return false;
            }

            // 2. التحقق من توفر الجهة وتاريخ الإصدار للفحص في قاعدة البيانات
            if (string.IsNullOrWhiteSpace(Sender) || IssueDate == default)
            {
                return true; // سيتم الفحص لاحقاً عند اختيار الجهة أو عند الحفظ
            }

            cancellationToken.ThrowIfCancellationRequested();

            var result = await _certificateRepository.CheckSampleUniquenessAsync(
                capturedValue,
                Sender,
                IssueDate.Year,
                SelectedCertificate?.Id);

            cancellationToken.ThrowIfCancellationRequested();

            // التحقق أن القيمة لم تتغير أثناء await
            if (sample.SampleNumber != capturedValue) return true;

            if (result.Status == SampleCheckResult.DuplicateActive)
            {
                sample.SampleNumber = string.Empty;
                SetNotification("تكرار رقم العينة",
                    $"إن رقم العينة ({norm}) مسجل مسبقاً للجهة ({result.Sender ?? Sender})\nفي الشهادة رقم ({result.CertificateNumber ?? "غير محدد"})\nبتاريخ ({result.IssueDate:yyyy/MM/dd}).\n\nيرجى التأكد من الرقم والمحاولة مرة أخرى.",
                    NotificationType.Error, "AlertCircleOutline");
                return false;
            }
            else if (result.Status == SampleCheckResult.FoundInDeleted)
            {
                // مفتاح مركب: رقم مطبّع + سنة + جهة مطبّعة
                string key = $"{norm}_{IssueDate.Year}_{SampleValidationHelper.NormalizeSender(Sender)}";
                if (!_notifiedDeletedSamples.Contains(key))
                {
                    _notifiedDeletedSamples.Add(key);
                    SetNotification("تنبيه - عينة لشهادة محذوفة",
                        $"تنبيه:\nرقم العينة ({norm}) كان مسجلاً سابقاً للجهة ({result.Sender ?? Sender})\nضمن الشهادة رقم ({result.CertificateNumber ?? "محذوفة"})\nبتاريخ ({result.IssueDate:yyyy/MM/dd})، ولكن هذه الشهادة محذوفة حالياً.\n\nيمكن استخدام رقم العينة في الشهادة الحالية.",
                        NotificationType.Information, "InformationOutline");
                }
            }

            return true;
        }

        private async Task RevalidateAllSamplesAsync()
        {
            if (_isLoadingCertificate || Samples == null || Samples.Count == 0 || string.IsNullOrWhiteSpace(Sender))
                return;

            var conflicts = new List<string>();

            foreach (var sample in Samples)
            {
                if (string.IsNullOrWhiteSpace(sample.SampleNumber)) continue;

                string norm = SampleValidationHelper.NormalizeSampleNumber(sample.SampleNumber);
                if (string.IsNullOrEmpty(norm)) continue;

                var result = await _certificateRepository.CheckSampleUniquenessAsync(
                    sample.SampleNumber,
                    Sender,
                    IssueDate.Year,
                    SelectedCertificate?.Id);

                if (result.Status == SampleCheckResult.DuplicateActive)
                {
                    conflicts.Add($"• العينة ({norm}) مسجلة في الشهادة ({result.CertificateNumber}) بتاريخ ({result.IssueDate:yyyy/MM/dd})");
                    sample.SampleNumber = string.Empty;
                }
            }

            if (conflicts.Count > 0)
            {
                SetNotification("تعارض في أرقام العينات",
                    $"تم العثور على تعارضات في أرقام العينات مع الجهة المحددة ({Sender}) لعام ({IssueDate.Year}):\n\n" +
                    string.Join("\n", conflicts) +
                    "\n\nتم تفريغ أرقام العينات المتعارضة، يرجى تصحيحها.",
                    NotificationType.Error, "AlertCircleOutline");
            }
        }

        #endregion
    }
}
