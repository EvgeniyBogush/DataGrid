using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace EnecaDataGrid;

public sealed class OrderRow : NotifyObject, IDataErrorInfo
{
    private int _orderId;
    private string _customer;
    private string _city;
    private string _status;
    private DateTime _dueDate;
    private decimal _amount;
    private bool _isActive;

    public OrderRow(
        int orderId,
        string customer,
        string city,
        string status,
        DateTime dueDate,
        decimal amount,
        bool isActive)
    {
        _orderId = orderId;
        _customer = customer;
        _city = city;
        _status = status;
        _dueDate = dueDate;
        _amount = amount;
        _isActive = isActive;
    }

    public int OrderId
    {
        get => _orderId;
        set => SetProperty(ref _orderId, value);
    }

    [Required]
    public string Customer
    {
        get => _customer;
        set
        {
            if (SetProperty(ref _customer, value))
            {
                OnPropertyChanged(nameof(ValidationMessage));
                OnPropertyChanged(nameof(HasValidationError));
            }
        }
    }

    public string City
    {
        get => _city;
        set => SetProperty(ref _city, value);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public DateTime DueDate
    {
        get => _dueDate;
        set => SetProperty(ref _dueDate, value);
    }

    public decimal Amount
    {
        get => _amount;
        set => SetProperty(ref _amount, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public string ValidationMessage => string.IsNullOrWhiteSpace(Customer) ? "Customer is required" : string.Empty;

    public bool HasValidationError => !string.IsNullOrEmpty(ValidationMessage);

    public string Error => ValidationMessage;

    public string this[string columnName] => columnName == nameof(Customer) ? ValidationMessage : string.Empty;

    public OrderRow CloneWithOrderId(int orderId)
    {
        return new OrderRow(orderId, Customer, City, Status, DueDate, Amount, IsActive);
    }
}
