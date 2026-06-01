namespace EnecaDataGrid;

public sealed class FilterOptionViewModel : NotifyObject
{
    private readonly Action _selectionChanged;
    private bool _isSelected;

    public FilterOptionViewModel(string value, bool isSelected, Action selectionChanged)
    {
        Value = value;
        DisplayValue = string.IsNullOrEmpty(value) ? "(empty)" : value;
        _isSelected = isSelected;
        _selectionChanged = selectionChanged;
    }

    public string Value { get; }

    public string DisplayValue { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                _selectionChanged();
            }
        }
    }
}
