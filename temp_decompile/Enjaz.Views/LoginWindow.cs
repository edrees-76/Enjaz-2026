using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using Enjaz.ViewModels;

namespace Enjaz.Views;

public class LoginWindow : Window, IComponentConnector
{
	internal PasswordBox HiddenPasswordBox;

	internal TextBox VisiblePasswordBox;

	private bool _contentLoaded;

	public LoginWindow(LoginViewModel viewModel)
	{
		InitializeComponent();
		base.DataContext = viewModel;
	}

	public LoginWindow()
	{
		InitializeComponent();
	}

	private void Window_MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed)
		{
			DragMove();
		}
	}

	private void CloseButton_Click(object sender, RoutedEventArgs e)
	{
		Application.Current.Shutdown();
	}

	private void MinimizeButton_Click(object sender, RoutedEventArgs e)
	{
		base.WindowState = WindowState.Minimized;
	}

	private void MaximizeButton_Click(object sender, RoutedEventArgs e)
	{
		if (base.WindowState == WindowState.Maximized)
		{
			base.WindowState = WindowState.Normal;
		}
		else
		{
			base.WindowState = WindowState.Maximized;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "8.0.21.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/Enjaz;component/views/loginwindow.xaml", UriKind.Relative);
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
			((LoginWindow)target).MouseDown += Window_MouseDown;
			break;
		case 2:
			((Button)target).Click += MinimizeButton_Click;
			break;
		case 3:
			((Button)target).Click += CloseButton_Click;
			break;
		case 4:
			HiddenPasswordBox = (PasswordBox)target;
			break;
		case 5:
			VisiblePasswordBox = (TextBox)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
