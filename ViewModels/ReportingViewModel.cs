using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Enjaz.Helpers;
using Enjaz.Models;
using Enjaz.Services;
using LiveCharts;
using LiveCharts.Wpf;

namespace Enjaz.ViewModels
{
    /// <summary>
    /// ViewModel لقسم التقارير الذكية
    /// </summary>
    public class ReportingViewModel : BaseViewModel
    {
        private readonly ReportingService _reportingService;
        private readonly INotificationService _notificationService;
        private readonly AdvancedExcelService _excelService;
        private readonly ReportPdfService _pdfService;
        private readonly ChartImageGenerator _chartGenerator;
        private readonly IOSService _osService;

        // Store distribution data for export
        private List<DistributionItem> _topSuppliers = new();
        private List<DistributionItem> _topSenders = new();

        #region Properties

        private int _currentStep = 1;
        public int CurrentStep
        {
            get => _currentStep;
            set => SetProperty(ref _currentStep, value);
        }

        private DateTime _startDate = DateTime.Today.AddMonths(-1);
        public DateTime StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        private DateTime _endDate = DateTime.Today;
        public DateTime EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private bool _isDataLoaded;
        public bool IsDataLoaded
        {
            get => _isDataLoaded;
            set => SetProperty(ref _isDataLoaded, value);
        }

        // Summary Statistics
        private ReportSummary? _summary;
        public ReportSummary? Summary
        {
            get => _summary;
            set => SetProperty(ref _summary, value);
        }

        // Filtered Certificates
        private ObservableCollection<Certificate> _filteredCertificates = new();
        public ObservableCollection<Certificate> FilteredCertificates
        {
            get => _filteredCertificates;
            set => SetProperty(ref _filteredCertificates, value);
        }

        // Column Selection
        private ObservableCollection<ColumnOption> _availableColumns = new();
        public ObservableCollection<ColumnOption> AvailableColumns
        {
            get => _availableColumns;
            set => SetProperty(ref _availableColumns, value);
        }

        // Chart Data
        private SeriesCollection _supplierSeries = new();
        public SeriesCollection SupplierSeries
        {
            get => _supplierSeries;
            set => SetProperty(ref _supplierSeries, value);
        }

        private SeriesCollection _senderSeries = new();
        public SeriesCollection SenderSeries
        {
            get => _senderSeries;
            set => SetProperty(ref _senderSeries, value);
        }

        private List<string> _supplierLabels = new();
        public List<string> SupplierLabels
        {
            get => _supplierLabels;
            set => SetProperty(ref _supplierLabels, value);
        }

        private List<string> _senderLabels = new();
        public List<string> SenderLabels
        {
            get => _senderLabels;
            set => SetProperty(ref _senderLabels, value);
        }

        private ObservableCollection<DashboardWidgetViewModel> _dashboardWidgets = new();
        public ObservableCollection<DashboardWidgetViewModel> DashboardWidgets
        {
            get => _dashboardWidgets;
            set => SetProperty(ref _dashboardWidgets, value);
        }

        // --- Senders Report Wizard Properties ---
        private int _senderReportCurrentStep = 1;
        public int SenderReportCurrentStep
        {
            get => _senderReportCurrentStep;
            set => SetProperty(ref _senderReportCurrentStep, value);
        }

        private DateTime _senderReportStartDate = DateTime.Today.AddMonths(-1);
        public DateTime SenderReportStartDate
        {
            get => _senderReportStartDate;
            set => SetProperty(ref _senderReportStartDate, value);
        }

        private DateTime _senderReportEndDate = DateTime.Today;
        public DateTime SenderReportEndDate
        {
            get => _senderReportEndDate;
            set => SetProperty(ref _senderReportEndDate, value);
        }

        private string? _selectedSenderReportSender;
        public string? SelectedSenderReportSender
        {
            get => _selectedSenderReportSender;
            set => SetProperty(ref _selectedSenderReportSender, value);
        }

        private ObservableCollection<string> _sendersList = new();
        public ObservableCollection<string> SendersList
        {
            get => _sendersList;
            set => SetProperty(ref _sendersList, value);
        }

        private bool _senderReportIsLoading;
        public bool SenderReportIsLoading
        {
            get => _senderReportIsLoading;
            set => SetProperty(ref _senderReportIsLoading, value);
        }

