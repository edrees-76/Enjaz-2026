using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Enjaz.Helpers;
using Enjaz.Models;
using Enjaz.Services;
using Enjaz.Services.Repositories;

namespace Enjaz.ViewModels
{
    public class SampleReceptionsViewModel : BaseViewModel
    {
        private readonly SampleReceptionRepository _receptionRepository;
        private readonly CertificateRepository _certificateRepository;
        private readonly ReportingService _reportingService;
        private readonly INotificationService _alertService;
        private readonly UserService _userService;

        public event Action<NavigationDestination>? RequestNavigation;

        // --- Sample Uniqueness Validation Fields ---
        private readonly HashSet<string> _notifiedDeletedSamples = new();
        private CancellationTokenSource? _sampleValidationCts;
        private bool _isLoadingReception = false;

        public SampleReceptionsViewModel(SampleReceptionRepository receptionRepository, 
                                       CertificateRepository certificateRepository,
                                       ReportingService reportingService, 
                                       INotificationService alertService,
                                       UserService userService)
        {
            _receptionRepository = receptionRepository;
            _certificateRepository = certificateRepository;
            _reportingService = reportingService;
            _alertService = alertService;
            _userService = userService;

            Receptions = new ObservableCollection<SampleReception>();
            SendersList = new ObservableCollection<string>();
            SearchCriteriaList = new ObservableCollection<string> { "رقم طلب التحليل", "الجهة المرسلة", "المورد", "التاريخ" };
            SearchCriteria = "رقم طلب التحليل";

            AddReceptionCommand = new RelayCommand(_ => AddReception());
            ConfirmTypeCommand = new RelayCommand(_ => ConfirmType(), _ => !string.IsNullOrEmpty(SelectedCertificateType));
            EditReceptionCommand = new RelayCommand(_ => EditReception(), _ => SelectedReception != null);
            SaveReceptionCommand = new AsyncRelayCommand(async _ => await SaveReceptionAsync());
            CancelEditCommand = new RelayCommand(_ => CancelEdit());
            DeleteReceptionCommand = new AsyncRelayCommand(async _ => await DeleteReceptionAsync(), _ => SelectedReception != null);

            SearchCommand = new AsyncRelayCommand(async _ => await SearchAsync());
            ClearSearchCommand = new AsyncRelayCommand(async _ => await LoadReceptionsAsync());

            ViewDetailsCommand = new AsyncRelayCommand(async _ => await ViewDetailsAsync(), _ => SelectedReception != null);
            CloseDetailsCommand = new RelayCommand(_ => CloseDetails());
            
            AddSampleCommand = new AsyncRelayCommand(async _ => await AddSampleAsync());
            RemoveSampleCommand = new RelayCommand(RemoveSample);

            NextPageCommand = new RelayCommand(_ => { if (CurrentPage < TotalPages) CurrentPage++; });
            PreviousPageCommand = new RelayCommand(_ => { if (CurrentPage > 1) CurrentPage--; });
        }

        // --- Properties ---
        public ObservableCollection<string> SendersList { get; }

        private string _newSampleNumber = string.Empty;
        public string NewSampleNumber
        {
            get => _newSampleNumber;
            set => SetProperty(ref _newSampleNumber, value);
        }

        private string _newSampleDescription = string.Empty;
        public string NewSampleDescription
        {
            get => _newSampleDescription;
            set => SetProperty(ref _newSampleDescription, value);
        }

