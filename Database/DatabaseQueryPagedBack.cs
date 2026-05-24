// =============================================================================
// DatabaseQueryPagedBack.cs — активность «Database: Постраничный запрос (QueryPaged)».
// Выполняет SQL-запрос с пагинацией, возвращая одну страницу результата и метаданные навигации.
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
    /// <summary>Активность для постраничного чтения данных из БД.</summary>
    public class DatabaseQueryPagedBack : PrimoComponentTO<DatabaseQueryPaged>
    {
        public override string GroupName { get => ActivityCategories.Database; protected set { } }

        protected override int sdkTimeOut
        {
            get
            {
                int seconds;
                return int.TryParse(Prop_CommandTimeoutSeconds, out seconds) && seconds > 0
                    ? seconds * 1000
                    : 30000;
            }
            set { }
        }

        private string _propProviderInvariantName = "\"" + DatabaseHelper.DefaultProviderInvariantName + "\"";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_DbProviderInvariantName)]
        /// <summary>Инвариантное имя ADO.NET провайдера (например, System.Data.SqlClient).</summary>
        public string Prop_ProviderInvariantName
        {
            get => _propProviderInvariantName;
            set { _propProviderInvariantName = value; InvokePropertyChanged(this, nameof(Prop_ProviderInvariantName)); }
        }

        private string _propConnectionString;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_ConnectionString)]
        /// <summary>Строка подключения к базе данных.</summary>
        public string Prop_ConnectionString
        {
            get => _propConnectionString;
            set { _propConnectionString = value; InvokePropertyChanged(this, nameof(Prop_ConnectionString)); }
        }

        private string _propTransactionId;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_TransactionId)]
        /// <summary>Идентификатор активной транзакции; если не задан, используется ambient-контекст или прямое соединение.</summary>
        public string Prop_TransactionId
        {
            get => _propTransactionId;
            set { _propTransactionId = value; InvokePropertyChanged(this, nameof(Prop_TransactionId)); }
        }

        private string _propCommandText;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_CommandText)]
        /// <summary>Исходный SQL-запрос, используемый как подзапрос для пагинации.</summary>
        public string Prop_CommandText
        {
            get => _propCommandText;
            set { _propCommandText = value; InvokePropertyChanged(this, nameof(Prop_CommandText)); }
        }

        private string _propOrderByExpression;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_OrderByExpression)]
        /// <summary>Выражение ORDER BY для стабильной и воспроизводимой пагинации.</summary>
        public string Prop_OrderByExpression
        {
            get => _propOrderByExpression;
            set { _propOrderByExpression = value; InvokePropertyChanged(this, nameof(Prop_OrderByExpression)); }
        }

        private string _propPageNumber = "1";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_PageNumber)]
        /// <summary>Номер запрашиваемой страницы, начиная с 1.</summary>
        public string Prop_PageNumber
        {
            get => _propPageNumber;
            set { _propPageNumber = value; InvokePropertyChanged(this, nameof(Prop_PageNumber)); }
        }

        private string _propPageSize = "100";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_PageSize)]
        /// <summary>Максимальное количество строк на одной странице.</summary>
        public string Prop_PageSize
        {
            get => _propPageSize;
            set { _propPageSize = value; InvokePropertyChanged(this, nameof(Prop_PageSize)); }
        }

        private string _propParameters;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(Dictionary<string, string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Parameters), System.ComponentModel.DisplayName(ActivityStrings.Field_ParametersDictionary)]
        /// <summary>Словарь параметров SQL-запроса (имя → значение).</summary>
        public string Prop_Parameters
        {
            get => _propParameters;
            set { _propParameters = value; InvokePropertyChanged(this, nameof(Prop_Parameters)); }
        }

        private string _propCommandTimeoutSeconds = "30";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_CommandTimeoutSeconds)]
        /// <summary>Таймаут выполнения запроса в секундах.</summary>
        public string Prop_CommandTimeoutSeconds
        {
            get => _propCommandTimeoutSeconds;
            set { _propCommandTimeoutSeconds = value; InvokePropertyChanged(this, nameof(Prop_CommandTimeoutSeconds)); }
        }

        private string _propResultTable;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DataTable))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_ResultTable)]
        /// <summary>Страница результата в виде DataTable.</summary>
        public string Prop_ResultTable
        {
            get => _propResultTable;
            set { _propResultTable = value; InvokePropertyChanged(this, nameof(Prop_ResultTable)); }
        }

        private string _propRowCount;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_RowCount)]
        /// <summary>Количество строк на текущей странице.</summary>
        public string Prop_RowCount
        {
            get => _propRowCount;
            set { _propRowCount = value; InvokePropertyChanged(this, nameof(Prop_RowCount)); }
        }

        private string _propTotalRows;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_TotalRows)]
        /// <summary>Общее количество строк во всём результирующем наборе.</summary>
        public string Prop_TotalRows
        {
            get => _propTotalRows;
            set { _propTotalRows = value; InvokePropertyChanged(this, nameof(Prop_TotalRows)); }
        }

        private string _propTotalPages;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_TotalPages)]
        /// <summary>Общее количество страниц при заданном размере страницы.</summary>
        public string Prop_TotalPages
        {
            get => _propTotalPages;
            set { _propTotalPages = value; InvokePropertyChanged(this, nameof(Prop_TotalPages)); }
        }

        private string _propHasNextPage;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_HasNextPage)]
        /// <summary>Признак наличия следующей страницы после текущей.</summary>
        public string Prop_HasNextPage
        {
            get => _propHasNextPage;
            set { _propHasNextPage = value; InvokePropertyChanged(this, nameof(Prop_HasNextPage)); }
        }

        private string _propHasPreviousPage;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_HasPreviousPage)]
        /// <summary>Признак наличия предыдущей страницы перед текущей.</summary>
        public string Prop_HasPreviousPage
        {
            get => _propHasPreviousPage;
            set { _propHasPreviousPage = value; InvokePropertyChanged(this, nameof(Prop_HasPreviousPage)); }
        }

        public DatabaseQueryPagedBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_DatabaseQueryPaged;
            sdkComponentHelp = @"Компонент ""Database: Постраничный запрос""
