using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using Enjaz.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Enjaz.Views;

public class LockScreenWindow : Window, IComponentConnector
{
	internal Grid LogoContainer;

	internal ScaleTransform LogoScale;

	private bool _contentLoaded;

	public LockScreenWindow()
	{
		InitializeComponent();
	}

	private void Window_MouseDown(object sender, MouseButtonEventArgs e)
	{
		TransitionToLogin();
	}

	private void Window_KeyDown(object sender, KeyEventArgs e)
	{
		TransitionToLogin();
	}

	private void TransitionToLogin()
	{
		if (Application.Current is App app)
		{
			LoginWindow requiredService = app.ServiceProvider.GetRequiredService<LoginWindow>();
			if (requiredService.DataContext is LoginViewModel loginViewModel)
			{
				loginViewModel.ShowSessionExpiredMessage();
			}
			requiredService.Show();
			Close();
		}
		else
		{
			LoginWindow loginWindow = new LoginWindow();
			loginWindow.Show();
			Close();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "8.0.21.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/Enjaz;component/views/lockscreenwindow.xaml", UriKind.Relative);
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
			((LockScreenWindow)target).MouseDown += Window_MouseDown;
			((LockScreenWindow)target).KeyDown += Window_KeyDown;
			break;
		case 2:
			LogoContainer = (Grid)target;
			break;
		case 3:
			LogoScale = (ScaleTransform)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
