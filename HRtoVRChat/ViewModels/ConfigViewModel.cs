using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using HRtoVRChat.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Tommy.Serializer;

namespace HRtoVRChat.ViewModels;

public class ConfigViewModel : ViewModelBase
{
    public ObservableCollection<ConfigItemViewModel> ConfigItemsLeft { get; } = new();
    public ObservableCollection<ConfigItemViewModel> ConfigItemsRight { get; } = new();

    [Reactive] public ConfigItemViewModel? SelectedConfigItem { get; set; }
    [Reactive] public string ConfigValueInput { get; set; } = "";

    public ReactiveCommand<ConfigItemViewModel, Unit> SwitchConfigSelectionCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveConfigCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenParameterNamesCommand { get; }

    public ConfigViewModel()
    {
        SwitchConfigSelectionCommand = ReactiveCommand.Create<ConfigItemViewModel>(item => {
            SelectedConfigItem = item;
        });

        SaveConfigCommand = ReactiveCommand.Create(SaveConfig);

        OpenParameterNamesCommand = ReactiveCommand.Create(() => {
            if (!ParameterNames.IsOpen)
                new ParameterNames().Show();
        });

        this.WhenAnyValue(x => x.SelectedConfigItem)
            .Where(x => x != null)
            .Subscribe(item => {
                if (item != null)
                {
                    var targetField = ConfigManager.LoadedConfig.GetType().GetField(item.FieldName);
                    if (targetField != null)
                    {
                        var val = targetField.GetValue(ConfigManager.LoadedConfig);
                        ConfigValueInput = val?.ToString() ?? "";
                    }
                }
            });

        Initialize();
    }

    private void Initialize()
    {
        LoadConfigItems();
    }

    private void LoadConfigItems()
    {
        var configValues = new List<string>();
        foreach (var fieldInfo in new Config().GetType().GetFields())
            configValues.Add(fieldInfo.Name);

        var allItems = new List<ConfigItemViewModel>();

        foreach (var configValue in configValues)
        {
             var field = new Config().GetType().GetField(configValue);
             if (field == null) continue;

             if (configValue == "ParameterNames") continue;

             var descAttr = (TommyComment)Attribute.GetCustomAttribute(field, typeof(TommyComment));
             var desc = descAttr?.Value ?? "";

             var item = new ConfigItemViewModel
             {
                 Name = field.Name,
                 FieldName = field.Name,
                 TypeName = FriendlyName(field.FieldType).ToLower(),
                 Description = desc,
                 Value = field.GetValue(ConfigManager.LoadedConfig)
             };
             allItems.Add(item);
        }

        int count = allItems.Count;
        int leftCount = count / 2;

        for (int i = 0; i < count; i++)
        {
            if (i < leftCount)
                ConfigItemsLeft.Add(allItems[i]);
            else
                ConfigItemsRight.Add(allItems[i]);
        }
    }

    private void SaveConfig()
    {
        if (SelectedConfigItem != null)
        {
            var targetField = ConfigManager.LoadedConfig.GetType().GetField(SelectedConfigItem.FieldName);
            if (targetField != null)
            {
                try {
                    targetField.SetValue(ConfigManager.LoadedConfig,
                        Convert.ChangeType(ConfigValueInput, targetField.FieldType));
                    ConfigManager.SaveConfig(ConfigManager.LoadedConfig);
                    SelectedConfigItem.Value = ConfigValueInput;
                } catch { }
            }
        }
    }

    // Helper from original code
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