Выполняет SQL-запрос с разбивкой на страницы. Оборачивает исходный запрос в подзапрос, считает общее количество строк и возвращает одну страницу результата вместе с метаданными навигации.

Основные:
Провайдер*: [String] Инвариантное имя ADO.NET провайдера (например, System.Data.SqlClient).
Строка подключения: [String] Строка подключения к БД. Обязательна, если не задан ID транзакции.
ID транзакции: [String] Идентификатор активной транзакции.
Текст запроса*: [String] Исходный SQL SELECT, используемый как подзапрос для пагинации.
ORDER BY*: [String] Выражение сортировки для стабильной и воспроизводимой пагинации (например, Id ASC).
Номер страницы*: [Int32] Номер запрашиваемой страницы, начиная с 1. По умолчанию 1.
Размер страницы*: [Int32] Максимальное количество строк на странице. По умолчанию 100.

Параметры:
Параметры: [Dictionary<String, String>] Словарь параметров исходного SQL-запроса.

Настройки:
Таймаут: [Int32] Таймаут выполнения запроса в секундах. По умолчанию 30.

Выходные данные:
Таблица результатов: [DataTable] Страница результата в виде DataTable.
Строк на странице: [Int32] Количество строк на текущей странице.
Всего строк: [Int32] Общее количество строк во всём результирующем наборе.
Всего страниц: [Int32] Общее количество страниц при заданном размере.
Есть следующая страница: [Boolean] True, если текущая страница не последняя.
Есть предыдущая страница: [Boolean] True, если текущая страница не первая.";
            sdkComponentIcon = ActivityIcons.Base;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_ProviderInvariantName", "ADO.NET provider invariant name"),
                PropertyBuilder.String("Prop_ConnectionString", "Строка подключения к БД"),
                PropertyBuilder.String("Prop_TransactionId", "ID транзакции (опционально)"),
                PropertyBuilder.String("Prop_CommandText", "Исходный SQL-запрос"),
                PropertyBuilder.String("Prop_OrderByExpression", "Выражение ORDER BY для стабильной пагинации"),
                PropertyBuilder.Int("Prop_PageNumber", "Номер страницы, начиная с 1"),
                PropertyBuilder.Int("Prop_PageSize", "Размер страницы"),
                PropertyBuilder.Script<Dictionary<string, string>>("Prop_Parameters", "Параметры запроса"),
                PropertyBuilder.Int("Prop_CommandTimeoutSeconds", "Таймаут выполнения в секундах"),
                PropertyBuilder.Variable<DataTable>("Prop_ResultTable", "Страница результата в виде DataTable"),
                PropertyBuilder.Variable<int>("Prop_RowCount", "Количество строк на текущей странице"),
                PropertyBuilder.Variable<int>("Prop_TotalRows", "Общее количество строк"),
                PropertyBuilder.Variable<int>("Prop_TotalPages", "Общее количество страниц"),
                PropertyBuilder.Variable<bool>("Prop_HasNextPage", "Есть ли следующая страница"),
                PropertyBuilder.Variable<bool>("Prop_HasPreviousPage", "Есть ли предыдущая страница")
            };
            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var provider = GetPropertyValue<string>(Prop_ProviderInvariantName, nameof(Prop_ProviderInvariantName), sd);
                var connectionString = GetPropertyValue<string>(Prop_ConnectionString, nameof(Prop_ConnectionString), sd);
                var explicitTransactionId = GetPropertyValue<string>(Prop_TransactionId, nameof(Prop_TransactionId), sd);
                var commandText = GetPropertyValue<string>(Prop_CommandText, nameof(Prop_CommandText), sd);
                var orderByExpression = GetPropertyValue<string>(Prop_OrderByExpression, nameof(Prop_OrderByExpression), sd);
                var parameters = GetPropertyValue<Dictionary<string, string>>(Prop_Parameters, nameof(Prop_Parameters), sd);
                var timeout = ParseIntOrDefault(GetPropertyValue<string>(Prop_CommandTimeoutSeconds, nameof(Prop_CommandTimeoutSeconds), sd), 30);
                var pageNumber = ParseIntOrDefault(GetPropertyValue<string>(Prop_PageNumber, nameof(Prop_PageNumber), sd), 1);
                var pageSize = ParseIntOrDefault(GetPropertyValue<string>(Prop_PageSize, nameof(Prop_PageSize), sd), 100);

                var transactionId = DatabaseTransactionResolver.ResolveOptional(explicitTransactionId);
                var transactionHandle = DatabaseTransactionManager.Get(transactionId);
                var result = transactionHandle != null
                    ? DatabaseHelper.ExecutePagedQuery(transactionHandle, commandText, orderByExpression, pageNumber, pageSize, timeout, parameters)
                    : DatabaseHelper.ExecutePagedQuery(provider, connectionString, commandText, orderByExpression, pageNumber, pageSize, timeout, parameters);

                SetVariableValue(Prop_ResultTable, result.ResultTable, sd);
                SetVariableValue(Prop_RowCount, result.ResultTable.Rows.Count, sd);
                SetVariableValue(Prop_TotalRows, result.TotalRows, sd);
                SetVariableValue(Prop_TotalPages, result.TotalPages, sd);
                SetVariableValue(Prop_HasNextPage, result.HasNextPage, sd);
                SetVariableValue(Prop_HasPreviousPage, result.HasPreviousPage, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"Получена страница {result.PageNumber} из {result.TotalPages}. Строк на странице: {result.ResultTable.Rows.Count}, всего: {result.TotalRows}"
                };
            }
            catch (System.Data.Common.DbException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка БД: {ex.Message}" };
            }
            catch (InvalidOperationException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Недопустимая операция: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка постраничного запроса к БД: {ex.Message}"
                };
            }
        }

        public override ValidationResult Validate()
        {
            var result = new ValidationResult();
            result.ValidateRequired(Prop_CommandText, ActivityStrings.Field_CommandText, ActivityStrings.Error_CommandTextRequired);
            result.ValidateRequired(Prop_OrderByExpression, ActivityStrings.Field_OrderByExpression, "ORDER BY обязателен для постраничного чтения");
            if (string.IsNullOrWhiteSpace(Prop_TransactionId) && string.IsNullOrWhiteSpace(DatabaseTransactionResolver.ResolveOptional(null)))
                result.ValidateRequired(Prop_ConnectionString, ActivityStrings.Field_ConnectionString, ActivityStrings.Error_ConnectionStringRequired);
            return result;
        }

        private static int ParseIntOrDefault(string value, int defaultValue)
        {
            int parsed;
            return int.TryParse(value, out parsed) && parsed > 0 ? parsed : defaultValue;
        }
    }
}