        private bool _senderReportIsDataLoaded;
        public bool SenderReportIsDataLoaded
        {
            get => _senderReportIsDataLoaded;
            set => SetProperty(ref _senderReportIsDataLoaded, value);
        }

        private ReportSummary? _senderReportSummary;
        public ReportSummary? SenderReportSummary
        {
            get => _senderReportSummary;
            set => SetProperty(ref _senderReportSummary, value);
        }

        private ObservableCollection<Certificate> _senderReportFilteredCertificates = new();
        public ObservableCollection<Certificate> SenderReportFilteredCertificates
        {
            get => _senderReportFilteredCertificates;
            set => SetProperty(ref _senderReportFilteredCertificates, value);
        }

        private ObservableCollection<ColumnOption> _senderReportAvailableColumns = new();
        public ObservableCollection<ColumnOption> SenderReportAvailableColumns
        {
            get => _senderReportAvailableColumns;
            set => SetProperty(ref _senderReportAvailableColumns, value);
        }

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; private set; } = null!;
        public ICommand NextStepCommand { get; private set; } = null!;
        public ICommand PreviousStepCommand { get; private set; } = null!;
        public ICommand ExportPdfCommand { get; private set; } = null!;
        public ICommand ExportExcelCommand { get; private set; } = null!;
        public ICommand OpenDashboardCommand { get; private set; } = null!;
        
        // Senders Report Commands
        public ICommand LoadSenderReportDataCommand { get; private set; } = null!;
        public ICommand SenderReportNextStepCommand { get; private set; } = null!;
        public ICommand SenderReportPreviousStepCommand { get; private set; } = null!;
        public ICommand ExportSenderReportPdfCommand { get; private set; } = null!;
        public ICommand ExportSenderReportExcelCommand { get; private set; } = null!;

        public event Action? RequestOpenDashboard;

        #endregion

        public ReportingViewModel(
            ReportingService reportingService, 
            INotificationService notificationService,
            AdvancedExcelService excelService,
            ReportPdfService pdfService,
            ChartImageGenerator chartGenerator,
            IOSService osService)
        {
            _reportingService = reportingService;
            _notificationService = notificationService;
            _excelService = excelService;
            _pdfService = pdfService;
            _chartGenerator = chartGenerator;
            _osService = osService;

            InitializeCommands();
            InitializeColumns();
            InitializeSenderReportColumns();
            _ = LoadSendersListAsync();
        }

        private void InitializeCommands()
        {
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            OpenDashboardCommand = new RelayCommand(_ => RequestOpenDashboard?.Invoke());
            NextStepCommand = new RelayCommand(_ => 
            {
                CurrentStep++;
                if (CurrentStep == 3)
                {
                    GenerateDashboard();
                }
            }, _ => CurrentStep < 4 && IsDataLoaded);
            PreviousStepCommand = new RelayCommand(_ => CurrentStep--, _ => CurrentStep > 1);
            ExportPdfCommand = new RelayCommand(async _ => await ExportToPdfAsync(), _ => IsDataLoaded);
            ExportExcelCommand = new RelayCommand(async _ => await ExportToExcelAsync(), _ => IsDataLoaded);

            // Senders Report Commands
            LoadSenderReportDataCommand = new RelayCommand(async _ => await LoadSenderReportDataAsync());
            SenderReportNextStepCommand = new RelayCommand(_ => 
            {
                if (SenderReportCurrentStep == 1 && (string.IsNullOrWhiteSpace(SelectedSenderReportSender) || SenderReportStartDate > SenderReportEndDate))
                {
                    _notificationService.ShowError("يرجى اختيار جهة وتاريخين صحيحين للمتابعة");
                    return;
                }
                
                if (SenderReportCurrentStep == 2 && !SenderReportAvailableColumns.Any(c => c.IsSelected))
                {
                    _notificationService.ShowError("يرجى اختيار عمود واحد على الأقل");
                    return;
                }

                SenderReportCurrentStep++;
                if (SenderReportCurrentStep == 3)
                {
                    _ = LoadSenderReportDataAsync();
                }
            }, _ => SenderReportCurrentStep < 3);

            SenderReportPreviousStepCommand = new RelayCommand(_ => SenderReportCurrentStep--, _ => SenderReportCurrentStep > 1);
            ExportSenderReportPdfCommand = new RelayCommand(async _ => await ExportSenderReportToPdfAsync(), _ => SenderReportIsDataLoaded);
            ExportSenderReportExcelCommand = new RelayCommand(async _ => await ExportSenderReportToExcelAsync(), _ => SenderReportIsDataLoaded);
        }

