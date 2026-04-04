using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using HRtoVR.Configs;
using HRtoVR.Infrastructure.Options;
using HRtoVR.Models;
using HRtoVR.ViewModels.Listeners;
using Material.Icons;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVR.ViewModels;

public class ListenersViewModel : ViewModelBase, IPageViewModel {
    public string Title => "Listeners";
    public MaterialIconKind Icon => MaterialIconKind.Home;
    private readonly ObservableAsPropertyHelper<ConnectionState?> _connectionState;
    public ConnectionState? State => _connectionState.Value;

    public ObservableCollection<ListenerViewModel> Listeners { get; } = [];
    [Reactive] public ListenerViewModel? ActiveListener { get; private set; }

    private readonly IOptionsManager<AppOptions> _appOptions;
    private readonly IEnumerable<IHrListener> _hrListeners;
    private readonly IServiceProvider _serviceProvider;

    public ListenersViewModel(IOptionsManager<AppOptions> appOptions,
        IEnumerable<IHrListener> hrListeners, IServiceProvider serviceProvider) {
        _appOptions = appOptions;
        _hrListeners = hrListeners;
        _serviceProvider = serviceProvider;

        LoadListeners();

        _connectionState = this.WhenAnyValue(x => x.ActiveListener)
            .Select(listener => listener != null
                ? listener.WhenAnyValue(x => (ConnectionState?)x.State)
                : Observable.Return((ConnectionState?)null))
            .Switch()
            .ToProperty(this, x => x.State);
    }

    private void LoadListeners() {
        Listeners.Clear();
        var config = _appOptions.CurrentValue;
        Listeners.AddRange(_hrListeners.Select(l => new ListenerViewModel(l, _serviceProvider)));

        // Expand the active listener
        ActiveListener = Listeners.FirstOrDefault(m =>
            m.Listener.IsAvailable &&
            m.Name.Equals(config.ActiveListener, StringComparison.OrdinalIgnoreCase));

        if (ActiveListener != null) {
            ActiveListener.IsExpanded = true;
        }

        // Accordion behavior
        foreach (var listener in Listeners) {
            listener.WhenAnyValue(x => x.IsExpanded)
                .Where(isExpanded => isExpanded)
                .Subscribe(_ => {
                    foreach (var other in Listeners) {
                        if (other != listener) {
                            other.IsExpanded = false;
                        }
                    }
                    ActiveListener = listener;
                    _appOptions.CurrentValue.ActiveListener = listener.Name;
                    _appOptions.Save();
                });
        }
    }
}