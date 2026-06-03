using System.Windows;
using System.Windows.Controls;

namespace EnecaDataGrid.Controls;

public interface IFilterVisibilityColumn
{
    bool IsFilterVisible { get; }
}

public sealed class EnecaDataGridTextColumn : DataGridTextColumn, IFilterVisibilityColumn
{
    public static readonly DependencyProperty IsFilterVisibleProperty =
        DependencyProperty.Register(
            nameof(IsFilterVisible),
            typeof(bool),
            typeof(EnecaDataGridTextColumn),
            new PropertyMetadata(false));

    public bool IsFilterVisible
    {
        get => (bool)GetValue(IsFilterVisibleProperty);
        set => SetValue(IsFilterVisibleProperty, value);
    }
}

public sealed class EnecaDataGridTemplateColumn : DataGridTemplateColumn, IFilterVisibilityColumn
{
    public static readonly DependencyProperty IsFilterVisibleProperty =
        DependencyProperty.Register(
            nameof(IsFilterVisible),
            typeof(bool),
            typeof(EnecaDataGridTemplateColumn),
            new PropertyMetadata(false));

    public bool IsFilterVisible
    {
        get => (bool)GetValue(IsFilterVisibleProperty);
        set => SetValue(IsFilterVisibleProperty, value);
    }
}
