// =============================================================================
// SftpListBack.cs — активность «SFTP: Список файлов».
// =============================================================================
using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Data;

namespace Primo.MIA
{
    /// <summary>Активность получения списка файлов и каталогов SFTP-сервера.</summary>
    public class SftpListBack : SftpActivityBase<SftpList>
    {
        public override string GroupName { get => ActivityCategories.Sftp; protected set { } }

        protected override int sdkTimeOut
        {
            get { int s; return int.TryParse(Prop_OperationTimeoutSeconds, out s) && s > 0 ? s * 1000 : 30000; }
            set { }
        }

        private string _propRemotePath;
        /// <summary>Удалённая директория листинга ("." или пусто = рабочая).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_SftpRemotePath)]
        public string Prop_RemotePath { get => _propRemotePath; set { _propRemotePath = value; InvokePropertyChanged(this, nameof(Prop_RemotePath)); } }

        private string _propEntries;
        /// <summary>DataTable записей: Name, Path, Type, SizeBytes, Modified.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DataTable))]
        [System.ComponentModel.Category(ActivityStrings.Sftp_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_SftpEntries)]
        public string Prop_Entries { get => _propEntries; set { _propEntries = value; InvokePropertyChanged(this, nameof(Prop_Entries)); } }

        private string _propNames;
        /// <summary>Список имён (без каталогов "." и "..").</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Sftp_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_SftpNames)]
        public string Prop_Names { get => _propNames; set { _propNames = value; InvokePropertyChanged(this, nameof(Prop_Names)); } }

        private string _propCount;
        /// <summary>Количество записей.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Sftp_Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_SftpCount)]
        public string Prop_Count { get => _propCount; set { _propCount = value; InvokePropertyChanged(this, nameof(Prop_Count)); } }

        public SftpListBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_SftpList;
            sdkComponentHelp = @"Компонент ""SFTP: Список файлов""
Возвращает список файлов и каталогов указанной директории SFTP-сервера. Записи отсортированы: каталоги первыми, затем по имени.

Основные:
Хост*: [String] Адрес SFTP-сервера.
Пользователь*: [String] Имя пользователя.
Пароль: [String] Пароль.
Удалённый путь: [String] Директория листинга; ""."" или пусто — рабочая директория.

Выходные данные:
Записи: [DataTable] Колонки: Name, Path, Type (Каталог/Файл), SizeBytes, Modified.
Имена: [List<String>] Имена записей.
Количество: [Int32] Число записей.";
            sdkComponentIcon = ActivityIcons.Sftp;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_Host", "Хост SFTP-сервера"),
                PropertyBuilder.Int("Prop_Port", "Порт (по умолчанию 22)"),
                PropertyBuilder.String("Prop_UserName", "Имя пользователя"),
                PropertyBuilder.String("Prop_Password", "Пароль"),
                PropertyBuilder.String("Prop_RemotePath", "Удалённая директория"),
                PropertyBuilder.Variable<DataTable>("Prop_Entries", "Записи (DataTable)"),
                PropertyBuilder.Variable<List<string>>("Prop_Names", "Имена записей"),
                PropertyBuilder.Variable<int>("Prop_Count", "Количество записей")
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
                    var entries = logic.OrderEntries(logic.MapListing(client.ListDirectory(remotePath)));

                    SetVariableValue(this.Prop_Entries, BuildEntriesTable(entries), sd);
                    SetVariableValue(this.Prop_Names, entries.ConvertAll(e => e.Name), sd);
                    SetVariableValue(this.Prop_Count, entries.Count, sd);

                    return new ExecutionResult { IsSuccess = true, SuccessMessage = $"Получено записей: {entries.Count}" };
                });
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка листинга: {ex.Message}" };
            }
        }

        /// <summary>
        /// Формирует DataTable из записей (колонки Name, Path, Type, SizeBytes, Modified).
        /// </summary>
        public static DataTable BuildEntriesTable(List<SftpEntry> entries)
        {
            var table = new DataTable("SftpEntries");
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Path", typeof(string));
            table.Columns.Add("Type", typeof(string));
            table.Columns.Add("SizeBytes", typeof(long));
            table.Columns.Add("Modified", typeof(DateTime));

            foreach (var entry in entries ?? new List<SftpEntry>())
                table.Rows.Add(entry.Name, entry.FullPath, entry.TypeDisplay, entry.SizeBytes, entry.ModifiedAt);

            return table;
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
            ret.ValidateRequired(this.Prop_Host, ActivityStrings.Field_SftpHost, ActivityStrings.Error_SftpHostRequired);
            return ret;
        }
    }
}
