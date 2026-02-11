using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using HRtoVRChat.Configs;
using HRtoVRChat.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace HRtoVRChat.ViewModels.Design;

public class DesignListenersViewModel : ListenersViewModel
{
    public static DesignListenersViewModel Instance { get; } = new();

    public DesignListenersViewModel() : base(new FakeAppOptionsManager(), new FakeConfiguration(), CreateFakeListeners(), new FakeServiceProvider())
    {
    }

    private class FakeAppOptionsManager : IOptionsManager<AppOptions>
    {
        public AppOptions CurrentValue { get; } = new AppOptions { ActiveListener = "FitBit" };
        public AppOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<AppOptions, string?> listener) => null;
        public void Save() { }
    }

    private class FakeConfiguration : IConfiguration
    {
        public string? this[string key] { get => null; set { } }
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
            public FakeConfigurationSection(string key) => Key = key;
        }

        private class FakeChangeToken : IChangeToken
        {
            public bool HasChanged => false;
            public bool ActiveChangeCallbacks => false;
            public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) => new FakeDisposable();
            private class FakeDisposable : IDisposable { public void Dispose() { } }
        }
    }

    private class FakeServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IOptionsManager<>))
            {
                var optionType = serviceType.GetGenericArguments()[0];
                var managerType = typeof(FakeOptionsManager<>).MakeGenericType(optionType);
                return Activator.CreateInstance(managerType);
            }
            return null;
        }
    }

    private class FakeOptionsManager<T> : IOptionsManager<T> where T : class, new()
    {
        public T CurrentValue { get; } = new T();
        public T Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
        public void Save() { }
    }

    private static IEnumerable<IHrListener> CreateFakeListeners()
    {
        yield return new FakeHrListener("FitBit", true, 75);
        yield return new FakeHrListener("HypeRate", false, 0);
        yield return new FakeHrListener("Pulsoid", true, 82);
    }

    private class FakeHrListener : IHrListener
    {
        public string Name { get; }
        public IObservable<int> HeartRate { get; }
        public IObservable<bool> IsConnected { get; }
        public Type? SettingsViewModelType => null;

        public FakeHrListener(string name, bool isConnected, int hr)
        {
            Name = name;
            IsConnected = Observable.Return(isConnected);
            HeartRate = Observable.Return(hr);
        }

        public void Start() { }
        public void Stop() { }
    }
}
