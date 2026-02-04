using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Memorandum.Desktop.Views;

public partial class DeadlinePickerControl : UserControl
{
    public DeadlinePickerControl()
    {
        InitializeComponent();
    }

    public void SetDeadline(DateTime? deadline)
    {
        if (deadline.HasValue)
        {
            PartCalendar.SelectedDate = deadline.Value.Date;
            PartTimePicker.SelectedTime = deadline.Value.TimeOfDay;
        }
        else
        {
            PartCalendar.SelectedDate = null;
            PartTimePicker.SelectedTime = null;
        }
    }

    public DateTime? GetDeadline()
    {
        if (!PartCalendar.SelectedDate.HasValue)
            return null;
        var date = PartCalendar.SelectedDate.Value;
        var time = PartTimePicker.SelectedTime ?? TimeSpan.Zero;
        return date.Add(time);
    }

    private void OnClearClick(object? sender, RoutedEventArgs e)
    {
        SetDeadline(null);
    }
}
