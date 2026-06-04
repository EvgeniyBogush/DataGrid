using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;

namespace EnecaDataGrid;

public sealed class ColumnFilterViewModel : NotifyObject
{
    private readonly Func<OrderRow, string> _valueSelector;
    private readonly Action _filterChanged;
    private string _searchText = string.Empty;
    private bool _isPopupOpen;
    private bool _isBulkUpdating;
    private bool? _selectAllState = true;
    private string? _selectedSingleValue;

    public ColumnFilterViewModel(
        string title,
        Func<OrderRow, string> valueSelector,
        Action filterChanged)
    {
        Title = title;
        _valueSelector = valueSelector;
        _filterChanged = filterChanged;

        OptionsView = CollectionViewSource.GetDefaultView(Options);
        OptionsView.Filter = MatchesOptionSearch;

        SelectAllCommand = new RelayCommand(SelectAll);
        ClearSelectionCommand = new RelayCommand(ClearSelection);
    }

    public string Title { get; }

    public ObservableCollection<FilterOptionViewModel> Options { get; } = new();

    public ICollectionView OptionsView { get; }

    public ICommand SelectAllCommand { get; }

    public ICommand ClearSelectionCommand { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                OptionsView.Refresh();
            }
        }
    }

    public bool IsPopupOpen
    {
        get => _isPopupOpen;
        set => SetProperty(ref _isPopupOpen, value);
    }

    public bool? SelectAllState
    {
        get => _selectAllState;
        set
        {
            if (SetProperty(ref _selectAllState, value))
            {
                ApplySelectAllState(value);
            }
        }
    }

    public string? SelectedSingleValue
    {
        get => _selectedSingleValue;
        set
        {
            if (SetProperty(ref _selectedSingleValue, value))
            {
                ApplySingleValueSelection();
            }
        }
    }

    public string FilterSummary
    {
        get
        {
            var selectedCount = Options.Count(option => option.IsSelected);
            return selectedCount == Options.Count ? "All values" : $"{selectedCount} of {Options.Count} selected";
        }
    }

    public bool Matches(OrderRow row)
    {
        var selectedValues = Options.Where(option => option.IsSelected).Select(option => option.Value).ToHashSet(StringComparer.Ordinal);
        return selectedValues.Contains(_valueSelector(row));
    }

    public void RebuildOptions(IEnumerable<OrderRow> rows)
    {
        var wasAllSelected = Options.Count == 0 || Options.All(option => option.IsSelected);
        var selectedValues = Options.Where(option => option.IsSelected).Select(option => option.Value).ToHashSet(StringComparer.Ordinal);
        var values = rows.Select(_valueSelector).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();

        Options.Clear();
        foreach (var value in values)
        {
            var isSelected = wasAllSelected || selectedValues.Contains(value);
            Options.Add(new FilterOptionViewModel(value, isSelected, HandleOptionSelectionChanged));
        }

        SyncSelectedSingleValueFromOptions();
        UpdateSelectAllState();
        NotifyFilterChanged();
        OptionsView.Refresh();
    }

    private void UpdateSelections(Func<FilterOptionViewModel, bool> resolveSelection)
    {
        _isBulkUpdating = true;
        try
        {
            foreach (var option in Options)
            {
                option.IsSelected = resolveSelection(option);
            }
        }
        finally
        {
            _isBulkUpdating = false;
        }

        NotifyFilterChanged();
    }

    private bool MatchesOptionSearch(object item)
    {
        if (item is not FilterOptionViewModel option)
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(SearchText)
            || option.DisplayValue.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
    }

    private void SelectAll()
    {
        UpdateSelections(_ => true);
    }

    private void ClearSelection()
    {
        UpdateSelections(_ => false);
    }

    private void HandleOptionSelectionChanged()
    {
        if (!_isBulkUpdating)
        {
            NotifyFilterChanged();
        }
    }

    private void NotifyFilterChanged()
    {
        SyncSelectedSingleValueFromOptions();
        UpdateSelectAllState();
        OnPropertyChanged(nameof(FilterSummary));
        _filterChanged();
    }

    private void ApplySingleValueSelection()
    {
        if (_isBulkUpdating)
        {
            return;
        }

        if (string.IsNullOrEmpty(SelectedSingleValue))
        {
            UpdateSelections(_ => true);
            return;
        }

        UpdateSelections(option => string.Equals(option.Value, SelectedSingleValue, StringComparison.Ordinal));
    }

    private void SyncSelectedSingleValueFromOptions()
    {
        var selectedValues = Options.Where(option => option.IsSelected).Select(option => option.Value).ToList();
        var nextValue = selectedValues.Count == 1 ? selectedValues[0] : null;

        if (string.Equals(_selectedSingleValue, nextValue, StringComparison.Ordinal))
        {
            return;
        }

        _selectedSingleValue = nextValue;
        OnPropertyChanged(nameof(SelectedSingleValue));
    }

    private void ApplySelectAllState(bool? state)
    {
        if (_isBulkUpdating || state is null)
        {
            return;
        }

        UpdateSelections(_ => state.Value);
    }

    private void UpdateSelectAllState()
    {
        var totalCount = Options.Count;
        bool? nextState = false;
        if (totalCount > 0)
        {
            var selectedCount = Options.Count(option => option.IsSelected);
            nextState = selectedCount == 0
                ? false
                : selectedCount == totalCount
                    ? true
                    : null;
        }

        if (_selectAllState == nextState)
        {
            return;
        }

        _selectAllState = nextState;
        OnPropertyChanged(nameof(SelectAllState));
    }
}
