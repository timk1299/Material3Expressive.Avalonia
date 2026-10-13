using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Material3Expressive.Avalonia;

/// <summary>
/// Attached properties for Material 3 text fields (<see cref="TextBox"/> and <see cref="ComboBox"/>).
/// </summary>
public static class TextFieldAssist
{
    /// <summary>Class added to a field while it has a non-empty <see cref="LabelProperty"/>.</summary>
    public const string HasLabelClass = "HasLabel";

    static TextFieldAssist()
    {
        LabelProperty.Changed.AddClassHandler<TemplatedControl>((control, e) =>
        {
            var hasLabel = !string.IsNullOrEmpty(e.GetNewValue<string?>());
            if (hasLabel && !control.Classes.Contains(HasLabelClass))
                control.Classes.Add(HasLabelClass);
            else if (!hasLabel)
                control.Classes.Remove(HasLabelClass);
        });
    }

    /// <summary>Floating label. Rests inside the field and moves up on focus or when there is content.</summary>
    public static readonly AttachedProperty<string?> LabelProperty =
        AvaloniaProperty.RegisterAttached<TemplatedControl, string?>("Label", typeof(TextFieldAssist));

    /// <summary>Helper text shown under the field. Replaced by validation errors when there are any.</summary>
    public static readonly AttachedProperty<string?> SupportingTextProperty =
        AvaloniaProperty.RegisterAttached<TemplatedControl, string?>("SupportingText", typeof(TextFieldAssist));

    public static string? GetLabel(TemplatedControl element) => element.GetValue(LabelProperty);

    public static void SetLabel(TemplatedControl element, string? value) => element.SetValue(LabelProperty, value);

    public static string? GetSupportingText(TemplatedControl element) => element.GetValue(SupportingTextProperty);

    public static void SetSupportingText(TemplatedControl element, string? value) =>
        element.SetValue(SupportingTextProperty, value);
}
