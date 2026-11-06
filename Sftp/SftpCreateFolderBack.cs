// =============================================================================
// SftpCreateFolderBack.cs — активность «SFTP: Создать папку».
// =============================================================================
using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Активность создания каталога на SFTP-сервере.</summary>
    public class SftpCreateFolderBack : SftpActivityBase<SftpCreateFolder>
    {
        public override string GroupName { get => ActivityCategories.Sftp; protected set { } }

        protected override int sdkTimeOut
        {
            get { return ActivityTimeoutParser.ParseSecondsToMs(Prop_OperationTimeoutSeconds, 30000); }
            set { }
        }

        private string _propRemotePath;
        /// <summary>Путь создаваемого каталога.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_SftpRemotePath)]
        public string Prop_RemotePath { get => _propRemotePath; set { _propRemotePath = value; InvokePropertyChanged(this, nameof(Prop_RemotePath)); } }

        public SftpCreateFolderBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_SftpCreateFolder;
            sdkComponentHelp = @"Компонент ""SFTP: Создать папку""
Создаёт каталог на SFTP-сервере. Если каталог уже существует — операция считается успешной (idempotent).

Основные:
Хост*: [String] Адрес SFTP-сервера.
Пользователь*: [String] Имя пользователя.
Пароль: [String] Пароль.
Удалённый путь*: [String] Путь создаваемого каталога.";
            sdkComponentIcon = ActivityIcons.Sftp;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_Host", "Хост SFTP-сервера"),
                PropertyBuilder.Int("Prop_Port", "Порт (по умолчанию 22)"),
                PropertyBuilder.String("Prop_UserName", "Имя пользователя"),
                PropertyBuilder.String("Prop_Password", "Пароль"),
                PropertyBuilder.String("Prop_RemotePath", "Путь создаваемого каталога")
            };
            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var remotePath = GetPropertyValue<string>(this.Prop_RemotePath, nameof(Prop_RemotePath), sd);
                var logic = new SftpLogic();
                remotePath = logic.NormalizeRemotePath(remotePath);

                return ExecuteSftp(sd, client =>
                {
                    if (client.Exists(remotePath))
                        return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Каталог уже существует: {remotePath}" };

                    client.CreateDirectory(remotePath);
                    return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Каталог создан: {remotePath}" };
                });
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка создания каталога: {ex.Message}" };
            }
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_Host, ActivityStrings.Field_SftpHost, ActivityStrings.Error_SftpHostRequired);
            ret.ValidateRequired(this.Prop_RemotePath, ActivityStrings.Field_SftpRemotePath, ActivityStrings.Error_SftpRemotePathRequired);
            return ret;
        }
    }
}