        private void InitializeSenderReportColumns()
        {
            SenderReportAvailableColumns = new ObservableCollection<ColumnOption>
            {
                new ColumnOption { Name = "رقم الشهادة", PropertyName = "CertificateNumber", IsSelected = true },
                new ColumnOption { Name = "نوع الشهادة", PropertyName = "CertificateType", IsSelected = true },
                new ColumnOption { Name = "تاريخ الإصدار", PropertyName = "IssueDate", IsSelected = true },
                new ColumnOption { Name = "المورد", PropertyName = "Supplier", IsSelected = true },
                new ColumnOption { Name = "بلد المنشأ", PropertyName = "Origin", IsSelected = true },
                new ColumnOption { Name = "رقم الإخطار", PropertyName = "NotificationNumber", IsSelected = true },
                new ColumnOption { Name = "رقم الإقرار الجمركي", PropertyName = "DeclarationNumber", IsSelected = true },
                new ColumnOption { Name = "رقم الإيصال المالي", PropertyName = "FinancialReceiptNumber", IsSelected = true },
                new ColumnOption { Name = "عدد العينات", PropertyName = "SampleCount", IsSelected = true },
                new ColumnOption { Name = "عدد عينات بيئية", PropertyName = "EnvironmentalSampleCount", IsSelected = true },
                new ColumnOption { Name = "عدد عينات استهلاكية", PropertyName = "ConsumableSampleCount", IsSelected = true },
                new ColumnOption { Name = "اسم المستخدم", PropertyName = "CreatedByName", IsSelected = true }
            };
        }

        private async Task LoadSendersListAsync()
        {
            try
            {
                // We use a dedicated method to get unique senders
                var senders = await _reportingService.GetUniqueSendersAsync();
                SendersList = new ObservableCollection<string>(senders);
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Failed to load senders list", ex);
            }
        }

