using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Tommy.Serializer;

namespace HRtoVRChat.ViewModels;

public class ConfigItemViewModel : ViewModelBase
{
    [Reactive] public string Name { get; set; } = "";
    [Reactive] public string Description { get; set; } = "";
    [Reactive] public string TypeName { get; set; } = "";

    // We use a string for the input field to avoid binding issues with different types
    [Reactive] public string StringValue { get; set; } = "";

    public FieldInfo FieldInfo { get; }
    public object TargetObject { get; }

    public ConfigItemViewModel(object targetObject, FieldInfo fieldInfo)
    {
        TargetObject = targetObject;
        FieldInfo = fieldInfo;
        Name = fieldInfo.Name;

        var descAttr = (TommyComment)Attribute.GetCustomAttribute(fieldInfo, typeof(TommyComment));
        Description = descAttr?.Value ?? "";

        TypeName = FriendlyName(fieldInfo.FieldType).ToLower();

        var val = fieldInfo.GetValue(targetObject);
        StringValue = val?.ToString() ?? "";

        this.WhenAnyValue(x => x.StringValue)
            .Skip(1)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(Save);
    }

    public void Save(string newValue)
    {
        try
        {
            var typedValue = Convert.ChangeType(newValue, FieldInfo.FieldType);
            FieldInfo.SetValue(TargetObject, typedValue);
            ConfigManager.SaveConfig(ConfigManager.LoadedConfig);
        }
        catch (Exception)
        {
            // Ignore conversion errors
        }
    }

    private static string ToCsv(IEnumerable<object> collectionToConvert, string separator = ", ") {
        return string.Join(separator, collectionToConvert.Select(o => o.ToString()));
    }

    private static string FriendlyName(Type type) {
        if (type.IsGenericType) {
            var namePrefix = type.Name.Split(new[] { '`' }, StringSplitOptions.RemoveEmptyEntries)[0];
            var genericParameters = ToCsv(type.GetGenericArguments().Select(FriendlyName));
            return namePrefix + "<" + genericParameters + ">";
        }

        return type.Name;
    }
}
