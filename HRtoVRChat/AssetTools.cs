using System;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace HRtoVRChat;

public static class AssetTools {
    public static Bitmap Icon { get; private set; } = null!;

    public static void Init() {
        var uri = new Uri("avares://HRtoVRChat/Assets/hrtovrchat_logo.ico");
        using var stream = AssetLoader.Open(uri);
        Icon = new Bitmap(stream);
    }
}
