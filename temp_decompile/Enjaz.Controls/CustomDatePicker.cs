using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using Enjaz.Services;

namespace Enjaz.Controls;

public class CustomDatePicker : UserControl, IComponentConnector
{
	public static readonly DependencyProperty SelectedDateProperty = DependencyProperty.Register("SelectedDate", typeof(DateTime?), typeof(CustomDatePicker), (PropertyMetadata)(object)new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnSelectedDateChanged)));

	public static readonly DependencyProperty HintProperty = DependencyProperty.Register("Hint", typeof(string), typeof(CustomDatePicker), new PropertyMetadata((object)"اختر تاريخ", new PropertyChangedCallback(OnHintChanged)));

	private DateTime _displayDate = DateTime.Today;

	private DateTime? _tempSelectedDate;

	private bool _isDarkMode;

	private bool _isYearPickerOpen;

	private static readonly Color LightBackground = Colors.White;

	private static readonly Color LightHeaderBg = Color.FromRgb(11, 25, 60);

	private static readonly Color LightHeaderText = Colors.White;

	private static readonly Color LightCalendarText = Color.FromRgb(11, 25, 60);

	private static readonly Color LightInactiveText = Color.FromRgb(180, 185, 195);

	private static readonly Color LightSelectedDayBg = Color.FromRgb(11, 25, 60);

	private static readonly Color LightSelectedDayText = Colors.White;

	private static readonly Color LightTodayBorder = Color.FromRgb(5, 150, 105);

	private static readonly Color LightButtonBg = Color.FromRgb(11, 25, 60);

	private static readonly Color LightButtonText = Colors.White;

	private static readonly Color LightCancelText = Color.FromRgb(11, 25, 60);

	private static readonly Color LightNavArrow = Color.FromRgb(11, 25, 60);

	private static readonly Color DarkBackground = Color.FromRgb(26, 28, 41);

	private static readonly Color DarkHeaderBg = Color.FromRgb(5, 150, 105);

	private static readonly Color DarkHeaderText = Colors.White;

	private static readonly Color DarkCalendarText = Colors.White;

	private static readonly Color DarkInactiveText = Color.FromRgb(100, 110, 130);

	private static readonly Color DarkSelectedDayBg = Color.FromRgb(5, 150, 105);

	private static readonly Color DarkSelectedDayText = Colors.White;

	private static readonly Color DarkTodayBorder = Color.FromRgb(5, 150, 105);

	private static readonly Color DarkButtonBg = Color.FromRgb(5, 150, 105);

	private static readonly Color DarkButtonText = Colors.White;

	private static readonly Color DarkCancelText = Colors.White;

	private static readonly Color DarkNavArrow = Colors.White;

	private static readonly string[] ArabicDayNames = new string[7] { "س", "ح", "ن", "ث", "ر", "خ", "ج" };

	private static readonly string[] ArabicMonthNames = new string[12]
	{
		"يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو", "يوليو", "أغسطس", "سبتمبر", "أكتوبر",
		"نوفمبر", "ديسمبر"
	};

	internal Border InputBorder;

	internal TextBlock DisplayText;

	internal Popup CalendarPopup;

	internal Border PopupBorder;

	internal Border HeaderBorder;

	internal TextBlock YearText;

	internal TextBlock FullDateText;

	internal Button PrevMonthBtn;

	internal TextBlock MonthYearText;

	internal Button NextMonthBtn;

	internal UniformGrid DayNamesGrid;

	internal UniformGrid DaysGrid;

	internal Grid YearPickerPanel;

	internal ScrollViewer YearScrollViewer;

	internal UniformGrid YearsGrid;

	internal Button CancelBtn;

	internal Button OkBtn;

	private bool _contentLoaded;

	public DateTime? SelectedDate
	{
		get
		{
			return (DateTime?)((DependencyObject)this).GetValue(SelectedDateProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(SelectedDateProperty, (object)value);
		}
	}

	public string Hint
	{
		get
		{
			return (string)((DependencyObject)this).GetValue(HintProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(HintProperty, (object)value);
		}
	}

	public CustomDatePicker()
	{
		InitializeComponent();
		_tempSelectedDate = SelectedDate;
		_isDarkMode = ThemeService.IsDarkMode;
		base.Loaded += OnLoaded;
		base.Unloaded += OnUnloaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		_isDarkMode = ThemeService.IsDarkMode;
		UpdateDisplayText();
		ThemeService.ThemeChanged += OnThemeChanged;
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		ThemeService.ThemeChanged -= OnThemeChanged;
	}

	private void OnThemeChanged(bool isDark)
	{
		((DispatcherObject)this).Dispatcher.Invoke((Action)delegate
		{
			_isDarkMode = isDark;
			if (CalendarPopup.IsOpen)
			{
				ApplyPopupTheme(isDark);
				BuildDayNamesHeader();
				GenerateCalendar();
			}
		});
	}

	private static void OnSelectedDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is CustomDatePicker customDatePicker)
		{
			customDatePicker._tempSelectedDate = (DateTime?)((DependencyPropertyChangedEventArgs)(ref e)).NewValue;
			if (customDatePicker._tempSelectedDate.HasValue)
			{
				customDatePicker._displayDate = customDatePicker._tempSelectedDate.Value;
			}
			customDatePicker.UpdateDisplayText();
			if (customDatePicker.CalendarPopup != null && customDatePicker.CalendarPopup.IsOpen)
			{
				customDatePicker.GenerateCalendar();
			}
		}
	}

	private static void OnHintChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is CustomDatePicker customDatePicker)
		{
			customDatePicker.UpdateDisplayText();
		}
	}

	private void UpdateDisplayText()
	{
		if (DisplayText == null)
		{
			return;
		}
		if (SelectedDate.HasValue)
		{
			DisplayText.Text = SelectedDate.Value.ToString("dd/MM/yyyy");
			try
			{
				DisplayText.Foreground = (Brush)FindResource("PrimaryTextBrush");
				return;
			}
			catch
			{
				DisplayText.Foreground = Brushes.Black;
				return;
			}
		}
		DisplayText.Text = Hint;
		try
		{
			DisplayText.Foreground = (Brush)FindResource("SecondaryTextBrush");
		}
		catch
		{
			DisplayText.Foreground = Brushes.Gray;
		}
	}

	private void UpdateHeaderDisplay()
	{
		if (YearText == null || FullDateText == null)
		{
			return;
		}
		DateTime dateTime = _tempSelectedDate ?? DateTime.Today;
		YearText.Text = dateTime.Year.ToString();
		try
		{
			string value = dateTime.ToString("dddd", new CultureInfo("ar"));
			FullDateText.Text = $"{value}، {dateTime.Day} {ArabicMonthNames[dateTime.Month - 1]}";
		}
		catch
		{
			FullDateText.Text = dateTime.ToString("D");
		}
	}

	private void ToggleCalendar_Click(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		_tempSelectedDate = SelectedDate;
		if (_tempSelectedDate.HasValue)
		{
			_displayDate = _tempSelectedDate.Value;
		}
		else
		{
			_displayDate = DateTime.Today;
		}
		CalendarPopup.IsOpen = !CalendarPopup.IsOpen;
		if (CalendarPopup.IsOpen)
		{
			_isDarkMode = ThemeService.IsDarkMode;
			ApplyPopupTheme(_isDarkMode);
			BuildDayNamesHeader();
			GenerateCalendar();
			UpdateHeaderDisplay();
		}
	}

	private void ApplyPopupTheme(bool isDark)
	{
		if (PopupBorder == null)
		{
			return;
		}
		if (isDark)
		{
			PopupBorder.Background = new SolidColorBrush(DarkBackground);
			HeaderBorder.Background = new SolidColorBrush(DarkHeaderBg);
			YearText.Foreground = new SolidColorBrush(DarkHeaderText);
			FullDateText.Foreground = new SolidColorBrush(DarkHeaderText);
			MonthYearText.Foreground = new SolidColorBrush(DarkCalendarText);
			PrevMonthBtn.Foreground = new SolidColorBrush(DarkNavArrow);
			NextMonthBtn.Foreground = new SolidColorBrush(DarkNavArrow);
			CancelBtn.Foreground = new SolidColorBrush(DarkCancelText);
			OkBtn.Background = new SolidColorBrush(DarkButtonBg);
			OkBtn.Foreground = new SolidColorBrush(DarkButtonText);
			if (YearPickerPanel != null)
			{
				YearPickerPanel.Background = new SolidColorBrush(DarkBackground);
				if (_isYearPickerOpen)
				{
					GenerateYearPicker();
				}
			}
			return;
		}
		PopupBorder.Background = new SolidColorBrush(LightBackground);
		HeaderBorder.Background = new SolidColorBrush(LightHeaderBg);
		YearText.Foreground = new SolidColorBrush(LightHeaderText);
		FullDateText.Foreground = new SolidColorBrush(LightHeaderText);
		MonthYearText.Foreground = new SolidColorBrush(LightCalendarText);
		PrevMonthBtn.Foreground = new SolidColorBrush(LightNavArrow);
		NextMonthBtn.Foreground = new SolidColorBrush(LightNavArrow);
		CancelBtn.Foreground = new SolidColorBrush(LightCancelText);
		OkBtn.Background = new SolidColorBrush(LightButtonBg);
		OkBtn.Foreground = new SolidColorBrush(LightButtonText);
		if (YearPickerPanel != null)
		{
			YearPickerPanel.Background = new SolidColorBrush(LightBackground);
			if (_isYearPickerOpen)
			{
				GenerateYearPicker();
			}
		}
	}

	private void BuildDayNamesHeader()
	{
		if (DayNamesGrid != null)
		{
			DayNamesGrid.Children.Clear();
			Color color = (_isDarkMode ? DarkCalendarText : LightCalendarText);
			string[] arabicDayNames = ArabicDayNames;
			foreach (string text in arabicDayNames)
			{
				TextBlock element = new TextBlock
				{
					Text = text,
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center,
					FontWeight = FontWeights.Bold,
					FontSize = 13.0,
					Foreground = new SolidColorBrush(color),
					Margin = new Thickness(0.0, 0.0, 0.0, 5.0)
				};
				DayNamesGrid.Children.Add(element);
			}
		}
	}

	private void GenerateCalendar()
	{
		if (DaysGrid != null)
		{
			DaysGrid.Children.Clear();
			if (MonthYearText != null)
			{
				MonthYearText.Text = $"{ArabicMonthNames[_displayDate.Month - 1]} {_displayDate.Year}";
			}
			UpdateHeaderDisplay();
			DateTime dateTime = new DateTime(_displayDate.Year, _displayDate.Month, 1);
			int num = DateTime.DaysInMonth(_displayDate.Year, _displayDate.Month);
			int num2 = (int)(dateTime.DayOfWeek + 1) % 7;
			int day = dateTime.AddDays(-1.0).Day;
			for (int i = 0; i < num2; i++)
			{
				int dayNumber = day - num2 + 1 + i;
				DateTime date = dateTime.AddDays(-(num2 - i));
				AddDayButton(dayNumber, date, isCurrentMonth: false);
			}
			for (int j = 1; j <= num; j++)
			{
				DateTime date2 = new DateTime(_displayDate.Year, _displayDate.Month, j);
				AddDayButton(j, date2, isCurrentMonth: true);
			}
			int num3 = num2 + num;
			int num4 = 42 - num3;
			for (int k = 1; k <= num4; k++)
			{
				DateTime date3 = new DateTime(_displayDate.Year, _displayDate.Month, num).AddDays(k);
				AddDayButton(k, date3, isCurrentMonth: false);
			}
		}
	}

	private void AddDayButton(int dayNumber, DateTime date, bool isCurrentMonth)
	{
		bool flag = date.Date == DateTime.Today;
		bool flag2 = _tempSelectedDate.HasValue && date.Date == _tempSelectedDate.Value.Date;
		Brush borderBrush = null;
		Color color;
		Color color2;
		if (flag2)
		{
			color = (_isDarkMode ? DarkSelectedDayBg : LightSelectedDayBg);
			color2 = (_isDarkMode ? DarkSelectedDayText : LightSelectedDayText);
		}
		else if (flag && isCurrentMonth)
		{
			color = Colors.Transparent;
			color2 = (_isDarkMode ? DarkCalendarText : LightCalendarText);
			borderBrush = new SolidColorBrush(_isDarkMode ? DarkTodayBorder : LightTodayBorder);
		}
		else if (!isCurrentMonth)
		{
			color = Colors.Transparent;
			color2 = (_isDarkMode ? DarkInactiveText : LightInactiveText);
		}
		else
		{
			color = Colors.Transparent;
			color2 = (_isDarkMode ? DarkCalendarText : LightCalendarText);
		}
		Button button = new Button
		{
			Content = new TextBlock
			{
				Text = dayNumber.ToString(),
				Foreground = new SolidColorBrush(color2),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			},
			Width = 34.0,
			Height = 34.0,
			FontSize = 13.0,
			Cursor = Cursors.Hand,
			Background = new SolidColorBrush(color),
			Tag = date,
			Template = CreateCircleBtnTemplate(new SolidColorBrush(color), borderBrush),
			FontWeight = ((flag2 || flag) ? FontWeights.Bold : FontWeights.Normal),
			Margin = new Thickness(1.0)
		};
		button.Click += DayButton_Click;
		DaysGrid.Children.Add(button);
	}

	private ControlTemplate CreateCircleBtnTemplate(Brush background, Brush? borderBrush = null)
	{
		ControlTemplate controlTemplate = new ControlTemplate(typeof(Button));
		FrameworkElementFactory frameworkElementFactory = new FrameworkElementFactory(typeof(Border));
		frameworkElementFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(17.0));
		frameworkElementFactory.SetValue(Border.BackgroundProperty, background);
		frameworkElementFactory.SetValue(FrameworkElement.WidthProperty, 34.0);
		frameworkElementFactory.SetValue(FrameworkElement.HeightProperty, 34.0);
		if (borderBrush != null)
		{
			frameworkElementFactory.SetValue(Border.BorderBrushProperty, borderBrush);
			frameworkElementFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1.5));
		}
		FrameworkElementFactory frameworkElementFactory2 = new FrameworkElementFactory(typeof(ContentPresenter));
		frameworkElementFactory2.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
		frameworkElementFactory2.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
		frameworkElementFactory.AppendChild(frameworkElementFactory2);
		controlTemplate.VisualTree = frameworkElementFactory;
		return controlTemplate;
	}

	private void DayButton_Click(object sender, RoutedEventArgs e)
	{
		if (sender is Button { Tag: var tag } && tag is DateTime value)
		{
			_tempSelectedDate = value;
			_displayDate = new DateTime(value.Year, value.Month, 1);
			GenerateCalendar();
			UpdateHeaderDisplay();
		}
	}

	private void PrevMonth_Click(object sender, RoutedEventArgs e)
	{
		_isYearPickerOpen = false;
		YearPickerPanel.Visibility = Visibility.Collapsed;
		_displayDate = _displayDate.AddMonths(-1);
		GenerateCalendar();
	}

	private void NextMonth_Click(object sender, RoutedEventArgs e)
	{
		_isYearPickerOpen = false;
		YearPickerPanel.Visibility = Visibility.Collapsed;
		_displayDate = _displayDate.AddMonths(1);
		GenerateCalendar();
	}

	private void YearText_Click(object sender, MouseButtonEventArgs e)
	{
		_isYearPickerOpen = !_isYearPickerOpen;
		if (_isYearPickerOpen)
		{
			YearPickerPanel.Visibility = Visibility.Visible;
			GenerateYearPicker();
		}
		else
		{
			YearPickerPanel.Visibility = Visibility.Collapsed;
		}
		e.Handled = true;
	}

	private void GenerateYearPicker()
	{
		if (YearsGrid == null)
		{
			return;
		}
		YearsGrid.Children.Clear();
		int year = DateTime.Today.Year;
		int num = _tempSelectedDate?.Year ?? _displayDate.Year;
		int y;
		for (y = 1900; y <= 2100; y++)
		{
			Button button = new Button
			{
				Content = y.ToString(),
				Margin = new Thickness(2.0),
				Padding = new Thickness(5.0),
				Background = Brushes.Transparent,
				BorderThickness = new Thickness(0.0),
				Cursor = Cursors.Hand,
				FontSize = 14.0,
				Tag = y
			};
			Color color = (_isDarkMode ? DarkCalendarText : LightCalendarText);
			if (y == num)
			{
				button.Background = new SolidColorBrush(_isDarkMode ? DarkSelectedDayBg : LightSelectedDayBg);
				button.Foreground = Brushes.White;
				button.FontWeight = FontWeights.Bold;
			}
			else
			{
				button.Foreground = new SolidColorBrush(color);
				button.FontWeight = FontWeights.Normal;
			}
			button.Click += delegate(object s, RoutedEventArgs ev)
			{
				_isYearPickerOpen = false;
				YearPickerPanel.Visibility = Visibility.Collapsed;
				int year2 = (int)((Button)s).Tag;
				int month = _displayDate.Month;
				int day = Math.Min(_tempSelectedDate?.Day ?? _displayDate.Day, DateTime.DaysInMonth(year2, month));
				_displayDate = new DateTime(year2, month, 1);
				if (_tempSelectedDate.HasValue)
				{
					_tempSelectedDate = new DateTime(year2, month, day);
				}
				GenerateCalendar();
				UpdateHeaderDisplay();
			};
			YearsGrid.Children.Add(button);
			if (y == num)
			{
				((DispatcherObject)this).Dispatcher.BeginInvoke((Delegate)(Action)delegate
				{
					double offset = (y - 1900) / 4 * 40;
					YearScrollViewer.ScrollToVerticalOffset(offset);
				}, (DispatcherPriority)4, Array.Empty<object>());
			}
		}
	}

	private void OkBtn_Click(object sender, RoutedEventArgs e)
	{
		SelectedDate = _tempSelectedDate;
		CalendarPopup.IsOpen = false;
	}

	private void CancelBtn_Click(object sender, RoutedEventArgs e)
	{
		_tempSelectedDate = SelectedDate;
		CalendarPopup.IsOpen = false;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "8.0.21.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/Enjaz;component/controls/customdatepicker.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "8.0.21.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			InputBorder = (Border)target;
			InputBorder.PreviewMouseLeftButtonDown += ToggleCalendar_Click;
			break;
		case 2:
			DisplayText = (TextBlock)target;
			break;
		case 3:
			CalendarPopup = (Popup)target;
			break;
		case 4:
			PopupBorder = (Border)target;
			break;
		case 5:
			HeaderBorder = (Border)target;
			break;
		case 6:
			YearText = (TextBlock)target;
			YearText.PreviewMouseLeftButtonDown += YearText_Click;
			break;
		case 7:
			FullDateText = (TextBlock)target;
			break;
		case 8:
			PrevMonthBtn = (Button)target;
			PrevMonthBtn.Click += PrevMonth_Click;
			break;
		case 9:
			MonthYearText = (TextBlock)target;
			break;
		case 10:
			NextMonthBtn = (Button)target;
			NextMonthBtn.Click += NextMonth_Click;
			break;
		case 11:
			DayNamesGrid = (UniformGrid)target;
			break;
		case 12:
			DaysGrid = (UniformGrid)target;
			break;
		case 13:
			YearPickerPanel = (Grid)target;
			break;
		case 14:
			YearScrollViewer = (ScrollViewer)target;
			break;
		case 15:
			YearsGrid = (UniformGrid)target;
			break;
		case 16:
			CancelBtn = (Button)target;
			CancelBtn.Click += CancelBtn_Click;
			break;
		case 17:
			OkBtn = (Button)target;
			OkBtn.Click += OkBtn_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
