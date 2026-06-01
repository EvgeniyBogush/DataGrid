using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;

namespace EnecaDataGrid;

public sealed class MainWindowViewModel : NotifyObject
{
    private string _filterText = string.Empty;
    private OrderRow? _selectedOrder;
    private bool _isEditingEnabled = true;

    public MainWindowViewModel()
    {
        Orders = new ObservableCollection<OrderRow>(OrderRows.CreateSample());
        OrdersView = CollectionViewSource.GetDefaultView(Orders);

        OrderIdFilter = new ColumnFilterViewModel("Order", nameof(OrderRow.OrderId), row => row.OrderId.ToString(), RefreshFilteredRows);
        CustomerFilter = new ColumnFilterViewModel("Customer", nameof(OrderRow.Customer), row => row.Customer, RefreshFilteredRows);
        CityFilter = new ColumnFilterViewModel("City", nameof(OrderRow.City), row => row.City, RefreshFilteredRows);
        StatusFilter = new ColumnFilterViewModel("Status", nameof(OrderRow.Status), row => row.Status, RefreshFilteredRows);
        DueDateFilter = new ColumnFilterViewModel("Due date", nameof(OrderRow.DueDate), row => row.DueDate.ToShortDateString(), RefreshFilteredRows);
        AmountFilter = new ColumnFilterViewModel("Amount", nameof(OrderRow.Amount), row => row.Amount.ToString("N2"), RefreshFilteredRows);
        ActiveFilter = new ColumnFilterViewModel("Active", nameof(OrderRow.IsActive), row => row.IsActive ? "Yes" : "No", RefreshFilteredRows);
        ValidationFilter = new ColumnFilterViewModel("Validation", nameof(OrderRow.ValidationMessage), row => row.ValidationMessage, RefreshFilteredRows);

        Orders.CollectionChanged += OrdersCollectionChanged;
        RebuildColumnFilterOptions();
        OrdersView.Filter = MatchesFilter;
        OrdersView.Refresh();
        SelectedOrder = Orders.FirstOrDefault();

        DuplicateSelectedCommand = new RelayCommand(DuplicateSelected, () => SelectedOrder is not null);
        DeleteSelectedCommand = new RelayCommand(DeleteSelected, () => SelectedOrder is not null);
        ToggleReadOnlyCommand = new RelayCommand(() => IsEditingEnabled = !IsEditingEnabled);
    }

    public ObservableCollection<OrderRow> Orders { get; }

    public ICollectionView OrdersView { get; }

    public ColumnFilterViewModel OrderIdFilter { get; }

    public ColumnFilterViewModel CustomerFilter { get; }

    public ColumnFilterViewModel CityFilter { get; }

    public ColumnFilterViewModel StatusFilter { get; }

    public ColumnFilterViewModel DueDateFilter { get; }

    public ColumnFilterViewModel AmountFilter { get; }

    public ColumnFilterViewModel ActiveFilter { get; }

    public ColumnFilterViewModel ValidationFilter { get; }

    public ICommand DuplicateSelectedCommand { get; }

    public ICommand DeleteSelectedCommand { get; }

    public ICommand ToggleReadOnlyCommand { get; }

    public string FilterText
    {
        get => _filterText;
        set
        {
            if (SetProperty(ref _filterText, value))
            {
                RefreshFilteredRows();
            }
        }
    }

    public OrderRow? SelectedOrder
    {
        get => _selectedOrder;
        set
        {
            if (SetProperty(ref _selectedOrder, value))
            {
                OnPropertyChanged(nameof(SelectedOrderSummary));
                RelayCommand.RaiseCanExecuteChanged(DuplicateSelectedCommand, DeleteSelectedCommand);
            }
        }
    }

    public bool IsEditingEnabled
    {
        get => _isEditingEnabled;
        set
        {
            if (SetProperty(ref _isEditingEnabled, value))
            {
                OnPropertyChanged(nameof(IsGridReadOnly));
                OnPropertyChanged(nameof(EditModeButtonText));
            }
        }
    }

    public bool IsGridReadOnly => !IsEditingEnabled;

    public string EditModeButtonText => IsEditingEnabled ? "Switch to read-only" : "Enable editing";

    public string SelectedOrderSummary =>
        SelectedOrder is null
            ? "No row selected"
            : $"Selected: #{SelectedOrder.OrderId} - {SelectedOrder.Customer} - {SelectedOrder.Status}";

    private bool MatchesFilter(object item)
    {
        if (item is not OrderRow row)
        {
            return false;
        }

        if (!MatchesColumnFilters(row))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(FilterText))
        {
            return true;
        }

        return row.Customer.Contains(FilterText, StringComparison.OrdinalIgnoreCase)
            || row.City.Contains(FilterText, StringComparison.OrdinalIgnoreCase)
            || row.Status.Contains(FilterText, StringComparison.OrdinalIgnoreCase);
    }

    private IEnumerable<ColumnFilterViewModel> ColumnFilters
    {
        get
        {
            yield return OrderIdFilter;
            yield return CustomerFilter;
            yield return CityFilter;
            yield return StatusFilter;
            yield return DueDateFilter;
            yield return AmountFilter;
            yield return ActiveFilter;
            yield return ValidationFilter;
        }
    }

    private bool MatchesColumnFilters(OrderRow row)
    {
        return ColumnFilters.All(filter => filter.Matches(row));
    }

    private void RefreshFilteredRows()
    {
        CommitPendingGridEdit();
        OrdersView.Refresh();
        if (SelectedOrder is null || !OrdersView.Contains(SelectedOrder))
        {
            SelectedOrder = OrdersView.Cast<OrderRow>().FirstOrDefault();
        }
    }

    private void CommitPendingGridEdit()
    {
        if (OrdersView is not IEditableCollectionView editableView)
        {
            return;
        }

        if (editableView.IsAddingNew)
        {
            editableView.CommitNew();
        }

        if (editableView.IsEditingItem)
        {
            editableView.CommitEdit();
        }
    }

    private void DuplicateSelected()
    {
        if (SelectedOrder is null)
        {
            return;
        }

        var clone = SelectedOrder.CloneWithOrderId(Orders.Max(order => order.OrderId) + 1);
        Orders.Add(clone);
        SelectedOrder = clone;
    }

    private void DeleteSelected()
    {
        if (SelectedOrder is null)
        {
            return;
        }

        var index = Orders.IndexOf(SelectedOrder);
        Orders.Remove(SelectedOrder);
        SelectedOrder = Orders.Count == 0 ? null : Orders[Math.Clamp(index, 0, Orders.Count - 1)];
    }

    private void OrdersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildColumnFilterOptions();
    }

    private void RebuildColumnFilterOptions()
    {
        foreach (var filter in ColumnFilters)
        {
            filter.RebuildOptions(Orders);
        }
    }
}
