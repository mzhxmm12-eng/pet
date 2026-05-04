using DeskPet.App.Assets;
using DeskPet.App.Domain;
using DeskPet.App.Platform;
using DeskPet.App.Storage;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Application = System.Windows.Application;
using ContextMenu = System.Windows.Controls.ContextMenu;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;

namespace DeskPet.App;

public partial class MainWindow : Window
{
    private readonly SettingsStore _settingsStore = new();
    private readonly DispatcherTimer _animationTimer = new();
    private readonly DispatcherTimer _behaviorTimer = new();
    private readonly Random _random = new();
    private readonly PetAsset _petAsset;
    private readonly FileFeedService _fileFeedService;
    private readonly NotifyIcon _trayIcon;
    private AppSettings _settings;
    private SpriteAnimation? _currentAnimation;
    private PetState _state = PetState.Idle;
    private int _frameIndex;
    private bool _isDragging;
    private bool _dragMoved;
    private Point _dragStartScreen;
    private double _dragStartLeft;
    private double _dragStartTop;
    private bool _isPaused;
    private int _walkDirection;
    private double _walkRemaining;
    private DateTime _nextAutoDecision = DateTime.UtcNow;
    private DateTime _forcedUntil = DateTime.MinValue;
    private Point _lastCursor;
    private DateTime _lastCursorSample = DateTime.UtcNow;
    private DateTime _lastPlayTrigger = DateTime.MinValue;

    public MainWindow()
    {
        InitializeComponent();

        _settings = _settingsStore.Load();
        _petAsset = new PetAssetLoader().Load(ResolvePetAssetDirectory());
        _fileFeedService = new FileFeedService(AppContext.BaseDirectory, _petAsset.RootDirectory);
        _trayIcon = CreateTrayIcon();

        ConfigureWindow();
        ConfigureTimers();
        ConfigureMenus();
        ConfigureDragDrop();

        Loaded += (_, _) =>
        {
            ClickThroughWindow.SetClickThrough(this, _settings.IsClickThrough);
            PlayState(PetState.Idle);
        };
        Closing += (_, _) => SavePosition();
    }

    public void DisposeTrayIcon()
    {
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
    }

