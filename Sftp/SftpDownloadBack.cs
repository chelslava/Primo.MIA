// =============================================================================
// SftpDownloadBack.cs — активность «SFTP: Скачать файл».
// =============================================================================
using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.IO;

namespace Primo.MIA
{
    /// <summary>Активность скачивания файла с SFTP-сервера.</summary>
    public class SftpDownloadBack : SftpActivityBase<SftpDownload>
    {
        public override string GroupName { get => ActivityCategories.Sftp; protected set { } }

        protected override int sdkTimeOut
        {
            get { return ActivityTimeoutParser.ParseSecondsToMs(Prop_OperationTimeoutSeconds, 30000); }
            set { }
        }

        private string _propRemotePath;
        /// <summary>Полный удалённый путь к файлу.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_SftpRemotePath)]
        public string Prop_RemotePath { get => _propRemotePath; set { _propRemotePath = value; InvokePropertyChanged(this, nameof(Prop_RemotePath)); } }

        private string _propLocalPath;
        /// <summary>Локальный путь сохранения.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_SftpLocalPath)]
        public string Prop_LocalPath { get => _propLocalPath; set { _propLocalPath = value; InvokePropertyChanged(this, nameof(Prop_LocalPath)); } }

        private bool _propOverwrite = true;
        /// <summary>Перезаписать локальный файл, если существует.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_SftpOverwrite)]
        public bool Prop_Overwrite { get => _propOverwrite; set { _propOverwrite = value; InvokePropertyChanged(this, nameof(Prop_Overwrite)); } }

        private string _propBytesDownloaded;
        /// <summary>Количество полученных байт.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(long))]
        [System.ComponentModel.Category(ActivityStrings.Sftp_Category_Output), System.ComponentModel.DisplayName("Получено байт")]
        public string Prop_BytesDownloaded { get => _propBytesDownloaded; set { _propBytesDownloaded = value; InvokePropertyChanged(this, nameof(Prop_BytesDownloaded)); } }

        public SftpDownloadBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_SftpDownload;
            sdkComponentHelp = @"Компонент ""SFTP: Скачать файл""
Скачивает файл с SFTP-сервера в локальный файл. Аутентификация: пароль или приватный ключ.

Основные:
Хост*: [String] Адрес SFTP-сервера.
Пользователь*: [String] Имя пользователя.
Пароль: [String] Пароль (если аутентификация по паролю).
Удалённый путь*: [String] Полный путь к файлу на сервере.
Локальный путь*: [String] Локальный путь сохранения.

Настройки:
Приватный ключ: [String] Путь к ключу (OpenSSH/PEM) — альтернатива паролю.
Перезаписать: [Boolean] Перезаписать существующий локальный файл. По умолчанию True.

Выходные данные:
Получено байт: [Int64] Размер скачанного файла.";
            sdkComponentIcon = ActivityIcons.Sftp;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_Host", "Хост SFTP-сервера"),
                PropertyBuilder.Int("Prop_Port", "Порт (по умолчанию 22)"),
                PropertyBuilder.String("Prop_UserName", "Имя пользователя"),
                PropertyBuilder.String("Prop_Password", "Пароль"),
                PropertyBuilder.String("Prop_RemotePath", "Удалённый путь к файлу"),
                PropertyBuilder.FileSelector("Prop_LocalPath", "Локальный путь сохранения"),
                PropertyBuilder.BooleanObject("Prop_Overwrite", "Перезаписать при существовании"),
                PropertyBuilder.Variable<long>("Prop_BytesDownloaded", "Получено байт")
            };
            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var remotePath = GetPropertyValue<string>(this.Prop_RemotePath, nameof(Prop_RemotePath), sd);
                var localPath = GetPropertyValue<string>(this.Prop_LocalPath, nameof(Prop_LocalPath), sd);

                var logic = new SftpLogic();
                remotePath = logic.NormalizeRemotePath(remotePath);

                if (string.IsNullOrWhiteSpace(localPath))
                    return new ExecutionResult { IsSuccess = false, ErrorMessage = ActivityStrings.Error_SftpLocalPathRequired };
                if (!this.Prop_Overwrite && File.Exists(localPath))
                    return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Локальный файл уже существует: {localPath}" };

                return ExecuteSftp(sd, client =>
                {
                    var directory = Path.GetDirectoryName(Path.GetFullPath(localPath));
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                        Directory.CreateDirectory(directory);

                    using (var stream = File.Create(localPath))
                    {
                        client.DownloadFile(remotePath, stream);
                        SetVariableValue(this.Prop_BytesDownloaded, stream.Length, sd);
                    }
                    return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Файл скачан: {remotePath} → {localPath}" };
                });
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка скачивания: {ex.Message}" };
            }
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_Host, ActivityStrings.Field_SftpHost, ActivityStrings.Error_SftpHostRequired);
            ret.ValidateRequired(this.Prop_RemotePath, ActivityStrings.Field_SftpRemotePath, ActivityStrings.Error_SftpRemotePathRequired);
            ret.ValidateRequired(this.Prop_LocalPath, ActivityStrings.Field_SftpLocalPath, ActivityStrings.Error_SftpLocalPathRequired);
            return ret;
        }
    }
}
