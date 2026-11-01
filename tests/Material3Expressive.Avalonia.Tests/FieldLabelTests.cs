using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Material3Expressive.Avalonia.Controls;
using Xunit;

namespace Material3Expressive.Avalonia.Tests;

/// <summary>
/// The label of a text field or combo box is laid out at its floating size and scaled up while resting
/// in an empty field. A label too long for the field ends in "…" inside the field at both sizes.
/// </summary>
public class FieldLabelTests
{
    private const string Label = "Automatic logout (min)";
    private const double RestingScale = 16.0 / 12.0;

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_long_label_resting_in_an_empty_text_field_is_trimmed_inside_the_field(bool outlined)
    {
        var field = new TextBox { Width = 180 };
        if (outlined)
        {
            field.Classes.Add("Outlined");
        }

        TextFieldAssist.SetLabel(field, Label);
        var window = Show(field);
        var label = LabelOf(field);

        Assert.Equal(RestingScale, label.RestingScale, 3);
        AssertInside(label, RestingScale);
        Assert.Contains(label.TextLayout.TextLines, line => line.HasCollapsed);

        // Floating at its own size, the same label fits and is shown in full.
        field.Text = "10";
        window.UpdateLayout();
        Assert.Equal(1, label.RestingScale);
        AssertInside(label, 1);
        Assert.DoesNotContain(label.TextLayout.TextLines, line => line.HasCollapsed);
        window.Close();
    }

    [AvaloniaFact]
    public void A_long_label_resting_in_a_combo_box_without_selection_is_trimmed_inside_the_field()
    {
        var combo = new ComboBox { Width = 200, Classes = { "Outlined" }, ItemsSource = new[] { "One", "Two" } };
        TextFieldAssist.SetLabel(combo, Label);
        var window = Show(combo);
        var label = LabelOf(combo);

        Assert.Equal(RestingScale, label.RestingScale, 3);
        AssertInside(label, RestingScale);

        combo.SelectedIndex = 0;
        window.UpdateLayout();
        Assert.Equal(1, label.RestingScale);
        Assert.DoesNotContain(label.TextLayout.TextLines, line => line.HasCollapsed);
        window.Close();
    }

    [AvaloniaFact]
    public void A_text_field_sized_to_its_content_keeps_the_width_of_its_label()
    {
        var field = new TextBox { HorizontalAlignment = HorizontalAlignment.Left };
        TextFieldAssist.SetLabel(field, Label);
        var window = Show(field);
        var label = LabelOf(field);

        Assert.DoesNotContain(label.TextLayout.TextLines, line => line.HasCollapsed);
        Assert.True(field.Bounds.Width >= label.Bounds.Width + label.Margin.Left + label.Margin.Right);
        window.Close();
    }

    private static Window Show(Control content)
    {
        var window = new Window { Width = 600, Height = 300, Content = new StackPanel { Margin = new Thickness(24), Children = { content } } };
        window.Show();
        window.UpdateLayout();
        return window;
    }

    private static FieldLabel LabelOf(TemplatedControl field) =>
        field.GetVisualDescendants().OfType<FieldLabel>().Single(label => label.Name == "PART_Label");

    /// <summary>The label, drawn at <paramref name="scale"/>, stays in the space the field gives it.</summary>
    private static void AssertInside(FieldLabel label, double scale)
    {
        var slot = LayoutInformation.GetPreviousArrangeBounds(label)!.Value;
        var room = slot.Width - label.Margin.Left - label.Margin.Right;
        Assert.True(label.Bounds.Width * scale <= room + 0.5, $"Label is {label.Bounds.Width * scale:0.#} wide drawn, room {room:0.#}");
    }
}