    private static string ResolvePetAssetDirectory()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "assets", "pets", "orange-cat"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "assets", "pets", "orange-cat")),
            Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "assets", "pets", "orange-cat"))
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(Path.Combine(candidate, "manifest.json")))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException("Could not locate assets/pets/orange-cat.");
    }

    private void ConfigureWindow()
    {
        Width = _petAsset.Manifest.Canvas.Width * _settings.Scale;
        Height = _petAsset.Manifest.Canvas.Height * _settings.Scale;

        var workArea = SystemParameters.WorkArea;
        Left = _settings.Left is > 0 ? Math.Min(_settings.Left.Value, workArea.Right - Width) : workArea.Right - Width - 80;
        Top = _settings.Top is > 0 ? Math.Min(_settings.Top.Value, workArea.Bottom - Height) : workArea.Bottom - Height - 80;
    }

    private void ConfigureTimers()
    {
        _animationTimer.Tick += (_, _) => AdvanceAnimation();
        _behaviorTimer.Interval = TimeSpan.FromMilliseconds(100);
        _behaviorTimer.Tick += (_, _) => TickBehavior();
        _behaviorTimer.Start();
    }

    private void ConfigureMenus()
    {
        var menu = new ContextMenu();
        menu.Items.Add(CreateWpfMenuItem("隐藏", (_, _) => HidePet()));
        menu.Items.Add(CreateWpfMenuItem("暂停/继续", (_, _) => TogglePaused()));
        menu.Items.Add(CreateWpfMenuItem("点击穿透", (_, _) => ToggleClickThrough()));
        menu.Items.Add(CreateWpfMenuItem("文件投喂", (_, _) => ToggleFeeding()));
        menu.Items.Add(CreateWpfMenuItem("设置", (_, _) => OpenSettings()));
        menu.Items.Add(CreateWpfMenuItem("重置位置", (_, _) => ResetPosition()));
        menu.Items.Add(CreateWpfMenuItem("退出", (_, _) => ExitApp()));
        ContextMenu = menu;

        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
        MouseMove += OnMouseMove;
    }

    private void ConfigureDragDrop()
    {
        DragEnter += OnDragEnter;
        DragOver += OnDragOver;
        DragLeave += (_, _) => EndForcedAnimation();
        Drop += OnDrop;
    }

    private NotifyIcon CreateTrayIcon()
    {
        var icon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = "DeskPet",
            Visible = true,
            ContextMenuStrip = new ContextMenuStrip()
        };
        icon.ContextMenuStrip.Items.Add("显示/隐藏", null, (_, _) => Dispatcher.Invoke(ToggleVisibility));
        icon.ContextMenuStrip.Items.Add("暂停/继续", null, (_, _) => Dispatcher.Invoke(TogglePaused));
        icon.ContextMenuStrip.Items.Add("点击穿透", null, (_, _) => Dispatcher.Invoke(ToggleClickThrough));
        icon.ContextMenuStrip.Items.Add("文件投喂", null, (_, _) => Dispatcher.Invoke(ToggleFeeding));
        icon.ContextMenuStrip.Items.Add("设置", null, (_, _) => Dispatcher.Invoke(OpenSettings));
        icon.ContextMenuStrip.Items.Add("重置位置", null, (_, _) => Dispatcher.Invoke(ResetPosition));
        icon.ContextMenuStrip.Items.Add("退出", null, (_, _) => Dispatcher.Invoke(ExitApp));
        icon.DoubleClick += (_, _) => Dispatcher.Invoke(ToggleVisibility);
        return icon;
    }

    private static MenuItem CreateWpfMenuItem(string header, RoutedEventHandler click)
    {
        var item = new MenuItem { Header = header };
        item.Click += click;
        return item;
    }

    private void PlayState(PetState state, TimeSpan? forceFor = null)
    {
        var animationName = ToAnimationName(state);
        if (!_petAsset.Animations.TryGetValue(animationName, out var animation))
        {
            animation = _petAsset.Animations["idle"];
        }

        _state = state;
        _currentAnimation = animation;
        _frameIndex = 0;
        ApplyFrame();
        var speed = Math.Max(0.25, _settings.AnimationSpeed);
        _animationTimer.Interval = TimeSpan.FromMilliseconds(1000.0 / Math.Max(1, animation.Definition.Fps) / speed);
        _animationTimer.Start();

        if (forceFor is { } duration)
        {
            _forcedUntil = DateTime.UtcNow + duration;
        }
    }

    private static string ToAnimationName(PetState state) => state switch
    {
        PetState.WalkLeft => "walk_left",
        PetState.WalkRight => "walk_right",
        PetState.Sleep => "sleep",
        PetState.Alert => "alert",
        PetState.Play => "play",
        PetState.Eat => "eat",
        PetState.Reject => "reject",
        PetState.Dragged => "dragged",
        _ => "idle"
    };

    private void AdvanceAnimation()
    {
        if (_currentAnimation is null)
        {
            return;
        }

        _frameIndex++;
        if (_frameIndex >= _currentAnimation.Definition.Frames)
        {
            if (_currentAnimation.Definition.Loop)
            {
                _frameIndex = 0;
            }
            else
            {
                _frameIndex = _currentAnimation.Definition.Frames - 1;
                if (DateTime.UtcNow >= _forcedUntil)
                {
                    PlayState(PetState.Idle);
                }
            }
        }

        ApplyFrame();
    }

    private void ApplyFrame()
    {
        if (_currentAnimation is null)
        {
            return;
        }

        var definition = _currentAnimation.Definition;
        PetImage.Source = new CroppedBitmap(
            _currentAnimation.Sheet,
            new System.Windows.Int32Rect(_frameIndex * definition.FrameWidth, 0, definition.FrameWidth, definition.FrameHeight));
    }

    private void TickBehavior()
    {
        if (_isPaused || _isDragging)
        {
            return;
        }

        if (_state is PetState.WalkLeft or PetState.WalkRight)
        {
            MoveDuringWalk();
        }

        if (DateTime.UtcNow < _forcedUntil)
        {
            return;
        }

        HandleCursorInteraction();

        if (DateTime.UtcNow >= _nextAutoDecision)
        {
            ChooseAutomaticState();
        }
    }

    private void MoveDuringWalk()
    {
        if (_walkRemaining <= 0)
        {
            PlayState(PetState.Idle);
            _nextAutoDecision = DateTime.UtcNow + TimeSpan.FromSeconds(_random.Next(4, 9));
            return;
        }

        var step = 2.0 * _settings.Scale;
        var workArea = SystemParameters.WorkArea;
        var nextLeft = Left + step * _walkDirection;
        if (nextLeft < workArea.Left || nextLeft + Width > workArea.Right)
        {
            _walkRemaining = 0;
            return;
        }

        Left = nextLeft;
        _walkRemaining -= step;
    }

    private void HandleCursorInteraction()
    {
        var cursor = PointToScreen(Mouse.GetPosition(this));
        var center = new Point(Left + Width / 2, Top + Height / 2);
        var distance = Distance(cursor, center);
        var now = DateTime.UtcNow;
        var elapsedMs = Math.Max(1, (now - _lastCursorSample).TotalMilliseconds);
        var speed = Distance(cursor, _lastCursor) / elapsedMs;
        _lastCursor = cursor;
        _lastCursorSample = now;

        if (distance < 250 && speed > 0.9 && now - _lastPlayTrigger > TimeSpan.FromSeconds(2))
        {
            _lastPlayTrigger = now;
            PlayState(PetState.Play, TimeSpan.FromMilliseconds(900));
            return;
        }

        if (distance < 160 && _state != PetState.Alert)
        {
            PlayState(PetState.Alert, TimeSpan.FromMilliseconds(700));
        }
    }

    private void ChooseAutomaticState()
    {
        var choice = _random.Next(100);
        if (choice < 45)
        {
            PlayState(PetState.Idle);
            _nextAutoDecision = DateTime.UtcNow + TimeSpan.FromSeconds(_random.Next(5, 12));
        }
        else if (choice < 75)
        {
            _walkDirection = _random.Next(2) == 0 ? -1 : 1;
            _walkRemaining = _random.Next(80, 240);
            PlayState(_walkDirection < 0 ? PetState.WalkLeft : PetState.WalkRight);
            _nextAutoDecision = DateTime.UtcNow + TimeSpan.FromSeconds(20);
        }
        else
        {
            PlayState(PetState.Sleep);
            _nextAutoDecision = DateTime.UtcNow + TimeSpan.FromSeconds(_random.Next(20, 60));
        }
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_settings.IsClickThrough)
        {
            return;
        }

        _isDragging = true;
        _dragMoved = false;
        _dragStartScreen = PointToScreen(e.GetPosition(this));
        _dragStartLeft = Left;
        _dragStartTop = Top;
        CaptureMouse();
        PlayState(PetState.Dragged);
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            ReleaseMouseCapture();
            if (_dragMoved)
            {
                ClampToWorkArea();
                SavePosition();
                PlayState(PetState.Idle);
            }
            else
            {
                PlayState(PetState.Play, TimeSpan.FromMilliseconds(900));
            }
        }
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging)
        {
            return;
        }

        if (e.LeftButton == MouseButtonState.Released)
        {
            OnMouseLeftButtonUp(sender, new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left));
            return;
        }

        var current = PointToScreen(e.GetPosition(this));
        var dx = current.X - _dragStartScreen.X;
        var dy = current.Y - _dragStartScreen.Y;
        if (Math.Abs(dx) > 3 || Math.Abs(dy) > 3)
        {
            _dragMoved = true;
        }

        if (_dragMoved)
        {
            Left = _dragStartLeft + dx;
            Top = _dragStartTop + dy;
        }
    }

    private void OnDragEnter(object sender, System.Windows.DragEventArgs e)
    {
        if (!_settings.IsFeedingEnabled || !e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            e.Effects = System.Windows.DragDropEffects.None;
            return;
        }

        e.Effects = System.Windows.DragDropEffects.Move;
        PlayState(PetState.Alert, TimeSpan.FromSeconds(3));
    }

    private void OnDragOver(object sender, System.Windows.DragEventArgs e)
    {
        e.Effects = _settings.IsFeedingEnabled ? System.Windows.DragDropEffects.Move : System.Windows.DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object sender, System.Windows.DragEventArgs e)
    {
        if (!_settings.IsFeedingEnabled || e.Data.GetData(System.Windows.DataFormats.FileDrop) is not string[] paths)
        {
            PlayState(PetState.Reject, TimeSpan.FromMilliseconds(800));
            return;
        }

        var validation = _fileFeedService.Validate(paths);
        if (!validation.IsValid)
        {
            PlayState(PetState.Reject, TimeSpan.FromMilliseconds(900));
            MessageBox.Show(validation.Error, "不能投喂", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_settings.ConfirmBeforeFeeding)
        {
            var result = MessageBox.Show(
                $"把 {validation.Paths.Count} 个文件/文件夹喂给猫猫，并移入回收站？",
                "确认投喂",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Question);
            if (result != MessageBoxResult.OK)
            {
                PlayState(PetState.Idle);
                return;
            }
        }

        var feedResult = _fileFeedService.MoveToRecycleBin(validation.Paths);
        if (feedResult.Failures.Count == 0 && feedResult.SuccessCount > 0)
        {
            PlayState(PetState.Eat, TimeSpan.FromMilliseconds(1000));
            return;
        }

        PlayState(PetState.Reject, TimeSpan.FromMilliseconds(900));
        MessageBox.Show(
            $"投喂完成 {feedResult.SuccessCount} 个，失败 {feedResult.Failures.Count} 个。\n{string.Join("\n", feedResult.Failures.Take(5))}",
            "投喂结果",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private void EndForcedAnimation()
    {
        if (DateTime.UtcNow < _forcedUntil)
        {
            _forcedUntil = DateTime.UtcNow;
            PlayState(PetState.Idle);
        }
    }

    private void ToggleVisibility()
    {
        if (IsVisible)
        {
            HidePet();
        }
        else
        {
            Show();
            Activate();
        }
    }

    private void HidePet() => Hide();

    private void TogglePaused()
    {
        _isPaused = !_isPaused;
        if (_isPaused)
        {
            _animationTimer.Stop();
        }
        else
        {
            _animationTimer.Start();
            PlayState(PetState.Idle);
        }
    }

    private void ToggleClickThrough()
    {
        _settings.IsClickThrough = !_settings.IsClickThrough;
        _settingsStore.Save(_settings);
        ClickThroughWindow.SetClickThrough(this, _settings.IsClickThrough);
    }

    private void ToggleFeeding()
    {
        _settings.IsFeedingEnabled = !_settings.IsFeedingEnabled;
        _settingsStore.Save(_settings);
    }

    private void OpenSettings()
    {
        var window = new SettingsWindow(_settings) { Owner = this };
        if (window.ShowDialog() != true)
        {
            return;
        }

        _settings = window.Settings;
        Width = _petAsset.Manifest.Canvas.Width * _settings.Scale;
        Height = _petAsset.Manifest.Canvas.Height * _settings.Scale;
        ClampToWorkArea();
        _settings.Left = Left;
        _settings.Top = Top;
        _settingsStore.Save(_settings);
        ClickThroughWindow.SetClickThrough(this, _settings.IsClickThrough);
        PlayState(_state);
    }

    private void ResetPosition()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Left + (workArea.Width - Width) / 2;
        Top = workArea.Top + (workArea.Height - Height) / 2;
        SavePosition();
    }

    private void ClampToWorkArea()
    {
        var workArea = SystemParameters.WorkArea;
        Left = Math.Clamp(Left, workArea.Left, workArea.Right - Width);
        Top = Math.Clamp(Top, workArea.Top, workArea.Bottom - Height);
    }

    private void SavePosition()
    {
        _settings.Left = Left;
        _settings.Top = Top;
        _settingsStore.Save(_settings);
    }

    private void ExitApp()
    {
        SavePosition();
        DisposeTrayIcon();
        Application.Current.Shutdown();
    }

    private static double Distance(Point a, Point b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
