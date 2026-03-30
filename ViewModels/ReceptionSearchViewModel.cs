using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Enjaz.Helpers;
using Enjaz.Models;
using Enjaz.Services.Repositories;

namespace Enjaz.ViewModels
{
    public class ReceptionSearchViewModel : BaseViewModel
    {
        private readonly SampleReceptionRepository _repository;

        private string _searchField = "رقم العينة";
        private string _searchValue = string.Empty;
        private ObservableCollection<SampleReception> _searchResults;
        private SampleReception? _selectedResult;

        public ReceptionSearchViewModel(SampleReceptionRepository repository)
        {
            _repository = repository;
            _searchResults = new ObservableCollection<SampleReception>();

            SearchCommand = new AsyncRelayCommand(async _ => await SearchAsync(), _ => !string.IsNullOrWhiteSpace(SearchValue));
        }

        public ObservableCollection<string> SearchFields { get; } = new ObservableCollection<string>
        {
            "رقم العينة",
            "رقم الإخطار",
            "رقم الإقرار"
        };

        public string SearchField
        {
            get => _searchField;
            set => SetProperty(ref _searchField, value);
        }

        public string SearchValue
        {
            get => _searchValue;
            set
            {
                SetProperty(ref _searchValue, value);
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ObservableCollection<SampleReception> SearchResults
        {
            get => _searchResults;
            set => SetProperty(ref _searchResults, value);
        }

        public SampleReception? SelectedResult
        {
            get => _selectedResult;
            set => SetProperty(ref _selectedResult, value);
        }

        public ICommand SearchCommand { get; }

        /// <summary>
        /// الدالة الخاصة التي يستدعيها الأمر. تمت إضافة event لإخطار النافذة بالإغلاق عند الاختيار.
        /// </summary>
        public event Action<SampleReception>? ReceptionSelected;

        public void ConfirmSelection()
        {
            if (SelectedResult != null)
            {
                ReceptionSelected?.Invoke(SelectedResult);
            }
        }

        private async System.Threading.Tasks.Task SearchAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "جاري البحث...";

                var results = await _repository.SearchReceptionsByFieldAsync(SearchField, SearchValue);

                SearchResults.Clear();
                int seq = 1;
                foreach (var rec in results)
                {
                    rec.Sequence = seq++;
                    SearchResults.Add(rec);
                }

                StatusMessage = $"تم العثور على {SearchResults.Count} نتيجة";
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطأ: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
