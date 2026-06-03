using System.Windows;

namespace EnecaDataGrid.Controls;

public static class ColumnHeaderState
{
    public static readonly DependencyProperty IsPressedProperty =
        DependencyProperty.RegisterAttached(
            "IsPressed",
            typeof(bool),
            typeof(ColumnHeaderState),
            new PropertyMetadata(false));

    public static readonly DependencyProperty IsClickedProperty =
        DependencyProperty.RegisterAttached(
            "IsClicked",
            typeof(bool),
            typeof(ColumnHeaderState),
            new PropertyMetadata(false));

    public static readonly DependencyProperty IsCheckBoxHeaderProperty =
        DependencyProperty.RegisterAttached(
            "IsCheckBoxHeader",
            typeof(bool),
            typeof(ColumnHeaderState),
            new PropertyMetadata(false));

    public static readonly DependencyProperty HideRightSeparatorProperty =
        DependencyProperty.RegisterAttached(
            "HideRightSeparator",
            typeof(bool),
            typeof(ColumnHeaderState),
            new PropertyMetadata(false));

    public static readonly DependencyProperty IsRowCheckBoxCheckedProperty =
        DependencyProperty.RegisterAttached(
            "IsRowCheckBoxChecked",
            typeof(bool),
            typeof(ColumnHeaderState),
            new PropertyMetadata(false));

    public static readonly DependencyProperty IsFilterVisibleProperty =
        DependencyProperty.RegisterAttached(
            "IsFilterVisible",
            typeof(bool),
            typeof(ColumnHeaderState),
            new PropertyMetadata(false));

    public static bool GetIsPressed(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsPressedProperty);
    }

    public static void SetIsPressed(DependencyObject obj, bool value)
    {
        obj.SetValue(IsPressedProperty, value);
    }

    public static bool GetIsClicked(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsClickedProperty);
    }

    public static void SetIsClicked(DependencyObject obj, bool value)
    {
        obj.SetValue(IsClickedProperty, value);
    }

    public static bool GetIsCheckBoxHeader(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsCheckBoxHeaderProperty);
    }

    public static void SetIsCheckBoxHeader(DependencyObject obj, bool value)
    {
        obj.SetValue(IsCheckBoxHeaderProperty, value);
    }

    public static bool GetHideRightSeparator(DependencyObject obj)
    {
        return (bool)obj.GetValue(HideRightSeparatorProperty);
    }

    public static void SetHideRightSeparator(DependencyObject obj, bool value)
    {
        obj.SetValue(HideRightSeparatorProperty, value);
    }

    public static bool GetIsRowCheckBoxChecked(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsRowCheckBoxCheckedProperty);
    }

    public static void SetIsRowCheckBoxChecked(DependencyObject obj, bool value)
    {
        obj.SetValue(IsRowCheckBoxCheckedProperty, value);
    }

    public static bool GetIsFilterVisible(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsFilterVisibleProperty);
    }

    public static void SetIsFilterVisible(DependencyObject obj, bool value)
    {
        obj.SetValue(IsFilterVisibleProperty, value);
    }
}
