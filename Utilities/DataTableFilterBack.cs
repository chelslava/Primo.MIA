// =============================================================================
// DataTableFilterBack.cs — активность «DataTable: Фильтрация».
//
// Фильтрует DataTable LINQ-выражением — порядок строк сохраняется,
// исходная таблица не изменяется. Возвращает две таблицы:
//   Prop_Filtered  — строки, прошедшие условие
//   Prop_Rejected  — строки, НЕ прошедшие условие
//
// Режимы (DataTableFilterMode):
//   SqlQuery        — SQL-подобное выражение (аналог WHERE):
//                     Age >= 18 AND Name LIKE 'Ив%' OR City IN ('Москва', 'Казань')
//   ConditionBuilder— конструктор условий: строки «Колонка + Оператор + Значение»
//                     с связкой И/ИЛИ (хранятся в Prop_Conditions как DSL).
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;

namespace Primo.MIA
{
    /// <summary>
    /// Активность «DataTable: Фильтрация».
    /// Отбирает строки DataTable по SQL-подобному выражению или условиям
    /// конструктора. Порядок строк в результатах соответствует исходному.
    /// </summary>
    public class DataTableFilterBack : PrimoComponentTO<DataTableFilter>
    {
        private bool _syncingRows;

        /// <inheritdoc/>
        public override string GroupName
        {
            get => ActivityCategories.Utilities;
            protected set { }
        }

        /// <inheritdoc/>
        protected override int sdkTimeOut
        {
            get => 30000;
            set { }
        }

        // ── INPUT ──────────────────────────────────────────────────────────────

        #region Prop_DataTable

        private string _propDataTable;

        /// <summary>Исходная таблица данных для фильтрации.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DataTable))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_DataTable)]
        public string Prop_DataTable
        {
            get => _propDataTable;
            set { _propDataTable = value; InvokePropertyChanged(this, nameof(Prop_DataTable)); }
        }

        #endregion

        #region Mode

        private DataTableFilterMode _mode = DataTableFilterMode.SqlQuery;

        /// <summary>Режим задания условий фильтрации.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_FilterMode)]
        public DataTableFilterMode Mode
        {
            get => _mode;
            set { _mode = value; InvokePropertyChanged(this, nameof(Mode)); }
        }

        #endregion

        #region Prop_SqlQuery

        private string _propSqlQuery;

        /// <summary>SQL-подобное выражение (аналог WHERE) для режима SqlQuery.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_SqlQuery)]
        public string Prop_SqlQuery
        {
            get => _propSqlQuery;
            set { _propSqlQuery = value; InvokePropertyChanged(this, nameof(Prop_SqlQuery)); }
        }

        #endregion

        #region Prop_Conditions

        private string _propConditions;

