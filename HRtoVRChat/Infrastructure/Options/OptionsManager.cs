using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using INotifyPropertyChanged = System.ComponentModel.INotifyPropertyChanged;

namespace HRtoVRChat.Infrastructure.Options;

internal class OptionsManager<T> : OptionsMonitor<T>, IOptionsManager<T>, IDisposable where T : class {
    private readonly IConfiguration _configuration;
    private readonly OptionsConfigPathResolver<T> _configPathResolver;
    private INotifyPropertyChanged? _lastPropertyChanged;

    public OptionsManager(IOptionsFactory<T> factory, IEnumerable<IOptionsChangeTokenSource<T>> sources,
        IOptionsMonitorCache<T> cache,
        IConfiguration configuration, OptionsConfigPathResolver<T> configPathResolver) : base(factory, sources, cache) {
        _configuration = configuration;
        _configPathResolver = configPathResolver;
        factory.Create(Microsoft.Extensions.Options.Options.DefaultName);
    }

    public override T Get(string? name) {
        var obj = base.Get(name);
        // ReSharper disable once PossibleUnintendedReferenceComparison
        if (obj != _lastPropertyChanged && obj is INotifyPropertyChanged notifyPropertyChanged) {
            _lastPropertyChanged = notifyPropertyChanged;
            notifyPropertyChanged.PropertyChanged += NotifyPropertyChangedOnPropertyChanged;
        }

        return obj;
    }

    private void NotifyPropertyChangedOnPropertyChanged(object? sender, PropertyChangedEventArgs e) {
        Save();
    }

    public void Save() {
        _configuration.Set(_configPathResolver.Path, CurrentValue);
    }

    void IDisposable.Dispose() {
        _lastPropertyChanged?.PropertyChanged -= NotifyPropertyChangedOnPropertyChanged;
        _lastPropertyChanged = null;
        base.Dispose();
    }
}