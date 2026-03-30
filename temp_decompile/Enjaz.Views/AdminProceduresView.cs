using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Enjaz.Views;

public class AdminProceduresView : UserControl, IComponentConnector
{
	internal StackPanel Stage1;

	internal StackPanel Stage2;

	internal StackPanel Stage3;

	private bool _contentLoaded;

	public AdminProceduresView()
	{
		InitializeComponent();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "8.0.21.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/Enjaz;component/views/adminproceduresview.xaml", UriKind.Relative);
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
			Stage1 = (StackPanel)target;
			break;
		case 2:
			Stage2 = (StackPanel)target;
			break;
		case 3:
			Stage3 = (StackPanel)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
