using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using HRtoVR.Models;
using ReactiveUI;
using SkiaSharp;

namespace HRtoVR.Services;

public interface ITrayIconService {
    void Init(Application app, Window mainWindow);
}

internal readonly record struct TrayState(ConnectionState DeviceState, bool VrcConnected);

public class TrayIconService : ITrayIconService {
    private Window? _mainWindow;

    private readonly NativeMenuItem _statusItem = new() {
        Header = "Status: STOPPED",
        ToggleType = NativeMenuItemToggleType.None
    };

    private readonly NativeMenuItem _hideItem = new() {
        Header = "Hide Application",
        ToggleType = NativeMenuItemToggleType.CheckBox
    };

    private readonly NativeMenuItem _exitItem = new() {
        Header = "Exit",
        ToggleType = NativeMenuItemToggleType.None
    };

    private TrayIcon? _trayIcon;
    private readonly CompositeDisposable _disposables = new();

    public TrayIconService(IHRService hrService) {
        _hideItem.Command = ReactiveCommand.Create(() => {
            if (_mainWindow == null) return;
            if (_mainWindow.IsVisible) _mainWindow.Hide();
            else _mainWindow.Show();
        });

        _exitItem.Command = ReactiveCommand.CreateFromTask(App.Shutdown);

        Observable.CombineLatest(
                hrService.IsConnected,
                hrService.HeartRate,
                hrService.HasActiveGameHandle,
                hrService.ActiveListener,
                (connected, hr, hasVrc, listener) => listener != null
                    ? new TrayState(ConnectionState.FromListenerState(connected, hr), hasVrc)
                    : new TrayState(ConnectionState.Disconnected, hasVrc))
            .DistinctUntilChanged()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(OnStateChanged)
            .DisposeWith(_disposables);
    }

    public void Init(Application app, Window mainWindow) {
        _mainWindow = mainWindow;

        _mainWindow.GetObservable(Visual.IsVisibleProperty)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(visible => _hideItem.IsChecked = !visible)
            .DisposeWith(_disposables);

        var menu = new NativeMenu();
        menu.Add(_statusItem);
        menu.Add(new NativeMenuItemSeparator());
        menu.Add(_hideItem);
        menu.Add(_exitItem);

        _trayIcon = new TrayIcon {
            Icon = RenderIcon(new TrayState(ConnectionState.Disconnected, false)),
            ToolTipText = "HRtoVR",
            Menu = menu
        };

        var icons = new TrayIcons { _trayIcon };
        TrayIcon.SetIcons(app, icons);
    }

    private void OnStateChanged(TrayState state) {
        _statusItem.Header = BuildStatusText(state);
        if (_trayIcon != null) {
            _trayIcon.Icon = RenderIcon(state);
            _trayIcon.ToolTipText = BuildTooltip(state);
        }
    }

    private static string BuildStatusText(TrayState state) {
        var deviceText = state.DeviceState switch {
            ConnectionState.Active => "CONNECTED",
            ConnectionState.Connecting => "CONNECTING",
            _ => "DISCONNECTED"
        };
        return $"Status: {deviceText}";
    }

    private static string BuildTooltip(TrayState state) {
        var deviceText = state.DeviceState switch {
            ConnectionState.Active => "Active",
            ConnectionState.Connecting => "Connecting",
            _ => "Disconnected"
        };
        var vrcText = state.VrcConnected ? "Connected" : "Not running";
        return $"HRtoVR — HR: {deviceText} | VRChat: {vrcText}";
    }

    private static WindowIcon RenderIcon(TrayState state) {
        const int size = 32;
        using var bitmap = new SKBitmap(size, size, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        DrawHeart(canvas, size, state);

        var avaBitmap = new WriteableBitmap(
            new PixelSize(size, size),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        using (var fb = avaBitmap.Lock()) {
            var src = bitmap.GetPixelSpan();
            unsafe {
                fixed (byte* srcPtr = src) {
                    Buffer.MemoryCopy(srcPtr, (void*)fb.Address, src.Length, src.Length);
                }
            }
        }

        return new WindowIcon(avaBitmap);
    }

    // TODO Redo the heart
    private static void DrawHeart(SKCanvas canvas, int size, TrayState state) {
        float s = size;
        float cx = s / 2f;

        using var path = new SKPath();
        path.MoveTo(cx, s * 0.9f);
        path.CubicTo(s * 0.0f, s * 0.6f, s * 0.0f, s * 0.15f, cx, s * 0.35f);
        path.CubicTo(s * 1.0f, s * 0.15f, s * 1.0f, s * 0.6f, cx, s * 0.9f);
        path.Close();

        float splitY = s * 0.52f;

        canvas.Save();
        canvas.ClipRect(SKRect.Create(0, 0, s, splitY));
        using (var paint = new SKPaint {
                   Color = DeviceStateColor(state.DeviceState),
                   IsAntialias = true,
                   Style = SKPaintStyle.Fill
               })
            canvas.DrawPath(path, paint);
        canvas.Restore();

        canvas.Save();
        canvas.ClipRect(SKRect.Create(0, splitY, s, s - splitY));
        using (var paint = new SKPaint {
                   Color = state.VrcConnected ? new SKColor(0x22, 0xCC, 0x44) : new SKColor(0xCC, 0x22, 0x22),
                   IsAntialias = true,
                   Style = SKPaintStyle.Fill
               })
            canvas.DrawPath(path, paint);
        canvas.Restore();

        using var outline = new SKPaint {
            Color = new SKColor(0, 0, 0, 100),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = s * 0.06f
        };
        canvas.DrawPath(path, outline);
    }

    private static SKColor DeviceStateColor(ConnectionState state) => state switch {
        ConnectionState.Active => new SKColor(0x22, 0xCC, 0x44),
        ConnectionState.Connecting => new SKColor(0xCC, 0xCC, 0x00),
        _ => new SKColor(0xCC, 0x22, 0x22)
    };
}
