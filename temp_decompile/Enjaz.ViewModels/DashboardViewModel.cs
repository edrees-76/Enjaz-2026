using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Enjaz.Models;
using Enjaz.Services;
using Enjaz.Services.Repositories;
using LiveCharts;
using LiveCharts.Wpf;

namespace Enjaz.ViewModels;

public class DashboardViewModel : BaseViewModel
{
	private readonly CertificateRepository _certificateRepository;

	private readonly DatabaseService _databaseService;

	private ObservableCollection<AuditLog> _recentActivities;

	private SeriesCollection _dashboardMonthlySeries;

	private SeriesCollection _typeSeries;

	private string[] _monthlyLabels;

	private Func<double, string> _yFormatter;

	private int _totalCertificatesCount;

	private int _todayCertificatesCount;

	private int _environmentalCertificatesCount;

	private int _consumerCertificatesCount;

	private int _totalSamplesCount;

	private int _todaySamplesCount;

	private int _environmentalSamplesCount;

	private int _consumableSamplesCount;

	private SeriesCollection _sampleMonthlySeries;

	private SeriesCollection _sampleTypeSeries;

	public SeriesCollection DashboardMonthlySeries
	{
		get
		{
			return _dashboardMonthlySeries;
		}
		set
		{
			SetProperty(ref _dashboardMonthlySeries, value, "DashboardMonthlySeries");
		}
	}

	public SeriesCollection TypeSeries
	{
		get
		{
			return _typeSeries;
		}
		set
		{
			SetProperty(ref _typeSeries, value, "TypeSeries");
		}
	}

	public SeriesCollection SampleMonthlySeries
	{
		get
		{
			return _sampleMonthlySeries;
		}
		set
		{
			SetProperty(ref _sampleMonthlySeries, value, "SampleMonthlySeries");
		}
	}

	public SeriesCollection SampleTypeSeries
	{
		get
		{
			return _sampleTypeSeries;
		}
		set
		{
			SetProperty(ref _sampleTypeSeries, value, "SampleTypeSeries");
		}
	}

	public ObservableCollection<AuditLog> RecentActivities
	{
		get
		{
			return _recentActivities;
		}
		set
		{
			SetProperty(ref _recentActivities, value, "RecentActivities");
		}
	}

	public string[] MonthlyLabels
	{
		get
		{
			return _monthlyLabels;
		}
		set
		{
			SetProperty(ref _monthlyLabels, value, "MonthlyLabels");
		}
	}

	public Func<double, string> YFormatter
	{
		get
		{
			return _yFormatter;
		}
		set
		{
			SetProperty(ref _yFormatter, value, "YFormatter");
		}
	}

	public int TotalCertificatesCount
	{
		get
		{
			return _totalCertificatesCount;
		}
		set
		{
			SetProperty(ref _totalCertificatesCount, value, "TotalCertificatesCount");
		}
	}

	public int TodayCertificatesCount
	{
		get
		{
			return _todayCertificatesCount;
		}
		set
		{
			SetProperty(ref _todayCertificatesCount, value, "TodayCertificatesCount");
		}
	}

	public int EnvironmentalCertificatesCount
	{
		get
		{
			return _environmentalCertificatesCount;
		}
		set
		{
			SetProperty(ref _environmentalCertificatesCount, value, "EnvironmentalCertificatesCount");
		}
	}

	public int ConsumerCertificatesCount
	{
		get
		{
			return _consumerCertificatesCount;
		}
		set
		{
			SetProperty(ref _consumerCertificatesCount, value, "ConsumerCertificatesCount");
		}
	}

	public int TotalSamplesCount
	{
		get
		{
			return _totalSamplesCount;
		}
		set
		{
			SetProperty(ref _totalSamplesCount, value, "TotalSamplesCount");
		}
	}

	public int TodaySamplesCount
	{
		get
		{
			return _todaySamplesCount;
		}
		set
		{
			SetProperty(ref _todaySamplesCount, value, "TodaySamplesCount");
		}
	}

