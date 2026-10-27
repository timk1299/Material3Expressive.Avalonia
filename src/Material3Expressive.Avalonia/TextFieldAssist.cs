using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Material3Expressive.Avalonia;

/// <summary>Look of the text fields that set no class of their own (<see cref="Material3ExpressiveTheme.TextFieldVariant"/>).</summary>
public enum TextFieldVariant
{
    Filled,
    Outlined,
}

/// <summary>
/// Attached properties for Material 3 text fields (<see cref="TextBox"/> and <see cref="ComboBox"/>).
/// </summary>
public static class TextFieldAssist
{
    /// <summary>Class added to a field while it has a non-empty <see cref="LabelProperty"/>.</summary>
    public const string HasLabelClass = "HasLabel";

    /// <summary>Class that keeps a field filled when <see cref="Material3ExpressiveTheme.TextFieldVariant"/> is Outlined.</summary>
    public const string FilledClass = "Filled";

    private static IDisposable? _templateApplied;
    private static IDisposable? _clearClicked;

    internal static TextFieldVariant DefaultVariant { get; set; }

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

    /// <summary>
    /// Gives text boxes and combo boxes the class <c>Outlined</c> while <see cref="DefaultVariant"/> is Outlined. Skipped:
    /// fields in a control template, fields with their own <c>Theme</c>, search bars and fields with the class
    /// <see cref="FilledClass"/>. Installed by <see cref="Material3ExpressiveTheme"/>.
    /// </summary>
    internal static void Install()
    {
        _templateApplied ??= TemplatedControl.TemplateAppliedEvent.AddClassHandler<TemplatedControl>((control, _) => ApplyDefaultVariant(control));
        _clearClicked ??= Button.ClickEvent.AddClassHandler<Button>((button, _) =>
        {
            // The clear button of a search bar (Themes/TextBox.axaml).
            if (button.Name == "PART_ClearButton" && button.TemplatedParent is TextBox textBox)
                textBox.Clear();
        });
    }

    private static void ApplyDefaultVariant(TemplatedControl control)
    {
        if (DefaultVariant != TextFieldVariant.Outlined || control is not (TextBox or ComboBox) ||
            control.TemplatedParent is not null || control.Theme is not null)
        {
            return;
        }

        if (!control.Classes.Contains("Outlined") && !control.Classes.Contains("Search") && !control.Classes.Contains(FilledClass))
            control.Classes.Add("Outlined");
    }

    public static string? GetLabel(TemplatedControl element) => element.GetValue(LabelProperty);

    public static void SetLabel(TemplatedControl element, string? value) => element.SetValue(LabelProperty, value);

    public static string? GetSupportingText(TemplatedControl element) => element.GetValue(SupportingTextProperty);

    public static void SetSupportingText(TemplatedControl element, string? value) =>
        element.SetValue(SupportingTextProperty, value);
}
