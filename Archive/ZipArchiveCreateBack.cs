// =============================================================================
// ZipArchiveCreateBack.cs — активность «Архив: Создать ZIP» (issue #28).
//
// Упаковывает файлы и папки (в т.ч. по маскам) в ZIP-архив.
// System.IO.Compression — внешних зависимостей нет.
// AES-пароли стандартный .NET не поддерживает (документировано в справке).
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Активность создания ZIP-архива из файлов и папок.</summary>
    public class ZipArchiveCreateBack : PrimoComponentTO<ZipArchiveCreate>
    {
        public override string GroupName { get => ActivityCategories.Archive; protected set { } }

        protected override int sdkTimeOut { get => 60000; set { } }

        // ── Входные параметры ──────────────────────────────────────────

        private string _propSourcePaths;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Archive_Category_Input), System.ComponentModel.DisplayName(ActivityStrings.Field_ArchiveSourcePaths)]
        /// <summary>Источники: файлы/папки/маски через точку с запятой.</summary>
        public string Prop_SourcePaths
        {
            get => _propSourcePaths;
            set { _propSourcePaths = value; InvokePropertyChanged(this, nameof(Prop_SourcePaths)); }
        }

        private string _propDestinationZip;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Archive_Category_Input), System.ComponentModel.DisplayName(ActivityStrings.Field_ArchiveDestination)]
        /// <summary>Путь к создаваемому ZIP-архиву.</summary>
        public string Prop_DestinationZip
        {
            get => _propDestinationZip;
            set { _propDestinationZip = value; InvokePropertyChanged(this, nameof(Prop_DestinationZip)); }
        }

        private ArchiveCompressionLevel _propCompressionLevel = ArchiveCompressionLevel.Optimal;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Archive_Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_ArchiveCompression)]
        /// <summary>Уровень сжатия.</summary>
        public ArchiveCompressionLevel Prop_CompressionLevel
        {
            get => _propCompressionLevel;
            set { _propCompressionLevel = value; InvokePropertyChanged(this, nameof(Prop_CompressionLevel)); }
        }

        // ── Выходные параметры ─────────────────────────────────────────

        private string _propResultPath;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Archive_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_ArchiveResultPath)]
        /// <summary>Полный путь к созданному архиву.</summary>
        public string Prop_ResultPath
        {
            get => _propResultPath;
            set { _propResultPath = value; InvokePropertyChanged(this, nameof(Prop_ResultPath)); }
        }

        private string _propEntryCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Archive_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_ArchiveEntryCount)]
        /// <summary>Число файлов в архиве (имя выходной переменной).</summary>
        public string Prop_EntryCount
        {
            get => _propEntryCount;
            set { _propEntryCount = value; InvokePropertyChanged(this, nameof(Prop_EntryCount)); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public ZipArchiveCreateBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_ZipArchiveCreate;
            sdkComponentHelp = @"Компонент ""Архив: Создать ZIP""
Упаковывает файлы и папки в ZIP-архив (System.IO.Compression).

Основные:
Источники*: [String] Пути через точку с запятой: файлы, папки (рекурсивно), маски (*.tmp, report_*.pdf).
Архив (результат)*: [String] Путь к создаваемому ZIP-файлу. Существующий пересоздаётся.

Настройки:
Сжатие: [Enum] Оптимальное / Быстрое / Без сжатия / Максимальное.

Выходные данные:
Путь архива: [String] Полный путь к созданному архиву.
Файлов в архиве: [Int] Число упакованных файлов.

Ограничение: стандартный System.IO.Compression не поддерживает пароли/AES для zip.";
            sdkComponentIcon = ActivityIcons.Archive;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_SourcePaths", "Источники через точку с запятой"),
                PropertyBuilder.FileSelector("Prop_DestinationZip", "Путь к создаваемому ZIP-архиву"),
                PropertyBuilder.Enum<ArchiveCompressionLevel>("Prop_CompressionLevel", "Уровень сжатия"),
                PropertyBuilder.Variable<string>("Prop_ResultPath", "Путь к созданному архиву"),
                PropertyBuilder.Variable<int>("Prop_EntryCount", "Число файлов в архиве")
            };

            InitClass(container);

            this.Prop_SourcePaths = "\"\"";
            this.Prop_DestinationZip = "\"\"";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            var logic = new ArchiveLogic();
            try
            {
                var sources = GetPropertyValue<string>(this.Prop_SourcePaths, nameof(Prop_SourcePaths), sd);
                var destination = GetPropertyValue<string>(this.Prop_DestinationZip, nameof(Prop_DestinationZip), sd);

                var result = logic.CreateZip(
                    (sources ?? string.Empty).Split(';'),
                    destination,
                    this.Prop_CompressionLevel);

                SetVariableValue(this.Prop_ResultPath, result.ArchivePath, sd);
                SetVariableValue(this.Prop_EntryCount, result.EntryCount, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[Создать ZIP] {result.EntryCount} файлов ({result.ArchiveSizeBytes} байт) → {result.ArchivePath}"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка создания ZIP: {ex.Message}"
                };
            }
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_SourcePaths, ActivityStrings.Field_ArchiveSourcePaths, ActivityStrings.Error_ArchiveSourceRequired);
            ret.ValidateRequired(this.Prop_DestinationZip, ActivityStrings.Field_ArchiveDestination, ActivityStrings.Error_ArchiveDestinationRequired);
            return ret;
        }
    }
}
