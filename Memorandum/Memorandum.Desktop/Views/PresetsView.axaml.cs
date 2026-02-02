using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Memorandum.Desktop.Models;
using Memorandum.Desktop.Themes;

namespace Memorandum.Desktop.Views;

public partial class PresetsView : UserControl
{
    private List<PresetItem> _presets = new();
    private PresetItem? _editingPreset;

    public PresetsView()
    {
        InitializeComponent();
        LoadPresets();
    }

    public System.Action? OnBack { get; set; }
    public System.Action<PresetItem>? OnApplyPresetRequested { get; set; }

    private void LoadPresets()
    {
        _presets = new List<PresetItem>
        {
            new("Срочная задача", "Прозрачность: 95%, 15 мин", "Стикер", "#dc2626"),
            new("Напоминание", "Прозрачность: 90%", "Стикер", "#fef08a"),
            new("Таймер помодоро", "Прозрачность: 95%, 25 мин", "Стикер", "#2dd4bf"),
            new("Обычная заметка", "Прозрачность: 100%", "Обычная", "#ffffff")
        };
        PresetsList.ItemsSource = _presets;
        ShowPlaceholder();
    }

    private void ShowPlaceholder()
    {
        PlaceholderPanel.IsVisible = true;
        DetailPanel.IsVisible = false;
        FormPanel.IsVisible = false;
    }

    private void ShowDetail(PresetItem preset)
    {
        PlaceholderPanel.IsVisible = false;
        DetailPanel.IsVisible = true;
        FormPanel.IsVisible = false;
        DetailTitle.Text = preset.Title;
        DetailTransparency.Text = preset.TransparencyPercent + "%";
        DetailDuration.Text = preset.DurationMinutes.HasValue ? preset.DurationMinutes + " мин" : "—";
        DetailType.Text = preset.TypeLabel;
        DetailTags.Text = preset.TagDisplayText;
        DetailColorSwatch.Background = preset.ColorBrush;
    }

    private void ShowForm(PresetItem? preset)
    {
        _editingPreset = preset;
        PlaceholderPanel.IsVisible = false;
        DetailPanel.IsVisible = false;
        FormPanel.IsVisible = true;
        FormTitleLabel.Text = preset == null ? "Новый пресет" : "Редактирование пресета";
        FormTitleBox.Text = preset?.Title ?? "";
        FormTransparencyBox.Text = preset != null ? preset.TransparencyPercent.ToString() : "95";
        FormDurationBox.Text = preset?.DurationMinutes?.ToString() ?? "";
        FormTypeSticker.IsChecked = preset?.IsSticker ?? false;
        FormTypeRegular.IsChecked = preset == null ? true : !preset.IsSticker;
        FormTagsBox.Text = preset != null && preset.TagLabels.Count > 0 ? string.Join(", ", preset.TagLabels) : "";
        FormColorBox.Text = preset?.ColorHex ?? PaletteConstants.DefaultPresetColorHex;
    }

    private void OnBackClick(object? sender, RoutedEventArgs e) => OnBack?.Invoke();

    private void OnCreatePresetClick(object? sender, RoutedEventArgs e)
    {
        ShowForm(null);
    }

    private void OnPresetSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (PresetsList.SelectedItem is PresetItem p)
            ShowDetail(p);
        else
            ShowPlaceholder();
    }

    private void OnApplyPresetClick(object? sender, RoutedEventArgs e)
    {
        if (PresetsList.SelectedItem is PresetItem p)
            OnApplyPresetRequested?.Invoke(p);
    }

    private void OnEditPresetClick(object? sender, RoutedEventArgs e)
    {
        if (PresetsList.SelectedItem is PresetItem p)
            ShowForm(p);
    }

    private void OnDeletePresetClick(object? sender, RoutedEventArgs e)
    {
        if (PresetsList.SelectedItem is not PresetItem p) return;
        _presets.Remove(p);
        PresetsList.ItemsSource = null;
        PresetsList.ItemsSource = _presets;
        PresetsList.SelectedItem = null;
        ShowPlaceholder();
    }

    private void OnFormSaveClick(object? sender, RoutedEventArgs e)
    {
        var title = (FormTitleBox.Text ?? "").Trim();
        if (string.IsNullOrEmpty(title))
        {
            FormTitleBox.Focus();
            return;
        }
        if (!int.TryParse((FormTransparencyBox.Text ?? "95").Trim(), out var transparency) || transparency < 1 || transparency > 100)
            transparency = 95;
        int? duration = null;
        if (!string.IsNullOrWhiteSpace(FormDurationBox.Text) && int.TryParse(FormDurationBox.Text.Trim(), out var d) && d > 0)
            duration = d;
        var isSticker = FormTypeSticker.IsChecked == true;
        var colorHex = (FormColorBox.Text ?? PaletteConstants.DefaultPresetColorHex).Trim();
        if (!colorHex.StartsWith("#") || colorHex.Length < 4)
            colorHex = PaletteConstants.DefaultPresetColorHex;
        var tagLabels = (FormTagsBox.Text ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        if (_editingPreset != null)
        {
            _editingPreset.Title = title;
            _editingPreset.TransparencyPercent = transparency;
            _editingPreset.DurationMinutes = duration;
            _editingPreset.IsSticker = isSticker;
            _editingPreset.ColorHex = colorHex;
            _editingPreset.TagLabels = tagLabels;
            PresetsList.ItemsSource = null;
            PresetsList.ItemsSource = _presets;
            PresetsList.SelectedItem = _editingPreset;
            ShowDetail(_editingPreset);
        }
        else
        {
            var newPreset = new PresetItem(title, transparency, duration, isSticker, colorHex, tagLabels);
            _presets.Insert(0, newPreset);
            PresetsList.ItemsSource = null;
            PresetsList.ItemsSource = _presets;
            PresetsList.SelectedItem = newPreset;
            ShowDetail(newPreset);
        }
        _editingPreset = null;
    }

    private void OnFormCancelClick(object? sender, RoutedEventArgs e)
    {
        if (_editingPreset != null)
        {
            PresetsList.SelectedItem = _editingPreset;
            ShowDetail(_editingPreset);
        }
        else
            ShowPlaceholder();
        _editingPreset = null;
    }
}
