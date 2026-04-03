using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Enjaz.Services;

namespace Enjaz.Controls
{
    public partial class CustomDatePicker : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty SelectedDateProperty =
            DependencyProperty.Register("SelectedDate", typeof(DateTime?), typeof(CustomDatePicker),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedDateChanged));

        public static readonly DependencyProperty HintProperty =
            DependencyProperty.Register("Hint", typeof(string), typeof(CustomDatePicker),
                new PropertyMetadata("اختر تاريخ", OnHintChanged));

        public DateTime? SelectedDate
        {
            get => (DateTime?)GetValue(SelectedDateProperty);
            set => SetValue(SelectedDateProperty, value);
        }

        public string Hint
        {
            get => (string)GetValue(HintProperty);
            set => SetValue(HintProperty, value);
        }

        #endregion

        #region Internal State

        private DateTime _displayDate = DateTime.Today;
        private DateTime? _tempSelectedDate;
        private bool _isDarkMode;
        private bool _isYearPickerOpen;

        #endregion

        #region Color Definitions — Light Mode (الوضع الفاتح)

        private static readonly Color LightBackground = Colors.White;
        private static readonly Color LightHeaderBg = Color.FromRgb(11, 25, 60);       // #0B193C كحلي
        private static readonly Color LightHeaderText = Colors.White;
        private static readonly Color LightCalendarText = Color.FromRgb(11, 25, 60);    // كحلي
        private static readonly Color LightInactiveText = Color.FromRgb(180, 185, 195);
        private static readonly Color LightSelectedDayBg = Color.FromRgb(11, 25, 60);   // كحلي
        private static readonly Color LightSelectedDayText = Colors.White;
        private static readonly Color LightTodayBorder = Color.FromRgb(5, 150, 105);    // #059669 أخضر
        private static readonly Color LightButtonBg = Color.FromRgb(11, 25, 60);
        private static readonly Color LightButtonText = Colors.White;
        private static readonly Color LightCancelText = Color.FromRgb(11, 25, 60);
        private static readonly Color LightNavArrow = Color.FromRgb(11, 25, 60);

        #endregion

        #region Color Definitions — Dark Mode (الوضع الداكن)

        private static readonly Color DarkBackground = Color.FromRgb(26, 28, 41);       // #1A1C29 كحلي عميق
        private static readonly Color DarkHeaderBg = Color.FromRgb(5, 150, 105);        // #059669 أخضر زمردي
        private static readonly Color DarkHeaderText = Colors.White;
        private static readonly Color DarkCalendarText = Colors.White;
        private static readonly Color DarkInactiveText = Color.FromRgb(100, 110, 130);
        private static readonly Color DarkSelectedDayBg = Color.FromRgb(5, 150, 105);   // أخضر زمردي
        private static readonly Color DarkSelectedDayText = Colors.White;
        private static readonly Color DarkTodayBorder = Color.FromRgb(5, 150, 105);
        private static readonly Color DarkButtonBg = Color.FromRgb(5, 150, 105);
        private static readonly Color DarkButtonText = Colors.White;
        private static readonly Color DarkCancelText = Colors.White;
        private static readonly Color DarkNavArrow = Colors.White;

        #endregion

        #region Arabic Localization

        private static readonly string[] ArabicDayNames = { "س", "ح", "ن", "ث", "ر", "خ", "ج" };
        private static readonly string[] ArabicMonthNames =
        {
            "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو",
            "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر"
        };

        #endregion

        #region Constructor

        public CustomDatePicker()
        {
            InitializeComponent();

            _tempSelectedDate = SelectedDate;
            _isDarkMode = ThemeService.IsDarkMode;

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
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

        #endregion

        #region Theme Changed Handler

        private void OnThemeChanged(bool isDark)
        {
            Dispatcher.Invoke(() =>
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

        #endregion

        #region Dependency Property Callbacks

        private static void OnSelectedDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CustomDatePicker picker)
            {
                picker._tempSelectedDate = (DateTime?)e.NewValue;
                if (picker._tempSelectedDate.HasValue)
                    picker._displayDate = picker._tempSelectedDate.Value;
                picker.UpdateDisplayText();
                if (picker.CalendarPopup != null && picker.CalendarPopup.IsOpen)
                    picker.GenerateCalendar();
            }
        }

        private static void OnHintChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CustomDatePicker picker)
                picker.UpdateDisplayText();
        }

        #endregion

        #region Display Text

        private void UpdateDisplayText()
        {
            if (DisplayText == null) return;

            if (SelectedDate.HasValue)
            {
                DisplayText.Text = SelectedDate.Value.ToString("dd/MM/yyyy HH:mm");
                try { DisplayText.Foreground = (Brush)FindResource("PrimaryTextBrush"); }
                catch { DisplayText.Foreground = Brushes.Black; }
            }
            else
            {
                DisplayText.Text = Hint;
                try { DisplayText.Foreground = (Brush)FindResource("SecondaryTextBrush"); }
                catch { DisplayText.Foreground = Brushes.Gray; }
            }
        }

        private void UpdateHeaderDisplay()
        {
            if (YearText == null || FullDateText == null) return;

            var date = _tempSelectedDate ?? DateTime.Today;
            YearText.Text = date.Year.ToString();

            try
            {
                string dayName = date.ToString("dddd", new CultureInfo("ar"));
                FullDateText.Text = $"{dayName}، {date.Day} {ArabicMonthNames[date.Month - 1]}";
            }
            catch
            {
                FullDateText.Text = date.ToString("D");
            }
        }

        #endregion

        #region Toggle Calendar

        private void ToggleCalendar_Click(object sender, MouseButtonEventArgs e)
        {
            // إيقاف انتقال الحدث للأعلى حتى لا تغلق النافذة فورا بسبب StaysOpen=False
            e.Handled = true;

            _tempSelectedDate = SelectedDate;
            if (_tempSelectedDate.HasValue)
                _displayDate = _tempSelectedDate.Value;
            else
                _displayDate = DateTime.Today;

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

        #endregion

        #region Theme Application — Direct Property Setting (Bypasses WPF Popup Bug)

        private void ApplyPopupTheme(bool isDark)
        {
            if (PopupBorder == null) return;

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
                    if (_isYearPickerOpen) GenerateYearPicker();
                }
            }
            else
            {
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
                    if (_isYearPickerOpen) GenerateYearPicker();
                }
            }
        }

        #endregion

        #region Day Names Header

        private void BuildDayNamesHeader()
        {
            if (DayNamesGrid == null) return;
            DayNamesGrid.Children.Clear();

            var textColor = _isDarkMode ? DarkCalendarText : LightCalendarText;

            foreach (var dayName in ArabicDayNames)
            {
                var tb = new TextBlock
                {
                    Text = dayName,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = FontWeights.Bold,
                    FontSize = 13,
                    Foreground = new SolidColorBrush(textColor),
                    Margin = new Thickness(0, 0, 0, 5)
                };
                DayNamesGrid.Children.Add(tb);
            }
        }

        #endregion

        #region Calendar Generation

        private void GenerateCalendar()
        {
            if (DaysGrid == null) return;
            DaysGrid.Children.Clear();

            if (MonthYearText != null)
                MonthYearText.Text = $"{ArabicMonthNames[_displayDate.Month - 1]} {_displayDate.Year}";

            UpdateHeaderDisplay();

            DateTime firstOfMonth = new DateTime(_displayDate.Year, _displayDate.Month, 1);
            int daysInMonth = DateTime.DaysInMonth(_displayDate.Year, _displayDate.Month);

            // Saturday-first: convert .NET DayOfWeek (Sun=0..Sat=6) to (Sat=0..Fri=6)
            int startOffset = ((int)firstOfMonth.DayOfWeek + 1) % 7;

            // Previous month trailing days
            DateTime prevMonthEnd = firstOfMonth.AddDays(-1);
            int prevMonthDays = prevMonthEnd.Day;
            for (int i = 0; i < startOffset; i++)
            {
                int day = prevMonthDays - startOffset + 1 + i;
                DateTime date = firstOfMonth.AddDays(-(startOffset - i));
                AddDayButton(day, date, false);
            }

            // Current month days
            for (int day = 1; day <= daysInMonth; day++)
            {
                DateTime date = new DateTime(_displayDate.Year, _displayDate.Month, day);
                AddDayButton(day, date, true);
            }

            // Next month leading days (fill to 42 = 6 rows)
            int totalCells = startOffset + daysInMonth;
            int remaining = 42 - totalCells;
            for (int i = 1; i <= remaining; i++)
            {
                DateTime date = new DateTime(_displayDate.Year, _displayDate.Month, daysInMonth).AddDays(i);
                AddDayButton(i, date, false);
            }
        }

        private void AddDayButton(int dayNumber, DateTime date, bool isCurrentMonth)
        {
            bool isToday = date.Date == DateTime.Today;
            bool isSelected = _tempSelectedDate.HasValue && date.Date == _tempSelectedDate.Value.Date;

            Color bgColor;
            Color fgColor;
            Brush? borderBrush = null;

            if (isSelected)
            {
                bgColor = _isDarkMode ? DarkSelectedDayBg : LightSelectedDayBg;
                fgColor = _isDarkMode ? DarkSelectedDayText : LightSelectedDayText;
            }
            else if (isToday && isCurrentMonth)
            {
                bgColor = Colors.Transparent;
                fgColor = _isDarkMode ? DarkCalendarText : LightCalendarText;
                borderBrush = new SolidColorBrush(_isDarkMode ? DarkTodayBorder : LightTodayBorder);
            }
            else if (!isCurrentMonth)
            {
                bgColor = Colors.Transparent;
                fgColor = _isDarkMode ? DarkInactiveText : LightInactiveText;
            }
            else
            {
                bgColor = Colors.Transparent;
                fgColor = _isDarkMode ? DarkCalendarText : LightCalendarText;
            }

            var btn = new Button
            {
                Content = new TextBlock 
                { 
                    Text = dayNumber.ToString(),
                    Foreground = new SolidColorBrush(fgColor),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                },
                Width = 34,
                Height = 34,
                FontSize = 13,
                Cursor = Cursors.Hand,
                Background = new SolidColorBrush(bgColor),
                Tag = date,
                Template = CreateCircleBtnTemplate(new SolidColorBrush(bgColor), borderBrush),
                FontWeight = isSelected || isToday ? FontWeights.Bold : FontWeights.Normal,
                Margin = new Thickness(1)
            };

            btn.Click += DayButton_Click;
            DaysGrid.Children.Add(btn);
        }

        private ControlTemplate CreateCircleBtnTemplate(Brush background, Brush? borderBrush = null)
        {
            var template = new ControlTemplate(typeof(Button));

            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(17));
            border.SetValue(Border.BackgroundProperty, background);
            border.SetValue(Border.WidthProperty, 34.0);
            border.SetValue(Border.HeightProperty, 34.0);

            if (borderBrush != null)
            {
                border.SetValue(Border.BorderBrushProperty, borderBrush);
                border.SetValue(Border.BorderThicknessProperty, new Thickness(1.5));
            }

            var content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            border.AppendChild(content);
            template.VisualTree = border;

            return template;
        }

        #endregion

        #region Event Handlers

        private void DayButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DateTime date)
            {
                // دمج التاريخ المختار مع الوقت الحالي للنظام لضمان دقة توقيت الشهادة
                _tempSelectedDate = date.Date.Add(DateTime.Now.TimeOfDay);
                
                _displayDate = new DateTime(date.Year, date.Month, 1);
                
                // تحديث واجهة التقويم لإبراز تحديد اليوم دون إغلاق النافذة (حتى يضغط موافق)
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
            // إيقاف انتقال الحدث حتى لا تغلق النافذة
            e.Handled = true;
        }

        private void GenerateYearPicker()
        {
            if (YearsGrid == null) return;
            YearsGrid.Children.Clear();

            int currentYear = DateTime.Today.Year;
            int selectedYear = _tempSelectedDate?.Year ?? _displayDate.Year;
            
            // نطاق السنين من 1900 إلى 2100
            for (int y = 1900; y <= 2100; y++)
            {
                var yearBtn = new Button
                {
                    Content = y.ToString(),
                    Margin = new Thickness(2),
                    Padding = new Thickness(5),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    FontSize = 14,
                    Tag = y
                };

                // تطبيق التنسيق
                Color fg = _isDarkMode ? DarkCalendarText : LightCalendarText;
                if (y == selectedYear)
                {
                    yearBtn.Background = new SolidColorBrush(_isDarkMode ? DarkSelectedDayBg : LightSelectedDayBg);
                    yearBtn.Foreground = Brushes.White;
                    yearBtn.FontWeight = FontWeights.Bold;
                }
                else
                {
                    yearBtn.Foreground = new SolidColorBrush(fg);
                    yearBtn.FontWeight = FontWeights.Normal;
                }

                yearBtn.Click += (s, ev) => {
                    _isYearPickerOpen = false;
                    YearPickerPanel.Visibility = Visibility.Collapsed;
                    
                    int year = (int)((Button)s).Tag;
                    int month = _displayDate.Month;
                    int day = Math.Min(_tempSelectedDate?.Day ?? _displayDate.Day, DateTime.DaysInMonth(year, month));
                    
                    _displayDate = new DateTime(year, month, 1);
                    if (_tempSelectedDate.HasValue)
                        _tempSelectedDate = new DateTime(year, month, day);

                    GenerateCalendar();
                    UpdateHeaderDisplay();
                };

                YearsGrid.Children.Add(yearBtn);

                if (y == selectedYear)
                {
                    // محاولة التمرير للسنة المختارة
                    Dispatcher.BeginInvoke(new Action(() => {
                        double offset = (y - 1900) / 4 * 40; // تقديري حسب الارتفاع
                        YearScrollViewer.ScrollToVerticalOffset(offset);
                    }), System.Windows.Threading.DispatcherPriority.Background);
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

        #endregion
    }
}
