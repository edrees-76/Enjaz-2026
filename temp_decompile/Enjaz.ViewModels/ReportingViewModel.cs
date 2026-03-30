using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Enjaz.Helpers;
using Enjaz.Models;
using Enjaz.Services;
using LiveCharts;
using LiveCharts.Wpf;

namespace Enjaz.ViewModels;

public class ReportingViewModel : BaseViewModel
{
	private readonly ReportingService _reportingService;

	private readonly INotificationService _notificationService;

	private readonly AdvancedExcelService _excelService;

	private readonly ReportPdfService _pdfService;

	private readonly ChartImageGenerator _chartGenerator;

	private readonly IOSService _osService;

	private List<DistributionItem> _topSuppliers = new List<DistributionItem>();

	private List<DistributionItem> _topSenders = new List<DistributionItem>();

	private int _currentStep = 1;

	private DateTime _startDate = DateTime.Today.AddMonths(-1);

	private DateTime _endDate = DateTime.Today;

	private bool _isLoading;

	private bool _isDataLoaded;

	private ReportSummary? _summary;

	private ObservableCollection<Certificate> _filteredCertificates = new ObservableCollection<Certificate>();

	private ObservableCollection<ColumnOption> _availableColumns = new ObservableCollection<ColumnOption>();

	private SeriesCollection _supplierSeries = new SeriesCollection();

	private SeriesCollection _senderSeries = new SeriesCollection();

	private List<string> _supplierLabels = new List<string>();

	private List<string> _senderLabels = new List<string>();

	private ObservableCollection<DashboardWidgetViewModel> _dashboardWidgets = new ObservableCollection<DashboardWidgetViewModel>();

	private int _senderReportCurrentStep = 1;

	private DateTime _senderReportStartDate = DateTime.Today.AddMonths(-1);

	private DateTime _senderReportEndDate = DateTime.Today;

	private string? _selectedSenderReportSender;

	private ObservableCollection<string> _sendersList = new ObservableCollection<string>();

	private bool _senderReportIsLoading;

	private bool _senderReportIsDataLoaded;

	private ReportSummary? _senderReportSummary;

	private ObservableCollection<Certificate> _senderReportFilteredCertificates = new ObservableCollection<Certificate>();

	private ObservableCollection<ColumnOption> _senderReportAvailableColumns = new ObservableCollection<ColumnOption>();

	public int CurrentStep
	{
		get
		{
			return _currentStep;
		}
		set
		{
			SetProperty(ref _currentStep, value, "CurrentStep");
		}
	}

	public DateTime StartDate
	{
		get
		{
			return _startDate;
		}
		set
		{
			SetProperty(ref _startDate, value, "StartDate");
		}
	}

	public DateTime EndDate
	{
		get
		{
			return _endDate;
		}
		set
		{
			SetProperty(ref _endDate, value, "EndDate");
		}
	}

	public bool IsLoading
	{
		get
		{
			return _isLoading;
		}
		set
		{
			SetProperty(ref _isLoading, value, "IsLoading");
		}
	}

	public bool IsDataLoaded
	{
		get
		{
			return _isDataLoaded;
		}
		set
		{
			SetProperty(ref _isDataLoaded, value, "IsDataLoaded");
		}
	}

	public ReportSummary? Summary
	{
		get
		{
			return _summary;
		}
		set
		{
			SetProperty(ref _summary, value, "Summary");
		}
	}

	public ObservableCollection<Certificate> FilteredCertificates
	{
		get
		{
			return _filteredCertificates;
		}
		set
		{
			SetProperty(ref _filteredCertificates, value, "FilteredCertificates");
		}
	}

	public ObservableCollection<ColumnOption> AvailableColumns
	{
		get
		{
			return _availableColumns;
		}
		set
		{
			SetProperty(ref _availableColumns, value, "AvailableColumns");
		}
	}

	public SeriesCollection SupplierSeries
	{
		get
		{
			return _supplierSeries;
		}
		set
		{
			SetProperty(ref _supplierSeries, value, "SupplierSeries");
		}
	}

	public SeriesCollection SenderSeries
	{
		get
		{
			return _senderSeries;
		}
		set
		{
			SetProperty(ref _senderSeries, value, "SenderSeries");
		}
	}

