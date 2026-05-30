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

internal readonly record struct TrayState(ConnectionState DeviceState, ConnectionState VrcState);

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
                    ? new TrayState(ConnectionState.FromListenerState(connected, hr), hasVrc ? ConnectionState.Active : ConnectionState.Disconnected)
                    : new TrayState(ConnectionState.Disconnected, hasVrc ? ConnectionState.Active : ConnectionState.Disconnected))
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
            Icon = RenderIcon(new TrayState(ConnectionState.Disconnected, ConnectionState.Disconnected)),
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
        var vrcText = state.VrcState switch {
            ConnectionState.Active => "Connected",
            _ => "Not running"
        };
        return $"HRtoVR — HR: {deviceText} | VRChat: {vrcText}";
    }

    private static WindowIcon RenderIcon(TrayState state) {
        const int size = 32;
        using var bitmap = new SKBitmap(size, size, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        DrawHeart(canvas, state);

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

    private const string HeartTopSvg =
        "M210.85,294.59c-1.6-2.32-3.44-4.57-4.83-7.07-4.4-7.87-8.59-15.86-12.94-23.75 0 0-3.11-6.01-8.23-14.4-4.08-6.7-5.63-8.62-7.68-8.71-3.5-.16-6.35,5.08-8.98,9.9-5.94,10.89-8.38,15.27-12.19,22.16-.35.63-1.61,1.08-2.45,1.08-19.66.06-39.33.09-58.99-.06-1.23,0-3.11-1.21-3.57-2.32-7.98-19.05-13.01-38.79-10.66-59.57.94-8.3,4.12-20.86,13.35-34.43,3.49-5.13,15.99-22.94,39.96-30.86,3.89-1.29,16.61-5.06,32.98-3.22,16.5,1.86,30.93,8.69,42.86,20.22,4.6,4.44,8.27,9.84,12.39,14.78.95,1.14,2.03,2.19,3.26,3.51,3.57-4.74,6.88-9.47,10.54-13.91,8.13-9.85,18.9-16.46,30.54-20.64,20.93-7.5,42.22-6.76,61.89,4.04,10.23,5.62,19.17,13.27,26.36,23.33,14.82,20.75,18.88,43.12,13.33,67.43-2.28,10-6.08,19.66-9.38,29.41-.34,1-2,2.22-3.06,2.23-21.66.13-43.32.1-65.44.1-5.17-15.13-10.23-30.29-15.54-45.35-2.44-6.92-5.25-13.73-8.14-20.48-2.11-4.92-8.74-6.38-11.8-2.13-3.64,5.07-6.15,10.97-8.95,16.61-4.83,9.73-9.56,19.51-14.25,29.3-6.79,14.16-13.5,28.36-20.36,42.78Z";

    private const string HeartBottomSvg =
        "M105.14,296.85c19.83,0,38.3-.25,56.77.14,1.49.03,5.09.29,7.87-1.93.9-.72,1.52-1.54,3.61-5.07,1.55-2.63,2.79-4.85,3.66-6.45,2.31,5.13,4.61,10.25,6.92,15.38,2.44,4.98,12.33,24.9,20.63,34.19,1.68,1.87,4.11,4.24,7.15,4.15,3.13-.09,5.59-2.75,7.51-5.44,7.98-11.14,20.13-41.32,22.02-46.03,5.2-11.27,10.39-22.54,15.59-33.81,2.49,6.59,4.69,12.41,6.88,18.24,2.65,7.07,5.36,14.12,7.88,21.23,1.41,3.95,4.07,5.46,8.15,5.45,18.99-.07,37.98-.03,56.97-.03,1.82,0,3.64,0,6.33,0-7.99,11-15.26,19.39-20.42,25.01-10.53,11.47-19.46,19.82-36.26,34.07-6.2,5.26-12.17,10.79-18.54,15.83-10.83,8.56-21.84,16.91-32.87,25.2-8.97,6.74-11.95,6.19-21.28-1.39-6.31-5.12-13.41-9.25-19.89-14.17-5.79-4.39-11.28-9.18-16.81-13.91-9.47-8.1-18.92-16.23-28.28-24.46-5.67-4.98-11.51-9.85-16.62-15.37-8.97-9.7-17.45-19.86-26.97-30.8Z";

    private static readonly SKPath _heartTopPath;
    private static readonly SKPath _heartBottomPath;

    static TrayIconService() {
        const int size = 32;

        var top = SKPath.ParseSvgPathData(HeartTopSvg);
        var bottom = SKPath.ParseSvgPathData(HeartBottomSvg);

        var bounds = top.Bounds;
        bounds.Union(bottom.Bounds);

        var padding = size * 0.06f;
        var available = size - padding * 2;
        var scale = available / Math.Max(bounds.Width, bounds.Height);
        var offsetX = padding + (available - bounds.Width * scale) / 2f - bounds.Left * scale;
        var offsetY = padding + (available - bounds.Height * scale) / 2f - bounds.Top * scale;

        var transform = SKMatrix.CreateScaleTranslation(scale, scale, offsetX, offsetY);
        top.Transform(transform);
        bottom.Transform(transform);

        _heartTopPath = top;
        _heartBottomPath = bottom;
    }

    private static void DrawHeart(SKCanvas canvas, TrayState state) {
        using var topPaint = new SKPaint();
        topPaint.Color = StateColor(state.DeviceState);
        topPaint.IsAntialias = true;
        topPaint.Style = SKPaintStyle.Fill;
        canvas.DrawPath(_heartTopPath, topPaint);

        using var bottomPaint = new SKPaint();
        bottomPaint.Color = StateColor(state.VrcState);
        bottomPaint.IsAntialias = true;
        bottomPaint.Style = SKPaintStyle.Fill;
        canvas.DrawPath(_heartBottomPath, bottomPaint);
    }

    private static SKColor StateColor(ConnectionState state) => state switch {
        ConnectionState.Active => new SKColor(0x22, 0xCC, 0x44),
        ConnectionState.Connecting => new SKColor(0xCC, 0xCC, 0x00),
        _ => new SKColor(0xCC, 0x22, 0x22)
    };
}
