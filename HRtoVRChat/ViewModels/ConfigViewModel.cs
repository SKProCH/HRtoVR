using System.Collections.ObjectModel;
using System.Reactive;
using HRtoVRChat.Configs;
using HRtoVRChat.Models;
using Material.Icons;
using Microsoft.Extensions.Options;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVRChat.ViewModels;

public class ConfigViewModel : ViewModelBase, IPageViewModel
{
    public string Title => "Config";
    public MaterialIconKind Icon => MaterialIconKind.Cog;
    public ConnectionState? State => null;

    [Reactive] public ParameterNamesOptions ParameterNamesOptions { get; set; }
    [Reactive] public EditableAppOptions AppOptions { get; set; }


    public ConfigViewModel(IOptionsMonitor<EditableAppOptions> appOptions, IOptionsMonitor<ParameterNamesOptions> parameterNamesOptions)
    {
        ParameterNamesOptions = parameterNamesOptions.CurrentValue;
        AppOptions = appOptions.CurrentValue;
    }
}
