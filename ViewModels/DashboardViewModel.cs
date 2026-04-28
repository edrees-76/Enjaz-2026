using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Enjaz.Services.Repositories;
using Enjaz.Models;
using Enjaz.Services;
using Enjaz.Helpers;
using Enjaz.Services.Statistics;

namespace Enjaz.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly IDashboardService _dashboardService;

        // Dashboard Properties
        private ObservableCollection<int> _availableYears;
        private int _selectedYear;
        private bool _showRecentActivities;
        private ObservableCollection<AuditLog> _recentActivities;

        // Native WPF Chart Data
        private List<MonthlyBarItem> _certMonthlyBars;
        private List<MonthlyBarItem> _sampleMonthlyBars;
        private List<DonutSlice> _certDonutSlices;
        private List<DonutSlice> _sampleDonutSlices;

        // KPI Counts
        private int _totalCertificatesCount;
        private int _todayCertificatesCount;
        private int _environmentalCertificatesCount;
        private int _consumerCertificatesCount;
        private int _totalSamplesCount;
        private int _todaySamplesCount;
        private int _environmentalSamplesCount;
        private int _consumableSamplesCount;

        // Chart max value for scaling
        private int _certMaxMonthly;
        private int _sampleMaxMonthly;

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

        public ObservableCollection<AuditLog> RecentActivities
        {
            get => _recentActivities;
            set => SetProperty(ref _recentActivities, value);
        }

        // Native Chart Properties
        public List<MonthlyBarItem> CertMonthlyBars
        {
            get => _certMonthlyBars;
            set => SetProperty(ref _certMonthlyBars, value);
        }

        public List<MonthlyBarItem> SampleMonthlyBars
        {
            get => _sampleMonthlyBars;
            set => SetProperty(ref _sampleMonthlyBars, value);
        }

        public List<DonutSlice> CertDonutSlices
        {
            get => _certDonutSlices;
            set => SetProperty(ref _certDonutSlices, value);
        }

        public List<DonutSlice> SampleDonutSlices
        {
            get => _sampleDonutSlices;
            set => SetProperty(ref _sampleDonutSlices, value);
        }

        public int CertMaxMonthly
        {
            get => _certMaxMonthly;
            set => SetProperty(ref _certMaxMonthly, value);
        }

        public int SampleMaxMonthly
        {
            get => _sampleMaxMonthly;
            set => SetProperty(ref _sampleMaxMonthly, value);
        }

        // KPI Properties
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

        public DashboardViewModel(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
            
            _availableYears = new ObservableCollection<int>();
            _selectedYear = DateTime.Now.Year;
            _showRecentActivities = true;
            _recentActivities = new ObservableCollection<AuditLog>();
            _certMonthlyBars = new List<MonthlyBarItem>();
            _sampleMonthlyBars = new List<MonthlyBarItem>();
            _certDonutSlices = new List<DonutSlice>();
            _sampleDonutSlices = new List<DonutSlice>();
        }

        public async Task LoadDashboardDataAsync()
        {
            try
            {
                IsBusy = true;
                BusyMessage = "جاري تحميل بيانات لوحة التحكم...";

                // 0. Initialize available years
                if (AvailableYears == null || AvailableYears.Count == 0)
                {
                    var yearsList = await _dashboardService.GetAvailableYearsAsync();
                    AvailableYears = new ObservableCollection<int>(yearsList);
                    if (!AvailableYears.Contains(_selectedYear))
                    {
                        AvailableYears.Insert(0, _selectedYear);
                    }
                }

                ShowRecentActivities = (_selectedYear == DateTime.Now.Year);

                // 1. Load all statistics
                var data = await _dashboardService.GetDashboardDataAsync(_selectedYear, DateTime.Now);

                TotalCertificatesCount = data.TotalCertificates;
                TodayCertificatesCount = data.TodayCertificates;
                EnvironmentalCertificatesCount = data.EnvCertificates;
                ConsumerCertificatesCount = data.ConCertificates;

                TotalSamplesCount = data.TotalSamples;
                TodaySamplesCount = data.TodaySamples;
                EnvironmentalSamplesCount = data.EnvSamples;
                ConsumableSamplesCount = data.ConSamples;

                RecentActivities = new ObservableCollection<AuditLog>(data.RecentActivities);

                // Modern color palette
                var envColor = new SolidColorBrush(Color.FromRgb(5, 150, 105));     // Emerald #059669
                var conColor = new SolidColorBrush(Color.FromRgb(14, 165, 233));     // Sky #0EA5E9
                var envLightColor = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Emerald-400
                var conLightColor = new SolidColorBrush(Color.FromRgb(56, 189, 248)); // Sky-400

                // 2. Arabic month names (native WPF TextBlock handles them perfectly)
                var arabicMonths = new[] { "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو", "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر" };

                // 3. Build Certificate Monthly Bars
                var certBars = new List<MonthlyBarItem>();
                int certMax = 1;
                for (int i = 1; i <= 12; i++)
                {
                    int env = data.MonthlyEnvCertificates.TryGetValue(i, out var ev) ? ev : 0;
                    int con = data.MonthlyConCertificates.TryGetValue(i, out var cv) ? cv : 0;
                    int monthMax = Math.Max(env, con);
                    if (monthMax > certMax) certMax = monthMax;
                    certBars.Add(new MonthlyBarItem
                    {
                        Month = arabicMonths[i - 1],
                        Value1 = env,
                        Value2 = con,
                        Label1 = "بيئية",
                        Label2 = "استهلاكية",
                        Color1 = envColor,
                        Color2 = conColor
                    });
                }
                // Set max for scaling
                foreach (var bar in certBars) bar.MaxValue = certMax;
                CertMaxMonthly = certMax;
                CertMonthlyBars = certBars;

                // 4. Build Certificate Donut
                int certTotal = EnvironmentalCertificatesCount + ConsumerCertificatesCount;
                CertDonutSlices = new List<DonutSlice>
                {
                    new DonutSlice
                    {
                        Label = "شهادات بيئية",
                        Value = EnvironmentalCertificatesCount,
                        Percentage = certTotal > 0 ? (double)EnvironmentalCertificatesCount / certTotal * 100 : 0,
                        Fill = envColor
                    },
                    new DonutSlice
                    {
                        Label = "شهادات استهلاكية",
                        Value = ConsumerCertificatesCount,
                        Percentage = certTotal > 0 ? (double)ConsumerCertificatesCount / certTotal * 100 : 0,
                        Fill = conColor
                    }
                };

                // 5. Build Sample Monthly Bars
                var sampleBars = new List<MonthlyBarItem>();
                int sampleMax = 1;
                for (int i = 1; i <= 12; i++)
                {
                    int env = data.MonthlyEnvSamples.TryGetValue(i, out var esv) ? esv : 0;
                    int con = data.MonthlyConSamples.TryGetValue(i, out var csv) ? csv : 0;
                    int monthMax = Math.Max(env, con);
                    if (monthMax > sampleMax) sampleMax = monthMax;
                    sampleBars.Add(new MonthlyBarItem
                    {
                        Month = arabicMonths[i - 1],
                        Value1 = env,
                        Value2 = con,
                        Label1 = "بيئية",
                        Label2 = "استهلاكية",
                        Color1 = envLightColor,
                        Color2 = conLightColor
                    });
                }
                foreach (var bar in sampleBars) bar.MaxValue = sampleMax;
                SampleMaxMonthly = sampleMax;
                SampleMonthlyBars = sampleBars;

                // 6. Build Sample Donut
                int sampleTotal = EnvironmentalSamplesCount + ConsumableSamplesCount;
                SampleDonutSlices = new List<DonutSlice>
                {
                    new DonutSlice
                    {
                        Label = "عينات بيئية",
                        Value = EnvironmentalSamplesCount,
                        Percentage = sampleTotal > 0 ? (double)EnvironmentalSamplesCount / sampleTotal * 100 : 0,
                        Fill = envLightColor
                    },
                    new DonutSlice
                    {
                        Label = "عينات استهلاكية",
                        Value = ConsumableSamplesCount,
                        Percentage = sampleTotal > 0 ? (double)ConsumableSamplesCount / sampleTotal * 100 : 0,
                        Fill = conLightColor
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