	public int EnvironmentalSamplesCount
	{
		get
		{
			return _environmentalSamplesCount;
		}
		set
		{
			SetProperty(ref _environmentalSamplesCount, value, "EnvironmentalSamplesCount");
		}
	}

	public int ConsumableSamplesCount
	{
		get
		{
			return _consumableSamplesCount;
		}
		set
		{
			SetProperty(ref _consumableSamplesCount, value, "ConsumableSamplesCount");
		}
	}

	public DashboardViewModel(CertificateRepository certificateRepository, DatabaseService databaseService)
	{
		_certificateRepository = certificateRepository;
		_databaseService = databaseService;
		_dashboardMonthlySeries = new SeriesCollection();
		_typeSeries = new SeriesCollection();
		_sampleMonthlySeries = new SeriesCollection();
		_sampleTypeSeries = new SeriesCollection();
		_recentActivities = new ObservableCollection<AuditLog>();
		_monthlyLabels = Array.Empty<string>();
		_yFormatter = (double value) => value.ToString("N0");
	}

	public async Task LoadDashboardDataAsync()
	{
		try
		{
			base.IsBusy = true;
			base.BusyMessage = "جاري تحميل بيانات لوحة التحكم...";
			int currentYear = DateTime.Now.Year;
			Task<int> totalCertTask = _certificateRepository.GetTotalCertificatesCountAsync(showDeleted: false, currentYear);
			Task<int> todayCertTask = _certificateRepository.GetCertificatesCountByDateAsync(DateTime.Now);
			Task<int> envCertTask = _certificateRepository.GetCertificatesCountByTypeAsync("بيئية", currentYear);
			Task<int> conCertTask = _certificateRepository.GetCertificatesCountByTypeAsync("استهلاكية", currentYear);
			Task<int> totalSampleTask = _certificateRepository.GetTotalSamplesCountAsync(currentYear);
			Task<int> todaySampleTask = _certificateRepository.GetSamplesCountByDateAsync(DateTime.Now);
			Task<int> envSampleTask = _certificateRepository.GetSamplesCountByTypeAsync("بيئية", currentYear);
			Task<int> conSampleTask = _certificateRepository.GetSamplesCountByTypeAsync("استهلاكية", currentYear);
			Task<List<AuditLog>> auditLogsTask = _databaseService.GetAuditLogsAsync(null, null, null, 5);
			await Task.WhenAll(totalCertTask, todayCertTask, envCertTask, conCertTask, totalSampleTask, todaySampleTask, envSampleTask, conSampleTask, auditLogsTask);
			TotalCertificatesCount = await totalCertTask;
			TodayCertificatesCount = await todayCertTask;
			EnvironmentalCertificatesCount = await envCertTask;
			ConsumerCertificatesCount = await conCertTask;
			TotalSamplesCount = await totalSampleTask;
			TodaySamplesCount = await todaySampleTask;
			EnvironmentalSamplesCount = await envSampleTask;
			ConsumableSamplesCount = await conSampleTask;
			RecentActivities = new ObservableCollection<AuditLog>(await auditLogsTask);
			TypeSeries = new SeriesCollection
			{
				new PieSeries
				{
					Title = "عينات بيئية",
					Values = new ChartValues<int> { EnvironmentalCertificatesCount },
					DataLabels = true,
					Fill = (Brush)Application.Current.Resources["SuccessBrush"]
				},
				new PieSeries
				{
					Title = "عينات استهلاكية",
					Values = new ChartValues<int> { ConsumerCertificatesCount },
					DataLabels = true,
					Fill = (Brush)Application.Current.Resources["SecondaryActionBrush"]
				}
			};
			Task<Dictionary<int, int>> envStatsTask = _certificateRepository.GetMonthlyStatisticsByTypeAsync(DateTime.Now.Year, "بيئية");
			Task<Dictionary<int, int>> conStatsTask = _certificateRepository.GetMonthlyStatisticsByTypeAsync(DateTime.Now.Year, "استهلاكية");
			await Task.WhenAll<Dictionary<int, int>>(envStatsTask, conStatsTask);
			Dictionary<int, int> envStats = await envStatsTask;
			Dictionary<int, int> conStats = await conStatsTask;
			ChartValues<int> envValues = new ChartValues<int>();
			ChartValues<int> conValues = new ChartValues<int>();
			string[] labels = new string[12];
			string[] arabicMonths = new string[12]
			{
				"يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو", "يوليو", "أغسطس", "سبتمبر", "أكتوبر",
				"نوفمبر", "ديسمبر"
			};
			for (int i = 1; i <= 12; i++)
			{
				envValues.Add(envStats.TryGetValue(i, out var envVal) ? envVal : 0);
				conValues.Add(conStats.TryGetValue(i, out var conVal) ? conVal : 0);
				labels[i - 1] = arabicMonths[i - 1];
			}
			DashboardMonthlySeries = new SeriesCollection
			{
				new ColumnSeries
				{
					Title = "عينات بيئية",
					Values = envValues,
					DataLabels = true,
					LabelPoint = (ChartPoint point) => (point.Y > 0.0) ? point.Y.ToString() : "",
					Fill = (Brush)Application.Current.Resources["SuccessBrush"]
				},
				new ColumnSeries
				{
					Title = "عينات استهلاكية",
					Values = conValues,
					DataLabels = true,
					LabelPoint = (ChartPoint point) => (point.Y > 0.0) ? point.Y.ToString() : "",
					Fill = (Brush)Application.Current.Resources["SecondaryActionBrush"]
				}
			};
			MonthlyLabels = labels;
			YFormatter = (double value) => value.ToString("N0");
			SampleTypeSeries = new SeriesCollection
			{
				new PieSeries
				{
					Title = "عينات بيئية",
					Values = new ChartValues<int> { EnvironmentalSamplesCount },
					DataLabels = true,
					Fill = (Brush)Application.Current.Resources["SuccessBrush"]
				},
				new PieSeries
				{
					Title = "عينات استهلاكية",
					Values = new ChartValues<int> { ConsumableSamplesCount },
					DataLabels = true,
					Fill = (Brush)Application.Current.Resources["SecondaryActionBrush"]
				}
			};
			Task<Dictionary<int, int>> envSampleStatsTask = _certificateRepository.GetMonthlySamplesStatisticsByTypeAsync(DateTime.Now.Year, "بيئية");
			Task<Dictionary<int, int>> conSampleStatsTask = _certificateRepository.GetMonthlySamplesStatisticsByTypeAsync(DateTime.Now.Year, "استهلاكية");
			await Task.WhenAll<Dictionary<int, int>>(envSampleStatsTask, conSampleStatsTask);
			Dictionary<int, int> envSampleStats = await envSampleStatsTask;
			Dictionary<int, int> conSampleStats = await conSampleStatsTask;
			ChartValues<int> envSampleValues = new ChartValues<int>();
			ChartValues<int> conSampleValues = new ChartValues<int>();
			for (int i2 = 1; i2 <= 12; i2++)
			{
				envSampleValues.Add(envSampleStats.TryGetValue(i2, out var envSampleVal) ? envSampleVal : 0);
				conSampleValues.Add(conSampleStats.TryGetValue(i2, out var conSampleVal) ? conSampleVal : 0);
			}
			SampleMonthlySeries = new SeriesCollection
			{
				new ColumnSeries
				{
					Title = "عينات بيئية",
					Values = envSampleValues,
					DataLabels = true,
					LabelPoint = (ChartPoint point) => (point.Y > 0.0) ? point.Y.ToString() : "",
					Fill = (Brush)Application.Current.Resources["SuccessBrush"]
				},
				new ColumnSeries
				{
					Title = "عينات استهلاكية",
					Values = conSampleValues,
					DataLabels = true,
					LabelPoint = (ChartPoint point) => (point.Y > 0.0) ? point.Y.ToString() : "",
					Fill = (Brush)Application.Current.Resources["SecondaryActionBrush"]
				}
			};
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			base.StatusMessage = "خطأ في تحميل بيانات الإحصائيات: " + ex2.Message;
			Console.WriteLine("Error loading dashboard data: " + ex2.Message);
		}
		finally
		{
			base.IsBusy = false;
		}
	}
}
