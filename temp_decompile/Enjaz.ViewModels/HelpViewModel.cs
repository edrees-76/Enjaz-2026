using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Enjaz.Helpers;
using Enjaz.Models;
using Enjaz.Services;

namespace Enjaz.ViewModels;

public class HelpViewModel : BaseViewModel
{
	private readonly HelpDataService _helpDataService;

	private ObservableCollection<HelpContent> _topics;

	private string _searchText = string.Empty;

	private HelpContent? _selectedTopic;

	private string _selectedCategory = "الكل";

	public ObservableCollection<HelpContent> Topics
	{
		get
		{
			return _topics;
		}
		set
		{
			SetProperty(ref _topics, value, "Topics");
		}
	}

	public ObservableCollection<string> Categories { get; }

	public string SelectedCategory
	{
		get
		{
			return _selectedCategory;
		}
		set
		{
			if (SetProperty(ref _selectedCategory, value, "SelectedCategory"))
			{
				LoadTopicsAsync();
			}
		}
	}

	public string SearchText
	{
		get
		{
			return _searchText;
		}
		set
		{
			if (SetProperty(ref _searchText, value, "SearchText"))
			{
				LoadTopicsAsync();
			}
		}
	}

	public HelpContent? SelectedTopic
	{
		get
		{
			return _selectedTopic;
		}
		set
		{
			if (SetProperty(ref _selectedTopic, value, "SelectedTopic"))
			{
				if (value != null)
				{
					_helpDataService.IncrementsViewsAsync(value.Id);
				}
				OnPropertyChanged("CurrentContent");
				OnPropertyChanged("HasSelectedTopic");
			}
		}
	}

	public string CurrentContent => SelectedTopic?.ContentSimple ?? string.Empty;

	public bool HasSelectedTopic => SelectedTopic != null;

	public ICommand SelectTopicCommand { get; private set; } = null;

	public ICommand ClearSelectionCommand { get; private set; } = null;

	public HelpViewModel(HelpDataService helpDataService)
	{
		_helpDataService = helpDataService;
		_topics = new ObservableCollection<HelpContent>();
		Categories = new ObservableCollection<string> { "الكل", "الشهادات", "التقارير", "الأمان", "النظام", "الاستلامات" };
		InitializeCommands();
		InitializeAsync();
	}

	private async Task InitializeAsync()
	{
		base.IsBusy = true;
		try
		{
			await _helpDataService.SeedInitialDataAsync();
			await LoadTopicsAsync();
		}
		finally
		{
			base.IsBusy = false;
		}
	}

	private void InitializeCommands()
	{
		SelectTopicCommand = new RelayCommand(delegate(object? obj)
		{
			SelectedTopic = obj as HelpContent;
		});
		ClearSelectionCommand = new RelayCommand(delegate
		{
			SelectedTopic = null;
		});
	}

	private async Task LoadTopicsAsync()
	{
		LoggerService.LogInfo($"[HelpCenter] Loading topics for Category: '{SelectedCategory}', Search: '{SearchText}'");
		List<HelpContent> results = ((!string.IsNullOrWhiteSpace(SearchText)) ? (await _helpDataService.SearchTopicsAsync(SearchText)) : ((!(SelectedCategory != "الكل")) ? (await _helpDataService.GetAllTopicsAsync()) : (await _helpDataService.GetTopicsByCategoryAsync(SelectedCategory))));
		LoggerService.LogInfo($"[HelpCenter] Found {results.Count} results.");
		Topics = new ObservableCollection<HelpContent>(results);
	}

	public async Task ShowHelpForViewAsync(string viewName)
	{
		SearchText = string.Empty;
		SelectedCategory = "الكل";
		await LoadTopicsAsync();
		HelpContent topic = Topics.FirstOrDefault((HelpContent t) => t.RelatedView == viewName);
		if (topic != null)
		{
			SelectedTopic = topic;
		}
	}
}