	public List<string> SupplierLabels
	{
		get
		{
			return _supplierLabels;
		}
		set
		{
			SetProperty(ref _supplierLabels, value, "SupplierLabels");
		}
	}

	public List<string> SenderLabels
	{
		get
		{
			return _senderLabels;
		}
		set
		{
			SetProperty(ref _senderLabels, value, "SenderLabels");
		}
	}

	public ObservableCollection<DashboardWidgetViewModel> DashboardWidgets
	{
		get
		{
			return _dashboardWidgets;
		}
		set
		{
			SetProperty(ref _dashboardWidgets, value, "DashboardWidgets");
		}
	}

	public int SenderReportCurrentStep
	{
		get
		{
			return _senderReportCurrentStep;
		}
		set
		{
			SetProperty(ref _senderReportCurrentStep, value, "SenderReportCurrentStep");
		}
	}

	public DateTime SenderReportStartDate
	{
		get
		{
			return _senderReportStartDate;
		}
		set
		{
			SetProperty(ref _senderReportStartDate, value, "SenderReportStartDate");
		}
	}

	public DateTime SenderReportEndDate
	{
		get
		{
			return _senderReportEndDate;
		}
		set
		{
			SetProperty(ref _senderReportEndDate, value, "SenderReportEndDate");
		}
	}

	public string? SelectedSenderReportSender
	{
		get
		{
			return _selectedSenderReportSender;
		}
		set
		{
			SetProperty(ref _selectedSenderReportSender, value, "SelectedSenderReportSender");
		}
	}

	public ObservableCollection<string> SendersList
	{
		get
		{
			return _sendersList;
		}
		set
		{
			SetProperty(ref _sendersList, value, "SendersList");
		}
	}

	public bool SenderReportIsLoading
	{
		get
		{
			return _senderReportIsLoading;
		}
		set
		{
			SetProperty(ref _senderReportIsLoading, value, "SenderReportIsLoading");
		}
	}

	public bool SenderReportIsDataLoaded
	{
		get
		{
			return _senderReportIsDataLoaded;
		}
		set
		{
			SetProperty(ref _senderReportIsDataLoaded, value, "SenderReportIsDataLoaded");
		}
	}

	public ReportSummary? SenderReportSummary
	{
		get
		{
			return _senderReportSummary;
		}
		set
		{
			SetProperty(ref _senderReportSummary, value, "SenderReportSummary");
		}
	}

	public ObservableCollection<Certificate> SenderReportFilteredCertificates
	{
		get
		{
			return _senderReportFilteredCertificates;
		}
		set
		{
			SetProperty(ref _senderReportFilteredCertificates, value, "SenderReportFilteredCertificates");
		}
	}

	public ObservableCollection<ColumnOption> SenderReportAvailableColumns
	{
		get
		{
			return _senderReportAvailableColumns;
		}
		set
		{
			SetProperty(ref _senderReportAvailableColumns, value, "SenderReportAvailableColumns");
		}
	}

	public ICommand LoadDataCommand { get; private set; } = null;

	public ICommand NextStepCommand { get; private set; } = null;

	public ICommand PreviousStepCommand { get; private set; } = null;

	public ICommand ExportPdfCommand { get; private set; } = null;

	public ICommand ExportExcelCommand { get; private set; } = null;

	public ICommand OpenDashboardCommand { get; private set; } = null;

	public ICommand LoadSenderReportDataCommand { get; private set; } = null;

	public ICommand SenderReportNextStepCommand { get; private set; } = null;

	public ICommand SenderReportPreviousStepCommand { get; private set; } = null;

	public ICommand ExportSenderReportPdfCommand { get; private set; } = null;

	public ICommand ExportSenderReportExcelCommand { get; private set; } = null;

	public event Action? RequestOpenDashboard;

