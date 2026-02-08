using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using HRtoVRChat.Configs;
using HRtoVRChat.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace HRtoVRChat.ViewModels.Design;

public class DesignListenersViewModel : ListenersViewModel
{
    public static DesignListenersViewModel Instance { get; } = new();

    public DesignListenersViewModel() : base(new FakeOptionsManager(), new FakeConfiguration(), CreateFakeListeners())
    {
    }

    private static IEnumerable<IHrListener> CreateFakeListeners()
    {
        yield return new FakeHrListener("Fitbit", true, 75, new FakeFitbitSettings { Url = "ws://localhost:8080", AutoConnect = true }, "FitbitOptions");
        yield return new FakeHrListener("HypeRate", false, 0, new FakeHypeRateSettings { SessionId = "12345" }, "HypeRateOptions");
        yield return new FakeHrListener("Pulsoid", true, 82, new FakePulsoidSettings { WidgetId = "abc-123" }, "PulsoidOptions");
    }

    private class FakeFitbitSettings
    {
        public string Url { get; set; } = "";
        public bool AutoConnect { get; set; }
    }

    private class FakeHypeRateSettings
    {
        public string SessionId { get; set; } = "";
    }

    private class FakePulsoidSettings
    {
        public string WidgetId { get; set; } = "";
    }

    private class FakeOptionsManager : IOptionsManager<AppOptions>
    {
        public AppOptions CurrentValue { get; } = new AppOptions { ActiveListener = "Fitbit" };
        public AppOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<AppOptions, string?> listener) => null;
        public void Save() { }
    }

    private class FakeConfiguration : IConfiguration
    {
        public string? this[string key]
        {
            get => null;
            set { }
        }
        public IConfigurationSection GetSection(string key) => new FakeConfigurationSection(key);
        public IEnumerable<IConfigurationSection> GetChildren() => Array.Empty<IConfigurationSection>();
        public IChangeToken GetReloadToken() => new FakeChangeToken();

        private class FakeConfigurationSection : IConfigurationSection
        {
            public string? this[string key] { get => null; set { } }
            public string Key { get; }
            public string Path => Key;
            public string? Value { get; set; }
            public IEnumerable<IConfigurationSection> GetChildren() => Array.Empty<IConfigurationSection>();
            public IChangeToken GetReloadToken() => new FakeChangeToken();
            public IConfigurationSection GetSection(string key) => new FakeConfigurationSection(key);

            public FakeConfigurationSection(string key)
            {
                Key = key;
            }
        }

        private class FakeChangeToken : IChangeToken
        {
            public bool HasChanged => false;
            public bool ActiveChangeCallbacks => false;
            public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) => new FakeDisposable();

            private class FakeDisposable : IDisposable
            {
                public void Dispose() { }
            }
        }
    }

    private class FakeHrListener : IHrListener
    {
        public string Name { get; }
        public object? Settings { get; }
        public string? SettingsSectionName { get; }
        public IObservable<int> HeartRate { get; }
        public IObservable<bool> IsConnected { get; }

        public FakeHrListener(string name, bool isConnected, int hr, object? settings = null, string? sectionName = null)
        {
            Name = name;
            Settings = settings;
            SettingsSectionName = sectionName;
            IsConnected = Observable.Return(isConnected);
            HeartRate = Observable.Return(hr);
        }

        public void Start() { }
        public void Stop() { }
    }
}
