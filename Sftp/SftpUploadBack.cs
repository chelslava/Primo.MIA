// =============================================================================
// SftpUploadBack.cs — активность «SFTP: Загрузить файл».
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
    /// <summary>Активность загрузки локального файла на SFTP-сервер.</summary>
    public class SftpUploadBack : SftpActivityBase<SftpUpload>
    {
        public override string GroupName { get => ActivityCategories.Sftp; protected set { } }

        protected override int sdkTimeOut
        {
            get { int s; return int.TryParse(Prop_OperationTimeoutSeconds, out s) && s > 0 ? s * 1000 : 30000; }
            set { }
        }

        private string _propLocalPath;
        /// <summary>Путь к локальному файлу.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_SftpLocalPath)]
        public string Prop_LocalPath { get => _propLocalPath; set { _propLocalPath = value; InvokePropertyChanged(this, nameof(Prop_LocalPath)); } }

        private string _propRemotePath;
        /// <summary>Полный удалённый путь назначения (включая имя файла).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_SftpRemotePath)]
        public string Prop_RemotePath { get => _propRemotePath; set { _propRemotePath = value; InvokePropertyChanged(this, nameof(Prop_RemotePath)); } }

        private bool _propOverwrite = true;
        /// <summary>Перезаписать файл, если существует.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_SftpOverwrite)]
        public bool Prop_Overwrite { get => _propOverwrite; set { _propOverwrite = value; InvokePropertyChanged(this, nameof(Prop_Overwrite)); } }

        private string _propBytesUploaded;
        /// <summary>Количество переданных байт.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(long))]
        [System.ComponentModel.Category(ActivityStrings.Sftp_Category_Output), System.ComponentModel.DisplayName("Передано байт")]
        public string Prop_BytesUploaded { get => _propBytesUploaded; set { _propBytesUploaded = value; InvokePropertyChanged(this, nameof(Prop_BytesUploaded)); } }

        public SftpUploadBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_SftpUpload;
            sdkComponentHelp = @"Компонент ""SFTP: Загрузить файл""
Загружает локальный файл на SFTP-сервер (SSH File Transfer Protocol). Аутентификация: пароль или приватный ключ.

Основные:
Хост*: [String] Адрес SFTP-сервера.
Порт: [Int32] По умолчанию 22.
Пользователь*: [String] Имя пользователя.
Пароль: [String] Пароль (если аутентификация по паролю).
Локальный путь*: [String] Путь к загружаемому файлу.
Удалённый путь*: [String] Полный путь назначения (включая имя файла).

Настройки:
Приватный ключ: [String] Путь к ключу (OpenSSH/PEM) — альтернатива паролю.
Перезаписать: [Boolean] Перезаписать существующий файл. По умолчанию True.

Выходные данные:
Передано байт: [Int64] Размер загруженного файла.";
            sdkComponentIcon = ActivityIcons.Sftp;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_Host", "Хост SFTP-сервера"),
                PropertyBuilder.Int("Prop_Port", "Порт (по умолчанию 22)"),
                PropertyBuilder.String("Prop_UserName", "Имя пользователя"),
                PropertyBuilder.String("Prop_Password", "Пароль"),
                PropertyBuilder.FileSelector("Prop_LocalPath", "Локальный файл"),
                PropertyBuilder.String("Prop_RemotePath", "Удалённый путь назначения"),
                PropertyBuilder.BooleanObject("Prop_Overwrite", "Перезаписать при существовании"),
                PropertyBuilder.Variable<long>("Prop_BytesUploaded", "Передано байт")
            };
            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var localPath = GetPropertyValue<string>(this.Prop_LocalPath, nameof(Prop_LocalPath), sd);
                var remotePath = GetPropertyValue<string>(this.Prop_RemotePath, nameof(Prop_RemotePath), sd);

                var logic = new SftpLogic();
                remotePath = logic.NormalizeRemotePath(remotePath);

                if (string.IsNullOrWhiteSpace(localPath))
                    return new ExecutionResult { IsSuccess = false, ErrorMessage = ActivityStrings.Error_SftpLocalPathRequired };
                if (!File.Exists(localPath))
                    return new ExecutionResult { IsSuccess = false, ErrorMessage = $"{ActivityStrings.Error_CryptoFileNotFound}: {localPath}" };

                return ExecuteSftp(sd, client =>
                {
                    if (this.Prop_Overwrite && client.Exists(remotePath))
                        client.DeleteFile(remotePath);

                    using (var stream = File.OpenRead(localPath))
                    {
                        client.UploadFile(stream, remotePath);
                        SetVariableValue(this.Prop_BytesUploaded, stream.Length, sd);
                    }
                    return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Файл загружен: {remotePath}" };
                });
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка загрузки: {ex.Message}" };
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
