using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Enjaz.Models;
using Enjaz.ViewModels;
using MaterialDesignThemes.Wpf;

namespace Enjaz.Views;

public class ReceptionSearchWindow : Window, IComponentConnector
{
	private readonly ReceptionSearchViewModel _viewModel;

	internal TextBox SearchValueBox;

	private bool _contentLoaded;

	public SampleReception? SelectedReception { get; private set; }

	public ReceptionSearchWindow(ReceptionSearchViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		base.DataContext = _viewModel;
		_viewModel.ReceptionSelected += OnReceptionSelected;
		_viewModel.PropertyChanged += delegate(object? s, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "IsNotificationDialogOpen" && _viewModel.IsNotificationDialogOpen)
			{
				DialogHost.Show(new object(), "SearchDialogHost");
			}
		};
		_viewModel.RequestConfirmation += delegate(string title, string message, NotificationType type, Action<bool> callback, string? icon)
		{
			_viewModel.IsConfirmMode = true;
			_viewModel.NotificationTitle = title;
			_viewModel.NotificationMessage = message;
			_viewModel.NotificationType = type;
			DialogHost.Show(new object(), "SearchDialogHost");
		};
		base.Loaded += delegate
		{
			SearchValueBox.Focus();
		};
	}

	private void OnReceptionSelected(SampleReception reception)
	{
		SelectedReception = reception;
		base.DialogResult = true;
		Close();
	}

	private void SelectButton_Click(object sender, RoutedEventArgs e)
	{
		if (_viewModel.SelectedResult != null)
		{
			_viewModel.ConfirmSelection();
		}
		else
		{
			_viewModel.SetNotification("تنبيه", "يرجى اختيار نموذج استلام من الجدول أولا\u064b", NotificationType.Warning);
		}
	}

	private void CancelButton_Click(object sender, RoutedEventArgs e)
	{
		base.DialogResult = false;
		Close();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "8.0.21.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/Enjaz;component/views/receptionsearchwindow.xaml", UriKind.Relative);
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
			SearchValueBox = (TextBox)target;
			break;
		case 2:
			((Button)target).Click += SelectButton_Click;
			break;
		case 3:
			((Button)target).Click += CancelButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
