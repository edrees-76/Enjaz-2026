using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using Enjaz.Helpers;
using Enjaz.Models;
using Enjaz.Services;
using Enjaz.Services.Repositories;

namespace Enjaz.ViewModels;

public class AdminProceduresViewModel : BaseViewModel
{
	private readonly CertificateRepository _certificateRepository;

	private readonly IPdfService _pdfService;

	private readonly INotificationService _notificationService;

	private readonly IOSService _osService;

	private readonly UserService _userService;

	private int _selectedTab;

	private int _currentStep;

	private DateTime _startDate;

	private DateTime _endDate;

	private string? _selectedSender;

	private ReferralLetter? _selectedReferral;

	private ICollectionView? _referralHistoryView;

	private string? _selectedFilterSender;

	private bool _includeCertNum = true;

	private bool _includeSupplier = true;

	private bool _includeSamples = true;

	private bool _includeNotification = true;

	public int SelectedTab
	{
		get
		{
			return _selectedTab;
		}
		set
		{
			SetProperty(ref _selectedTab, value, "SelectedTab");
		}
	}

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

	public string? SelectedSender
	{
		get
		{
			return _selectedSender;
		}
		set
		{
			SetProperty(ref _selectedSender, value, "SelectedSender");
		}
	}

	public ReferralLetter? SelectedReferral
	{
		get
		{
			return _selectedReferral;
		}
		set
		{
			SetProperty(ref _selectedReferral, value, "SelectedReferral");
		}
	}

	public ObservableCollection<string> Senders { get; }

	public ObservableCollection<ReferralLetter> ReferralHistory { get; } = new ObservableCollection<ReferralLetter>();

	public ICollectionView? ReferralHistoryView
	{
		get
		{
			return _referralHistoryView;
		}
		set
		{
			SetProperty(ref _referralHistoryView, value, "ReferralHistoryView");
		}
	}

	public ObservableCollection<string> FilterSenders { get; } = new ObservableCollection<string>();

	public string? SelectedFilterSender
	{
		get
		{
			return _selectedFilterSender;
		}
		set
		{
			if (SetProperty(ref _selectedFilterSender, value, "SelectedFilterSender"))
			{
				ICollectionView? referralHistoryView = _referralHistoryView;
				if (referralHistoryView != null)
				{
					referralHistoryView.Refresh();
				}
			}
		}
	}

	public bool IncludeCertNum
	{
		get
		{
			return _includeCertNum;
		}
		set
		{
			if (SetProperty(ref _includeCertNum, value, "IncludeCertNum"))
			{
				OnPropertyChanged("SummaryColumns");
			}
		}
	}

	public bool IncludeSupplier
	{
		get
		{
			return _includeSupplier;
		}
		set
		{
			if (SetProperty(ref _includeSupplier, value, "IncludeSupplier"))
			{
				OnPropertyChanged("SummaryColumns");
			}
		}
	}

	public bool IncludeSamples
	{
		get
		{
			return _includeSamples;
		}
		set
		{
			if (SetProperty(ref _includeSamples, value, "IncludeSamples"))
			{
				OnPropertyChanged("SummaryColumns");
			}
		}
	}

	public bool IncludeNotification
	{
		get
		{
			return _includeNotification;
		}
		set
		{
			if (SetProperty(ref _includeNotification, value, "IncludeNotification"))
			{
				OnPropertyChanged("SummaryColumns");
			}
		}
	}

	public IEnumerable<string> SummaryColumns
	{
		get
		{
			List<string> list = new List<string>();
			if (IncludeCertNum)
			{
				list.Add("رقم الشهادة");
			}
			if (IncludeSupplier)
			{
				list.Add("اسم المورد");
			}
			if (IncludeSamples)
			{
				list.Add("أرقام العينات");
			}
			if (IncludeNotification)
			{
				list.Add("رقم الإخطار");
			}
			return list;
		}
	}

	public ICommand NextStepCommand { get; }

	public ICommand PreviousStepCommand { get; }

	public ICommand GenerateLetterCommand { get; }

	public ICommand ViewLetterCommand { get; }

	public ICommand PrintLetterCommand { get; }