        /// <summary>
        /// Условия конструктора в формате DSL: «Join|Column|Operator|Value»,
        /// по строке на условие. Заполняется формой, можно править вручную.
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_Conditions)]
        public string Prop_Conditions
        {
            get => _propConditions;
            set
            {
                _propConditions = value;
                InvokePropertyChanged(this, nameof(Prop_Conditions));
                if (!_syncingRows) LoadRowsFromDsl();
            }
        }

        #endregion

        #region Prop_CaseSensitive

        private bool _propCaseSensitive = false;

        /// <summary>Учитывать регистр при сравнении строк.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_CaseSensitive)]
        public bool Prop_CaseSensitive
        {
            get => _propCaseSensitive;
            set { _propCaseSensitive = value; InvokePropertyChanged(this, nameof(Prop_CaseSensitive)); }
        }

        #endregion

        // ── OUTPUT ─────────────────────────────────────────────────────────────

        #region Prop_Filtered

        private string _propFiltered;

        /// <summary>Таблица строк, прошедших условие (порядок исходный).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DataTable))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_FilteredTable)]
        public string Prop_Filtered
        {
            get => _propFiltered;
            set { _propFiltered = value; InvokePropertyChanged(this, nameof(Prop_Filtered)); }
        }

        #endregion

        #region Prop_Rejected

        private string _propRejected;

        /// <summary>Таблица строк, НЕ прошедших условие (порядок исходный).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(DataTable))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_RejectedTable)]
        public string Prop_Rejected
        {
            get => _propRejected;
            set { _propRejected = value; InvokePropertyChanged(this, nameof(Prop_Rejected)); }
        }

        #endregion

        #region Prop_FilteredCount

        private string _propFilteredCount;

        /// <summary>Количество строк, прошедших условие.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_FilteredCount)]
        public string Prop_FilteredCount
        {
            get => _propFilteredCount;
            set { _propFilteredCount = value; InvokePropertyChanged(this, nameof(Prop_FilteredCount)); }
        }

        #endregion

        #region Prop_RejectedCount

        private string _propRejectedCount;

        /// <summary>Количество отсеянных строк.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_RejectedCount)]
        public string Prop_RejectedCount
        {
            get => _propRejectedCount;
            set { _propRejectedCount = value; InvokePropertyChanged(this, nameof(Prop_RejectedCount)); }
        }

        #endregion

        // ── КОНСТРУКТОР ────────────────────────────────────────────────────────

        public DataTableFilterBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_DataTableFilter;
            sdkComponentHelp =
                "Компонент \"DataTable: Фильтрация\" (DataTableFilterBack)\n" +
                "Отбирает строки DataTable по условию. Порядок строк сохраняется,\n" +
                "исходная таблица не изменяется. Возвращает ДВЕ таблицы:\n" +
                "прошедшие фильтр и не прошедшие (остаток).\n\n" +
                "── Режимы ─────────────────────────────────────────────\n" +
                "SqlQuery — SQL-подобное выражение (аналог WHERE):\n" +
                "  Age >= 18 AND Name LIKE 'Ив%' OR (City = 'Москва' AND Age < 30)\n" +
                "  Операторы: =, ==, <>, !=, >, <, >=, <=, LIKE, IN (...), IS NULL, IS NOT NULL\n" +
                "  Логика: AND (И), OR (ИЛИ), NOT (НЕ), скобки\n" +
                "  Литералы: 'строка', \"строка\", числа, true/false; колонка — [Имя в скобках]\n\n" +
                "ConditionBuilder — конструктор условий (форма):\n" +
                "  строки «Колонка + Оператор + Значение» со связкой И/ИЛИ.\n" +
                "  Операторы: =, <>, >, >=, <, <=, Contains, StartsWith, EndsWith,\n" +
                "  Like (% и _), In (значения через запятую), IsNull, IsNotNull\n\n" +
                "── Основные ───────────────────────────────────────────\n" +
                "Таблица данных*: [DataTable] исходная таблица.\n" +
                "Режим фильтрации: [DataTableFilterMode] SqlQuery / ConditionBuilder.\n" +
                "SQL-выражение: [String] условие для режима SqlQuery.\n" +
                "Условия: [String] DSL конструктора (Join|Column|Operator|Value).\n" +
                "Учитывать регистр: [Boolean] при сравнении строк (по умолчанию нет).\n\n" +
                "── Выходные данные ────────────────────────────────────\n" +
                "Прошли фильтр: [DataTable] строки, прошедшие условие.\n" +
                "Не прошли фильтр: [DataTable] остальные строки.\n" +
                "Кол-во прошедших: [Int32].\n" +
                "Кол-во отсеянных: [Int32].";
            sdkComponentIcon = ActivityIcons.Table;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Script<DataTable>("Prop_DataTable", "Исходная таблица данных для фильтрации"),
                PropertyBuilder.Enum<DataTableFilterMode>("Mode", "Способ задания условий: SQL-выражение или конструктор"),
                PropertyBuilder.Script<string>("Prop_SqlQuery", "SQL-подобное выражение (Age >= 18 AND Name LIKE 'Ив%')"),
                PropertyBuilder.String("Prop_Conditions", "Условия конструктора (DSL: Join|Column|Operator|Value)"),
                PropertyBuilder.BooleanObject("Prop_CaseSensitive", "Учитывать регистр при сравнении строк"),
                PropertyBuilder.Variable<DataTable>("Prop_Filtered", "Таблица строк, прошедших фильтр"),
                PropertyBuilder.Variable<DataTable>("Prop_Rejected", "Таблица строк, не прошедших фильтр"),
                PropertyBuilder.Variable<int>("Prop_FilteredCount", "Кол-во прошедших строк"),
                PropertyBuilder.Variable<int>("Prop_RejectedCount", "Кол-во отсеянных строк")
            };

            InitClass(container);

            // Форма конструктора: строки условий из сохранённого DSL
            LoadRowsFromDsl();
            if (ConditionRows.Count == 0)
                ConditionRows.Add(new DataTableConditionRow());

            ConditionRows.CollectionChanged += OnRowsCollectionChanged;
            foreach (var row in ConditionRows) SubscribeRow(row);
        }

        // ── ФОРМА КОНСТРУКТОРА ─────────────────────────────────────────────────

        /// <summary>Строки условий для формы (не сериализуются — хранятся в Prop_Conditions).</summary>
        public ObservableCollection<DataTableConditionRow> ConditionRows { get; }
            = new ObservableCollection<DataTableConditionRow>();

        /// <summary>Добавить пустое условие (кнопка «Добавить условие»).</summary>
        public void AddConditionRow()
        {
            ConditionRows.Add(new DataTableConditionRow());
        }

        /// <summary>Удалить условие (кнопка «✕» в строке).</summary>
        public void RemoveConditionRow(DataTableConditionRow row)
        {
            ConditionRows.Remove(row);
        }

        private void OnRowsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems) SubscribeRow(item as DataTableConditionRow);
            }
        }

        private void SubscribeRow(DataTableConditionRow row)
        {
            if (row != null) row.PropertyChanged += RowChanged;
        }

        private void LoadRowsFromDsl()
        {
            _syncingRows = true;
            try
            {
                foreach (var row in ConditionRows) row.PropertyChanged -= RowChanged;
                ConditionRows.Clear();

                var conditions = DataTableFilterLogic.ParseConditionsDsl(Prop_Conditions);
                foreach (var c in conditions)
                {
                    ConditionRows.Add(new DataTableConditionRow
                    {
                        Join = c.Join,
                        Column = c.Column,
                        Operator = c.Operator,
                        Value = c.Value
                    });
                }
            }
            catch (UtilityException)
            {
                // Некорректный DSL — оставляем форму пустой, ошибку вернёт валидация при выполнении
            }
            finally
            {
                _syncingRows = false;
            }
        }

        private void RowChanged(object sender, PropertyChangedEventArgs e) => SyncDslFromRows();

        private void SyncDslFromRows()
        {
            if (_syncingRows) return;

            _syncingRows = true;
            try
            {
                var conditions = ConditionRows
                    .Where(r => !string.IsNullOrWhiteSpace(r.Column))
                    .Select(r => new DataTableFilterCondition
                    {
                        Join = r.Join,
                        Column = r.Column,
                        Operator = r.Operator,
                        Value = r.Value
                    })
                    .ToList();

                Prop_Conditions = DataTableFilterLogic.ToConditionsDsl(conditions);
            }
            finally
            {
                _syncingRows = false;
            }
        }

        // ── ДЕЙСТВИЕ ───────────────────────────────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var table = (DataTable)GetPropertyValue<object>(this.Prop_DataTable, "Prop_DataTable", sd);
                if (table == null)
                {
                    return new ExecutionResult
                    {
                        IsSuccess = false,
                        ErrorMessage = ActivityStrings.Error_DataTableRequired
                    };
                }

                var logic = new DataTableFilterLogic();
                DataTableFilterResult result;

                if (this.Mode == DataTableFilterMode.SqlQuery)
                {
                    string sql = GetPropertyValue<string>(this.Prop_SqlQuery, "Prop_SqlQuery", sd) ?? this.Prop_SqlQuery;
                    result = logic.Filter(table, sql, this.Prop_CaseSensitive);
                }
                else
                {
                    var conditions = DataTableFilterLogic.ParseConditionsDsl(this.Prop_Conditions);
                    result = logic.Filter(table, conditions, this.Prop_CaseSensitive);
                }

                SetVariableValue(this.Prop_Filtered, result.Filtered, sd);
                SetVariableValue(this.Prop_Rejected, result.Rejected, sd);
                SetVariableValue(this.Prop_FilteredCount, result.FilteredCount, sd);
                SetVariableValue(this.Prop_RejectedCount, result.RejectedCount, sd);

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"Прошло: {result.FilteredCount}, отсеяно: {result.RejectedCount}"
                };
            }
            catch (UtilityException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = ex.Message };
            }
            catch (ArgumentException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Неверный аргумент: {ex.Message}" };
            }
            catch (InvalidOperationException ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Недопустимая операция: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка фильтрации DataTable: {ex.Message}" };
            }
        }

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();

            if (string.IsNullOrWhiteSpace(this.Prop_DataTable))
            {
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = ActivityStrings.Field_DataTable,
                    Error = ActivityStrings.Error_DataTableRequired
                });
            }

            if (this.Mode == DataTableFilterMode.SqlQuery && string.IsNullOrWhiteSpace(this.Prop_SqlQuery))
            {
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = ActivityStrings.Field_SqlQuery,
                    Error = ActivityStrings.Error_SqlQueryRequired
                });
            }

            if (this.Mode == DataTableFilterMode.ConditionBuilder &&
                !ConditionRows.Any(r => !string.IsNullOrWhiteSpace(r.Column)))
            {
                ret.Items.Add(new ValidationResult.ValidationItem
                {
                    PropertyName = ActivityStrings.Field_Conditions,
                    Error = ActivityStrings.Error_ConditionsRequired
                });
            }

            return ret;
        }
    }

    /// <summary>
    /// Строка формы конструктора условий (привязывается к XAML).
    /// </summary>
    public class DataTableConditionRow : INotifyPropertyChanged
    {
        private DataTableConditionJoin _join = DataTableConditionJoin.And;
        private string _column = string.Empty;
        private DataTableConditionOperator _operator = DataTableConditionOperator.Equal;
        private string _value = string.Empty;

        /// <summary>Связка с предыдущим условием (И/ИЛИ).</summary>
        public DataTableConditionJoin Join
        {
            get => _join;
            set { _join = value; Raise(nameof(Join)); }
        }

        /// <summary>Имя колонки таблицы.</summary>
        public string Column
        {
            get => _column;
            set { _column = value; Raise(nameof(Column)); }
        }

        /// <summary>Оператор сравнения.</summary>
        public DataTableConditionOperator Operator
        {
            get => _operator;
            set { _operator = value; Raise(nameof(Operator)); }
        }

        /// <summary>Значение для сравнения.</summary>
        public string Value
        {
            get => _value;
            set { _value = value; Raise(nameof(Value)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Raise(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
