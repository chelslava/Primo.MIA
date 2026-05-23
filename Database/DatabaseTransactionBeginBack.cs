// =============================================================================
// DatabaseTransactionBeginBack.cs — активность «Database: Начать транзакцию (TransactionBegin)».
// Открывает соединение с БД, начинает транзакцию и помещает её в ambient-контекст модуля.
// =============================================================================
using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Активность для открытия транзакции БД.</summary>
    public class DatabaseTransactionBeginBack : PrimoComponentTO<DatabaseTransactionBegin>
    {
        public override string GroupName { get => ActivityCategories.Database; protected set { } }

        protected override int sdkTimeOut
        {
            get => 30000;
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

        private DatabaseIsolationLevel _propIsolationLevel = DatabaseIsolationLevel.ReadCommitted;
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_IsolationLevel)]
        /// <summary>Уровень изоляции открываемой транзакции.</summary>
        public DatabaseIsolationLevel Prop_IsolationLevel
        {
            get => _propIsolationLevel;
            set { _propIsolationLevel = value; InvokePropertyChanged(this, nameof(Prop_IsolationLevel)); }
        }

        private string _propTransactionId;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_TransactionId)]
        /// <summary>Уникальный идентификатор открытой транзакции для передачи в последующие активности.</summary>
        public string Prop_TransactionId
        {
            get => _propTransactionId;
            set { _propTransactionId = value; InvokePropertyChanged(this, nameof(Prop_TransactionId)); }
        }

        private string _propStartedAtUtc;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_StartedAtUtc)]
        /// <summary>Метка времени начала транзакции в формате ISO 8601 UTC.</summary>
        public string Prop_StartedAtUtc
        {
            get => _propStartedAtUtc;
            set { _propStartedAtUtc = value; InvokePropertyChanged(this, nameof(Prop_StartedAtUtc)); }
        }

        public DatabaseTransactionBeginBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_DatabaseTransactionBegin;
            sdkComponentHelp =
                "Открывает соединение, начинает транзакцию и сохраняет её в ambient-контекст DB-модуля.\n" +
                "Следующие DB-активности могут использовать её без явной передачи transactionId.";
            sdkComponentIcon = ActivityIcons.Base;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_ProviderInvariantName", "ADO.NET provider invariant name"),
                PropertyBuilder.String("Prop_ConnectionString", "Строка подключения к БД"),
                PropertyBuilder.Enum<DatabaseIsolationLevel>("Prop_IsolationLevel", "Уровень изоляции транзакции"),
                PropertyBuilder.Variable<string>("Prop_TransactionId", "ID открытой транзакции"),
                PropertyBuilder.Variable<string>("Prop_StartedAtUtc", "Метка времени начала транзакции в UTC")
            };
            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var provider = GetPropertyValue<string>(Prop_ProviderInvariantName, nameof(Prop_ProviderInvariantName), sd);
                var connectionString = GetPropertyValue<string>(Prop_ConnectionString, nameof(Prop_ConnectionString), sd);

                var handle = DatabaseTransactionManager.Begin(provider, connectionString, Prop_IsolationLevel);
                DatabaseTransactionContext.Push(handle.TransactionId);

                SetVariableValue(Prop_TransactionId, handle.TransactionId, sd);
                SetVariableValue(Prop_StartedAtUtc, handle.StartedAtUtc.ToString("O"), sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"Транзакция открыта: {handle.TransactionId}"
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
                    ErrorMessage = $"Ошибка открытия транзакции БД: {ex.Message}"
                };
            }
        }

        public override ValidationResult Validate()
        {
            var result = new ValidationResult();
            result.ValidateRequired(Prop_ConnectionString, ActivityStrings.Field_ConnectionString, ActivityStrings.Error_ConnectionStringRequired);
            return result;
        }
    }
}