        public ObservableCollection<SampleReception> Receptions { get; }
        public ObservableCollection<SampleReception> PagedReceptions { get; } = new ObservableCollection<SampleReception>();

        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    UpdatePagedReceptions();
                }
            }
        }

        private int _itemsPerPage = 10;
        public int ItemsPerPage
        {
            get => _itemsPerPage;
            set
            {
                if (SetProperty(ref _itemsPerPage, value))
                {
                    CurrentPage = 1;
                    UpdatePagedReceptions();
                }
            }
        }

        private int _totalPages;
        public int TotalPages
        {
            get => _totalPages;
            private set => SetProperty(ref _totalPages, value);
        }

        // --- Totals ---
        public int TotalSamplesSum => Receptions.Sum(r => r.SampleCount);
        public int TotalConsumableSamples => Receptions.Where(r => r.CertificateType.Contains("استهلاكية")).Sum(r => r.SampleCount);
        public int TotalEnvironmentalSamples => Receptions.Where(r => r.CertificateType.Contains("بيئية")).Sum(r => r.SampleCount);
        
        private SampleReception? _selectedReception;
        public SampleReception? SelectedReception
        {
            get => _selectedReception;
            set
            {
                SetProperty(ref _selectedReception, value);
            }
        }

        private SampleReception? _editingReception;
        public SampleReception? EditingReception
        {
            get => _editingReception;
            set => SetProperty(ref _editingReception, value);
        }

        private bool _isEditing;
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

        private bool _isSelectingType;
        public bool IsSelectingType
        {
            get => _isSelectingType;
            set => SetProperty(ref _isSelectingType, value);
        }

        private string _selectedCertificateType = string.Empty;
        public string SelectedCertificateType
        {
            get => _selectedCertificateType;
            set => SetProperty(ref _selectedCertificateType, value);
        }

        // --- Search Properties ---
        public ObservableCollection<string> SearchCriteriaList { get; }
        private string _searchCriteria = string.Empty;
        public string SearchCriteria
        {
            get => _searchCriteria;
            set => SetProperty(ref _searchCriteria, value);
        }

        public ObservableCollection<string> CertificateStatusFilters { get; } = new ObservableCollection<string> { "الكل", "تم إصدار شهادة", "لم يتم إصدار شهادة" };

        private string _selectedCertificateStatusFilter = "الكل";
        public string SelectedCertificateStatusFilter
        {
            get => _selectedCertificateStatusFilter;
            set
            {
                if (SetProperty(ref _selectedCertificateStatusFilter, value))
                {
                    CurrentPage = 1;
                    UpdatePagedReceptions();
                }
            }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        private DateTime? _searchStartDate;
        public DateTime? SearchStartDate
        {
            get => _searchStartDate;
            set => SetProperty(ref _searchStartDate, value);
        }

        private DateTime? _searchEndDate;
        public DateTime? SearchEndDate
        {
            get => _searchEndDate;
            set => SetProperty(ref _searchEndDate, value);
        }

        // --- Commands ---
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


        // --- Methods ---

        public async Task LoadReceptionsAsync()
        {
            try
            {
                var list = await _receptionRepository.GetAllReceptionsAsync();
                
                Receptions.Clear();
                foreach (var r in list.OrderByDescending(x => x.CreatedAt))
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
                _alertService.ShowError("حدث خطأ أثناء تحميل الاستلامات: " + ex.Message);
            }
        }

        // قائمة الجهات المرسلة المعروفة (ثابتة وموحدة)
        private static readonly List<string> DefaultSenders = new List<string>
        {
            "مركز الرقابة على الأغذية والأدوية - طرابلس",
            "مركز الرقابة على الأغذية والأدوية - بنغازي",
            "مركز الرقابة على الأغذية والأدوية - مصراتة",
            "مركز الرقابة على الأغذية والأدوية - الخمس",
            "مركز الرقابة على الأغذية والأدوية - زوارة",
            "مركز الرقابة على الأغذية والأدوية - البطنان"
        };

        public async Task LoadSuggestionsAsync()
        {
            try
            {
                SendersList.Clear();

                // 1. إضافة الجهات المرسلة الثابتة (الموحدة)
                foreach (var s in DefaultSenders)
                {
                    SendersList.Add(s);
                }

                // 2. دمج أي جهات إضافية من قاعدة البيانات بعد التأكد من عدم تكرارها تطبيعياً
                var dbSenders = await _reportingService.GetUniqueSendersAsync();
                foreach (var s in dbSenders)
                {
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        string norm = SampleValidationHelper.NormalizeSender(s);
                        if (!SendersList.Any(existing => SampleValidationHelper.NormalizeSender(existing) == norm))
                        {
                            SendersList.Add(s.Trim());
                        }
                    }
                }
            }
            catch (Exception)
            {
                // في حالة فشل الاتصال بقاعدة البيانات، نضمن وجود القائمة الثابتة
                if (SendersList.Count == 0)
                {
                    foreach (var s in DefaultSenders)
                    {
                        SendersList.Add(s);
                    }
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
            string certType = SelectedCertificateType;
            EditingReception = new SampleReception
            {
                Date = DateTime.Now,
                CertificateType = certType,
                Status = "لم يتم إصدار شهادة"
            };
            _notifiedDeletedSamples.Clear();
            IsSelectingType = false;
            IsEditing = true;
            RequestNavigation?.Invoke(NavigationDestination.SampleReceptionForm);
        }

        private void EditReception()
        {
            if (SelectedReception == null) return;
            
            _isLoadingReception = true;
            try
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
                    Samples = new ObservableCollection<ReceptionSample>(SelectedReception.Samples.Select(s => new ReceptionSample
                    {
                        Id = s.Id,
                        ReceptionId = s.ReceptionId,
                        Root = s.Root,
                        SampleNumber = s.SampleNumber,
                        Description = s.Description
                    }))
                };

                foreach (var s in EditingReception.Samples)
                {
                    s.PropertyChanged -= ReceptionSample_PropertyChanged;
                    s.PropertyChanged += ReceptionSample_PropertyChanged;
                }
            }
            finally
            {
                _isLoadingReception = false;
            }

            IsEditing = true;
            RequestNavigation?.Invoke(NavigationDestination.SampleReceptionForm);
        }

        private async Task SaveReceptionAsync()
        {
            if (EditingReception == null) return;

            if (string.IsNullOrWhiteSpace(EditingReception.AnalysisRequestNumber))
            {
                SetNotification("بيانات ناقصة", "يجب إدخال رقم طلب التحليل.", NotificationType.Error, "AlertCircleOutline");
                return;
            }

            // التحقق النهائي من تكرار أرقام العينات قبل الحفظ (خط الدفاع الأخير)
            if (EditingReception.Samples != null && EditingReception.Samples.Count > 0)
            {
                // أ. التحقق من التكرار الداخلي بين أسطر جدول الاستلام
                var internalDuplicates = EditingReception.Samples
                    .Where(s => !string.IsNullOrWhiteSpace(s.SampleNumber))
                    .GroupBy(s => SampleValidationHelper.NormalizeSampleNumber(s.SampleNumber))
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (internalDuplicates.Count > 0)
                {
                    SetNotification("تكرار داخلي لأرقام العينات",
                        "لا يمكن الحفظ، يوجد تكرار في أرقام العينات داخل نفس قائمة الاستلام:\n• " +
                        string.Join("\n• ", internalDuplicates) +
                        "\n\nيرجى تعديل أرقام العينات المكررة قبل الحفظ.",
                        NotificationType.Error, "AlertCircleOutline");
                    return;
                }

                // ب. التحقق من التكرار في قاعدة البيانات لنفس الجهة وسنة الاستلام
                if (!string.IsNullOrWhiteSpace(EditingReception.Sender))
                {
                    var sampleConflicts = new List<string>();
                    foreach (var sample in EditingReception.Samples)
                    {
                        if (string.IsNullOrWhiteSpace(sample.SampleNumber)) continue;

                        var check = await _receptionRepository.CheckSampleUniquenessAsync(
                            sample.SampleNumber,
                            EditingReception.Sender,
                            EditingReception.Date.Year,
                            EditingReception.Id > 0 ? EditingReception.Id : null);

                        if (check.Status == SampleCheckResult.DuplicateActive)
                        {
                            string normNum = SampleValidationHelper.NormalizeSampleNumber(sample.SampleNumber);
                            sampleConflicts.Add($"• العينة ({normNum}) مسجلة في {check.CertificateNumber} بتاريخ ({check.IssueDate:yyyy/MM/dd})");
                        }
                    }

                    if (sampleConflicts.Count > 0)
                    {
                        SetNotification("تعارض في أرقام العينات",
                            $"لا يمكن حفظ الاستلام. تم العثور على تعارض في أرقام العينات مع الجهة ({EditingReception.Sender}) لعام ({EditingReception.Date.Year}):\n\n" +
                            string.Join("\n", sampleConflicts) +
                            "\n\nيرجى تصحيح أرقام العينات والمحاولة مرة أخرى.",
                            NotificationType.Error, "AlertCircleOutline");
                        return;
                    }
                }
            }

            try
            {
                if (EditingReception.Id == 0)
                {
                    // Add New
                    EditingReception.CreatedAt = DateTime.Now;
                    await _receptionRepository.AddSampleReceptionAsync(EditingReception);
                    _alertService.ShowSuccess("تمت إضافة الاستلام بنجاح.");
                }
                else
                {
                    // Update Exisiting
                    await _receptionRepository.UpdateSampleReceptionAsync(EditingReception);
                    
                    // مزامنة التعديلات مع الشهادة المرتبطة إن وجدت
                    // Sync changes with associated certificate if any
                    try
                    {
                        var associatedCert = await _certificateRepository.GetCertificateByReceptionIdAsync(EditingReception.Id);
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
                        LoggerService.LogWarning("Failed to sync reception changes to certificate: " + ex.Message);
                    }

                    _alertService.ShowSuccess("تم تحديث الاستلام بنجاح.");
                }

                IsEditing = false;
                EditingReception = null;
                RequestNavigation?.Invoke(NavigationDestination.SampleReceptions);
                await LoadReceptionsAsync();
            }
            catch (Exception ex)
            {
                _alertService.ShowError("حدث خطأ أثناء الحفظ: " + ex.Message);
            }
        }

        private void CancelEdit()
        {
            IsEditing = false;
            IsSelectingType = false;
            EditingReception = null;
            RequestNavigation?.Invoke(NavigationDestination.SampleReceptions);
        }

        private Task DeleteReceptionAsync()
        {
            if (SelectedReception == null) return Task.CompletedTask;

            if (_userService.CurrentUser == null || _userService.CurrentUser.Role < UserRole.Admin)
            {
                _alertService.ShowError("عذراً، لا تملك صلاحية لحذف استلام العينات. يتطلب ذلك صلاحية مدير نظام.");
                return Task.CompletedTask;
            }

            RaiseConfirmation(
                "تأكيد الحذف",
                "هل أنت متأكد من حذف استلام العينات هذا؟",
                NotificationType.Warning,
                async (confirmed) => 
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
                var results = await _receptionRepository.SearchSampleReceptionsAsync(SearchCriteria != "التاريخ" ? SearchText : "");
                
                if (SearchCriteria == "التاريخ" && SearchStartDate.HasValue && SearchEndDate.HasValue)
                {
                    results = results.Where(r => r.Date >= SearchStartDate.Value && r.Date <= SearchEndDate.Value).ToList();
                }

                Receptions.Clear();
                foreach (var r in results.OrderByDescending(x => x.CreatedAt))
                {
                    Receptions.Add(r);
                }
                CurrentPage = 1;
                UpdatePagedReceptions();
                UpdateTotals();
            }
            catch (Exception ex)
            {
                _alertService.ShowError("خطأ في البحث: " + ex.Message);
            }
        }

        private string GetSearchCriteriaMap()
        {
            return SearchCriteria switch
            {
                "رقم طلب التحليل" => "AnalysisRequestNumber",
                "الجهة المرسلة" => "Sender",
                "المورد" => "Supplier",
                _ => "All"
            };
        }

        private async Task ViewDetailsAsync()
        {
            if (SelectedReception == null) return;
            
            IsViewingDetails = true;
            await Task.CompletedTask; // Future proofing
        }

        private void CloseDetails()
        {
            IsViewingDetails = false;
        }
        
        private async Task AddSampleAsync()
        {
            if (EditingReception == null) return;
            
            if (string.IsNullOrWhiteSpace(NewSampleNumber))
            {
                SetNotification("رقم العينة مطلوب", "يرجى إدخال رقم العينة قبل الإضافة.", NotificationType.Warning, "AlertCircleOutline");
                return;
            }

            string norm = SampleValidationHelper.NormalizeSampleNumber(NewSampleNumber);
            if (string.IsNullOrEmpty(norm)) return;

            // 1. فحص التكرار الداخلي في جدول الاستلام الحالي
            bool isInternalDuplicate = EditingReception.Samples.Any(s =>
                !string.IsNullOrWhiteSpace(s.SampleNumber) &&
                SampleValidationHelper.NormalizeSampleNumber(s.SampleNumber) == norm);

            if (isInternalDuplicate)
            {
                SetNotification("تكرار رقم العينة",
                    $"رقم العينة ({norm}) مكرر في نفس قائمة الاستلام الحالية.\nيرجى كتابة رقم عينة غير مكرر.",
                    NotificationType.Error, "AlertCircleOutline");
                return;
            }

            // 2. التحقق من توفر الجهة وتاريخ الاستلام للفحص في قاعدة البيانات
            if (!string.IsNullOrWhiteSpace(EditingReception.Sender))
            {
                var result = await _receptionRepository.CheckSampleUniquenessAsync(
                    NewSampleNumber,
                    EditingReception.Sender,
                    EditingReception.Date.Year,
                    EditingReception.Id > 0 ? EditingReception.Id : null);

                if (result.Status == SampleCheckResult.DuplicateActive)
                {
                    SetNotification("تكرار رقم العينة",
                        $"إن رقم العينة ({norm}) مسجل مسبقاً للجهة ({result.Sender ?? EditingReception.Sender})\nفي ({result.CertificateNumber ?? "سجل سابق"})\nبتاريخ ({result.IssueDate:yyyy/MM/dd}).\n\nيرجى التأكد من الرقم والمحاولة مرة أخرى.",
                        NotificationType.Error, "AlertCircleOutline");
                    return;
                }
                else if (result.Status == SampleCheckResult.FoundInDeleted)
                {
                    string key = $"{norm}_{EditingReception.Date.Year}_{SampleValidationHelper.NormalizeSender(EditingReception.Sender)}";
                    if (!_notifiedDeletedSamples.Contains(key))
                    {
                        _notifiedDeletedSamples.Add(key);
                        SetNotification("تنبيه - عينة لسجل محذوف",
                            $"تنبيه:\nرقم العينة ({norm}) كان مسجلاً سابقاً للجهة ({result.Sender ?? EditingReception.Sender})\nضمن ({result.CertificateNumber ?? "سجل محذوف"})\nبتاريخ ({result.IssueDate:yyyy/MM/dd})، ولكن هذا السجل محذوف حالياً.\n\nيمكن استخدام رقم العينة.",
                            NotificationType.Information, "InformationOutline");
                    }
                }
            }

            string newRoot = (EditingReception.Samples.Count + 1).ToString();
            var newSample = new ReceptionSample
            {
                Root = newRoot,
                SampleNumber = NewSampleNumber.Trim(),
                Description = NewSampleDescription?.Trim() ?? string.Empty
            };

            newSample.PropertyChanged += ReceptionSample_PropertyChanged;

            EditingReception.Samples.Add(newSample);

            NewSampleNumber = "";
            NewSampleDescription = "";
        }

        private async void ReceptionSample_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_isLoadingReception) return;
            if (sender is not ReceptionSample sample) return;

            if (e.PropertyName == nameof(ReceptionSample.SampleNumber))
            {
                _sampleValidationCts?.Cancel();
                var cts = new CancellationTokenSource();
                _sampleValidationCts = cts;

                try
                {
                    await ValidateSingleSampleAsync(sample, cts.Token);
                }
                catch (OperationCanceledException) { }
            }
        }

        private async Task<bool> ValidateSingleSampleAsync(ReceptionSample sample, CancellationToken cancellationToken = default)
        {
            if (_isLoadingReception) return true;
            if (string.IsNullOrWhiteSpace(sample.SampleNumber) || EditingReception == null) return true;

            string capturedValue = sample.SampleNumber;
            string norm = SampleValidationHelper.NormalizeSampleNumber(capturedValue);
            if (string.IsNullOrEmpty(norm)) return true;

            // 1. فحص التكرار الداخلي
            int internalMatches = EditingReception.Samples.Count(s =>
                !string.IsNullOrWhiteSpace(s.SampleNumber) &&
                SampleValidationHelper.NormalizeSampleNumber(s.SampleNumber) == norm);

            if (internalMatches > 1)
            {
                if (sample.SampleNumber != capturedValue) return true;
                sample.SampleNumber = string.Empty;
                SetNotification("تكرار رقم العينة",
                    $"رقم العينة ({norm}) مكرر في نفس قائمة الاستلام الحالية.\nيرجى كتابة رقم عينة غير مكرر.",
                    NotificationType.Error, "AlertCircleOutline");
                return false;
            }

            if (string.IsNullOrWhiteSpace(EditingReception.Sender)) return true;

            cancellationToken.ThrowIfCancellationRequested();

            var result = await _receptionRepository.CheckSampleUniquenessAsync(
                capturedValue,
                EditingReception.Sender,
                EditingReception.Date.Year,
                EditingReception.Id > 0 ? EditingReception.Id : null);

            cancellationToken.ThrowIfCancellationRequested();

            if (sample.SampleNumber != capturedValue) return true;

            if (result.Status == SampleCheckResult.DuplicateActive)
            {
                sample.SampleNumber = string.Empty;
                SetNotification("تكرار رقم العينة",
                    $"إن رقم العينة ({norm}) مسجل مسبقاً للجهة ({result.Sender ?? EditingReception.Sender})\nفي ({result.CertificateNumber ?? "سجل سابق"})\nبتاريخ ({result.IssueDate:yyyy/MM/dd}).\n\nيرجى التأكد من الرقم والمحاولة مرة أخرى.",
                    NotificationType.Error, "AlertCircleOutline");
                return false;
            }
            else if (result.Status == SampleCheckResult.FoundInDeleted)
            {
                string key = $"{norm}_{EditingReception.Date.Year}_{SampleValidationHelper.NormalizeSender(EditingReception.Sender)}";
                if (!_notifiedDeletedSamples.Contains(key))
                {
                    _notifiedDeletedSamples.Add(key);
                    SetNotification("تنبيه - عينة لسجل محذوف",
                        $"تنبيه:\nرقم العينة ({norm}) كان مسجلاً سابقاً للجهة ({result.Sender ?? EditingReception.Sender})\nضمن ({result.CertificateNumber ?? "سجل محذوف"})\nبتاريخ ({result.IssueDate:yyyy/MM/dd})، ولكن هذا السجل محذوف حالياً.\n\nيمكن استخدام رقم العينة.",
                        NotificationType.Information, "InformationOutline");
                }
            }

            return true;
        }
        
        private void RemoveSample(object? obj)
        {
            if (EditingReception == null || obj is not ReceptionSample sample) return;
            
            EditingReception.Samples.Remove(sample);
            
            // Re-sequence roots
            for(int i = 0; i < EditingReception.Samples.Count; i++)
            {
                EditingReception.Samples[i].Root = (i + 1).ToString();
            }
        }
        private void UpdatePagedReceptions()
        {
            var filteredList = Receptions.AsEnumerable();
            
            if (SelectedCertificateStatusFilter == "تم إصدار شهادة")
                filteredList = filteredList.Where(r => r.Status == "تم إصدار شهادة");
            else if (SelectedCertificateStatusFilter == "لم يتم إصدار شهادة")
                filteredList = filteredList.Where(r => r.Status == "لم يتم إصدار شهادة");

            var listCount = filteredList.Count();
            TotalPages = (int)Math.Ceiling(listCount / (double)ItemsPerPage);
            if (TotalPages == 0) TotalPages = 1;
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;

            var items = filteredList.Skip((CurrentPage - 1) * ItemsPerPage).Take(ItemsPerPage).ToList();
            
            PagedReceptions.Clear();
            for (int i = 0; i < items.Count; i++)
            {
                items[i].Sequence = ((CurrentPage - 1) * ItemsPerPage) + i + 1;
                PagedReceptions.Add(items[i]);
            }
            
            OnPropertyChanged(nameof(TotalPages));
        }

        private void UpdateTotals()
        {
            OnPropertyChanged(nameof(TotalSamplesSum));
            OnPropertyChanged(nameof(TotalConsumableSamples));
            OnPropertyChanged(nameof(TotalEnvironmentalSamples));
        }
    }
}
