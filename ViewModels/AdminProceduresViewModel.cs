using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.IO;
using Enjaz.Models;
using Enjaz.Services;
using Enjaz.Services.Repositories;
using Enjaz.Helpers;

namespace Enjaz.ViewModels
{
    public class AdminProceduresViewModel : BaseViewModel
    {
        private readonly CertificateRepository _certificateRepository;
        private readonly IPdfService _pdfService;
        private readonly INotificationService _notificationService;
        private readonly IOSService _osService;
        private readonly UserService _userService;

        public AdminProceduresViewModel(
            CertificateRepository certificateRepository,
            IPdfService pdfService,
            INotificationService notificationService,
            IOSService osService,
            UserService userService)
        {
            _certificateRepository = certificateRepository;
            _pdfService = pdfService;
            _notificationService = notificationService;
            _osService = osService;
            _userService = userService;

            // Default values
            StartDate = DateTime.Today.AddDays(-30);
            EndDate = DateTime.Today;
            CurrentStep = 1;

            NextStepCommand = new AsyncRelayCommand(async _ => await NextStep(), _ => CanNextStep());
            PreviousStepCommand = new RelayCommand(_ => PreviousStep(), _ => CanPreviousStep());
            GenerateLetterCommand = new AsyncRelayCommand(async _ => await GenerateLetter());
            ViewLetterCommand = new AsyncRelayCommand(async l => await ViewLetter(l as ReferralLetter));
            PrintLetterCommand = new AsyncRelayCommand(async l => await PrintLetter(l as ReferralLetter));

            Senders = new ObservableCollection<string>();
            ReferralHistory = new ObservableCollection<ReferralLetter>();
            
            // Load initial data
            _ = LoadSendersAsync();
            _ = LoadReferralHistoryAsync();
        }

        private int _selectedTab;
        public int SelectedTab
        {
            get => _selectedTab;
            set => SetProperty(ref _selectedTab, value);
        }



        /// <summary>
        /// Reloads the senders list from the database. Call this when the view becomes visible.
        /// </summary>
        public async Task RefreshSendersAsync()
        {
            await LoadSendersAsync();
        }

        private async Task LoadSendersAsync()
        {
            try
            {
                var dbSenders = await _certificateRepository.GetDistinctFieldValuesAsync("Sender");
                
                Senders.Clear();
                
                // Add only database values (like CertificatesViewModel does)
                foreach (var sender in dbSenders)
                {
                    if (!string.IsNullOrWhiteSpace(sender) && !Senders.Contains(sender))
                    {
                        Senders.Add(sender);
                    }
                }

                LoggerService.LogInfo($"[ReferralWizard] Loaded {Senders.Count} senders from database");
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Error loading senders for referral wizard", ex);
            }
        }

        public async Task LoadReferralHistoryAsync()
        {
            try
            {
                var history = await _certificateRepository.GetReferralLettersAsync();
                ReferralHistory.Clear();
                int seq = 1;
                foreach (var item in history)
                {
                    item.Sequence = seq++;
                    ReferralHistory.Add(item);
                }
                
                // Set up the CollectionView for filtering
                if (_referralHistoryView == null)
                {
                    _referralHistoryView = CollectionViewSource.GetDefaultView(ReferralHistory);
                    _referralHistoryView.Filter = item =>
                    {
                        if (string.IsNullOrWhiteSpace(SelectedFilterSender) || SelectedFilterSender == "الكل")
                            return true;
                        
                        if (item is ReferralLetter letter)
                            return letter.SenderName == SelectedFilterSender;
                        
                        return false;
                    };
                    OnPropertyChanged(nameof(ReferralHistoryView));
                }
                else
                {
                    _referralHistoryView.Refresh(); // Refresh in case data changed
                }

                // Update filter dropdown
                string? prevFilter = SelectedFilterSender;
                FilterSenders.Clear();
                FilterSenders.Add("الكل");
                
                var uniqueSenders = ReferralHistory
                    .Select(r => r.SenderName)
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Distinct();
                    
                foreach (var s in uniqueSenders)
                {
                    FilterSenders.Add(s);
                }
                
                if (prevFilter != null && FilterSenders.Contains(prevFilter))
                    SelectedFilterSender = prevFilter;
                else
                    SelectedFilterSender = "الكل";
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Error loading referral history", ex);
            }
        }

        #region Properties

        private int _currentStep;
        public int CurrentStep
        {
            get => _currentStep;
            set => SetProperty(ref _currentStep, value);
        }

        // --- Step 1 ---
        private DateTime _startDate;
        public DateTime StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        private DateTime _endDate;
        public DateTime EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        private string? _selectedSender;
        public string? SelectedSender
        {
            get => _selectedSender;
            set => SetProperty(ref _selectedSender, value);
        }

