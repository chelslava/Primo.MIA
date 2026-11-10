// =============================================================================
// FtpsDownloadBack.cs — активность «FTPS: Скачать файл» (issue #31).
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Активность скачивания файла с FTPS-сервера.</summary>
    public class FtpsDownloadBack : FtpsActivityBase<FtpsDownload>
    {
        public override string GroupName { get => ActivityCategories.Ftp; protected set { } }

        protected override int sdkTimeOut { get => 300000; set { } }

        private string _propRemotePath;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_FtpRemotePath)]
        /// <summary>Удалённый путь к файлу.</summary>
        public string Prop_RemotePath
        {
            get => _propRemotePath;
            set { _propRemotePath = value; InvokePropertyChanged(this, nameof(Prop_RemotePath)); }
        }

        private string _propLocalPath;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_FtpLocalPath)]
        /// <summary>Локальный путь сохранения.</summary>
        public string Prop_LocalPath
        {
            get => _propLocalPath;
            set { _propLocalPath = value; InvokePropertyChanged(this, nameof(Prop_LocalPath)); }
        }

        private bool _propOverwrite = true;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_FtpOverwrite)]
        /// <summary>Перезаписать локальный файл при существовании.</summary>
        public bool Prop_Overwrite
        {
            get => _propOverwrite;
            set { _propOverwrite = value; InvokePropertyChanged(this, nameof(Prop_Overwrite)); }
        }

        private string _propBytesDownloaded;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(long))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_FtpBytesDownloaded)]
        /// <summary>Получено байт (имя выходной переменной).</summary>
        public string Prop_BytesDownloaded
        {
            get => _propBytesDownloaded;
            set { _propBytesDownloaded = value; InvokePropertyChanged(this, nameof(Prop_BytesDownloaded)); }
        }

        public FtpsDownloadBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_FtpsDownload;
            sdkComponentHelp = @"Компонент ""FTPS: Скачать файл""
Скачивает файл с FTPS-сервера (explicit TLS).

Основные:
Хост*: [String] Адрес FTPS-сервера.
Порт: [Int32] По умолчанию 21.
Пользователь: [String] Пусто = anonymous.
Пароль: [String]
Удалённый путь*: [String] Путь к файлу на сервере.
Локальный путь*: [String] Куда сохранить.

Настройки:
Перезаписать: [Boolean] Заменить существующий локальный файл (по умолчанию да).
Таймауты подключения/операции: [Int32] сек.

Выходные данные:
Получено байт: [Int64].";
            sdkComponentIcon = ActivityIcons.Ftp;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>(ConnectionProperties())
            {
                PropertyBuilder.String("Prop_RemotePath", "Удалённый путь к файлу"),
                PropertyBuilder.FileSelector("Prop_LocalPath", "Локальный путь сохранения"),
                PropertyBuilder.BooleanObject("Prop_Overwrite", "Перезаписать локальный файл"),
                PropertyBuilder.Variable<long>("Prop_BytesDownloaded", "Получено байт")
            };

            InitClass(container);

            this.Prop_RemotePath = "\"\"";
            this.Prop_LocalPath = "\"\"";
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            return ExecuteFtps(sd, (settings, logic) =>
            {
                var remotePath = GetPropertyValue<string>(this.Prop_RemotePath, nameof(Prop_RemotePath), sd);
                var localPath = GetPropertyValue<string>(this.Prop_LocalPath, nameof(Prop_LocalPath), sd);

                var (_, bytes) = logic.DownloadFile(settings, remotePath, localPath, this.Prop_Overwrite);
                SetVariableValue(this.Prop_BytesDownloaded, bytes, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[FTPS] Скачано {bytes} байт → {localPath}"
                };
            });
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_Host, ActivityStrings.Field_FtpHost, ActivityStrings.Error_FtpHostRequired);
            ret.ValidateRequired(this.Prop_RemotePath, ActivityStrings.Field_FtpRemotePath, ActivityStrings.Error_FtpRemotePathRequired);
            ret.ValidateRequired(this.Prop_LocalPath, ActivityStrings.Field_FtpLocalPath, ActivityStrings.Error_FtpLocalPathRequired);
            return ret;
        }
    }
}
