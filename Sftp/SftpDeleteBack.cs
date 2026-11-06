// =============================================================================
// SftpDeleteBack.cs — активность «SFTP: Удалить» (файл или каталог).
// =============================================================================
using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Активность удаления файла или каталога на SFTP-сервере.</summary>
    public class SftpDeleteBack : SftpActivityBase<SftpDelete>
    {
        public override string GroupName { get => ActivityCategories.Sftp; protected set { } }

        protected override int sdkTimeOut
        {
            get { int s; return int.TryParse(Prop_OperationTimeoutSeconds, out s) && s > 0 ? s * 1000 : 30000; }
            set { }
        }

        private string _propRemotePath;
        /// <summary>Удалённый путь к файлу или каталогу.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_SftpRemotePath)]
        public string Prop_RemotePath { get => _propRemotePath; set { _propRemotePath = value; InvokePropertyChanged(this, nameof(Prop_RemotePath)); } }

        private bool _propRecursive;
        /// <summary>Удалять каталог рекурсивно (с содержимым).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_SftpRecursive)]
        public bool Prop_Recursive { get => _propRecursive; set { _propRecursive = value; InvokePropertyChanged(this, nameof(Prop_Recursive)); } }

        public SftpDeleteBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_SftpDelete;
            sdkComponentHelp = @"Компонент ""SFTP: Удалить""
Удаляет файл или каталог на SFTP-сервере.

Основные:
Хост*: [String] Адрес SFTP-сервера.
Пользователь*: [String] Имя пользователя.
Пароль: [String] Пароль.
Удалённый путь*: [String] Путь к удаляемому файлу или каталогу.

Настройки:
Рекурсивно: [Boolean] Удалять каталог вместе с содержимым. По умолчанию False.";
            sdkComponentIcon = ActivityIcons.Sftp;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_Host", "Хост SFTP-сервера"),
                PropertyBuilder.Int("Prop_Port", "Порт (по умолчанию 22)"),
                PropertyBuilder.String("Prop_UserName", "Имя пользователя"),
                PropertyBuilder.String("Prop_Password", "Пароль"),
                PropertyBuilder.String("Prop_RemotePath", "Удалённый путь"),
                PropertyBuilder.BooleanObject("Prop_Recursive", "Рекурсивное удаление каталога")
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
                    if (!client.Exists(remotePath))
                        return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Объект не найден: {remotePath}" };

                    var attributes = client.GetAttributes(remotePath);
                    if (attributes.IsDirectory)
                    {
                        if (this.Prop_Recursive)
                            logic.DeleteDirectoryRecursive(client, remotePath);
                        else
                            client.DeleteDirectory(remotePath);
                    }
                    else
                    {
                        client.DeleteFile(remotePath);
                    }

                    return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Удалено: {remotePath}" };
                });
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка удаления: {ex.Message}" };
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