	public ReportingViewModel(ReportingService reportingService, INotificationService notificationService, AdvancedExcelService excelService, ReportPdfService pdfService, ChartImageGenerator chartGenerator, IOSService osService)
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
		LoadSendersListAsync();
	}

	private void InitializeCommands()
	{
		LoadDataCommand = new RelayCommand(async delegate
		{
			await LoadDataAsync();
		});
		OpenDashboardCommand = new RelayCommand(delegate
		{
			this.RequestOpenDashboard?.Invoke();
		});
		NextStepCommand = new RelayCommand(delegate
		{
			CurrentStep++;
			if (CurrentStep == 3)
			{
				GenerateDashboard();
			}
		}, (object? _) => CurrentStep < 4 && IsDataLoaded);
		PreviousStepCommand = new RelayCommand(delegate
		{
			CurrentStep--;
		}, (object? _) => CurrentStep > 1);
		ExportPdfCommand = new RelayCommand(async delegate
		{
			await ExportToPdfAsync();
		}, (object? _) => IsDataLoaded);
		ExportExcelCommand = new RelayCommand(async delegate
		{
			await ExportToExcelAsync();
		}, (object? _) => IsDataLoaded);
		LoadSenderReportDataCommand = new RelayCommand(async delegate
		{
			await LoadSenderReportDataAsync();
		});
		SenderReportNextStepCommand = new RelayCommand(delegate(object? _)
		{
			if (SenderReportCurrentStep == 1 && (string.IsNullOrWhiteSpace(SelectedSenderReportSender) || SenderReportStartDate > SenderReportEndDate))
			{
				_notificationService.ShowError("يرجى اختيار جهة وتاريخين صحيحين للمتابعة");
			}
			else if (SenderReportCurrentStep == 2 && !SenderReportAvailableColumns.Any((ColumnOption c) => c.IsSelected))
			{
				_notificationService.ShowError("يرجى اختيار عمود واحد على الأقل");
			}
			else
			{
				SenderReportCurrentStep++;
				if (SenderReportCurrentStep == 3)
				{
					_ = LoadSenderReportDataAsync();
				}
			}
		}, (object? _) => SenderReportCurrentStep < 3);
		SenderReportPreviousStepCommand = new RelayCommand(delegate
		{
			SenderReportCurrentStep--;
		}, (object? _) => SenderReportCurrentStep > 1);
		ExportSenderReportPdfCommand = new RelayCommand(async delegate
		{
			await ExportSenderReportToPdfAsync();
		}, (object? _) => SenderReportIsDataLoaded);
		ExportSenderReportExcelCommand = new RelayCommand(async delegate
		{
			await ExportSenderReportToExcelAsync();
		}, (object? _) => SenderReportIsDataLoaded);
	}

	private void InitializeSenderReportColumns()
	{
		SenderReportAvailableColumns = new ObservableCollection<ColumnOption>
		{
			new ColumnOption
			{
				Name = "رقم الشهادة",
				PropertyName = "CertificateNumber",
				IsSelected = true
			},
			new ColumnOption
			{
				Name = "نوع الشهادة",
				PropertyName = "CertificateType",
				IsSelected = true
			},
			new ColumnOption
			{
				Name = "تاريخ الإصدار",
				PropertyName = "IssueDate",
				IsSelected = true
			},
			new ColumnOption
			{
				Name = "المورد",
				PropertyName = "Supplier",
				IsSelected = true
			},
			new ColumnOption
			{
				Name = "بلد المنشأ",
				PropertyName = "Origin",
				IsSelected = true
			},
			new ColumnOption
			{
				Name = "رقم الإخطار",
				PropertyName = "NotificationNumber",
				IsSelected = true
			},
			new ColumnOption
			{
				Name = "رقم الإقرار الجمركي",
				PropertyName = "DeclarationNumber",
				IsSelected = true
			},
			new ColumnOption
			{
				Name = "رقم الإيصال المالي",
				PropertyName = "FinancialReceiptNumber",
				IsSelected = true
			},
			new ColumnOption
			{
				Name = "عدد العينات",
				PropertyName = "SampleCount",
				IsSelected = true
			},
			new ColumnOption
			{
				Name = "عدد عينات بيئية",
				PropertyName = "EnvironmentalSampleCount",
				IsSelected = true
			},
			new ColumnOption
			{
				Name = "عدد عينات استهلاكية",
				PropertyName = "ConsumableSampleCount",
				IsSelected = true
			},
			new ColumnOption
			{
				Name = "اسم المستخدم",
				PropertyName = "CreatedByName",
				IsSelected = true
			}
		};
	}

	private async Task LoadSendersListAsync()
	{
		try
		{
			SendersList = new ObservableCollection<string>(await _reportingService.GetUniqueSendersAsync());
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			LoggerService.LogError("Failed to load senders list", ex2);
		}
	}

	private async Task LoadSenderReportDataAsync()
	{
		if (string.IsNullOrWhiteSpace(SelectedSenderReportSender))
		{
			return;
		}
		SenderReportIsLoading = true;
		SenderReportIsDataLoaded = false;
		try
		{
			(ReportSummary Summary, List<Certificate> Certificates) result = await _reportingService.GetSenderReportDataAsync(SenderReportStartDate, SenderReportEndDate, SelectedSenderReportSender);
			SenderReportSummary = result.Summary;
			SenderReportFilteredCertificates = new ObservableCollection<Certificate>(result.Certificates);
			SenderReportIsDataLoaded = true;
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			_notificationService.ShowError("خطأ في تحميل بيانات التقرير: " + ex2.Message);
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
			new ColumnOption
			{
				Name = "رقم الشهادة",
				PropertyName = "CertificateNumber",
				IsSelected = true
			},
			new ColumnOption
			{
				Name = "نوع الشهادة",
				PropertyName = "CertificateType",
				IsSelected = true
			},
			new ColumnOption
			{
				Name = "تاريخ الإصدار",
				PropertyName = "IssueDate",
				IsSelected = true
			},
			new ColumnOption
			{
				Name = "الجهة المرسلة",
				PropertyName = "Sender",
				IsSelected = true
			},
			new ColumnOption
			{
				Name = "المورد",
				PropertyName = "Supplier",
				IsSelected = true
			},
			new ColumnOption
			{
				Name = "بلد المنشأ",
				PropertyName = "Origin",
				IsSelected = true
			},
			new ColumnOption
			{
				Name = "رقم الإخطار",
				PropertyName = "NotificationNumber",
				IsSelected = false
			},
			new ColumnOption
			{
				Name = "رقم الإقرار الجمركي",
				PropertyName = "DeclarationNumber",
				IsSelected = false
			},
			new ColumnOption
			{
				Name = "رقم الإيصال المالي",
				PropertyName = "FinancialReceiptNumber",
				IsSelected = false
			},
			new ColumnOption
			{
				Name = "عدد العينات",
				PropertyName = "SampleCount",
				IsSelected = true
			},
			new ColumnOption
			{
				Name = "عدد عينات بيئية",
				PropertyName = "EnvironmentalSampleCount",
				IsSelected = false
			},
			new ColumnOption
			{
				Name = "عدد عينات استهلاكية",
				PropertyName = "ConsumableSampleCount",
				IsSelected = false
			},
			new ColumnOption
			{
				Name = "اسم المستخدم",
				PropertyName = "CreatedByName",
				IsSelected = false
			}
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
			Summary = await _reportingService.GetReportSummaryAsync(StartDate, EndDate);
			List<Certificate> certs = await _reportingService.GetCertificatesByDateRangeAsync(StartDate, EndDate);
			FilteredCertificates = new ObservableCollection<Certificate>(certs);
			await LoadChartDataAsync();
			IsDataLoaded = true;
			_notificationService.ShowSuccess($"تم تحميل {certs.Count} شهادة");
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			_notificationService.ShowError("خطأ في تحميل البيانات: " + ex2.Message);
		}
		finally
		{
			IsLoading = false;
		}
	}

	private async Task LoadChartDataAsync()
	{
		_topSuppliers = await _reportingService.GetTopSuppliersAsync(StartDate, EndDate, 5);
		SupplierLabels = _topSuppliers.Select((DistributionItem s) => s.Name).ToList();
		SupplierSeries = new SeriesCollection
		{
			new PieSeries
			{
				Title = "الموردين",
				Values = new ChartValues<int>(_topSuppliers.Select((DistributionItem s) => s.Count)),
				DataLabels = true
			}
		};
		_topSenders = await _reportingService.GetTopSendersAsync(StartDate, EndDate, 5);
		SenderLabels = _topSenders.Select((DistributionItem s) => s.Name).ToList();
		SenderSeries = new SeriesCollection
		{
			new PieSeries
			{
				Title = "الجهات المرسلة",
				Values = new ChartValues<int>(_topSenders.Select((DistributionItem s) => s.Count)),
				DataLabels = true
			}
		};
	}

	private async Task ExportToPdfAsync()
	{
		try
		{
			if (Summary == null)
			{
				return;
			}
			Dictionary<string, byte[]> chartImages = _chartGenerator.GenerateChartImages(DashboardWidgets);
			List<string> selectedProps = GetSelectedPropertyNames();
			string result = _pdfService.GenerateReportWithDialog(FilteredCertificates.ToList(), Summary, selectedProps, _topSuppliers, _topSenders, chartImages);
			if (result != null)
			{
				if (File.Exists(result))
				{
					_notificationService.ShowSuccess("تم حفظ التقرير بنجاح: " + Path.GetFileName(result));
					_osService.OpenFile(result);
					RaiseConfirmation("الانتهاء من التقرير", "هل تريد الانتهاء من التقرير العام وإغلاق هذه النافذة للبدء بتقرير جديد؟", NotificationType.Question, delegate(bool confirmed)
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
			Exception ex2 = ex;
			_notificationService.ShowError("فشل التصدير: " + ex2.Message);
		}
		await Task.CompletedTask;
	}

	private async Task ExportToExcelAsync()
	{
		try
		{
			if (Summary == null)
			{
				return;
			}
			Dictionary<string, byte[]> chartImages = _chartGenerator.GenerateChartImages(DashboardWidgets);
			List<string> selectedProps = GetSelectedPropertyNames();
			string result = _excelService.ExportReportWithDialog(FilteredCertificates.ToList(), Summary, selectedProps, _topSuppliers, _topSenders, chartImages);
			if (result != null)
			{
				_notificationService.ShowSuccess("تم حفظ التقرير: " + result);
				_osService.OpenFile(result);
				RaiseConfirmation("الانتهاء من التقرير", "هل تريد الانتهاء من التقرير العام وإغلاق هذه النافذة للبدء بتقرير جديد؟", NotificationType.Question, delegate(bool confirmed)
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
			Exception ex2 = ex;
			_notificationService.ShowError("فشل التصدير: " + ex2.Message);
		}
		await Task.CompletedTask;
	}

	private async Task ExportSenderReportToPdfAsync()
	{
		try
		{
			if (SenderReportSummary == null)
			{
				return;
			}
			List<string> selectedProps = (from c in SenderReportAvailableColumns
				where c.IsSelected
				select c.PropertyName).ToList();
			string result = _pdfService.GenerateSenderReportWithDialog(SenderReportFilteredCertificates.ToList(), SenderReportSummary, selectedProps, SelectedSenderReportSender ?? "جهة غير معروفة");
			if (result != null)
			{
				_notificationService.ShowSuccess("تم حفظ تقرير الجهة بنجاح");
				_osService.OpenFile(result);
				RaiseConfirmation("الانتهاء من التقرير", "هل تريد الانتهاء من تقرير الجهة المرسلة للبدء بتقرير جديد؟", NotificationType.Question, delegate(bool confirmed)
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
			Exception ex2 = ex;
			_notificationService.ShowError("فشل تصدير التقرير: " + ex2.Message);
		}
		await Task.CompletedTask;
	}

	private async Task ExportSenderReportToExcelAsync()
	{
		try
		{
			List<string> selectedProps = (from c in SenderReportAvailableColumns
				where c.IsSelected
				select c.PropertyName).ToList();
			string result = await _excelService.ExportSenderReportWithDialogAsync(SenderReportFilteredCertificates.ToList(), SenderReportSummary, selectedProps, SelectedSenderReportSender ?? "جهة غير معروفة");
			if (result == null)
			{
				return;
			}
			_notificationService.ShowSuccess("تم حفظ تقرير الجهة (Excel) بنجاح");
			_osService.OpenFile(result);
			RaiseConfirmation("الانتهاء من التقرير", "هل تريد الانتهاء من تقرير الجهة المرسلة للبدء بتقرير جديد؟", NotificationType.Question, delegate(bool confirmed)
			{
				if (confirmed)
				{
					ResetReportingState();
				}
			});
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			_notificationService.ShowError("فشل تصدير Excel: " + ex2.Message);
		}
	}

	private void GenerateDashboard()
	{
		try
		{
			DashboardWidgets = new ObservableCollection<DashboardWidgetViewModel>();
			if (FilteredCertificates == null)
			{
				return;
			}
			List<Certificate> source = FilteredCertificates.ToList();
			if (!source.Any())
			{
				return;
			}
			if (AvailableColumns.Any((ColumnOption c) => c.IsSelected && (c.PropertyName == "SampleCount" || c.PropertyName == "IssueDate")))
			{
				double totalDays = (EndDate - StartDate).TotalDays;
				bool isDaily = totalDays <= 60.0;
				var source2 = (from c in source
					group c by isDaily ? c.IssueDate.Date : new DateTime(c.IssueDate.Year, c.IssueDate.Month, 1) into g
					orderby g.Key
					select new
					{
						Date = g.Key,
						Count = g.Sum((Certificate c) => c.SampleCount)
					}).ToList();
				ChartValues<int> values = new ChartValues<int>(source2.Select(x => x.Count));
				string[] labels = source2.Select(x => isDaily ? x.Date.ToString("MM/dd") : x.Date.ToString("yyyy/MM")).ToArray();
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
			if (AvailableColumns.Any((ColumnOption c) => c.IsSelected && c.PropertyName == "CertificateType"))
			{
				var enumerable = from c in source
					group c by c.CertificateType ?? "غير محدد" into g
					select new
					{
						Type = g.Key,
						Count = g.Count()
					};
				SeriesCollection seriesCollection = new SeriesCollection();
				foreach (var item3 in enumerable)
				{
					seriesCollection.Add(new PieSeries
					{
						Title = item3.Type,
						Values = new ChartValues<int> { item3.Count },
						DataLabels = true
					});
				}
				DashboardWidgets.Add(new PieWidgetViewModel
				{
					Title = "توزيع أنواع الشهادات",
					Series = seriesCollection
				});
			}
			if (AvailableColumns.Any((ColumnOption c) => c.IsSelected && c.PropertyName == "Supplier"))
			{
				var source3 = (from c in source
					where !string.IsNullOrEmpty(c.Supplier)
					group c by c.Supplier into g
					select new
					{
						Name = g.Key,
						Count = g.Count()
					} into x
					orderby x.Count descending
					select x).Take(7).ToList();
				SeriesCollection series = new SeriesCollection
				{
					new RowSeries
					{
						Title = "الشهادات",
						Values = new ChartValues<int>(source3.Select(x => x.Count)),
						DataLabels = true
					}
				};
				DashboardWidgets.Add(new RowWidgetViewModel
				{
					Title = "أعلى الموردين نشاطا\u064b",
					Series = series,
					Labels = source3.Select(x => x.Name ?? "غير معروف").ToArray()
				});
			}
			if (AvailableColumns.Any((ColumnOption c) => c.IsSelected && c.PropertyName == "Sender"))
			{
				var list = (from c in source
					where !string.IsNullOrEmpty(c.Sender)
					group c by c.Sender into g
					select new
					{
						Name = g.Key,
						Count = g.Count()
					} into x
					orderby x.Count descending
					select x).Take(5).ToList();
				SeriesCollection seriesCollection2 = new SeriesCollection();
				foreach (var item4 in list)
				{
					seriesCollection2.Add(new PieSeries
					{
						Title = item4.Name,
						Values = new ChartValues<int> { item4.Count },
						DataLabels = true
					});
				}
				DashboardWidgets.Add(new PieWidgetViewModel
				{
					Title = "أبرز الجهات المرسلة",
					Series = seriesCollection2
				});
			}
			if (AvailableColumns.Any((ColumnOption c) => c.IsSelected && (c.PropertyName == "EnvironmentalSampleCount" || c.PropertyName == "ConsumableSampleCount")))
			{
				int item = source.Sum((Certificate c) => c.EnvironmentalSampleCount);
				int item2 = source.Sum((Certificate c) => c.ConsumableSampleCount);
				SeriesCollection series2 = new SeriesCollection
				{
					new PieSeries
					{
						Title = "بيئية",
						Values = new ChartValues<int> { item },
						DataLabels = true
					},
					new PieSeries
					{
						Title = "استهلاكية",
						Values = new ChartValues<int> { item2 },
						DataLabels = true
					}
				};
				DashboardWidgets.Add(new PieWidgetViewModel
				{
					Title = "مقارنة أنواع العينات",
					Series = series2
				});
			}
			if (AvailableColumns.Any((ColumnOption c) => c.IsSelected && c.PropertyName == "Origin"))
			{
				var source4 = (from c in source
					where !string.IsNullOrEmpty(c.Origin)
					group c by c.Origin into g
					select new
					{
						Name = g.Key,
						Count = g.Count()
					} into x
					orderby x.Count descending
					select x).Take(7).ToList();
				SeriesCollection series3 = new SeriesCollection
				{
					new RowSeries
					{
						Title = "الشهادات",
						Values = new ChartValues<int>(source4.Select(x => x.Count)),
						DataLabels = true
					}
				};
				DashboardWidgets.Add(new RowWidgetViewModel
				{
					Title = "أهم دول المنشأ",
					Series = series3,
					Labels = source4.Select(x => x.Name ?? "غير معروف").ToArray()
				});
			}
			var list2 = (from c in source
				group c by new
				{
					c.IssueDate.Year,
					c.IssueDate.Month
				} into g
				orderby g.Key.Year, g.Key.Month
				select new
				{
					Name = $"{g.Key.Year}/{g.Key.Month:D2}",
					Count = g.Count()
				}).ToList();
			if (list2.Count > 1)
			{
				DashboardWidgets.Add(new LineWidgetViewModel
				{
					Title = "مقارنة الأداء الشهري",
					Series = new SeriesCollection
					{
						new LineSeries
						{
							Title = "الشهادات",
							Values = new ChartValues<int>(list2.Select(x => x.Count)),
							PointGeometry = DefaultGeometries.Circle,
							PointGeometrySize = 10.0
						}
					},
					Labels = list2.Select(x => x.Name).ToArray(),
					ColumnSpan = 2
				});
			}
			if (AvailableColumns.Any((ColumnOption c) => c.IsSelected && c.PropertyName == "AnalysisType"))
			{
				var source5 = (from c in source
					where !string.IsNullOrEmpty(c.AnalysisType)
					group c by c.AnalysisType into g
					select new
					{
						Name = g.Key,
						Count = g.Count()
					} into x
					orderby x.Count descending
					select x).Take(7).ToList();
				DashboardWidgets.Add(new RowWidgetViewModel
				{
					Title = "تحليل السلع (حسب نوع التحليل)",
					Series = new SeriesCollection
					{
						new RowSeries
						{
							Title = "الطلبات",
							Values = new ChartValues<int>(source5.Select(x => x.Count)),
							DataLabels = true
						}
					},
					Labels = source5.Select(x => x.Name ?? "غير معروف").ToArray()
				});
			}
		}
		catch (Exception ex)
		{
			_notificationService.ShowError("خطأ في توليد اللوحة: " + ex.Message);
			Console.WriteLine(ex.StackTrace);
		}
	}

	public List<string> GetSelectedColumnNames()
	{
		return (from c in AvailableColumns
			where c.IsSelected
			select c.Name).ToList();
	}

	public List<string> GetSelectedPropertyNames()
	{
		return (from c in AvailableColumns
			where c.IsSelected
			select c.PropertyName).ToList();
	}

	public void ResetReportingState()
	{
		CurrentStep = 1;
		IsDataLoaded = false;
		Summary = null;
		FilteredCertificates.Clear();
		StartDate = DateTime.Today.AddMonths(-1);
		EndDate = DateTime.Today;
		foreach (ColumnOption availableColumn in AvailableColumns)
		{
			availableColumn.IsSelected = false;
		}
		InitializeColumns();
		SenderReportCurrentStep = 1;
		SenderReportIsDataLoaded = false;
		SenderReportSummary = null;
		SenderReportFilteredCertificates.Clear();
		SelectedSenderReportSender = null;
		SenderReportStartDate = DateTime.Today.AddMonths(-1);
		SenderReportEndDate = DateTime.Today;
		foreach (ColumnOption senderReportAvailableColumn in SenderReportAvailableColumns)
		{
			senderReportAvailableColumn.IsSelected = false;
		}
		InitializeSenderReportColumns();
	}
}
