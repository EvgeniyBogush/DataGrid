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
        OnPropertyChanged(nameof(FilterSummary));
        _filterChanged();
    }
}
