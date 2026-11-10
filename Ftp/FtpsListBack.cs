// =============================================================================
// FtpsListBack.cs — активность «FTPS: Список файлов» (issue #31).
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Активность получения списка файлов/папок FTPS-сервера.</summary>
    public class FtpsListBack : FtpsActivityBase<FtpsList>
    {
        public override string GroupName { get => ActivityCategories.Ftp; protected set { } }

        protected override int sdkTimeOut { get => 60000; set { } }

        private string _propRemotePath;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_FtpRemotePath)]
        /// <summary>Удалённый путь (папка).</summary>
        public string Prop_RemotePath
        {
            get => _propRemotePath;
            set { _propRemotePath = value; InvokePropertyChanged(this, nameof(Prop_RemotePath)); }
        }

        private string _propResult;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_FtpListResult)]
        /// <summary>Список имён файлов/папок (имя выходной переменной).</summary>
        public string Prop_Result
        {
            get => _propResult;
            set { _propResult = value; InvokePropertyChanged(this, nameof(Prop_Result)); }
        }

        public FtpsListBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_FtpsList;
            sdkComponentHelp = @"Компонент ""FTPS: Список файлов""
Получает имена файлов и папок в удалённой папке (explicit TLS, LIST).

Основные:
Хост*: [String] Адрес FTPS-сервера.
Порт: [Int32] По умолчанию 21.
Пользователь: [String] Пусто = anonymous.
Пароль: [String]
Удалённый путь: [String] Папка; пусто = корневая (/).

Выходные данные:
Список: [List<string>] Имена записей (без «.» и «..»).";
            sdkComponentIcon = ActivityIcons.Ftp;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>(ConnectionProperties())
            {
                PropertyBuilder.String("Prop_RemotePath", "Удалённая папка; пусто = корень /"),
                PropertyBuilder.Variable<List<string>>("Prop_Result", "Список имён файлов/папок")
            };

            InitClass(container);

            this.Prop_RemotePath = "\"\"";
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            return ExecuteFtps(sd, (settings, logic) =>
            {
                var remotePath = GetPropertyValue<string>(this.Prop_RemotePath, nameof(Prop_RemotePath), sd);
                if (string.IsNullOrWhiteSpace(remotePath))
                    remotePath = "/";

                var names = logic.ListDirectory(settings, remotePath);
                SetVariableValue(this.Prop_Result, names, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[FTPS] {names.Count} записей в {remotePath}"
                };
            });
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_Host, ActivityStrings.Field_FtpHost, ActivityStrings.Error_FtpHostRequired);
            return ret;
        }
    }
}