        private ReferralLetter? _selectedReferral;
        public ReferralLetter? SelectedReferral
        {
            get => _selectedReferral;
            set => SetProperty(ref _selectedReferral, value);
        }

        public ObservableCollection<string> Senders { get; }
        public ObservableCollection<ReferralLetter> ReferralHistory { get; } = new ObservableCollection<ReferralLetter>();

        private ICollectionView? _referralHistoryView;
        public ICollectionView? ReferralHistoryView
        {
            get => _referralHistoryView;
            set => SetProperty(ref _referralHistoryView, value);
        }

        public ObservableCollection<string> FilterSenders { get; } = new ObservableCollection<string>();

        private string? _selectedFilterSender;
        public string? SelectedFilterSender
        {
            get => _selectedFilterSender;
            set
            {
                if (SetProperty(ref _selectedFilterSender, value))
                {
                    _referralHistoryView?.Refresh();
                }
            }
        }

        // --- Step 2 ---
        private bool _includeCertNum = true;
        public bool IncludeCertNum
        {
            get => _includeCertNum;
            set
            {
                if (SetProperty(ref _includeCertNum, value))
                    OnPropertyChanged(nameof(SummaryColumns));
            }
        }

        private bool _includeSupplier = true;
        public bool IncludeSupplier
        {
            get => _includeSupplier;
            set
            {
                if (SetProperty(ref _includeSupplier, value))
                    OnPropertyChanged(nameof(SummaryColumns));
            }
        }

        private bool _includeSamples = true;
        public bool IncludeSamples
        {
            get => _includeSamples;
            set
            {
                if (SetProperty(ref _includeSamples, value))
                    OnPropertyChanged(nameof(SummaryColumns));
            }
        }

        private bool _includeNotification = true;
        public bool IncludeNotification
        {
            get => _includeNotification;
            set
            {
                if (SetProperty(ref _includeNotification, value))
                    OnPropertyChanged(nameof(SummaryColumns));
            }
        }

        public IEnumerable<string> SummaryColumns
        {
            get
            {
                var cols = new List<string>();
                if (IncludeCertNum) cols.Add("رقم الشهادة");
                if (IncludeSupplier) cols.Add("اسم المورد");
                if (IncludeSamples) cols.Add("أرقام العينات");
                if (IncludeNotification) cols.Add("رقم الإخطار");
                return cols;
            }
        }

        #endregion

        #region Commands

        public ICommand NextStepCommand { get; }
        public ICommand PreviousStepCommand { get; }
        public ICommand GenerateLetterCommand { get; }
        public ICommand ViewLetterCommand { get; }
        public ICommand PrintLetterCommand { get; }

        private Task NextStep()
        {
            if (CurrentStep == 1)
            {
                if (string.IsNullOrWhiteSpace(SelectedSender))
                {
                    _notificationService.ShowWarning("يرجى اختيار الجهة المرسلة");
                    return Task.CompletedTask;
                }
                CurrentStep = 2;
            }
            else if (CurrentStep == 2)
            {
                if (!IncludeCertNum && !IncludeSupplier && !IncludeSamples && !IncludeNotification)
                {
                    _notificationService.ShowWarning("يرجى اختيار عمود واحد على الأقل");
                    return Task.CompletedTask;
                }
                CurrentStep = 3;
            }
            return Task.CompletedTask;
        }

        private bool CanNextStep() => CurrentStep < 3;

        private void PreviousStep()
        {
            if (CurrentStep > 1)
                CurrentStep--;
        }

        private bool CanPreviousStep() => CurrentStep > 1;

