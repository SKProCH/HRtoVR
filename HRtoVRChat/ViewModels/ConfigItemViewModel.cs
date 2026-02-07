using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace HRtoVRChat.ViewModels;

public class ConfigItemViewModel : ViewModelBase
{
    [Reactive] public string Name { get; set; } = "";
    [Reactive] public string Description { get; set; } = "";
    [Reactive] public string TypeName { get; set; } = "";

    // We use a string for the input field to avoid binding issues with different types
    [Reactive] public string StringValue { get; set; } = "";

    public PropertyInfo PropertyInfo { get; }
    public object TargetObject { get; }
    public string ConfigPath { get; }

    private readonly IConfiguration _configuration;

    public ConfigItemViewModel(object targetObject, PropertyInfo propertyInfo, string configPath, IConfiguration configuration)
    {
        TargetObject = targetObject;
        PropertyInfo = propertyInfo;
        ConfigPath = configPath;
        _configuration = configuration;
        Name = propertyInfo.Name;

        var descAttr = propertyInfo.GetCustomAttribute<DescriptionAttribute>();
        Description = descAttr?.Description ?? "";

        TypeName = FriendlyName(propertyInfo.PropertyType).ToLower();

        var val = propertyInfo.GetValue(targetObject);
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
            var typedValue = Convert.ChangeType(newValue, PropertyInfo.PropertyType);
            PropertyInfo.SetValue(TargetObject, typedValue);

            _configuration?[ConfigPath] = newValue;
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
