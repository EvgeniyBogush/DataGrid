using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace EnecaDataGrid;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
        Loaded += (_, _) => ConfigureColumnFilterHeaders();
    }

    private void ConfigureColumnFilterHeaders()
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        foreach (var column in OrdersGrid.Columns)
        {
            if (column is DataGridTextColumn textColumn)
            {
                var path = (textColumn.Binding as Binding)?.Path?.Path;
                if (string.Equals(path, nameof(OrderRow.OrderId), StringComparison.Ordinal))
                {
                    textColumn.Header = vm.OrderIdFilter;
                }
                else if (string.Equals(path, nameof(OrderRow.Customer), StringComparison.Ordinal))
                {
                    textColumn.Header = vm.CustomerFilter;
                }
                else if (string.Equals(path, nameof(OrderRow.Amount), StringComparison.Ordinal))
                {
                    textColumn.Header = vm.AmountFilter;
                }
            }
        }

        var templateColumns = OrdersGrid.Columns
            .OfType<DataGridTemplateColumn>()
            .Where(column => !IsCheckBoxSelectionColumn(column))
            .ToList();
        if (templateColumns.Count > 0)
        {
            templateColumns[0].Header = vm.CityFilter;
        }

        if (templateColumns.Count > 1)
        {
            templateColumns[1].Header = vm.StatusFilter;
        }

        if (templateColumns.Count > 2)
        {
            templateColumns[2].Header = vm.DueDateFilter;
        }
    }

    private static bool IsCheckBoxSelectionColumn(DataGridTemplateColumn column)
    {
        if (column.Header is CheckBox)
        {
            return true;
        }

        return column.Width.UnitType == DataGridLengthUnitType.Pixel
            && Math.Abs(column.Width.DisplayValue - 32d) < 0.1
            && !column.CanUserReorder
            && !column.CanUserResize;
    }
}
