// =============================================================================
// DatabaseTransactionRollbackBack.cs — активность «Database: Откатить транзакцию (TransactionRollback)».
// Откатывает транзакцию по явному идентификатору или текущему ambient-контексту модуля.
// =============================================================================
using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>Активность для отката транзакции БД.</summary>
    public class DatabaseTransactionRollbackBack : PrimoComponentTO<DatabaseTransactionRollback>
    {
        public override string GroupName { get => ActivityCategories.Database; protected set { } }

        protected override int sdkTimeOut
        {
            get => 30000;
            set { }
        }

        private string _propTransactionId;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_TransactionId)]
        /// <summary>Идентификатор транзакции для отката; если не задан, используется ambient-контекст.</summary>
        public string Prop_TransactionId
        {
            get => _propTransactionId;
            set { _propTransactionId = value; InvokePropertyChanged(this, nameof(Prop_TransactionId)); }
        }

        public DatabaseTransactionRollbackBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_DatabaseTransactionRollback;
            sdkComponentHelp = "Откатывает транзакцию по явному transactionId или по текущему ambient-контексту.";
            sdkComponentIcon = ActivityIcons.Base;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_TransactionId", "ID транзакции (необязательно, если есть ambient-контекст)")
            };
            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var explicitId = GetPropertyValue<string>(Prop_TransactionId, nameof(Prop_TransactionId), sd);
                var transactionId = DatabaseTransactionResolver.ResolveRequired(explicitId);
                DatabaseTransactionManager.Rollback(transactionId);

                if (DatabaseTransactionContext.Current == transactionId)
                    DatabaseTransactionContext.Pop();

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"Транзакция откатена: {transactionId}"
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
                    ErrorMessage = $"Ошибка отката транзакции БД: {ex.Message}"
                };
            }
        }
    }
}
