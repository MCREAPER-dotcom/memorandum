using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Memorandum.Desktop.Models;
using Memorandum.Desktop.Resources;
using Memorandum.Desktop.Services;
using Memorandum.Desktop.Themes;

namespace Memorandum.Desktop.Views;

public partial class PresetsView : UserControl
{
    private List<PresetItem> _presets = new();
    private PresetItem? _editingPreset;
    private string? _selectedFolderPath;
    private readonly HashSet<string> _selectedPresetTags = new(StringComparer.OrdinalIgnoreCase);
    private List<string> _availableTags = new();
    private List<FolderRowForEdit> _folderRows = new();
    private readonly Dictionary<string, string> _tagColors = new(StringComparer.OrdinalIgnoreCase);

    public PresetsView()
    {
        InitializeComponent();
        LoadPresets();
    }

    public System.Action? OnBack { get; set; }
    public System.Action<PresetItem>? OnApplyPresetRequested { get; set; }
    public IFolderTagCreationService? FolderTagCreationService { get; set; }
    public Func<IReadOnlyList<FolderRowForEdit>>? GetFoldersForPreset { get; set; }
    public Func<IReadOnlyList<string>>? GetTagNamesForPreset { get; set; }
    public Func<IReadOnlyDictionary<string, string>>? GetTagColorKeys { get; set; }
    public Action? RefreshPresetFormRequested { get; set; }

    private void LoadPresets()
    {
        var dtos = PresetStorage.Load();
        _presets = dtos.Select(PresetStorage.FromDto).ToList();
        PresetsList.ItemsSource = _presets;
        ShowPlaceholder();
    }

    private void SavePresets()
    {
        PresetStorage.Save(_presets);
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
        DetailFolder.Text = string.IsNullOrEmpty(preset.FolderName) ? "—" : preset.FolderName;
        DetailTags.Text = preset.TagDisplayText;
        DetailColorSwatch.Background = preset.ColorBrush;
    }

    private void ShowForm(PresetItem? preset)
    {
        _editingPreset = preset;
        PlaceholderPanel.IsVisible = false;
        DetailPanel.IsVisible = false;
        FormPanel.IsVisible = true;
        FormTitleLabel.Text = preset == null ? UiStrings.NewPreset : UiStrings.EditPreset;
        FormTitleBox.Text = preset?.Title ?? "";
        FormTransparencyBox.Text = preset != null ? preset.TransparencyPercent.ToString() : "95";
        FormDurationBox.Text = preset?.DurationMinutes?.ToString() ?? "";
        FormTypeSticker.IsChecked = preset?.IsSticker ?? false;
        FormTypeRegular.IsChecked = preset == null || !preset.IsSticker;
        _selectedFolderPath = preset?.FolderName;
        _selectedPresetTags.Clear();
        if (preset?.TagLabels != null)
            foreach (var t in preset.TagLabels)
                _selectedPresetTags.Add(t);
        FormColorBox.Text = preset?.ColorHex ?? PaletteConstants.DefaultPresetColorHex;
        RefreshPresetFormFolders();
        RefreshPresetFormTags();
    }

    public void RefreshPresetFormFolders()
    {
        _folderRows = GetFoldersForPreset?.Invoke()?.ToList() ?? new List<FolderRowForEdit>();
        FormFolderPanel.Children.Clear();
        var noFolderBtn = new Button
        {
            Content = UiStrings.NoFolder,
            Tag = (string?)null,
            Classes = { "NavItem" },
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            Padding = new Thickness(10, 6),
            Margin = new Thickness(0, 0, 0, 2)
        };
        if (Application.Current?.Resources?.TryGetResource("PrimaryForeground", null, out var fg0) == true && fg0 is IBrush b0)
            noFolderBtn.Foreground = b0;
        noFolderBtn.Click += (_, _) => { _selectedFolderPath = null; ApplyPresetFolderSelection(); };
        FormFolderPanel.Children.Add(noFolderBtn);
        foreach (var row in _folderRows)
        {
            var path = row.Path;
            var btn = new Button
            {
                Content = $"{row.DisplayName} ({row.Count})",
                Tag = path,
                Classes = { "NavItem" },
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                Margin = new Thickness(row.Depth * 14, 0, 0, 2),
                Padding = new Thickness(10, 6)
            };
            if (Application.Current?.Resources?.TryGetResource("PrimaryForeground", null, out var fg) == true && fg is IBrush brush)
                btn.Foreground = brush;
            btn.Click += (_, _) => { _selectedFolderPath = path; ApplyPresetFolderSelection(); };
            FormFolderPanel.Children.Add(btn);
        }
        ApplyPresetFolderSelection();
    }

    private void ApplyPresetFolderSelection()
    {
        foreach (var child in FormFolderPanel.Children)
            if (child is Button b)
            {
                if (b.Tag is string path && string.Equals(path, _selectedFolderPath, StringComparison.OrdinalIgnoreCase))
                    b.Classes.Add("Selected");
                else if (b.Tag == null && _selectedFolderPath == null)
                    b.Classes.Add("Selected");
                else
                    b.Classes.Remove("Selected");
            }
    }

