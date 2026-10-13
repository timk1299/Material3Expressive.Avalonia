using Avalonia.Controls;
using Material3Expressive.Avalonia.Controls;

namespace Material3Expressive.Avalonia.Demo.Views;

public partial class ActionsPage : UserControl
{
    private static readonly string[] Ranges = ["Day", "Week", "Month", "Year"];

    public ActionsPage()
    {
        InitializeComponent();

        ConnectedSingle.PropertyChanged += (_, e) =>
        {
            if (e.Property == ButtonGroup.SelectedIndexProperty)
                UpdateSelectionText();
        };
        UpdateSelectionText();

        SaveSplit.Click += (_, _) => Snackbar.Show(this, "Saved");
        foreach (var item in Menu.Items)
        {
            if (item is FabMenuItem menuItem)
                menuItem.Click += (_, _) => Snackbar.Show(this, $"{menuItem.Content}");
        }
    }

    private void UpdateSelectionText()
    {
        var index = ConnectedSingle.SelectedIndex;
        SelectionText.Text = index >= 0 ? $"Showing: {Ranges[index]}" : "Nothing selected";
    }
}
