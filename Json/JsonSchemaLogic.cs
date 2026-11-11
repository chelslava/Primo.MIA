// =============================================================================
// JsonSchemaLogic.cs — чистая логика JSON Schema (issue #34).
//
// Validate: разбор схемы → валидация JSON (NJsonSchema, draft-07 подмножество),
// плоский список ошибок (path + kind).
// Generate: схема из примера JSON (для быстрого старта контракта).
//
// NJsonSchema 11.x API асинхронный — в активности вызывается через
// GetAwaiter().GetResult() (без SyncContext в роботе — дедлока нет,
// тот же стиль, что в HTTP-активностях).
// =============================================================================

using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Primo.MIA
{
    /// <summary>Результат валидации JSON по схеме.</summary>
    public class JsonSchemaValidationResult
    {
        /// <summary>JSON соответствует схеме.</summary>
        public bool IsValid { get; set; }

        /// <summary>Ошибки: путь (Property/Path) → описание (Kind).</summary>
        public List<(string Path, string Error)> Errors { get; } = new List<(string, string)>();
    }

    /// <summary>
    /// Логика валидации и генерации JSON Schema.
    /// </summary>
    public class JsonSchemaLogic
    {
        /// <summary>
        /// Валидирует JSON по JSON Schema.
        /// </summary>
        /// <param name="json">Проверяемый JSON.</param>
        /// <param name="schemaJson">JSON Schema (draft-07 подмножество).</param>
        public JsonSchemaValidationResult Validate(string json, string schemaJson)
        {
            Guard.NotNullOrWhiteSpace(json, nameof(json));
            Guard.NotNullOrWhiteSpace(schemaJson, nameof(schemaJson));

            var schema = ParseSchema(schemaJson);

            var result = new JsonSchemaValidationResult();
            var errors = schema.Validate(json);

            foreach (var error in errors)
            {
                var path = !string.IsNullOrEmpty(error.Path)
                    ? error.Path
                    : (error.Property ?? string.Empty);
                result.Errors.Add((path, error.Kind.ToString()));
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        /// <summary>
        /// Таблица ошибок для выхода активности: колонки path, error.
        /// </summary>
        public DataTable ErrorsToDataTable(JsonSchemaValidationResult result)
        {
            Guard.NotNull(result, nameof(result));

            var table = new DataTable("SchemaErrors");
            table.Columns.Add("path", typeof(string));
            table.Columns.Add("error", typeof(string));
            foreach (var (path, error) in result.Errors)
                table.Rows.Add(path, error);
            return table;
        }

        /// <summary>
        /// Генерирует JSON Schema из примера JSON.
        /// </summary>
        /// <param name="sampleJson">Пример валидного JSON.</param>
        /// <param name="title">Заголовок схемы (опционально).</param>
        /// <param name="prettyPrint">Форматировать с отступами.</param>
        public string Generate(string sampleJson, string title, bool prettyPrint)
        {
            Guard.NotNullOrWhiteSpace(sampleJson, nameof(sampleJson));

            var schema = NJsonSchema.JsonSchema.FromJsonAsync(sampleJson)
                .GetAwaiter().GetResult();

            if (!string.IsNullOrWhiteSpace(title))
                schema.Title = title;

            return prettyPrint
                ? schema.ToJson()
                : schema.ToJson().Replace("\r\n", "").Replace("\n", "").Replace("  ", "");
        }

        /// <summary>
        /// Разбирает JSON Schema с понятной ошибкой при некорректном документе.
        /// FromSchemaAsync в 11.6.2 отсутствует — FromJsonAsync парсит и
        /// документы схем.
        /// </summary>
        private NJsonSchema.JsonSchema ParseSchema(string schemaJson)
        {
            try
            {
                return NJsonSchema.JsonSchema.FromJsonAsync(schemaJson)
                    .GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Некорректная JSON Schema: {ex.Message}", ex);
            }
        }
    }
}
