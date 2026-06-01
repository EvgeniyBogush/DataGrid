using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace EnecaDataGrid.Controls;

public static class ResizeThumbIconBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(ResizeThumbIconBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static readonly DependencyProperty IconSourceProperty =
        DependencyProperty.RegisterAttached(
            "IconSource",
            typeof(ImageSource),
            typeof(ResizeThumbIconBehavior),
            new PropertyMetadata(null, OnIconSourceChanged));

    private static readonly DependencyProperty StateProperty =
        DependencyProperty.RegisterAttached(
            "State",
            typeof(ResizeThumbIconState),
            typeof(ResizeThumbIconBehavior),
            new PropertyMetadata(null));

    public static bool GetIsEnabled(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsEnabledProperty);
    }

    public static void SetIsEnabled(DependencyObject obj, bool value)
    {
        obj.SetValue(IsEnabledProperty, value);
    }

    public static ImageSource? GetIconSource(DependencyObject obj)
    {
        return (ImageSource?)obj.GetValue(IconSourceProperty);
    }

    public static void SetIconSource(DependencyObject obj, ImageSource? value)
    {
        obj.SetValue(IconSourceProperty, value);
    }

    private static ResizeThumbIconState? GetState(DependencyObject obj)
    {
        return (ResizeThumbIconState?)obj.GetValue(StateProperty);
    }

    private static void SetState(DependencyObject obj, ResizeThumbIconState? value)
    {
        obj.SetValue(StateProperty, value);
    }

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Thumb thumb)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            Attach(thumb);
        }
        else
        {
            Detach(thumb);
        }
    }

    private static void OnIconSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Thumb thumb && GetState(thumb) is { } state)
        {
            state.Icon.Source = (ImageSource?)e.NewValue;
        }
    }

    private static void Attach(Thumb thumb)
    {
        if (GetState(thumb) is not null)
        {
            return;
        }

        var icon = new Image
        {
            Width = 16,
            Height = 16,
            Source = GetIconSource(thumb),
            IsHitTestVisible = false
        };

        var popup = new Popup
        {
            AllowsTransparency = true,
            Focusable = false,
            IsHitTestVisible = false,
            Placement = PlacementMode.Relative,
            PlacementTarget = thumb,
            Child = icon
        };

        var state = new ResizeThumbIconState(popup, icon);
        SetState(thumb, state);

        thumb.Cursor = Cursors.None;
        thumb.MouseEnter += ThumbMouseEnter;
        thumb.MouseMove += ThumbMouseMove;
        thumb.MouseLeave += ThumbMouseLeave;
        thumb.DragStarted += ThumbDragStarted;
        thumb.DragDelta += ThumbDragDelta;
        thumb.DragCompleted += ThumbDragCompleted;
        thumb.Unloaded += ThumbUnloaded;
    }

    private static void Detach(Thumb thumb)
    {
        if (GetState(thumb) is not { } state)
        {
            return;
        }

        state.Popup.IsOpen = false;
        SetState(thumb, null);

        thumb.MouseEnter -= ThumbMouseEnter;
        thumb.MouseMove -= ThumbMouseMove;
        thumb.MouseLeave -= ThumbMouseLeave;
        thumb.DragStarted -= ThumbDragStarted;
        thumb.DragDelta -= ThumbDragDelta;
        thumb.DragCompleted -= ThumbDragCompleted;
        thumb.Unloaded -= ThumbUnloaded;
    }

    private static void ThumbMouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is Thumb thumb)
        {
            ShowAtMouse(thumb);
        }
    }

    private static void ThumbMouseMove(object sender, MouseEventArgs e)
    {
        if (sender is Thumb thumb)
        {
            ShowAtMouse(thumb);
        }
    }

    private static void ThumbMouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is Thumb thumb && GetState(thumb) is { IsDragging: false } state)
        {
            state.Popup.IsOpen = false;
        }
    }

    private static void ThumbDragStarted(object sender, DragStartedEventArgs e)
    {
        if (sender is Thumb thumb && GetState(thumb) is { } state)
        {
            state.IsDragging = true;
            ShowAtMouse(thumb);
        }
    }

    private static void ThumbDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (sender is Thumb thumb)
        {
            ShowAtMouse(thumb);
        }
    }

    private static void ThumbDragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (sender is Thumb thumb && GetState(thumb) is { } state)
        {
            state.IsDragging = false;
            state.Popup.IsOpen = thumb.IsMouseOver;
            if (thumb.IsMouseOver)
            {
                ShowAtMouse(thumb);
            }
        }
    }

    private static void ThumbUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is Thumb thumb)
        {
            Detach(thumb);
        }
    }

    private static void ShowAtMouse(Thumb thumb)
    {
        if (GetState(thumb) is not { } state)
        {
            return;
        }

        var position = Mouse.GetPosition(thumb);
        state.Popup.HorizontalOffset = position.X - state.Icon.Width / 2;
        state.Popup.VerticalOffset = position.Y - state.Icon.Height / 2;
        state.Popup.IsOpen = true;
    }

    private sealed class ResizeThumbIconState
    {
        public ResizeThumbIconState(Popup popup, Image icon)
        {
            Popup = popup;
            Icon = icon;
        }

        public Popup Popup { get; }

        public Image Icon { get; }

        public bool IsDragging { get; set; }
    }
}
