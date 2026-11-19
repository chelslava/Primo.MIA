// =============================================================================
// LogSettingsBack.cs — активность «Лог: Настройки».
//
// Единая точка управления общим логированием MIA: уровень детализации
// (Debug/Info/None) и файловый лог (ActivityFileLogger). Ставится в начало
// сценария; настройка действует на все последующие активности процесса
// (один робот на процесс — ограничение Primo Studio, см. AGENTS.md).
//
// Уровни: Debug — вся диагностика (параметры, тайминги, решения);
// Info — ключевые события (запуск/завершение, счётчики) — по умолчанию;
// None — логирование выключено. Ошибки в консоль Studio попадают и без
// лога — через ExecutionException.
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Активность настройки логирования MIA.</summary>
    public class LogSettingsBack : PrimoComponentTO<LogSettings>
    {
        public override string GroupName { get => ActivityCategories.Utilities; protected set { } }

        protected override int sdkTimeOut { get => 10000; set { } }

        private ActivityLogLevel _propLevel = ActivityLogLevel.Info;
        /// <summary>Уровень логирования (Debug/Info/None).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_LogLevel)]
        public ActivityLogLevel Prop_Level
        {
            get => _propLevel;
            set { _propLevel = value; InvokePropertyChanged(this, nameof(Prop_Level)); }
        }

        private bool _propFileLog;
        /// <summary>Дублировать лог в файл.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_LogFileEnabled)]
        public bool Prop_FileLog
        {
            get => _propFileLog;
            set { _propFileLog = value; InvokePropertyChanged(this, nameof(Prop_FileLog)); }
        }

        private string _propLogDirectory;
        /// <summary>Каталог логов (пусто — по умолчанию %LOCALAPPDATA%).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_LogDirectory)]
        public string Prop_LogDirectory
        {
            get => _propLogDirectory;
            set { _propLogDirectory = value; InvokePropertyChanged(this, nameof(Prop_LogDirectory)); }
        }

        public LogSettingsBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_LogSettings;
            sdkComponentHelp = @"Компонент ""Лог: Настройки""
Настройка общего логирования активностей MIA. Ставится в начало сценария; действует на все активности процесса.

Основные:
Уровень логирования: [Enum] Debug — вся диагностика (параметры запросов, критерии, тайминги, решения); Info — ключевые события (по умолчанию); None — логирование выключено.
Уровень также фильтрует активность «Логирование»: None — сообщения не пишутся; Info — скрываются сообщения «Отладка»; Debug — пишутся все.

Настройки:
Файловый лог: [Bool] Дублировать сообщения в файл (по умолчанию также включается переменной окружения PRIMO_MIA_LOG_FILE=1).
Директория логов: [String] Каталог файла лога; пусто — %LOCALAPPDATA%\Primo.MIA\logs (файл на календарный день, ротация 10 МБ).

Переменная окружения PRIMO_MIA_LOG_LEVEL (debug/info/none) задаёт уровень до выполнения сценария. Ошибки активностей видны в консоли Studio и без лога — через ExecutionException.";
            sdkComponentIcon = ActivityIcons.Log;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Enum<ActivityLogLevel>("Prop_Level", "Уровень логирования (Debug/Info/None)"),
                PropertyBuilder.BooleanObject("Prop_FileLog", "Файловый лог"),
                PropertyBuilder.String("Prop_LogDirectory", "Директория логов (пусто — по умолчанию)")
            };
            InitClass(container);

            this.Prop_LogDirectory = "\"\"";
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var directory = GetPropertyValue<string>(this.Prop_LogDirectory, nameof(Prop_LogDirectory), sd);
                var effective = LogSettingsLogic.Apply(this.Prop_Level, this.Prop_FileLog, directory);

                ActivityFileLogger.Info(sdkComponentName,
                    $"настройки применены: уровень {this.Prop_Level}, файловый лог {this.Prop_FileLog}, каталог {effective}");

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"Лог: {this.Prop_Level}" + (this.Prop_FileLog ? $" (файл: {effective})" : string.Empty)
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка настройки логирования: {ex.Message}" };
            }
        }
    }
}
