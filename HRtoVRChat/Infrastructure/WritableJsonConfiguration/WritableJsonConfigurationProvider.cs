using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration.Json;

namespace WritableJsonConfiguration
{
    public class WritableJsonConfigurationProvider : JsonConfigurationProvider
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = true };

        // Конструктор класса, наследуемого от JsonConfigurationProvider
        public WritableJsonConfigurationProvider(JsonConfigurationSource source) : base(source)
        {
        }

        // Метод для сохранения JSON-объекта в файл
        private void Save(JsonNode jsonObj)
        {
            // Получаем полный путь к файлу конфигурации
            var path = base.Source.Path ?? throw new InvalidOperationException("Source path is not set.");
            var fileProvider = base.Source.FileProvider ?? throw new InvalidOperationException("File provider is not set.");
            var fileFullPath = fileProvider.GetFileInfo(path).PhysicalPath ?? throw new InvalidOperationException("Physical path could not be determined.");

            // Сериализуем объект в форматированную JSON-строку
            string output = jsonObj.ToJsonString(JsonSerializerOptions);
            // Записываем строку в файл, перезаписывая его содержимое
            File.WriteAllText(fileFullPath, output);
        }

        // Установка значения по ключу в JSON-объекте
        private void SetValue(string key, string? value, JsonNode jsonObj)
        {
            // Вызов базового метода Set для установки значения
            base.Set(key, value);
            // Разделение ключа на части для навигации по структуре JSON
            var split = key.Split(':');
            var context = jsonObj;
            for (int i = 0; i < split.Length; i++)
            {
                var currentKey = split[i];
                if (i < split.Length - 1) // Если не последний элемент пути, обрабатываем вложенные объекты или массивы
                {
                    var child = GetChild(context, currentKey);
                    if (child == null) // Если вложенный объект или массив не существует, создаем его
                    {
                        JsonNode newNode = (i + 1 < split.Length && int.TryParse(split[i + 1], out _))
                            ? (JsonNode)new JsonArray()
                            : (JsonNode)new JsonObject();

                        SetChild(context, currentKey, newNode);
                        child = newNode;
                    }
                    context = child;
                }
                else // Если последний элемент пути, устанавливаем значение
                {
                    SetChild(context, currentKey, JsonValue.Create(value));
                }
            }
        }

        private JsonNode? GetChild(JsonNode context, string key)
        {
            if (context is JsonObject obj) return obj[key];
            if (context is JsonArray arr && int.TryParse(key, out var idx) && idx >= 0 && idx < arr.Count) return arr[idx];
            return null;
        }

        private void SetChild(JsonNode context, string key, JsonNode? value)
        {
            if (context is JsonObject obj)
            {
                obj[key] = value;
            }
            else if (context is JsonArray arr && int.TryParse(key, out var idx) && idx >= 0)
            {
                while (arr.Count <= idx) arr.Add(null);
                arr[idx] = value;
            }
        }

        // Получение JSON-объекта из файла конфигурации
        private JsonNode GetJsonObj()
        {
            // Получаем полный путь к файлу конфигурации
            var path = base.Source.Path ?? throw new InvalidOperationException("Source path is not set.");
            var fileProvider = base.Source.FileProvider ?? throw new InvalidOperationException("File provider is not set.");
            var fileFullPath = fileProvider.GetFileInfo(path).PhysicalPath ?? throw new InvalidOperationException("Physical path could not be determined.");

            // Читаем содержимое файла, если файл существует, иначе используем пустой объект
            var json = File.Exists(fileFullPath) ? File.ReadAllText(fileFullPath) : "{}";
            // Десериализуем JSON-строку в объект
            return JsonNode.Parse(json) ?? new JsonObject();
        }

        // Переопределение метода Set для установки значения по ключу
        public override void Set(string key, string? value)
        {
            var jsonObj = GetJsonObj(); // Получаем текущий JSON-объект
            SetValue(key, value, jsonObj); // Устанавливаем значение
            Save(jsonObj); // Сохраняем изменения в файл
            OnReload();
        }

        // Перегрузка метода Set для установки значения любого типа
        public void Set(string key, object value)
        {
            var jsonObj = GetJsonObj(); // Получаем текущий JSON-объект
            var serialized = JsonSerializer.Serialize(value); // Сериализуем значение
            var jsonNode = JsonNode.Parse(serialized) ?? JsonValue.Create(value); // Преобразуем сериализованное значение в JsonNode
            WalkAndSet(key, jsonNode, jsonObj); // Рекурсивно устанавливаем значение в JSON-объект
            Save(jsonObj); // Сохраняем изменения в файл
            OnReload();
        }

        // Рекурсивный метод для установки значения в JSON-объект
        private void WalkAndSet(string key, JsonNode? value, JsonNode jsonObj)
        {
            if (value is JsonArray jArray) // Обработка массива значений
            {
                for (int index = 0; index < jArray.Count; index++)
                {
                    var currentKey = $"{key}:{index}"; // Генерация ключа для элемента массива
                    var elementValue = jArray[index]; // Получение элемента массива
                    if (elementValue != null)
                        WalkAndSet(currentKey, elementValue, jsonObj); // Рекурсивный вызов
                }
            }
            else if (value is JsonObject jObject) // Обработка объекта
            {
                foreach (var property in jObject) // Перебор свойств объекта
                {
                    var propName = property.Key; // Имя свойства
                    var currentKey = key == null ? propName : $"{key}:{propName}"; // Генерация ключа для свойства
                    var propValue = property.Value; // Получение значения свойства
                    if (propValue != null)
                        WalkAndSet(currentKey, propValue, jsonObj); // Рекурсивный вызов
                }
            }
            else // Обработка примитивного значения
            {
                SetValue(key, value?.ToString(), jsonObj); // Установка значения
            }
        }
    }
}