	public AdminProceduresViewModel(CertificateRepository certificateRepository, IPdfService pdfService, INotificationService notificationService, IOSService osService, UserService userService)
	{
		_certificateRepository = certificateRepository;
		_pdfService = pdfService;
		_notificationService = notificationService;
		_osService = osService;
		_userService = userService;
		StartDate = DateTime.Today.AddDays(-30.0);
		EndDate = DateTime.Today;
		CurrentStep = 1;
		NextStepCommand = new AsyncRelayCommand((Func<object?, Task>)async delegate
		{
			await NextStep();
		}, (Predicate<object?>?)((object? _) => CanNextStep()), (AsyncRelayCommand.IErrorHandler?)null);
		PreviousStepCommand = new RelayCommand(delegate
		{
			PreviousStep();
		}, (object? _) => CanPreviousStep());
		GenerateLetterCommand = new AsyncRelayCommand((Func<object?, Task>)async delegate
		{
			await GenerateLetter();
		}, (Predicate<object?>?)null, (AsyncRelayCommand.IErrorHandler?)null);
		ViewLetterCommand = new AsyncRelayCommand(async delegate(object? l)
		{
			await ViewLetter(l as ReferralLetter);
		});
		PrintLetterCommand = new AsyncRelayCommand(async delegate(object? l)
		{
			await PrintLetter(l as ReferralLetter);
		});
		Senders = new ObservableCollection<string>();
		ReferralHistory = new ObservableCollection<ReferralLetter>();
		LoadSendersAsync();
		LoadReferralHistoryAsync();
	}

	public async Task RefreshSendersAsync()
	{
		await LoadSendersAsync();
	}

	private async Task LoadSendersAsync()
	{
		try
		{
			List<string> dbSenders = await _certificateRepository.GetDistinctFieldValuesAsync("Sender");
			Senders.Clear();
			foreach (string sender in dbSenders)
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
			Exception ex2 = ex;
			LoggerService.LogError("Error loading senders for referral wizard", ex2);
		}
	}