        private async Task GenerateLetter()
        {
            IsBusy = true;
            try
            {
                string senderName = SelectedSender!;
                
                LoggerService.LogInfo($"[ReferralWizard] Querying: Sender='{senderName}', From={StartDate:yyyy-MM-dd}, To={EndDate:yyyy-MM-dd}");
                
                var certificates = await _certificateRepository.GetCertificatesBySenderAndDateAsync(
                    senderName, StartDate, EndDate);

                LoggerService.LogInfo($"[ReferralWizard] Found {certificates?.Count ?? 0} certificates");

                if (certificates == null || !certificates.Any())
                {
                    _notificationService.ShowError($"لم يتم العثور على شهادات للجهة '{senderName}' في الفترة من {StartDate:yyyy-MM-dd} إلى {EndDate:yyyy-MM-dd}");
                    return;
                }

                string fileName = $"Referral_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                if (!Directory.Exists(downloadsPath)) Directory.CreateDirectory(downloadsPath);
                
                string outputPath = Path.Combine(downloadsPath, fileName);

                bool success = _pdfService.GenerateReferralLetterPdf(
                    certificates, 
                    outputPath, 
                    senderName,
                    IncludeCertNum,
                    IncludeSupplier,
                    IncludeSamples,
                    IncludeNotification);

                if (success)
                {
                    // Save to history
                    var historyRecord = new ReferralLetter
                    {
                        SenderName = senderName,
                        CertificateCount = certificates.Count,
                        SampleCount = certificates.Sum(c => c.SampleCount),
                        OutputPath = outputPath,
                        StartDate = StartDate,
                        EndDate = EndDate,
                        IncludedColumns = $"{(IncludeCertNum ? "CertNum," : "")}{(IncludeSupplier ? "Supplier," : "")}{(IncludeSamples ? "Samples," : "")}{(IncludeNotification ? "Notification" : "")}".TrimEnd(',')
                    };

                    await _certificateRepository.AddReferralLetterAsync(historyRecord);
                    
                    // Log to Audit Log
                    var user = _userService.CurrentUser;
                    await _certificateRepository.LogReferralLetterGenerationAsync(user?.Id, user?.FullName ?? "غير معروف", senderName, certificates.Count);
                    
                    await LoadReferralHistoryAsync();

                    _osService.OpenFile(outputPath);
                    _notificationService.ShowSuccess("تم حفظ البيانات وإصدار الرسالة بنجاح");
                    
                    // Automatically switch to the history tab and reset wizard
                    SelectedTab = 1;
                    ResetWizard();
                }
                else
                {
                    _notificationService.ShowError("فشل إنشاء ملف PDF");
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("حدث خطأ أثناء المعالجة: " + ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ResetWizard()
        {
            CurrentStep = 1;
            SelectedSender = null;
            StartDate = DateTime.Today.AddDays(-30);
            EndDate = DateTime.Today;
            IncludeCertNum = true;
            IncludeSupplier = true;
            IncludeSamples = true;
            IncludeNotification = true;
        }

        private async Task ViewLetter(ReferralLetter? letter)
        {
            if (letter == null) return;
            if (File.Exists(letter.OutputPath))
            {
                _osService.OpenFile(letter.OutputPath);
            }
            else
            {
                _notificationService.ShowInfo("الملف غير موجود، جاري إعادة التوليد...");
                await RegeneratePdf(letter);
            }
        }

        private async Task PrintLetter(ReferralLetter? letter)
        {
            if (letter == null) return;

            try
            {
                var certificates = await _certificateRepository.GetCertificatesBySenderAndDateAsync(
                    letter.SenderName, letter.StartDate, letter.EndDate);

                if (certificates == null || !certificates.Any())
                {
                    _notificationService.ShowError("لا يمكن طباعة الرسالة: لم يتم العثور على الشهادات الأصلية.");
                    return;
                }

                bool incCert = letter.IncludedColumns.Contains("CertNum");
                bool incSupp = letter.IncludedColumns.Contains("Supplier");
                bool incSamp = letter.IncludedColumns.Contains("Samples");
                bool incNotif = letter.IncludedColumns.Contains("Notification");

                _pdfService.PrintReferralLetterPdf(
                    certificates,
                    letter.SenderName,
                    incCert,
                    incSupp,
                    incSamp,
                    incNotif);
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Error in PrintLetter command", ex);
                _notificationService.ShowError("حدث خطأ أثناء محاولة الطباعة");
            }
        }

        private async Task RegeneratePdf(ReferralLetter? letter)
        {
            if (letter == null) return;
            
            IsBusy = true;
            try
            {
                var certificates = await _certificateRepository.GetCertificatesBySenderAndDateAsync(
                    letter.SenderName, letter.StartDate, letter.EndDate);

                if (certificates == null || !certificates.Any())
                {
                    _notificationService.ShowError("لم يتم العثور على الشهادات لإعادة الإنشاء");
                    return;
                }

                bool incCert = letter.IncludedColumns.Contains("CertNum");
                bool incSupp = letter.IncludedColumns.Contains("Supplier");
                bool incSamp = letter.IncludedColumns.Contains("Samples");
                bool incNotif = letter.IncludedColumns.Contains("Notification");

                bool success = _pdfService.GenerateReferralLetterPdf(
                    certificates, letter.OutputPath, letter.SenderName, incCert, incSupp, incSamp, incNotif);

                if (success)
                {
                    _notificationService.ShowSuccess("تم إعادة إنشاء ملف PDF بنجاح");
                    await ViewLetter(letter);
                }
                else
                {
                    _notificationService.ShowError("فشل إعادة إنشاء ملف PDF");
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("خطأ في إعادة الإنشاء: " + ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion
    }
}
