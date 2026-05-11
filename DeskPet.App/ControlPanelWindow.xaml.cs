using DeskPet.App.Assets;
using DeskPet.App.Storage;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Cursors = System.Windows.Input.Cursors;
using Image = System.Windows.Controls.Image;
using HorizontalAlignment = System.Windows.HorizontalAlignment;

namespace DeskPet.App;

public partial class ControlPanelWindow : Window
{
    private readonly AppSettings _settings;
    private readonly PetCatalog _catalog;
    private readonly Dictionary<string, Border> _cards = new(StringComparer.OrdinalIgnoreCase);
    private string _selectedPetId;

    public ControlPanelWindow(
        AppSettings settings,
        PetCatalog catalog,
        string selectedPetId,
        bool isPetVisible,
        bool isPaused)
    {
        InitializeComponent();
        _settings = settings;
        _catalog = catalog;
        _selectedPetId = selectedPetId;

        StatusText.Text = isPaused ? "宠物暂停中" : isPetVisible ? "宠物运行中" : "宠物隐藏中";
        BuildCatCards();
        LoadSelectedPetSettings();
    }

    public event EventHandler<ControlPanelApplyRequest>? ApplyRequested;

    public event EventHandler? ResetPositionRequested;

    public event EventHandler? PauseToggleRequested;

    private void BuildCatCards()
    {
        CatCardsPanel.Children.Clear();
        _cards.Clear();

        foreach (var pet in _catalog.Cats)
        {
            var card = CreatePetCard(pet);
            _cards[pet.Manifest.PetId] = card;
            CatCardsPanel.Children.Add(card);
        }

        CatCardsPanel.Children.Add(CreateAddPetPlaceholder());
        RefreshCardSelection();
    }

    private Border CreatePetCard(PetAsset pet)
    {
        var image = new Image
        {
            Width = 64,
            Height = 64,
            Source = LoadImage(pet.PreviewPath)
        };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
        var name = new TextBlock
        {
            Text = pet.Manifest.Name,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontWeight = FontWeights.SemiBold
        };
        var panel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };
        panel.Children.Add(image);
        panel.Children.Add(name);

        var card = new Border
        {
            Width = 192,
            Height = 90,
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 10, 10),
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            BorderBrush = Brushes.Transparent,
            Background = Brushes.White,
            Child = panel,
            Cursor = Cursors.Hand,
            Tag = pet.Manifest.PetId
        };
        card.MouseLeftButtonUp += (_, _) =>
        {
            _selectedPetId = pet.Manifest.PetId;
            LoadSelectedPetSettings();
            RefreshCardSelection();
        };
        return card;
    }

    private static Border CreateAddPetPlaceholder()
    {
        return new Border
        {
            Width = 192,
            Height = 90,
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 10, 10),
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.FromRgb(220, 214, 207)),
            Background = new SolidColorBrush(Color.FromRgb(250, 247, 242)),
            Child = new TextBlock
            {
                Text = "+ 添加猫咪\n将标准资产包放入 assets/pets",
                Foreground = new SolidColorBrush(Color.FromRgb(130, 120, 110)),
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            }
        };
    }

    private void RefreshCardSelection()
    {
        foreach (var (petId, card) in _cards)
        {
            var selected = string.Equals(petId, _selectedPetId, StringComparison.OrdinalIgnoreCase);
            card.BorderBrush = selected
                ? new SolidColorBrush(Color.FromRgb(236, 86, 136))
                : new SolidColorBrush(Color.FromRgb(220, 214, 207));
            card.Background = selected
                ? new SolidColorBrush(Color.FromRgb(255, 237, 246))
                : Brushes.White;
        }
    }

    private void LoadSelectedPetSettings()
    {
        var petSettings = _settings.PetFor(_selectedPetId);
        ScaleSlider.Value = SnapScale(petSettings.Scale);
        MoveSpeedSlider.Value = SnapMoveSpeed(petSettings.MoveSpeed);
        SelectInteraction(petSettings.InteractionLevel);
        ClickThroughBox.IsChecked = _settings.IsClickThrough;
        FeedingBox.IsChecked = _settings.IsFeedingEnabled;
        ConfirmBox.IsChecked = _settings.ConfirmBeforeFeeding;
        UpdateScaleText();
        UpdateMoveSpeedText();

        var pet = _catalog.Find(_selectedPetId);
        PreviewImage.Source = pet is not null ? LoadImage(pet.PreviewPath) : null;
    }

    private void OnApplyClick(object sender, RoutedEventArgs e)
    {
        ApplyRequested?.Invoke(this, new ControlPanelApplyRequest(
            _selectedPetId,
            ScaleSlider.Value,
            MoveSpeedSlider.Value,
            SelectedInteraction(),
            1,
            ClickThroughBox.IsChecked == true,
            FeedingBox.IsChecked == true,
            ConfirmBox.IsChecked == true));
    }

    private void OnResetClick(object sender, RoutedEventArgs e) => ResetPositionRequested?.Invoke(this, EventArgs.Empty);

    private void OnPauseClick(object sender, RoutedEventArgs e) => PauseToggleRequested?.Invoke(this, EventArgs.Empty);

    private void OnScaleChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdateScaleText();

    private void OnMoveSpeedChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdateMoveSpeedText();

    private void UpdateScaleText()
    {
        if (ScaleValueText is not null)
        {
            ScaleValueText.Text = $"{ScaleSlider.Value:0.#}x";
        }
    }

    private void UpdateMoveSpeedText()
    {
        if (MoveSpeedValueText is null)
        {
            return;
        }

        MoveSpeedValueText.Text = MoveSpeedSlider.Value switch
        {
            < 0.9 => "慢",
            > 1.2 => "快",
            _ => "中"
        };
    }

    private void SelectInteraction(string level)
    {
        foreach (ComboBoxItem item in InteractionBox.Items)
        {
            if (string.Equals(item.Tag?.ToString(), level, StringComparison.OrdinalIgnoreCase))
            {
                InteractionBox.SelectedItem = item;
                return;
            }
        }

        InteractionBox.SelectedIndex = 1;
    }

    private string SelectedInteraction()
    {
        return InteractionBox.SelectedItem is ComboBoxItem item
            ? item.Tag?.ToString() ?? "medium"
            : "medium";
    }

    private static double SnapScale(double value)
    {
        return new[] { 1.0, 1.5, 2.0, 2.5, 3.0 }
            .OrderBy(candidate => Math.Abs(candidate - value))
            .First();
    }

    private static double SnapMoveSpeed(double value)
    {
        return new[] { 0.75, 1.05, 1.35 }
            .OrderBy(candidate => Math.Abs(candidate - value))
            .First();
    }

    private static BitmapImage? LoadImage(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(Path.GetFullPath(path), UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
}

public sealed record ControlPanelApplyRequest(
    string ActivePetId,
    double Scale,
    double MoveSpeed,
    string InteractionLevel,
    double Opacity,
    bool IsClickThrough,
    bool IsFeedingEnabled,
    bool ConfirmBeforeFeeding);