        private async Task LoadSenderReportDataAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedSenderReportSender)) return;

            SenderReportIsLoading = true;
            SenderReportIsDataLoaded = false;

            try
            {
                // Load summary and certificates for specific sender
                var result = await _reportingService.GetSenderReportDataAsync(
                    SenderReportStartDate, 
                    SenderReportEndDate, 
                    SelectedSenderReportSender);

                SenderReportSummary = result.Summary;
                SenderReportFilteredCertificates = new ObservableCollection<Certificate>(result.Certificates);
                SenderReportIsDataLoaded = true;
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"خطأ في تحميل بيانات التقرير: {ex.Message}");
            }
            finally
            {
                SenderReportIsLoading = false;
            }
        }

        private void InitializeColumns()
        {
            AvailableColumns = new ObservableCollection<ColumnOption>
            {
                new ColumnOption { Name = "رقم الشهادة", PropertyName = "CertificateNumber", IsSelected = true },
                new ColumnOption { Name = "نوع الشهادة", PropertyName = "CertificateType", IsSelected = true },
                new ColumnOption { Name = "تاريخ الإصدار", PropertyName = "IssueDate", IsSelected = true },
                new ColumnOption { Name = "الجهة المرسلة", PropertyName = "Sender", IsSelected = true },
                new ColumnOption { Name = "المورد", PropertyName = "Supplier", IsSelected = true },
                new ColumnOption { Name = "بلد المنشأ", PropertyName = "Origin", IsSelected = true },
                new ColumnOption { Name = "رقم الإخطار", PropertyName = "NotificationNumber", IsSelected = false },
                new ColumnOption { Name = "رقم الإقرار الجمركي", PropertyName = "DeclarationNumber", IsSelected = false },
                new ColumnOption { Name = "رقم الإيصال المالي", PropertyName = "FinancialReceiptNumber", IsSelected = false },
                new ColumnOption { Name = "عدد العينات", PropertyName = "SampleCount", IsSelected = true },
                new ColumnOption { Name = "عدد عينات بيئية", PropertyName = "EnvironmentalSampleCount", IsSelected = false },
                new ColumnOption { Name = "عدد عينات استهلاكية", PropertyName = "ConsumableSampleCount", IsSelected = false },
                new ColumnOption { Name = "اسم المستخدم", PropertyName = "CreatedByName", IsSelected = false }
            };
        }

        private async Task LoadDataAsync()
        {
            if (StartDate > EndDate)
            {
                _notificationService.ShowError("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");
                return;
            }

            IsLoading = true;
            IsDataLoaded = false;

            try
            {
                // Load summary
                Summary = await _reportingService.GetReportSummaryAsync(StartDate, EndDate);

                // Load certificates
                var certs = await _reportingService.GetCertificatesByDateRangeAsync(StartDate, EndDate);
                FilteredCertificates = new ObservableCollection<Certificate>(certs);

                // Load chart data
                await LoadChartDataAsync();

                IsDataLoaded = true;
                _notificationService.ShowSuccess($"تم تحميل {certs.Count} شهادة");
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"خطأ في تحميل البيانات: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadChartDataAsync()
        {
            // Supplier distribution
            _topSuppliers = await _reportingService.GetTopSuppliersAsync(StartDate, EndDate, 5);
            SupplierLabels = _topSuppliers.Select(s => s.Name).ToList();
            SupplierSeries = new SeriesCollection
            {
                new PieSeries
                {
                    Title = "الموردين",
                    Values = new ChartValues<int>(_topSuppliers.Select(s => s.Count)),
                    DataLabels = true
                }
            };

            // Sender distribution
            _topSenders = await _reportingService.GetTopSendersAsync(StartDate, EndDate, 5);
            SenderLabels = _topSenders.Select(s => s.Name).ToList();
            SenderSeries = new SeriesCollection
            {
                new PieSeries
                {
                    Title = "الجهات المرسلة",
                    Values = new ChartValues<int>(_topSenders.Select(s => s.Count)),
                    DataLabels = true
                }
            };
        }

        private async Task ExportToPdfAsync()
        {
            try
            {
                if (Summary == null) return;

                // Generate chart images for export
                var chartImages = _chartGenerator.GenerateChartImages(DashboardWidgets);

                var selectedProps = GetSelectedPropertyNames();
                var result = _pdfService.GenerateReportWithDialog(
                    FilteredCertificates.ToList(),
                    Summary,
                    selectedProps,
                    _topSuppliers,
                    _topSenders,
                    chartImages);

                if (result != null)
                {
                    if (System.IO.File.Exists(result))
                    {
                        _notificationService.ShowSuccess($"تم حفظ التقرير بنجاح: {System.IO.Path.GetFileName(result)}");
                        
                        // فتح الملف باستخدام الخدمة الجديدة
                        _osService.OpenFile(result);

                        RaiseConfirmation(
                            "الانتهاء من التقرير",
                            "هل تريد الانتهاء من التقرير العام وإغلاق هذه النافذة للبدء بتقرير جديد؟",
                            NotificationType.Question,
                            (confirmed) => 
                            {
                                if (confirmed)
                                {
                                    ResetReportingState();
                                }
                            });
                    }
                    else
                    {
                        _notificationService.ShowError("حدث خطأ: لم يتم العثور على ملف التقرير بعد الحفظ!");
                    }
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"فشل التصدير: {ex.Message}");
            }
            await Task.CompletedTask;
        }

        private async Task ExportToExcelAsync()
        {
            try
            {
                if (Summary == null) return;

                // Generate chart images for export
                var chartImages = _chartGenerator.GenerateChartImages(DashboardWidgets);

                var selectedProps = GetSelectedPropertyNames();
                var result = _excelService.ExportReportWithDialog(
                    FilteredCertificates.ToList(),
                    Summary,
                    selectedProps,
                    _topSuppliers,
                    _topSenders,
                    chartImages);

                if (result != null)
                {
                    _notificationService.ShowSuccess($"تم حفظ التقرير: {result}");
                    
                    // فتح الملف باستخدام الخدمة الجديدة
                    _osService.OpenFile(result);

                    RaiseConfirmation(
                        "الانتهاء من التقرير",
                        "هل تريد الانتهاء من التقرير العام وإغلاق هذه النافذة للبدء بتقرير جديد؟",
                        NotificationType.Question,
                        (confirmed) => 
                        {
                            if (confirmed)
                            {
                                ResetReportingState();
                            }
                        });
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"فشل التصدير: {ex.Message}");
            }
            await Task.CompletedTask;
        }

        private async Task ExportSenderReportToPdfAsync()
        {
            try
            {
                if (SenderReportSummary == null) return;

                var selectedProps = SenderReportAvailableColumns
                    .Where(c => c.IsSelected)
                    .Select(c => c.PropertyName)
                    .ToList();

                var result = _pdfService.GenerateSenderReportWithDialog(
                    SenderReportFilteredCertificates.ToList(),
                    SenderReportSummary,
                    selectedProps,
                    SelectedSenderReportSender ?? "جهة غير معروفة");

                if (result != null)
                {
                    _notificationService.ShowSuccess($"تم حفظ تقرير الجهة بنجاح");
                    _osService.OpenFile(result);

                    RaiseConfirmation(
                        "الانتهاء من التقرير",
                        "هل تريد الانتهاء من تقرير الجهة المرسلة للبدء بتقرير جديد؟",
                        NotificationType.Question,
                        (confirmed) => 
                        {
                            if (confirmed)
                            {
                                ResetReportingState();
                            }
                        });
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"فشل تصدير التقرير: {ex.Message}");
            }
            await Task.CompletedTask;
        }

        private async Task ExportSenderReportToExcelAsync()
        {
            try
            {
                var selectedProps = SenderReportAvailableColumns
                    .Where(c => c.IsSelected)
                    .Select(c => c.PropertyName)
                    .ToList();

                var result = await _excelService.ExportSenderReportWithDialogAsync(
                    SenderReportFilteredCertificates.ToList(), 
                    SenderReportSummary!,
                    selectedProps,
                    SelectedSenderReportSender ?? "جهة غير معروفة");

                if (result != null)
                {
                    _notificationService.ShowSuccess($"تم حفظ تقرير الجهة (Excel) بنجاح");
                    _osService.OpenFile(result);

                    RaiseConfirmation(
                        "الانتهاء من التقرير",
                        "هل تريد الانتهاء من تقرير الجهة المرسلة للبدء بتقرير جديد؟",
                        NotificationType.Question,
                        (confirmed) => 
                        {
                            if (confirmed)
                            {
                                ResetReportingState();
                            }
                        });
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"فشل تصدير Excel: {ex.Message}");
            }
        }

        private void GenerateDashboard()
        {
            try
            {
                DashboardWidgets = new ObservableCollection<DashboardWidgetViewModel>();
                // Ensure FilteredCertificates is not null
                if (FilteredCertificates == null) return;

                var certs = FilteredCertificates.ToList();
                if (!certs.Any()) return;

                // 1. Sample Count Trend
                if (AvailableColumns.Any(c => c.IsSelected && (c.PropertyName == "SampleCount" || c.PropertyName == "IssueDate")))
                {
                    var days = (EndDate - StartDate).TotalDays;
                    var isDaily = days <= 60;

                    var groupedInfo = certs
                        .GroupBy(c => isDaily ? c.IssueDate.Date : new DateTime(c.IssueDate.Year, c.IssueDate.Month, 1))
                        .OrderBy(g => g.Key)
                        .Select(g => new { Date = g.Key, Count = g.Sum(c => c.SampleCount) })
                        .ToList();

                    var values = new ChartValues<int>(groupedInfo.Select(x => x.Count));
                    var labels = groupedInfo.Select(x => isDaily ? x.Date.ToString("MM/dd") : x.Date.ToString("yyyy/MM")).ToArray();

                    DashboardWidgets.Add(new ColumnWidgetViewModel
                    {
                        Title = "اتجاه عدد العينات",
                        Series = new SeriesCollection
                        {
                            new ColumnSeries
                            {
                                Title = "العينات",
                                Values = values,
                                DataLabels = true
                            }
                        },
                        Labels = labels,
                        ColumnSpan = 2 
                    });
                }

                // 2. Certificate Types (Pie)
                if (AvailableColumns.Any(c => c.IsSelected && c.PropertyName == "CertificateType"))
                {
                    var types = certs.GroupBy(c => c.CertificateType ?? "غير محدد")
                        .Select(g => new { Type = g.Key, Count = g.Count() });

                    var series = new SeriesCollection();
                    foreach (var t in types)
                    {
                        series.Add(new PieSeries
                        {
                            Title = t.Type,
                            Values = new ChartValues<int> { t.Count },
                            DataLabels = true
                        });
                    }

                    DashboardWidgets.Add(new PieWidgetViewModel
                    {
                        Title = "توزيع أنواع الشهادات",
                        Series = series
                    });
                }

                // 3. Top Suppliers (Row)
                if (AvailableColumns.Any(c => c.IsSelected && c.PropertyName == "Supplier"))
                {
                    var topSuppliers = certs.Where(c => !string.IsNullOrEmpty(c.Supplier))
                        .GroupBy(c => c.Supplier)
                        .Select(g => new { Name = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .Take(7)
                        .ToList();

                    var series = new SeriesCollection
                    {
                        new RowSeries
                        {
                            Title = "الشهادات",
                            Values = new ChartValues<int>(topSuppliers.Select(x => x.Count)),
                            DataLabels = true
                        }
                    };

                    DashboardWidgets.Add(new RowWidgetViewModel
                    {
                        Title = "أعلى الموردين نشاطاً",
                        Series = series,
                        Labels = topSuppliers.Select(x => x.Name ?? "غير معروف").ToArray()
                    });
                }

                // 4. Senders (Bottom Left - Pie)
                if (AvailableColumns.Any(c => c.IsSelected && c.PropertyName == "Sender"))
                {
                    var topSenders = certs.Where(c => !string.IsNullOrEmpty(c.Sender))
                        .GroupBy(c => c.Sender)
                        .Select(g => new { Name = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .Take(5)
                        .ToList();

                     var series = new SeriesCollection();
                    foreach (var t in topSenders)
                    {
                        series.Add(new PieSeries
                        {
                            Title = t.Name,
                            Values = new ChartValues<int> { t.Count },
                            DataLabels = true
                        });
                    }

                    DashboardWidgets.Add(new PieWidgetViewModel
                    {
                        Title = "أبرز الجهات المرسلة",
                        Series = series
                    });
                }

                // 5. Environmental vs Consumable (Comparison)
                if (AvailableColumns.Any(c => c.IsSelected && (c.PropertyName == "EnvironmentalSampleCount" || c.PropertyName == "ConsumableSampleCount")))
                {
                    var env = certs.Sum(c => c.EnvironmentalSampleCount);
                    var cons = certs.Sum(c => c.ConsumableSampleCount);

                     var series = new SeriesCollection
                     {
                         new PieSeries { Title = "بيئية", Values = new ChartValues<int> { env }, DataLabels = true },
                         new PieSeries { Title = "استهلاكية", Values = new ChartValues<int> { cons }, DataLabels = true }
                     };

                     DashboardWidgets.Add(new PieWidgetViewModel
                     {
                         Title = "مقارنة أنواع العينات",
                         Series = series
                     });
                }

                // 6. Top Origins (Row)
                if (AvailableColumns.Any(c => c.IsSelected && c.PropertyName == "Origin"))
                {
                    var topOrigins = certs.Where(c => !string.IsNullOrEmpty(c.Origin))
                        .GroupBy(c => c.Origin)
                        .Select(g => new { Name = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .Take(7)
                        .ToList();

                    var series = new SeriesCollection
                    {
                        new RowSeries
                        {
                            Title = "الشهادات",
                            Values = new ChartValues<int>(topOrigins.Select(x => x.Count)),
                            DataLabels = true
                        }
                    };

                    DashboardWidgets.Add(new RowWidgetViewModel
                    {
                        Title = "أهم دول المنشأ",
                        Series = series,
                        Labels = topOrigins.Select(x => x.Name ?? "غير معروف").ToArray()
                    });
                }

                // 7. Monthly Performance Comparison (Line Chart)
                var monthlyData = certs
                    .GroupBy(c => new { c.IssueDate.Year, c.IssueDate.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                    .Select(g => new { Name = $"{g.Key.Year}/{g.Key.Month:D2}", Count = g.Count() })
                    .ToList();

                if (monthlyData.Count > 1)
                {
                    DashboardWidgets.Add(new LineWidgetViewModel
                    {
                        Title = "مقارنة الأداء الشهري",
                        Series = new SeriesCollection
                        {
                            new LineSeries
                            {
                                Title = "الشهادات",
                                Values = new ChartValues<int>(monthlyData.Select(x => x.Count)),
                                PointGeometry = DefaultGeometries.Circle,
                                PointGeometrySize = 10
                            }
                        },
                        Labels = monthlyData.Select(x => x.Name).ToArray(),
                        ColumnSpan = 2
                    });
                }

                // 8. Top Analysis Types (Goods)
                if (AvailableColumns.Any(c => c.IsSelected && c.PropertyName == "AnalysisType"))
                {
                    var topGoods = certs.Where(c => !string.IsNullOrEmpty(c.AnalysisType))
                        .GroupBy(c => c.AnalysisType)
                        .Select(g => new { Name = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .Take(7)
                        .ToList();

                    DashboardWidgets.Add(new RowWidgetViewModel
                    {
                        Title = "تحليل السلع (حسب نوع التحليل)",
                        Series = new SeriesCollection
                        {
                            new RowSeries
                            {
                                Title = "الطلبات",
                                Values = new ChartValues<int>(topGoods.Select(x => x.Count)),
                                DataLabels = true
                            }
                        },
                        Labels = topGoods.Select(x => x.Name ?? "غير معروف").ToArray()
                    });
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"خطأ في توليد اللوحة: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        public List<string> GetSelectedColumnNames()
        {
            return AvailableColumns.Where(c => c.IsSelected).Select(c => c.Name).ToList();
        }

        public List<string> GetSelectedPropertyNames()
        {
            return AvailableColumns.Where(c => c.IsSelected).Select(c => c.PropertyName).ToList();
        }

        /// <summary>
        /// إعادة ضبط حالة التقارير (الخطوات والبيانات) عند التنقل
        /// </summary>
        public void ResetReportingState()
        {
            // 1. إعادة ضبط تقارير ذكية
            CurrentStep = 1;
            IsDataLoaded = false;
            Summary = null;
            FilteredCertificates.Clear();
            StartDate = DateTime.Today.AddMonths(-1);
            EndDate = DateTime.Today;
            foreach (var col in AvailableColumns) col.IsSelected = false;
            InitializeColumns(); // لتفعيل الاختيارات الافتراضية

            // 2. إعادة ضبط تقارير الجهات
            SenderReportCurrentStep = 1;
            SenderReportIsDataLoaded = false;
            SenderReportSummary = null;
            SenderReportFilteredCertificates.Clear();
            SelectedSenderReportSender = null;
            SenderReportStartDate = DateTime.Today.AddMonths(-1);
            SenderReportEndDate = DateTime.Today;
            foreach (var col in SenderReportAvailableColumns) col.IsSelected = false;
            InitializeSenderReportColumns(); // لتفعيل الاختيارات الافتراضية
        }
    }

    /// <summary>
    /// خيار عمود للتقرير
    /// </summary>
    public class ColumnOption : BaseViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }

    public abstract class DashboardWidgetViewModel : BaseViewModel
    {
        public string Title { get; set; } = string.Empty;
        public SeriesCollection Series { get; set; } = new();
        public int ColumnSpan { get; set; } = 1;
    }

    public class PieWidgetViewModel : DashboardWidgetViewModel
    {
        // No extra properties needed for now
    }

    public class ColumnWidgetViewModel : DashboardWidgetViewModel
    {
        public string[] Labels { get; set; } = Array.Empty<string>();
        public Func<double, string> Formatter { get; set; } = x => x.ToString("N0");
    }

    public class RowWidgetViewModel : DashboardWidgetViewModel
    {
        public string[] Labels { get; set; } = Array.Empty<string>();
        public Func<double, string> Formatter { get; set; } = x => x.ToString("N0");
    }

    public class LineWidgetViewModel : DashboardWidgetViewModel
    {
        public string[] Labels { get; set; } = Array.Empty<string>();
        public Func<double, string> Formatter { get; set; } = x => x.ToString("N0");
    }
}
