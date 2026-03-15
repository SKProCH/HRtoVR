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
    [Reactive] public ListenerViewModel? SelectedListener { get; set; }

    private readonly IOptionsManager<AppOptions> _appOptions;
    private readonly IEnumerable<IHrListener> _hrListeners;
    private readonly IServiceProvider _serviceProvider;

    public ListenersViewModel(IOptionsManager<AppOptions> appOptions,
        IEnumerable<IHrListener> hrListeners, IServiceProvider serviceProvider) {
        _appOptions = appOptions;
        _hrListeners = hrListeners;
        _serviceProvider = serviceProvider;

        LoadListeners();

        // Handle selection changes
        this.WhenAnyValue(x => x.SelectedListener)
            .Subscribe(listener => {
                if (listener != null) {
                    // Expand the selected listener and collapse others
                    foreach (var l in Listeners) {
                        l.IsExpanded = l == listener;
                    }

                    _appOptions.CurrentValue.ActiveListener = listener.Name;
                    _appOptions.Save();
                }
            });

        _connectionState = this.WhenAnyValue(x => x.SelectedListener)
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

        // Select the active listener
        SelectedListener = Listeners.FirstOrDefault(m => 
            m.Listener.IsAvailable &&
            m.Name.Equals(config.ActiveListener, StringComparison.OrdinalIgnoreCase));

        // Sync manual expansion with selection
        foreach (var listener in Listeners) {
            listener.WhenAnyValue(x => x.IsExpanded)
                .Subscribe(isExpanded => {
                    if (isExpanded) {
                        SelectedListener = listener;
                    }
                });
        }
    }
}