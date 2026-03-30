using System;
using System.Windows;
using Enjaz.Services.Repositories;
using LiveCharts;
using LiveCharts.Wpf;
using System.Collections.ObjectModel;
using Enjaz.Models;
using Enjaz.Services;

namespace Enjaz.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly CertificateRepository _certificateRepository;
        private readonly DatabaseService _databaseService;

        // Dashboard Properties
        private ObservableCollection<int> _availableYears;
        private int _selectedYear;
        private bool _showRecentActivities;
        private ObservableCollection<AuditLog> _recentActivities;
        private SeriesCollection _dashboardMonthlySeries;
        private SeriesCollection _typeSeries;
        private string[] _monthlyLabels;
        private Func<double, string> _yFormatter;
        private int _totalCertificatesCount;
        private int _todayCertificatesCount;
        private int _environmentalCertificatesCount;
        private int _consumerCertificatesCount;
        
        // Sample counts
        private int _totalSamplesCount;
        private int _todaySamplesCount;
        private int _environmentalSamplesCount;
        private int _consumableSamplesCount;
        
        // Sample Charts
        private SeriesCollection _sampleMonthlySeries;
        private SeriesCollection _sampleTypeSeries;

        public ObservableCollection<int> AvailableYears
        {
            get => _availableYears;
            set => SetProperty(ref _availableYears, value);
        }

        public int SelectedYear
        {
            get => _selectedYear;
            set
            {
                if (SetProperty(ref _selectedYear, value))
                {
                    _ = LoadDashboardDataAsync();
                }
            }
        }

        public bool ShowRecentActivities
        {
            get => _showRecentActivities;
            set => SetProperty(ref _showRecentActivities, value);
        }

        public SeriesCollection DashboardMonthlySeries
        {
            get => _dashboardMonthlySeries;
            set => SetProperty(ref _dashboardMonthlySeries, value);
        }

        public SeriesCollection TypeSeries
        {
            get => _typeSeries;
            set => SetProperty(ref _typeSeries, value);
        }

        public SeriesCollection SampleMonthlySeries
        {
            get => _sampleMonthlySeries;
            set => SetProperty(ref _sampleMonthlySeries, value);
        }

        public SeriesCollection SampleTypeSeries
        {
            get => _sampleTypeSeries;
            set => SetProperty(ref _sampleTypeSeries, value);
        }

        public ObservableCollection<AuditLog> RecentActivities
        {
            get => _recentActivities;
            set => SetProperty(ref _recentActivities, value);
        }

        public string[] MonthlyLabels
        {
            get => _monthlyLabels;
            set => SetProperty(ref _monthlyLabels, value);
        }

        public Func<double, string> YFormatter
        {
            get => _yFormatter;
            set => SetProperty(ref _yFormatter, value);
        }

        public int TotalCertificatesCount
        {
            get => _totalCertificatesCount;
            set => SetProperty(ref _totalCertificatesCount, value);
        }

        public int TodayCertificatesCount
        {
            get => _todayCertificatesCount;
            set => SetProperty(ref _todayCertificatesCount, value);
        }

        public int EnvironmentalCertificatesCount
        {
            get => _environmentalCertificatesCount;
            set => SetProperty(ref _environmentalCertificatesCount, value);
        }

        public int ConsumerCertificatesCount
        {
            get => _consumerCertificatesCount;
            set => SetProperty(ref _consumerCertificatesCount, value);
        }

        public int TotalSamplesCount
        {
            get => _totalSamplesCount;
            set => SetProperty(ref _totalSamplesCount, value);
        }

        public int TodaySamplesCount
        {
            get => _todaySamplesCount;
            set => SetProperty(ref _todaySamplesCount, value);
        }

        public int EnvironmentalSamplesCount
        {
            get => _environmentalSamplesCount;
            set => SetProperty(ref _environmentalSamplesCount, value);
        }

        public int ConsumableSamplesCount
        {
            get => _consumableSamplesCount;
            set => SetProperty(ref _consumableSamplesCount, value);
        }

        public DashboardViewModel(CertificateRepository certificateRepository, DatabaseService databaseService)
        {
            _certificateRepository = certificateRepository;
            _databaseService = databaseService;
            
            // Initialize arrays and collections to prevent null warnings
            _availableYears = new ObservableCollection<int>();
            _selectedYear = DateTime.Now.Year;
            _showRecentActivities = true;
            _dashboardMonthlySeries = new SeriesCollection();
            _typeSeries = new SeriesCollection();
            _sampleMonthlySeries = new SeriesCollection();
            _sampleTypeSeries = new SeriesCollection();
            _recentActivities = new ObservableCollection<AuditLog>();
            _monthlyLabels = Array.Empty<string>();
            _yFormatter = value => value.ToString("N0");
        }

        public async System.Threading.Tasks.Task LoadDashboardDataAsync()
        {
            try
            {
                IsBusy = true;
                BusyMessage = "جاري تحميل بيانات لوحة التحكم...";

                // 0. تهيئة قائمة السنوات المتاحة إذا كانت فارغة
                if (AvailableYears == null || AvailableYears.Count == 0)
                {
                    var yearsList = await _certificateRepository.GetAvailableYearsAsync();
                    AvailableYears = new ObservableCollection<int>(yearsList);
                    if (!AvailableYears.Contains(_selectedYear))
                    {
                        AvailableYears.Insert(0, _selectedYear);
                    }
                }

                ShowRecentActivities = (_selectedYear == DateTime.Now.Year);

                // 1. تحميل الإحصائيات الرقمية (بشكل متوازي لتحسين الأداء)
                int currentYear = _selectedYear;
                var totalCertTask = _certificateRepository.GetTotalCertificatesCountAsync(false, currentYear);
                var todayCertTask = _certificateRepository.GetCertificatesCountByDateAsync(DateTime.Now);
                var envCertTask = _certificateRepository.GetCertificatesCountByTypeAsync("بيئية", currentYear);
                var conCertTask = _certificateRepository.GetCertificatesCountByTypeAsync("استهلاكية", currentYear);

                var totalSampleTask = _certificateRepository.GetTotalSamplesCountAsync(currentYear);
                var todaySampleTask = _certificateRepository.GetSamplesCountByDateAsync(DateTime.Now);
                var envSampleTask = _certificateRepository.GetSamplesCountByTypeAsync("بيئية", currentYear);
                var conSampleTask = _certificateRepository.GetSamplesCountByTypeAsync("استهلاكية", currentYear);
                var auditLogsTask = _databaseService.GetAuditLogsAsync(limit: 5);

                await System.Threading.Tasks.Task.WhenAll(
                    totalCertTask, todayCertTask, envCertTask, conCertTask,
                    totalSampleTask, todaySampleTask, envSampleTask, conSampleTask,
                    auditLogsTask
                );

                TotalCertificatesCount = await totalCertTask;
                TodayCertificatesCount = await todayCertTask;
                EnvironmentalCertificatesCount = await envCertTask;
                ConsumerCertificatesCount = await conCertTask;

                TotalSamplesCount = await totalSampleTask;
                TodaySamplesCount = await todaySampleTask;
                EnvironmentalSamplesCount = await envSampleTask;
                ConsumableSamplesCount = await conSampleTask;

                RecentActivities = new ObservableCollection<AuditLog>(await auditLogsTask);

                // 2. إعداد الرسم البياني الدائري (توزيع الأنواع)
                TypeSeries = new SeriesCollection
                {
                    new PieSeries
                    {
                        Title = "عينات بيئية",
                        Values = new ChartValues<int> { EnvironmentalCertificatesCount },
                        DataLabels = true,
                        Fill = (System.Windows.Media.Brush)Application.Current.Resources["SuccessBrush"]
                    },
                    new PieSeries
                    {
                        Title = "عينات استهلاكية",
                        Values = new ChartValues<int> { ConsumerCertificatesCount },
                        DataLabels = true,
                        Fill = (System.Windows.Media.Brush)Application.Current.Resources["SecondaryActionBrush"]
                    }
                };

                // 3. إعداد الرسم البياني الشريطي (الإصدار الشهري)
                var envStatsTask = _certificateRepository.GetMonthlyStatisticsByTypeAsync(currentYear, "بيئية");
                var conStatsTask = _certificateRepository.GetMonthlyStatisticsByTypeAsync(currentYear, "استهلاكية");

                await System.Threading.Tasks.Task.WhenAll(envStatsTask, conStatsTask);

                var envStats = await envStatsTask;
                var conStats = await conStatsTask;

                var envValues = new ChartValues<int>();
                var conValues = new ChartValues<int>();
                var labels = new string[12];
                var arabicMonths = new[] { "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو", "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر" };

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
                        LabelPoint = point => point.Y > 0 ? point.Y.ToString() : "",
                        Fill = (System.Windows.Media.Brush)Application.Current.Resources["SuccessBrush"]
                    },
                    new ColumnSeries
                    {
                        Title = "عينات استهلاكية",
                        Values = conValues,
                        DataLabels = true,
                        LabelPoint = point => point.Y > 0 ? point.Y.ToString() : "",
                        Fill = (System.Windows.Media.Brush)Application.Current.Resources["SecondaryActionBrush"]
                    }
                };

                MonthlyLabels = labels;
                YFormatter = value => value.ToString("N0");

                // 4. إعداد الرسوم البيانية للعينات
                SampleTypeSeries = new SeriesCollection
                {
                    new PieSeries
                    {
                        Title = "عينات بيئية",
                        Values = new ChartValues<int> { EnvironmentalSamplesCount },
                        DataLabels = true,
                        Fill = (System.Windows.Media.Brush)Application.Current.Resources["SuccessBrush"]
                    },
                    new PieSeries
                    {
                        Title = "عينات استهلاكية",
                        Values = new ChartValues<int> { ConsumableSamplesCount },
                        DataLabels = true,
                        Fill = (System.Windows.Media.Brush)Application.Current.Resources["SecondaryActionBrush"]
                    }
                };

                var envSampleStatsTask = _certificateRepository.GetMonthlySamplesStatisticsByTypeAsync(currentYear, "بيئية");
                var conSampleStatsTask = _certificateRepository.GetMonthlySamplesStatisticsByTypeAsync(currentYear, "استهلاكية");

                await System.Threading.Tasks.Task.WhenAll(envSampleStatsTask, conSampleStatsTask);

                var envSampleStats = await envSampleStatsTask;
                var conSampleStats = await conSampleStatsTask;

                var envSampleValues = new ChartValues<int>();
                var conSampleValues = new ChartValues<int>();

                for (int i = 1; i <= 12; i++)
                {
                    envSampleValues.Add(envSampleStats.TryGetValue(i, out var envSampleVal) ? envSampleVal : 0);
                    conSampleValues.Add(conSampleStats.TryGetValue(i, out var conSampleVal) ? conSampleVal : 0);
                }

                SampleMonthlySeries = new SeriesCollection
                {
                    new ColumnSeries
                    {
                        Title = "عينات بيئية",
                        Values = envSampleValues,
                        DataLabels = true,
                        LabelPoint = point => point.Y > 0 ? point.Y.ToString() : "",
                        Fill = (System.Windows.Media.Brush)Application.Current.Resources["SuccessBrush"]
                    },
                    new ColumnSeries
                    {
                        Title = "عينات استهلاكية",
                        Values = conSampleValues,
                        DataLabels = true,
                        LabelPoint = point => point.Y > 0 ? point.Y.ToString() : "",
                        Fill = (System.Windows.Media.Brush)Application.Current.Resources["SecondaryActionBrush"]
                    }
                };
            }
            catch (Exception ex)
            {
               StatusMessage = $"خطأ في تحميل بيانات الإحصائيات: {ex.Message}";
               Console.WriteLine($"Error loading dashboard data: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
