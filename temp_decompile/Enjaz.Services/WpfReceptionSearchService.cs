using System.Linq;
using System.Windows;
using Enjaz.Models;
using Enjaz.Services.Repositories;
using Enjaz.ViewModels;
using Enjaz.Views;

namespace Enjaz.Services;

public class WpfReceptionSearchService : IReceptionSearchService
{
	private readonly SampleReceptionRepository _sampleReceptionRepository;

	public WpfReceptionSearchService(SampleReceptionRepository sampleReceptionRepository)
	{
		_sampleReceptionRepository = sampleReceptionRepository;
	}

	public SampleReception? ShowSearchDialog()
	{
		ReceptionSearchViewModel viewModel = new ReceptionSearchViewModel(_sampleReceptionRepository);
		ReceptionSearchWindow receptionSearchWindow = new ReceptionSearchWindow(viewModel);
		Window window = Application.Current.Windows.OfType<Window>().FirstOrDefault((Window w) => w.IsActive);
		if (window != null && window != receptionSearchWindow)
		{
			receptionSearchWindow.Owner = window;
		}
		else if (Application.Current.MainWindow != null && Application.Current.MainWindow != receptionSearchWindow)
		{
			receptionSearchWindow.Owner = Application.Current.MainWindow;
		}
		if (receptionSearchWindow.ShowDialog() == true && receptionSearchWindow.SelectedReception != null)
		{
			return receptionSearchWindow.SelectedReception;
		}
		return null;
	}
}
