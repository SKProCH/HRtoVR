using ReactiveUI.Fody.Helpers;

namespace HRtoVRChat.ViewModels;

public class ConfigItemViewModel : ViewModelBase
{
    [Reactive] public string Name { get; set; } = "";
    [Reactive] public string Description { get; set; } = "";
    [Reactive] public string TypeName { get; set; } = "";
    [Reactive] public object? Value { get; set; }
    [Reactive] public string FieldName { get; set; } = "";
}
