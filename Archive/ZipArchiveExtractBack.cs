// =============================================================================
// ZipArchiveExtractBack.cs — активность «Архив: Распаковать ZIP» (issue #28).
//
// Распаковка с политикой перезаписи (Пропускать/Перезаписывать/Ошибка),
// опциональной маской файлов и защитой от Zip Slip.
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Активность распаковки ZIP-архива.</summary>
    public class ZipArchiveExtractBack : PrimoComponentTO<ZipArchiveExtract>
    {
        public override string GroupName { get => ActivityCategories.Archive; protected set { } }

        protected override int sdkTimeOut { get => 60000; set { } }

        // ── Входные параметры ──────────────────────────────────────────

        private string _propZipPath;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Archive_Category_Input), System.ComponentModel.DisplayName(ActivityStrings.Field_ArchiveZipPath)]
        /// <summary>Путь к ZIP-архиву.</summary>
        public string Prop_ZipPath
        {
            get => _propZipPath;
            set { _propZipPath = value; InvokePropertyChanged(this, nameof(Prop_ZipPath)); }
        }

        private string _propDestinationFolder;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Archive_Category_Input), System.ComponentModel.DisplayName(ActivityStrings.Field_ArchiveExtractFolder)]
        /// <summary>Папка распаковки (создаётся при отсутствии).</summary>
        public string Prop_DestinationFolder
        {
            get => _propDestinationFolder;
            set { _propDestinationFolder = value; InvokePropertyChanged(this, nameof(Prop_DestinationFolder)); }
        }

        private ArchiveOverwritePolicy _propOverwritePolicy = ArchiveOverwritePolicy.Skip;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Archive_Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_ArchiveOverwritePolicy)]
        /// <summary>Политика перезаписи существующих файлов.</summary>
        public ArchiveOverwritePolicy Prop_OverwritePolicy
        {
            get => _propOverwritePolicy;
            set { _propOverwritePolicy = value; InvokePropertyChanged(this, nameof(Prop_OverwritePolicy)); }
        }

        private string _propFileMask;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Archive_Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_ArchiveFileMask)]
        /// <summary>Маска извлекаемых файлов (* и ?); пусто = все.</summary>
        public string Prop_FileMask
        {
            get => _propFileMask;
            set { _propFileMask = value; InvokePropertyChanged(this, nameof(Prop_FileMask)); }
        }

        // ── Выходные параметры ─────────────────────────────────────────

        private string _propExtractedCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Archive_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_ArchiveExtractedCount)]
        /// <summary>Число извлечённых файлов (имя выходной переменной).</summary>
        public string Prop_ExtractedCount
        {
            get => _propExtractedCount;
            set { _propExtractedCount = value; InvokePropertyChanged(this, nameof(Prop_ExtractedCount)); }
        }

        private string _propSkippedCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Archive_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_ArchiveSkippedCount)]
        /// <summary>Число пропущенных (политика «Пропускать»); имя выходной переменной.</summary>
        public string Prop_SkippedCount
        {
            get => _propSkippedCount;
            set { _propSkippedCount = value; InvokePropertyChanged(this, nameof(Prop_SkippedCount)); }
        }

        private string _propResultFolder;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Archive_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_ArchiveResultFolder)]
        /// <summary>Полный путь к папке распаковки.</summary>
        public string Prop_ResultFolder
        {
            get => _propResultFolder;
            set { _propResultFolder = value; InvokePropertyChanged(this, nameof(Prop_ResultFolder)); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public ZipArchiveExtractBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_ZipArchiveExtract;
            sdkComponentHelp = @"Компонент ""Архив: Распаковать ZIP""
Распаковывает ZIP-архив в папку (System.IO.Compression).

Основные:
Архив*: [String] Путь к ZIP-файлу.
Папка распаковки*: [String] Папка назначения (создаётся при отсутствии).

Настройки:
Перезапись: [Enum] Пропускать / Перезаписывать / Ошибка при существующем файле.
Маска файлов: [String] Извлекать только совпавшие файлы (*.pdf), пусто = все.

Выходные данные:
Извлечено: [Int] Число извлечённых файлов.
Пропущено: [Int] Число пропущенных по политике.
Папка: [String] Полный путь к папке распаковки.

Защита от Zip Slip включена: пути записей не могут выйти за пределы папки.";
            sdkComponentIcon = ActivityIcons.Archive;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.FileSelector("Prop_ZipPath", "Путь к ZIP-архиву"),
                PropertyBuilder.FolderSelector("Prop_DestinationFolder", "Папка распаковки"),
                PropertyBuilder.Enum<ArchiveOverwritePolicy>("Prop_OverwritePolicy", "Политика перезаписи"),
                PropertyBuilder.String("Prop_FileMask", "Маска файлов (* и ?), пусто = все"),
                PropertyBuilder.Variable<int>("Prop_ExtractedCount", "Число извлечённых файлов"),
                PropertyBuilder.Variable<int>("Prop_SkippedCount", "Число пропущенных файлов"),
                PropertyBuilder.Variable<string>("Prop_ResultFolder", "Папка распаковки")
            };

            InitClass(container);

            this.Prop_ZipPath = "\"\"";
            this.Prop_DestinationFolder = "\"\"";
            this.Prop_FileMask = "\"\"";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            var logic = new ArchiveLogic();
            try
            {
                var zipPath = GetPropertyValue<string>(this.Prop_ZipPath, nameof(Prop_ZipPath), sd);
                var destination = GetPropertyValue<string>(this.Prop_DestinationFolder, nameof(Prop_DestinationFolder), sd);
                var mask = GetPropertyValue<string>(this.Prop_FileMask, nameof(Prop_FileMask), sd);

                var result = logic.ExtractZip(zipPath, destination, this.Prop_OverwritePolicy, mask);

                SetVariableValue(this.Prop_ExtractedCount, result.ExtractedCount, sd);
                SetVariableValue(this.Prop_SkippedCount, result.SkippedCount, sd);
                SetVariableValue(this.Prop_ResultFolder, result.DestinationFolder, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[Распаковать ZIP] {result.ExtractedCount} файлов (пропущено {result.SkippedCount}) → {result.DestinationFolder}"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка распаковки ZIP: {ex.Message}"
                };
            }
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_ZipPath, ActivityStrings.Field_ArchiveZipPath, ActivityStrings.Error_ArchiveZipRequired);
            ret.ValidateRequired(this.Prop_DestinationFolder, ActivityStrings.Field_ArchiveExtractFolder, ActivityStrings.Error_ArchiveFolderRequired);
            return ret;
        }
    }
}