	public async Task LoadReferralHistoryAsync()
	{
		try
		{
			List<ReferralLetter> history = await _certificateRepository.GetReferralLettersAsync();
			ReferralHistory.Clear();
			int seq = 1;
			foreach (ReferralLetter item in history)
			{
				item.Sequence = seq++;
				ReferralHistory.Add(item);
			}
			if (_referralHistoryView == null)
			{
				_referralHistoryView = CollectionViewSource.GetDefaultView(ReferralHistory);
				_referralHistoryView.Filter = delegate(object obj)
				{
					if (string.IsNullOrWhiteSpace(SelectedFilterSender) || SelectedFilterSender == "الكل")
					{
						return true;
					}
					return obj is ReferralLetter referralLetter && referralLetter.SenderName == SelectedFilterSender;
				};
				OnPropertyChanged("ReferralHistoryView");
			}
			else
			{
				_referralHistoryView.Refresh();
			}
			string prevFilter = SelectedFilterSender;
			FilterSenders.Clear();
			FilterSenders.Add("الكل");
			IEnumerable<string> uniqueSenders = (from r in ReferralHistory
				select r.SenderName into value
				where !string.IsNullOrEmpty(value)
				select value).Distinct();
			foreach (string s in uniqueSenders)
			{
				FilterSenders.Add(s);
			}
			if (prevFilter != null && FilterSenders.Contains(prevFilter))
			{
				SelectedFilterSender = prevFilter;
			}
			else
			{
				SelectedFilterSender = "الكل";
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			LoggerService.LogError("Error loading referral history", ex2);
		}
	}

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

	private bool CanNextStep()
	{
		return CurrentStep < 3;
	}

	private void PreviousStep()
	{
		if (CurrentStep > 1)
		{
			CurrentStep--;
		}
	}

	private bool CanPreviousStep()
	{
		return CurrentStep > 1;
	}

	private async Task GenerateLetter()
	{
		base.IsBusy = true;
		try
		{
			string senderName = SelectedSender;
			LoggerService.LogInfo($"[ReferralWizard] Querying: Sender='{senderName}', From={StartDate:yyyy-MM-dd}, To={EndDate:yyyy-MM-dd}");
			List<Certificate> certificates = await _certificateRepository.GetCertificatesBySenderAndDateAsync(senderName, StartDate, EndDate);
			LoggerService.LogInfo($"[ReferralWizard] Found {certificates?.Count ?? 0} certificates");
			if (certificates == null || !certificates.Any())
			{
				_notificationService.ShowError($"لم يتم العثور على شهادات للجهة '{senderName}' في الفترة من {StartDate:yyyy-MM-dd} إلى {EndDate:yyyy-MM-dd}");
				return;
			}
			string fileName = $"Referral_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
			string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
			if (!Directory.Exists(downloadsPath))
			{
				Directory.CreateDirectory(downloadsPath);
			}
			string outputPath = Path.Combine(downloadsPath, fileName);
			if (_pdfService.GenerateReferralLetterPdf(certificates, outputPath, senderName, IncludeCertNum, IncludeSupplier, IncludeSamples, IncludeNotification))
			{
				ReferralLetter historyRecord = new ReferralLetter
				{
					SenderName = senderName,
					CertificateCount = certificates.Count,
					SampleCount = certificates.Sum((Certificate c) => c.SampleCount),
					OutputPath = outputPath,
					StartDate = StartDate,
					EndDate = EndDate,
					IncludedColumns = ((IncludeCertNum ? "CertNum," : "") + (IncludeSupplier ? "Supplier," : "") + (IncludeSamples ? "Samples," : "") + (IncludeNotification ? "Notification" : "")).TrimEnd(',')
				};
				await _certificateRepository.AddReferralLetterAsync(historyRecord);
				User user = _userService.CurrentUser;
				await _certificateRepository.LogReferralLetterGenerationAsync(user?.Id, user?.FullName ?? "غير معروف", senderName, certificates.Count);
				await LoadReferralHistoryAsync();
				_osService.OpenFile(outputPath);
				_notificationService.ShowSuccess("تم حفظ البيانات وإصدار الرسالة بنجاح");
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
			Exception ex2 = ex;
			_notificationService.ShowError("حدث خطأ أثناء المعالجة: " + ex2.Message);
		}
		finally
		{
			base.IsBusy = false;
		}
	}

	private void ResetWizard()
	{
		CurrentStep = 1;
		SelectedSender = null;
		StartDate = DateTime.Today.AddDays(-30.0);
		EndDate = DateTime.Today;
		IncludeCertNum = true;
		IncludeSupplier = true;
		IncludeSamples = true;
		IncludeNotification = true;
	}

	private async Task ViewLetter(ReferralLetter? letter)
	{
		if (letter != null)
		{
			if (File.Exists(letter.OutputPath))
			{
				_osService.OpenFile(letter.OutputPath);
				return;
			}
			_notificationService.ShowInfo("الملف غير موجود، جاري إعادة التوليد...");
			await RegeneratePdf(letter);
		}
	}

	private async Task PrintLetter(ReferralLetter? letter)
	{
		if (letter == null)
		{
			return;
		}
		try
		{
			List<Certificate> certificates = await _certificateRepository.GetCertificatesBySenderAndDateAsync(letter.SenderName, letter.StartDate, letter.EndDate);
			if (certificates == null || !certificates.Any())
			{
				_notificationService.ShowError("لا يمكن طباعة الرسالة: لم يتم العثور على الشهادات الأصلية.");
				return;
			}
			bool incCert = letter.IncludedColumns.Contains("CertNum");
			bool incSupp = letter.IncludedColumns.Contains("Supplier");
			bool incSamp = letter.IncludedColumns.Contains("Samples");
			bool incNotif = letter.IncludedColumns.Contains("Notification");
			_pdfService.PrintReferralLetterPdf(certificates, letter.SenderName, incCert, incSupp, incSamp, incNotif);
		}
		catch (Exception ex)
		{
			LoggerService.LogError("Error in PrintLetter command", ex);
			_notificationService.ShowError("حدث خطأ أثناء محاولة الطباعة");
		}
	}

	private async Task RegeneratePdf(ReferralLetter? letter)
	{
		if (letter == null)
		{
			return;
		}
		base.IsBusy = true;
		try
		{
			List<Certificate> certificates = await _certificateRepository.GetCertificatesBySenderAndDateAsync(letter.SenderName, letter.StartDate, letter.EndDate);
			if (certificates == null || !certificates.Any())
			{
				_notificationService.ShowError("لم يتم العثور على الشهادات لإعادة الإنشاء");
				return;
			}
			bool incCert = letter.IncludedColumns.Contains("CertNum");
			bool incSupp = letter.IncludedColumns.Contains("Supplier");
			bool incSamp = letter.IncludedColumns.Contains("Samples");
			bool incNotif = letter.IncludedColumns.Contains("Notification");
			if (_pdfService.GenerateReferralLetterPdf(certificates, letter.OutputPath, letter.SenderName, incCert, incSupp, incSamp, incNotif))
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
			Exception ex2 = ex;
			_notificationService.ShowError("خطأ في إعادة الإنشاء: " + ex2.Message);
		}
		finally
		{
			base.IsBusy = false;
		}
	}
}
