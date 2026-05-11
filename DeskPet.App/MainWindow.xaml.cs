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
    private readonly PetCatalogService _catalogService = new();
    private readonly DispatcherTimer _animationTimer = new();
    private readonly DispatcherTimer _behaviorTimer = new();
    private readonly Random _random = new();
    private readonly NotifyIcon _trayIcon;
    private AppSettings _settings;
    private PetCatalog _catalog;
    private PetAsset _petAsset;
    private FileFeedService _fileFeedService;
    private ControlPanelWindow? _controlPanelWindow;
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
    private DateTime _lastCursorSample = DateTime.MinValue;
    private DateTime _lastPlayTrigger = DateTime.MinValue;
    private DateTime _playGestureStartedAt = DateTime.MinValue;
    private double _playGestureHorizontalDelta;
    private double _playGestureTotalDelta;
    private bool _wasCursorInPlayArea;

    public MainWindow()
    {
        InitializeComponent();

        _settings = _settingsStore.Load();
        _catalog = _catalogService.LoadCatalog();
        _petAsset = ResolveActivePet();
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

    private PetAsset ResolveActivePet()
    {
        var active = _catalog.Find(_settings.ActivePetId);
        if (active is not null)
        {
            return active;
        }

        var fallback = _catalog.Cats.FirstOrDefault() ?? _catalog.Pets.FirstOrDefault();
        if (fallback is not null)
        {
            _settings.ActivePetId = fallback.Manifest.PetId;
            _settingsStore.Save(_settings);
            return fallback;
        }

        throw new DirectoryNotFoundException("No valid pet assets were found under assets/pets.");
    }

    private void ConfigureWindow()
    {
        ApplyPetWindowSize();

        var workArea = SystemParameters.WorkArea;
        var petSettings = _settings.ActivePet();
        Left = petSettings.Left is > 0 ? Math.Min(petSettings.Left.Value, workArea.Right - Width) : workArea.Right - Width - 80;
        Top = petSettings.Top is > 0 ? Math.Min(petSettings.Top.Value, workArea.Bottom - Height) : workArea.Bottom - Height - 80;
        Opacity = petSettings.Opacity;
        Topmost = _settings.IsTopmost;
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
        menu.Items.Add(CreateWpfMenuItem("打开控制面板", (_, _) => OpenControlPanel()));
        menu.Items.Add(CreateWpfMenuItem("隐藏", (_, _) => HidePet()));
        menu.Items.Add(CreateWpfMenuItem("暂停/继续", (_, _) => TogglePaused()));
        menu.Items.Add(CreateWpfMenuItem("点击穿透", (_, _) => ToggleClickThrough()));
        menu.Items.Add(CreateWpfMenuItem("文件投喂", (_, _) => ToggleFeeding()));
        menu.Items.Add(CreateWpfMenuItem("设置", (_, _) => OpenControlPanel()));
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
        icon.ContextMenuStrip.Items.Add("打开控制面板", null, (_, _) => Dispatcher.Invoke(OpenControlPanel));
        icon.ContextMenuStrip.Items.Add("暂停/继续", null, (_, _) => Dispatcher.Invoke(TogglePaused));
        icon.ContextMenuStrip.Items.Add("点击穿透", null, (_, _) => Dispatcher.Invoke(ToggleClickThrough));
        icon.ContextMenuStrip.Items.Add("文件投喂", null, (_, _) => Dispatcher.Invoke(ToggleFeeding));
        icon.ContextMenuStrip.Items.Add("设置", null, (_, _) => Dispatcher.Invoke(OpenControlPanel));
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
        var speed = Math.Max(0.25, _settings.ActivePet().AnimationSpeed);
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
        PetState.Play => "play",
        PetState.PlayLeft => "play_left",
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
            SampleCursorPosition();
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
            _nextAutoDecision = DateTime.UtcNow + TimeSpan.FromSeconds(_random.Next(2, 6));
            return;
        }

        var step = 2.0 * _settings.ActivePet().Scale * _settings.ActivePet().MoveSpeed;
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
        var cursor = CursorTracker.GetScreenPosition();
        if (double.IsNaN(cursor.X) || double.IsNaN(cursor.Y))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var hasPreviousSample = _lastCursorSample != DateTime.MinValue;
        var elapsedMs = hasPreviousSample ? Math.Max(1, (now - _lastCursorSample).TotalMilliseconds) : 100;
        var previousCursor = _lastCursor;
        var cursorDelta = hasPreviousSample ? Distance(cursor, _lastCursor) : 0;
        _lastCursor = cursor;
        _lastCursorSample = now;

        var center = PointToScreen(new Point(Width / 2, Height / 2));
        if (!IsCursorInPlayArea(cursor))
        {
            ResetPlayGesture();
            _wasCursorInPlayArea = false;
            return;
        }

        if (!_wasCursorInPlayArea || !hasPreviousSample || elapsedMs > 350)
        {
            _wasCursorInPlayArea = true;
            ResetPlayGesture();
            return;
        }

        if (_playGestureStartedAt == DateTime.MinValue)
        {
            _playGestureStartedAt = now;
        }
        else if (now - _playGestureStartedAt > TimeSpan.FromMilliseconds(900))
        {
            ResetPlayGesture();
            _playGestureStartedAt = now;
        }

        _playGestureHorizontalDelta += cursor.X - previousCursor.X;
        _playGestureTotalDelta += cursorDelta;

        var profile = CurrentInteractionProfile();
        var canPlay = now - _lastPlayTrigger > TimeSpan.FromMilliseconds(profile.CooldownMs);
        var hasEnoughMotion = _playGestureTotalDelta >= profile.TotalMotionThreshold ||
                              Math.Abs(_playGestureHorizontalDelta) >= profile.HorizontalMotionThreshold;
        if (hasEnoughMotion && canPlay)
        {
            _lastPlayTrigger = now;
            _walkRemaining = 0;
            var state = ChoosePlayState(center, previousCursor, cursor, _playGestureHorizontalDelta);
            ResetPlayGesture();
            PlayState(state, TimeSpan.FromMilliseconds(850));
        }
    }

    private static PetState ChoosePlayState(
        Point petCenter,
        Point previousCursor,
        Point currentCursor,
        double gestureHorizontalDelta)
    {
        if (Math.Abs(gestureHorizontalDelta) >= 2)
        {
            return gestureHorizontalDelta < 0 ? PetState.PlayLeft : PetState.Play;
        }

        var currentHorizontalDelta = currentCursor.X - previousCursor.X;
        if (Math.Abs(currentHorizontalDelta) >= 1)
        {
            return currentHorizontalDelta < 0 ? PetState.PlayLeft : PetState.Play;
        }

        return currentCursor.X < petCenter.X ? PetState.PlayLeft : PetState.Play;
    }

    private bool IsCursorInPlayArea(Point cursor)
    {
        var localCursor = PointFromScreen(cursor);
        var hitArea = _petAsset.Manifest.HitArea;
        var hasHitArea = hitArea.Width > 0 && hitArea.Height > 0;
        var scale = _settings.ActivePet().Scale;
        var x = hasHitArea ? hitArea.X * scale : 0;
        var y = hasHitArea ? hitArea.Y * scale : 0;
        var width = hasHitArea ? hitArea.Width * scale : Width;
        var height = hasHitArea ? hitArea.Height * scale : Height;
        var padding = 14 * scale;

        return localCursor.X >= x - padding &&
               localCursor.X <= x + width + padding &&
               localCursor.Y >= y - padding &&
               localCursor.Y <= y + height + padding;
    }

    private InteractionProfile CurrentInteractionProfile() => _settings.ActivePet().InteractionLevel.ToLowerInvariant() switch
    {
        "low" => new InteractionProfile(900, 9, 7),
        "high" => new InteractionProfile(450, 4, 3),
        _ => new InteractionProfile(650, 6, 4)
    };

    private void ResetPlayGesture()
    {
        _playGestureStartedAt = DateTime.MinValue;
        _playGestureHorizontalDelta = 0;
        _playGestureTotalDelta = 0;
    }

    private void SampleCursorPosition()
    {
        var cursor = CursorTracker.GetScreenPosition();
        if (double.IsNaN(cursor.X) || double.IsNaN(cursor.Y))
        {
            return;
        }

        _lastCursor = cursor;
        _lastCursorSample = DateTime.UtcNow;
    }

    private void ChooseAutomaticState()
    {
        var choice = _random.Next(100);
        if (choice < 25)
        {
            PlayState(PetState.Idle);
            _nextAutoDecision = DateTime.UtcNow + TimeSpan.FromSeconds(_random.Next(3, 8));
        }
        else if (choice < 85)
        {
            _walkDirection = _random.Next(2) == 0 ? -1 : 1;
            _walkRemaining = _random.Next(160, 420) * _settings.ActivePet().Scale;
            PlayState(_walkDirection < 0 ? PetState.WalkLeft : PetState.WalkRight);
            _nextAutoDecision = DateTime.UtcNow + TimeSpan.FromSeconds(20);
        }
        else
        {
            PlayState(PetState.Sleep);
            _nextAutoDecision = DateTime.UtcNow + TimeSpan.FromMinutes(1);
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
        PlayState(PetState.Idle, TimeSpan.FromSeconds(1));
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

    private void OpenControlPanel()
    {
        if (_controlPanelWindow is { IsVisible: true })
        {
            _controlPanelWindow.Activate();
            return;
        }

        _catalog = _catalogService.LoadCatalog();
        _controlPanelWindow = new ControlPanelWindow(_settings, _catalog, _settings.ActivePetId, IsVisible, _isPaused)
        {
            Owner = this
        };
        _controlPanelWindow.ApplyRequested += (_, request) => ApplyControlPanelRequest(request);
        _controlPanelWindow.ResetPositionRequested += (_, _) => ResetPosition();
        _controlPanelWindow.PauseToggleRequested += (_, _) => TogglePaused();
        _controlPanelWindow.Closed += (_, _) => _controlPanelWindow = null;
        _controlPanelWindow.Show();
    }

    private void ApplyControlPanelRequest(ControlPanelApplyRequest request)
    {
        SavePosition();

        _settings.ActivePetId = request.ActivePetId;
        _settings.IsClickThrough = request.IsClickThrough;
        _settings.IsFeedingEnabled = request.IsFeedingEnabled;
        _settings.ConfirmBeforeFeeding = request.ConfirmBeforeFeeding;

        var petSettings = _settings.PetFor(request.ActivePetId);
        petSettings.Scale = request.Scale;
        petSettings.MoveSpeed = request.MoveSpeed;
        petSettings.InteractionLevel = request.InteractionLevel;
        petSettings.Opacity = request.Opacity;

        _catalog = _catalogService.LoadCatalog();
        _petAsset = ResolveActivePet();
        _fileFeedService = new FileFeedService(AppContext.BaseDirectory, _petAsset.RootDirectory);

        ApplyPetWindowSize();
        Opacity = petSettings.Opacity;
        ClampToWorkArea();
        SavePosition();
        _settingsStore.Save(_settings);
        ClickThroughWindow.SetClickThrough(this, _settings.IsClickThrough);
        ResetPlayGesture();
        PlayState(PetState.Idle);
    }

    private void ApplyPetWindowSize()
    {
        var scale = _settings.ActivePet().Scale;
        Width = _petAsset.Manifest.Canvas.Width * scale;
        Height = _petAsset.Manifest.Canvas.Height * scale;
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
        var petSettings = _settings.ActivePet();
        petSettings.Left = Left;
        petSettings.Top = Top;
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
    
    private readonly record struct InteractionProfile(int CooldownMs, double TotalMotionThreshold, double HorizontalMotionThreshold);
}
