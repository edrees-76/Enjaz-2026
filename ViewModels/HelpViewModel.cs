using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using Enjaz.Models;
using Enjaz.Services;
using Enjaz.Helpers;

namespace Enjaz.ViewModels
{
    public class HelpViewModel : BaseViewModel
    {
        private readonly HelpDataService _helpDataService;
        private ObservableCollection<HelpContent> _topics;
        private string _searchText = string.Empty;
        private HelpContent? _selectedTopic;
        private string _selectedCategory = "الكل";

        public HelpViewModel(HelpDataService helpDataService)
        {
            _helpDataService = helpDataService;
            _topics = new ObservableCollection<HelpContent>();
            Categories = new ObservableCollection<string> { "الكل", "الشهادات", "التقارير", "الأمان", "النظام", "الاستلامات" };
            
            InitializeCommands();
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            IsBusy = true;
            try
            {
                await _helpDataService.SeedInitialDataAsync();
                await LoadTopicsAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        public ObservableCollection<HelpContent> Topics
        {
            get => _topics;
            set => SetProperty(ref _topics, value);
        }

        public ObservableCollection<string> Categories { get; }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    _ = LoadTopicsAsync();
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = LoadTopicsAsync();
                }
            }
        }

        public HelpContent? SelectedTopic
        {
            get => _selectedTopic;
            set
            {
                if (SetProperty(ref _selectedTopic, value))
                {
                    if (value != null)
                    {
                        _ = _helpDataService.IncrementsViewsAsync(value.Id);
                    }
                    OnPropertyChanged(nameof(CurrentContent));
                    OnPropertyChanged(nameof(HasSelectedTopic));
                }
            }
        }

        public string CurrentContent => SelectedTopic?.ContentSimple ?? string.Empty;
        public bool HasSelectedTopic => SelectedTopic != null;

        public ICommand SelectTopicCommand { get; private set; } = null!;
        public ICommand ClearSelectionCommand { get; private set; } = null!;
        private void InitializeCommands()
        {
            SelectTopicCommand = new RelayCommand(obj => SelectedTopic = obj as HelpContent);
            ClearSelectionCommand = new RelayCommand(_ => SelectedTopic = null);
        }

        private async Task LoadTopicsAsync()
        {
            List<HelpContent> results;
            LoggerService.LogInfo($"[HelpCenter] Loading topics for Category: '{SelectedCategory}', Search: '{SearchText}'");

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                results = await _helpDataService.SearchTopicsAsync(SearchText);
            }
            else if (SelectedCategory != "الكل")
            {
                results = await _helpDataService.GetTopicsByCategoryAsync(SelectedCategory);
            }
            else
            {
                results = await _helpDataService.GetAllTopicsAsync();
            }

            LoggerService.LogInfo($"[HelpCenter] Found {results.Count} results.");
            Topics = new ObservableCollection<HelpContent>(results);
        }

        // Contextual Help Method
        public async Task ShowHelpForViewAsync(string viewName)
        {
            SearchText = string.Empty;
            SelectedCategory = "الكل";
            await LoadTopicsAsync();
            
            var topic = Topics.FirstOrDefault(t => t.RelatedView == viewName);
            if (topic != null)
            {
                SelectedTopic = topic;
            }
        }
    }
}
