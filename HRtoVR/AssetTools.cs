using System;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace HRtoVR;

public static class AssetTools {
    public static Bitmap Icon { get; private set; } = null!;

    public static void Init() {
        var uri = new Uri("avares://HRtoVR/Assets/hrtovrchat_logo.ico");
        using var stream = AssetLoader.Open(uri);
        Icon = new Bitmap(stream);
    }
}