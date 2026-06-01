using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace EnecaDataGrid.Controls;

public partial class ColumnHeaderControl : UserControl
{
    private DataGridColumnHeader? _ownerHeader;

    public static readonly DependencyProperty IsHeaderPressedProperty =
        DependencyProperty.Register(
            nameof(IsHeaderPressed),
            typeof(bool),
            typeof(ColumnHeaderControl),
            new PropertyMetadata(false));

    public static readonly DependencyProperty IsHeaderClickedProperty =
        DependencyProperty.Register(
            nameof(IsHeaderClicked),
            typeof(bool),
            typeof(ColumnHeaderControl),
            new PropertyMetadata(false));

    public ColumnHeaderControl()
    {
        InitializeComponent();

        Loaded += ColumnHeaderControlLoaded;
        Unloaded += ColumnHeaderControlUnloaded;
    }

    public bool IsHeaderPressed
    {
        get => (bool)GetValue(IsHeaderPressedProperty);
        set => SetValue(IsHeaderPressedProperty, value);
    }

    public bool IsHeaderClicked
    {
        get => (bool)GetValue(IsHeaderClickedProperty);
        set => SetValue(IsHeaderClickedProperty, value);
    }

    private void ColumnHeaderControlLoaded(object sender, RoutedEventArgs e)
    {
        _ownerHeader = FindAncestor<DataGridColumnHeader>(this);
        if (_ownerHeader is null)
        {
            return;
        }

        _ownerHeader.AddHandler(PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(HeaderPreviewMouseLeftButtonDown), true);
        _ownerHeader.AddHandler(PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(HeaderPreviewMouseLeftButtonUp), true);
        _ownerHeader.MouseLeave += HeaderMouseLeave;
        IsEnabled = _ownerHeader.IsEnabled;
        IsHeaderClicked = ColumnHeaderState.GetIsClicked(_ownerHeader);
    }

    private void HeaderPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        IsHeaderPressed = true;
        IsHeaderClicked = false;
        if (_ownerHeader is not null)
        {
            ColumnHeaderState.SetIsPressed(_ownerHeader, true);
            ColumnHeaderState.SetIsClicked(_ownerHeader, false);
        }
    }

    private void HeaderPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        IsHeaderPressed = false;
        IsHeaderClicked = true;
        if (_ownerHeader is not null)
        {
            ResetClickedHeaders(_ownerHeader);
            ColumnHeaderState.SetIsPressed(_ownerHeader, false);
            ColumnHeaderState.SetIsClicked(_ownerHeader, true);
        }
    }

    private void HeaderMouseLeave(object sender, MouseEventArgs e)
    {
        IsHeaderPressed = false;
        if (_ownerHeader is not null)
        {
            ColumnHeaderState.SetIsPressed(_ownerHeader, false);
        }
    }

    private void ColumnHeaderControlUnloaded(object sender, RoutedEventArgs e)
    {
        if (_ownerHeader is not null)
        {
            _ownerHeader.RemoveHandler(PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(HeaderPreviewMouseLeftButtonDown));
            _ownerHeader.RemoveHandler(PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(HeaderPreviewMouseLeftButtonUp));
            _ownerHeader.MouseLeave -= HeaderMouseLeave;
            ColumnHeaderState.SetIsPressed(_ownerHeader, false);
            _ownerHeader = null;
        }

        Loaded -= ColumnHeaderControlLoaded;
        Unloaded -= ColumnHeaderControlUnloaded;
    }

    private static T? FindAncestor<T>(DependencyObject current)
        where T : DependencyObject
    {
        var parent = VisualTreeHelper.GetParent(current);
        while (parent is not null)
        {
            if (parent is T match)
            {
                return match;
            }

            parent = VisualTreeHelper.GetParent(parent);
        }

        return null;
    }

    private static void ResetClickedHeaders(DataGridColumnHeader activeHeader)
    {
        var dataGrid = FindAncestor<DataGrid>(activeHeader);
        if (dataGrid is null)
        {
            return;
        }

        foreach (var header in FindDescendants<DataGridColumnHeader>(dataGrid))
        {
            if (!ReferenceEquals(header, activeHeader))
            {
                ColumnHeaderState.SetIsPressed(header, false);
                ColumnHeaderState.SetIsClicked(header, false);
                if (FindDescendants<ColumnHeaderControl>(header).FirstOrDefault() is { } control)
                {
                    control.IsHeaderPressed = false;
                    control.IsHeaderClicked = false;
                }
            }
        }
    }

    private static IEnumerable<T> FindDescendants<T>(DependencyObject current)
        where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(current); i++)
        {
            var child = VisualTreeHelper.GetChild(current, i);
            if (child is T match)
            {
                yield return match;
            }

            foreach (var descendant in FindDescendants<T>(child))
            {
                yield return descendant;
            }
        }
    }
}
