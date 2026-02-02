using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Memorandum.Desktop.Models;

namespace Memorandum.Desktop;

/// <summary>
/// Выбирает DataTemplate по типу блока (TextContentBlock, FileContentBlock, ImageContentBlock) из Application.DataTemplates.
/// </summary>
public class ContentBlockTemplateSelector : IDataTemplate
{
    public bool Match(object? data) => data is ContentBlockItem;

    public Control? Build(object? data)
    {
        if (data == null) return null;
        var app = Application.Current;
        if (app?.DataTemplates == null) return FallbackControl(data);

        var dataType = data.GetType();
        foreach (var template in app.DataTemplates)
        {
            var templateDataType = GetDataType(template);
            if (templateDataType != null && templateDataType.IsInstanceOfType(data))
            {
                var control = template.Build(data);
                if (control != null)
                    control.DataContext = data;
                return control;
            }
        }

        return FallbackControl(data);
    }

    private static Type? GetDataType(IDataTemplate template)
    {
        var prop = template.GetType().GetProperty("DataType", BindingFlags.Public | BindingFlags.Instance);
        return prop?.GetValue(template) as Type;
    }

    private static Control FallbackControl(object data)
    {
        return new TextBlock
        {
            Text = data.ToString(),
            FontSize = 14
        };
    }
}
