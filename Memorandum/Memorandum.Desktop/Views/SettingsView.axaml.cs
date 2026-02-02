using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Memorandum.Desktop.Models;
using Memorandum.Desktop.Services;

namespace Memorandum.Desktop.Views;

public partial class SettingsView : UserControl
{
    private List<HotkeyConfigItem> _hotkeyItems = new();
    private int _recordingIndex = -1;

    public SettingsView()
    {
        InitializeComponent();
        ManagementTab.Classes.Add("Selected");
        LoadHotkeys();
        BuildHotkeyPanel();
    }

    public System.Action? OnBack { get; set; }
    public System.Action? OnHotkeysSaved { get; set; }

    private void LoadHotkeys()
    {
        _hotkeyItems = HotkeyConfigStorage.Load().ToList();
    }

    private void BuildHotkeyPanel()
    {
        HotkeyRowsPanel.Children.Clear();
        for (var i = 0; i < _hotkeyItems.Count; i++)
        {
            var index = i;
            var item = _hotkeyItems[i];
            var displayText = new TextBlock
            {
                Text = item.DisplayName,
                Foreground = this.TryFindResource("PrimaryForeground", out var fr) && fr is IBrush f ? f : Brushes.White,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            var comboText = new TextBlock
            {
                Text = string.IsNullOrEmpty(item.KeyCombo) ? "—" : item.KeyCombo,
                Foreground = this.TryFindResource("PrimaryForeground", out var fr2) && fr2 is IBrush f2 ? f2 : Brushes.White,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Tag = index
            };
            var changeBtn = new Button
            {
                Content = "Изменить",
                Classes = { "SecondaryButton" },
                Tag = index,
                Padding = new Thickness(12, 6)
            };
            changeBtn.Click += (_, _) => StartRecording(index, comboText);

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
                Margin = new Thickness(0, 4)
            };
            grid.Children.Add(displayText);
            Grid.SetColumn(displayText, 0);
            grid.Children.Add(comboText);
            Grid.SetColumn(comboText, 1);
            grid.Children.Add(changeBtn);
            Grid.SetColumn(changeBtn, 2);
            HotkeyRowsPanel.Children.Add(grid);
        }
    }

    private void StartRecording(int index, TextBlock comboTextBlock)
    {
        _recordingIndex = index;
        comboTextBlock.Text = "Нажмите сочетание...";
        HotkeyConflictWarning.IsVisible = false;
        AddHandler(KeyDownEvent, OnWindowKeyDownForCapture, RoutingStrategies.Bubble);
    }

    private void OnWindowKeyDownForCapture(object? sender, KeyEventArgs e)
    {
        if (_recordingIndex < 0) return;
        RemoveHandler(KeyDownEvent, OnWindowKeyDownForCapture);
        ApplyCapturedCombo(e.Key, e.KeyModifiers);
        e.Handled = true;
    }

    private void ApplyCapturedCombo(Key key, KeyModifiers modifiers)
    {
        if (_recordingIndex < 0) return;
        if (IsModifierKeyOnly(key))
            return;
        var combo = HotkeyComboHelper.FromAvalonia(key, modifiers);
        if (string.IsNullOrEmpty(combo))
            return;
        for (var j = 0; j < _hotkeyItems.Count; j++)
        {
            if (j != _recordingIndex && string.Equals(_hotkeyItems[j].KeyCombo, combo, StringComparison.OrdinalIgnoreCase))
            {
                HotkeyConflictWarning.IsVisible = true;
                return;
            }
        }
        _hotkeyItems[_recordingIndex].KeyCombo = combo;
        var row = HotkeyRowsPanel.Children[_recordingIndex] as Grid;
        if (row?.Children.Count > 1 && row.Children[1] is TextBlock tb)
            tb.Text = combo;
        HotkeyConflictWarning.IsVisible = false;
        _recordingIndex = -1;
    }

    private static bool IsModifierKeyOnly(Key key)
    {
        return key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin;
    }

    private void OnBackClick(object? sender, RoutedEventArgs e) => OnBack?.Invoke();

    private void OnCancelClick(object? sender, RoutedEventArgs e) => OnBack?.Invoke();

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        HotkeyConfigStorage.Save(_hotkeyItems);
        OnHotkeysSaved?.Invoke();
        OnBack?.Invoke();
    }

    private void OnManagementTabClick(object? sender, RoutedEventArgs e)
    {
        ManagementTab.Classes.Add("Selected");
        AdditionalTab.Classes.Remove("Selected");
        ManagementPanel.IsVisible = true;
        AdditionalPanel.IsVisible = false;
    }

    private void OnAdditionalTabClick(object? sender, RoutedEventArgs e)
    {
        AdditionalTab.Classes.Add("Selected");
        ManagementTab.Classes.Remove("Selected");
        ManagementPanel.IsVisible = false;
        AdditionalPanel.IsVisible = true;
    }
}