    public void RefreshPresetFormTags()
    {
        _availableTags = GetTagNamesForPreset?.Invoke()?.ToList() ?? new List<string>();
        var colorKeys = GetTagColorKeys?.Invoke();
        _tagColors.Clear();
        for (var i = 0; i < _availableTags.Count; i++)
        {
            var name = _availableTags[i];
            if (colorKeys != null && colorKeys.TryGetValue(name, out var key) && !string.IsNullOrEmpty(key))
                _tagColors[name] = key;
            else if (PaletteConstants.DefaultTagNameToKey.TryGetValue(name, out var k))
                _tagColors[name] = k;
            else
                _tagColors[name] = PaletteConstants.TagPillResourceKeys[Math.Abs(name.GetHashCode()) % PaletteConstants.TagPillResourceKeys.Length];
        }
        BuildPresetTagsPanel();
    }

    private static IBrush ResolveTagBrush(string colorKey)
    {
        if (Application.Current?.Resources?.TryGetResource(colorKey, null, out var value) == true && value is IBrush brush)
            return brush;
        return new SolidColorBrush(Color.Parse(PaletteConstants.DefaultTagPillFallbackHex));
    }

    private void BuildPresetTagsPanel()
    {
        FormTagsWrap.Children.Clear();
        foreach (var name in _availableTags)
        {
            var colorKey = _tagColors.TryGetValue(name, out var c) ? c : PaletteConstants.TagPillResourceKeys[0];
            var border = new Border
            {
                Tag = name,
                Margin = new Thickness(0, 0, 6, 6),
                Padding = new Thickness(8, 4),
                CornerRadius = new CornerRadius(6),
                Background = ResolveTagBrush(colorKey),
                BorderThickness = new Thickness(0),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                Child = new TextBlock
                {
                    Text = name,
                    FontSize = 12,
                    Foreground = this.TryFindResource("PrimaryForeground", out var fr) && fr is IBrush fb ? fb : Brushes.White,
                    IsHitTestVisible = false
                }
            };
            ApplyPresetTagStyle(border, _selectedPresetTags.Contains(name));
            border.PointerPressed += OnPresetTagPointerPressed;
            FormTagsWrap.Children.Add(border);
        }
    }

    private static void ApplyPresetTagStyle(Border border, bool selected)
    {
        if (selected)
        {
            border.BorderThickness = new Thickness(2);
            border.BorderBrush = Brushes.White;
        }
        else
        {
            border.BorderThickness = new Thickness(0);
            border.BorderBrush = null;
        }
    }

    private void OnPresetTagPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        var border = sender as Border ?? (sender as Control)?.Parent as Border;
        if (border?.Tag is not string name) return;
        if (_selectedPresetTags.Contains(name))
            _selectedPresetTags.Remove(name);
        else
            _selectedPresetTags.Add(name);
        ApplyPresetTagStyle(border, _selectedPresetTags.Contains(name));
    }

    private async void OnFormAddFolderClick(object? sender, RoutedEventArgs e)
    {
        if (FolderTagCreationService == null) return;
        await FolderTagCreationService.AddRootFolderAsync().ConfigureAwait(true);
        RefreshPresetFormRequested?.Invoke();
    }

    private async void OnFormCreateTagClick(object? sender, RoutedEventArgs e)
    {
        if (FolderTagCreationService == null) return;
        var result = await FolderTagCreationService.CreateTagAsync("Новый тег").ConfigureAwait(true);
        if (result != null)
        {
            _selectedPresetTags.Add(result.Name);
            RefreshPresetFormRequested?.Invoke();
        }
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
        SavePresets();
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
        var tagLabels = _selectedPresetTags.ToList();
        var folderName = string.IsNullOrWhiteSpace(_selectedFolderPath) ? null : _selectedFolderPath.Trim();

        if (_editingPreset != null)
        {
            _editingPreset.Title = title;
            _editingPreset.TransparencyPercent = transparency;
            _editingPreset.DurationMinutes = duration;
            _editingPreset.IsSticker = isSticker;
            _editingPreset.ColorHex = colorHex;
            _editingPreset.FolderName = folderName;
            _editingPreset.TagLabels = tagLabels;
            PresetsList.ItemsSource = null;
            PresetsList.ItemsSource = _presets;
            PresetsList.SelectedItem = _editingPreset;
            ShowDetail(_editingPreset);
        }
        else
        {
            var newPreset = new PresetItem(title, transparency, duration, isSticker, colorHex, tagLabels, folderName);
            _presets.Insert(0, newPreset);
            PresetsList.ItemsSource = null;
            PresetsList.ItemsSource = _presets;
            PresetsList.SelectedItem = newPreset;
            ShowDetail(newPreset);
        }
        SavePresets();
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
