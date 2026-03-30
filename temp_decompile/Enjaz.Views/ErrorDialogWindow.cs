using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace Enjaz.Views;

public class ErrorDialogWindow : Window, IComponentConnector
{
	internal TextBlock TitleText;

	internal TextBlock MessageText;

	private bool _contentLoaded;

	public ErrorDialogWindow(string title, string message)
	{
		InitializeComponent();
		TitleText.Text = title;
		MessageText.Text = message;
	}

	private void CloseButton_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Clipboard.SetText(MessageText.Text);
		}
		catch
		{
		}
	}

	private void Window_MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (e.ChangedButton == MouseButton.Left)
		{
			DragMove();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "8.0.21.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/Enjaz;component/views/errordialogwindow.xaml", UriKind.Relative);
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
			((ErrorDialogWindow)target).MouseDown += Window_MouseDown;
			break;
		case 2:
			TitleText = (TextBlock)target;
			break;
		case 3:
			MessageText = (TextBlock)target;
			break;
		case 4:
			((Button)target).Click += CopyToClipboard_Click;
			break;
		case 5:
			((Button)target).Click += CloseButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
