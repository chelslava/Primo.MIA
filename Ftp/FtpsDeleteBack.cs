// =============================================================================
// FtpsDeleteBack.cs — активность «FTPS: Удалить файл» (issue #31).
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Активность удаления файла на FTPS-сервере.</summary>
    public class FtpsDeleteBack : FtpsActivityBase<FtpsDelete>
    {
        public override string GroupName { get => ActivityCategories.Ftp; protected set { } }

        protected override int sdkTimeOut { get => 60000; set { } }

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

        public FtpsDeleteBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_FtpsDelete;
            sdkComponentHelp = @"Компонент ""FTPS: Удалить файл""
Удаляет файл на FTPS-сервере (explicit TLS). Операция необратима.

Основные:
Хост*: [String] Адрес FTPS-сервера.
Порт: [Int32] По умолчанию 21.
Пользователь: [String] Пусто = anonymous.
Пароль: [String]
Удалённый путь*: [String] Путь к удаляемому файлу.";
            sdkComponentIcon = ActivityIcons.Ftp;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>(ConnectionProperties())
            {
                PropertyBuilder.String("Prop_RemotePath", "Удалённый путь к удаляемому файлу")
            };

            InitClass(container);

            this.Prop_RemotePath = "\"\"";
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            return ExecuteFtps(sd, (settings, logic) =>
            {
                var remotePath = GetPropertyValue<string>(this.Prop_RemotePath, nameof(Prop_RemotePath), sd);

                logic.DeleteFile(settings, remotePath);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[FTPS] Удалён {remotePath}"
                };
            });
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_Host, ActivityStrings.Field_FtpHost, ActivityStrings.Error_FtpHostRequired);
            ret.ValidateRequired(this.Prop_RemotePath, ActivityStrings.Field_FtpRemotePath, ActivityStrings.Error_FtpRemotePathRequired);
            return ret;
        }
    }
}
