using DeskPet.App.Storage;
using System.Windows;

namespace DeskPet.App;

public partial class SettingsWindow : Window
{
    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        Settings = new AppSettings
        {
            Left = settings.Left,
            Top = settings.Top,
            Scale = settings.Scale,
            AnimationSpeed = settings.AnimationSpeed,
            IsClickThrough = settings.IsClickThrough,
            IsFeedingEnabled = settings.IsFeedingEnabled,
            ConfirmBeforeFeeding = settings.ConfirmBeforeFeeding
        };

        SelectComboValue(ScaleBox, Settings.Scale);
        SelectComboValue(SpeedBox, Settings.AnimationSpeed);
        ClickThroughBox.IsChecked = Settings.IsClickThrough;
        FeedingBox.IsChecked = Settings.IsFeedingEnabled;
        ConfirmBox.IsChecked = Settings.ConfirmBeforeFeeding;
    }

    public AppSettings Settings { get; }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        Settings.Scale = SelectedComboValue(ScaleBox, 2);
        Settings.AnimationSpeed = SelectedComboValue(SpeedBox, 1);
        Settings.IsClickThrough = ClickThroughBox.IsChecked == true;
        Settings.IsFeedingEnabled = FeedingBox.IsChecked == true;
        Settings.ConfirmBeforeFeeding = ConfirmBox.IsChecked == true;
        DialogResult = true;
    }

    private static void SelectComboValue(System.Windows.Controls.ComboBox comboBox, double value)
    {
        foreach (System.Windows.Controls.ComboBoxItem item in comboBox.Items)
        {
            if (double.TryParse(item.Tag?.ToString(), out var itemValue) && Math.Abs(itemValue - value) < 0.001)
            {
                comboBox.SelectedItem = item;
                return;
            }
        }

        comboBox.SelectedIndex = 0;
    }

    private static double SelectedComboValue(System.Windows.Controls.ComboBox comboBox, double fallback)
    {
        return comboBox.SelectedItem is System.Windows.Controls.ComboBoxItem item && double.TryParse(item.Tag?.ToString(), out var value)
            ? value
            : fallback;
    }
}
