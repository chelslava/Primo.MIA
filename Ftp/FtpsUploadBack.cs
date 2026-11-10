// =============================================================================
// FtpsUploadBack.cs — активность «FTPS: Загрузить файл» (issue #31).
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Активность загрузки файла на FTPS-сервер.</summary>
    public class FtpsUploadBack : FtpsActivityBase<FtpsUpload>
    {
        public override string GroupName { get => ActivityCategories.Ftp; protected set { } }

        protected override int sdkTimeOut { get => 300000; set { } }

        private string _propLocalPath;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_FtpLocalPath)]
        /// <summary>Локальный файл для загрузки.</summary>
        public string Prop_LocalPath
        {
            get => _propLocalPath;
            set { _propLocalPath = value; InvokePropertyChanged(this, nameof(Prop_LocalPath)); }
        }

        private string _propRemotePath;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_FtpRemotePath)]
        /// <summary>Удалённый путь назначения (включая имя файла).</summary>
        public string Prop_RemotePath
        {
            get => _propRemotePath;
            set { _propRemotePath = value; InvokePropertyChanged(this, nameof(Prop_RemotePath)); }
        }

        private string _propBytesUploaded;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(long))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_FtpBytesUploaded)]
        /// <summary>Передано байт (имя выходной переменной).</summary>
        public string Prop_BytesUploaded
        {
            get => _propBytesUploaded;
            set { _propBytesUploaded = value; InvokePropertyChanged(this, nameof(Prop_BytesUploaded)); }
        }

        public FtpsUploadBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_FtpsUpload;
            sdkComponentHelp = @"Компонент ""FTPS: Загрузить файл""
Загружает локальный файл на FTPS-сервер (explicit TLS).

Основные:
Хост*: [String] Адрес FTPS-сервера.
Порт: [Int32] По умолчанию 21.
Пользователь: [String] Пусто = anonymous.
Пароль: [String]
Локальный путь*: [String] Файл для загрузки.
Удалённый путь*: [String] Путь назначения (включая имя файла).

Настройки:
Таймауты подключения/операции: [Int32] сек.

Выходные данные:
Передано байт: [Int64] Размер файла.

TLS: explicit (AUTH TLS), пассивный режим, binary. Сертификаты — стандартная валидация (fail-closed).";
            sdkComponentIcon = ActivityIcons.Ftp;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>(ConnectionProperties())
            {
                PropertyBuilder.FileSelector("Prop_LocalPath", "Локальный файл"),
                PropertyBuilder.String("Prop_RemotePath", "Удалённый путь назначения"),
                PropertyBuilder.Variable<long>("Prop_BytesUploaded", "Передано байт")
            };

            InitClass(container);

            this.Prop_LocalPath = "\"\"";
            this.Prop_RemotePath = "\"\"";
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            return ExecuteFtps(sd, (settings, logic) =>
            {
                var localPath = GetPropertyValue<string>(this.Prop_LocalPath, nameof(Prop_LocalPath), sd);
                var remotePath = GetPropertyValue<string>(this.Prop_RemotePath, nameof(Prop_RemotePath), sd);

                var bytes = logic.UploadFile(settings, localPath, remotePath);
                SetVariableValue(this.Prop_BytesUploaded, bytes, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[FTPS] Загружено {bytes} байт → {remotePath}"
                };
            });
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_Host, ActivityStrings.Field_FtpHost, ActivityStrings.Error_FtpHostRequired);
            ret.ValidateRequired(this.Prop_LocalPath, ActivityStrings.Field_FtpLocalPath, ActivityStrings.Error_FtpLocalPathRequired);
            ret.ValidateRequired(this.Prop_RemotePath, ActivityStrings.Field_FtpRemotePath, ActivityStrings.Error_FtpRemotePathRequired);
            return ret;
        }
    }
}
