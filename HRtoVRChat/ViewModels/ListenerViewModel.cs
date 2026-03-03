using System.Reactive.Linq;
using HRtoVRChat.Models;
using HRtoVRChat.ViewModels.Listeners;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVRChat.ViewModels;

public class ListenerViewModel : ViewModelBase {
    [Reactive] public bool IsExpanded { get; set; }
    [Reactive] public int HeartRate { get; set; }
    [Reactive] public bool IsConnected { get; set; }
    [Reactive] public ConnectionState State { get; set; }
    [Reactive] public string Name { get; set; }
    [Reactive] public IListenerSettingsViewModel? Settings { get; set; }

    public ListenerViewModel(string name) {
        Name = name;
        this.WhenAnyValue(model => model.IsConnected, model => model.HeartRate)
            .Select(tuple => ConnectionState.FromListenerState(tuple.Item1, tuple.Item2))
            .BindTo(this, model => model.State);
    }
}