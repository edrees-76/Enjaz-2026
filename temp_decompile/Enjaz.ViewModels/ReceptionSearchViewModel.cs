using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Enjaz.Helpers;
using Enjaz.Models;
using Enjaz.Services.Repositories;

namespace Enjaz.ViewModels;

public class ReceptionSearchViewModel : BaseViewModel
{
	private readonly SampleReceptionRepository _repository;

	private string _searchField = "رقم العينة";

	private string _searchValue = string.Empty;

	private ObservableCollection<SampleReception> _searchResults;

	private SampleReception? _selectedResult;

	public ObservableCollection<string> SearchFields { get; } = new ObservableCollection<string> { "رقم العينة", "رقم الإخطار", "رقم الإقرار" };

	public string SearchField
	{
		get
		{
			return _searchField;
		}
		set
		{
			SetProperty(ref _searchField, value, "SearchField");
		}
	}

	public string SearchValue
	{
		get
		{
			return _searchValue;
		}
		set
		{
			SetProperty(ref _searchValue, value, "SearchValue");
			CommandManager.InvalidateRequerySuggested();
		}
	}

	public ObservableCollection<SampleReception> SearchResults
	{
		get
		{
			return _searchResults;
		}
		set
		{
			SetProperty(ref _searchResults, value, "SearchResults");
		}
	}

	public SampleReception? SelectedResult
	{
		get
		{
			return _selectedResult;
		}
		set
		{
			SetProperty(ref _selectedResult, value, "SelectedResult");
		}
	}

	public ICommand SearchCommand { get; }

	public event Action<SampleReception>? ReceptionSelected;

	public ReceptionSearchViewModel(SampleReceptionRepository repository)
	{
		_repository = repository;
		_searchResults = new ObservableCollection<SampleReception>();
		SearchCommand = new AsyncRelayCommand((Func<object?, Task>)async delegate
		{
			await SearchAsync();
		}, (Predicate<object?>?)((object? _) => !string.IsNullOrWhiteSpace(SearchValue)), (AsyncRelayCommand.IErrorHandler?)null);
	}

	public void ConfirmSelection()
	{
		if (SelectedResult != null)
		{
			this.ReceptionSelected?.Invoke(SelectedResult);
		}
	}

	private async Task SearchAsync()
	{
		try
		{
			base.IsBusy = true;
			base.StatusMessage = "جاري البحث...";
			List<SampleReception> results = await _repository.SearchReceptionsByFieldAsync(SearchField, SearchValue);
			SearchResults.Clear();
			int seq = 1;
			foreach (SampleReception rec in results)
			{
				rec.Sequence = seq++;
				SearchResults.Add(rec);
			}
			base.StatusMessage = $"تم العثور على {SearchResults.Count} نتيجة";
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			base.StatusMessage = "خطأ: " + ex2.Message;
		}
		finally
		{
			base.IsBusy = false;
		}
	}
}
