using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Enjaz.ViewModels;

namespace Enjaz.Views;

public class MainWindow : Window, IComponentConnector
{
	private bool _isLoggingOut = false;

	internal StackPanel NavigationPanel;

	internal Button AlertsButton;

	private bool _contentLoaded;

	public MainWindow(MainViewModel viewModel)
	{
		MainWindow owner = this;
		InitializeComponent();
		base.DataContext = viewModel;
		base.Closing += MainWindow_Closing;
		viewModel.ReportingVM.RequestOpenDashboard += delegate
		{
			DashboardWindow dashboardWindow = new DashboardWindow
			{
				DataContext = viewModel.ReportingVM,
				Owner = owner
			};
			dashboardWindow.Show();
		};
	}

	private void MainWindow_Closing(object? sender, CancelEventArgs e)
	{
		if (_isLoggingOut)
		{
			return;
		}
		if (base.DataContext is MainViewModel { IsAnyModalOpen: not false } mainViewModel)
		{
			e.Cancel = true;
			mainViewModel.SetNotification("نافذة مفتوحة", "يرجى إغلاق النافذة الحالية أو إنهاء العملية القائمة قبل إغلاق المنظومة.", NotificationType.Warning);
			return;
		}
		e.Cancel = true;
		if (base.DataContext is MainViewModel mainViewModel2)
		{
			mainViewModel2.PerformLogoutAsync(askConfirmation: true);
		}
	}

	public void SetLoggingOut()
	{
		_isLoggingOut = true;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "8.0.21.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/Enjaz;component/views/mainwindow.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "8.0.21.0")]
	internal Delegate _CreateDelegate(Type delegateType, string handler)
	{
		return Delegate.CreateDelegate(delegateType, this, handler);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "8.0.21.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			NavigationPanel = (StackPanel)target;
			break;
		case 2:
			AlertsButton = (Button)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
