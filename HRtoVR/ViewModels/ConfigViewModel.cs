using HRtoVR.Configs;
using HRtoVR.Models;
using Material.Icons;
using Microsoft.Extensions.Options;
using ReactiveUI.Fody.Helpers;

namespace HRtoVR.ViewModels;

public class ConfigViewModel : ViewModelBase, IPageViewModel {
    public string Title => "Config";
    public MaterialIconKind Icon => MaterialIconKind.Cog;
    public ConnectionState? State => null;

    [Reactive] public ParameterNamesOptions ParameterNamesOptions { get; set; }
    [Reactive] public EditableAppOptions AppOptions { get; set; }


    public ConfigViewModel(IOptionsMonitor<EditableAppOptions> appOptions,
        IOptionsMonitor<ParameterNamesOptions> parameterNamesOptions) {
        ParameterNamesOptions = parameterNamesOptions.CurrentValue;
        AppOptions = appOptions.CurrentValue;
    }
}