// =============================================================================
// DatabaseCheckConnectionBack.cs — активность «Database: Проверить подключение (CheckConnection)».
// Открывает соединение с БД через ADO.NET провайдер, читает версию сервера и немедленно закрывает.
// =============================================================================
using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Primo.MIA
{
    /// <summary>Активность для проверки подключения к БД.</summary>
    public class DatabaseCheckConnectionBack : PrimoComponentTO<DatabaseCheckConnection>
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

        private string _propCommandTimeoutSeconds = "30";
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Settings), System.ComponentModel.DisplayName(ActivityStrings.Field_CommandTimeoutSeconds)]
        /// <summary>Таймаут попытки подключения в секундах.</summary>
        public string Prop_CommandTimeoutSeconds
        {
            get => _propCommandTimeoutSeconds;
            set { _propCommandTimeoutSeconds = value; InvokePropertyChanged(this, nameof(Prop_CommandTimeoutSeconds)); }
        }

        private string _propIsAvailable;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_IsAvailable)]
        /// <summary>Признак успешного подключения к базе данных.</summary>
        public string Prop_IsAvailable
        {
            get => _propIsAvailable;
            set { _propIsAvailable = value; InvokePropertyChanged(this, nameof(Prop_IsAvailable)); }
        }

        private string _propServerVersion;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_ServerVersion)]
        /// <summary>Версия сервера базы данных, возвращённая при успешном подключении.</summary>
        public string Prop_ServerVersion
        {
            get => _propServerVersion;
            set { _propServerVersion = value; InvokePropertyChanged(this, nameof(Prop_ServerVersion)); }
        }

        private string _propElapsedMs;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_ElapsedMs)]
        /// <summary>Длительность проверки подключения в миллисекундах.</summary>
        public string Prop_ElapsedMs
        {
            get => _propElapsedMs;
            set { _propElapsedMs = value; InvokePropertyChanged(this, nameof(Prop_ElapsedMs)); }
        }

        private string _propErrorText;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output), System.ComponentModel.DisplayName(ActivityStrings.Field_ErrorText)]
        /// <summary>Текст ошибки подключения; пустая строка при успехе.</summary>
        public string Prop_ErrorText
        {
            get => _propErrorText;
            set { _propErrorText = value; InvokePropertyChanged(this, nameof(Prop_ErrorText)); }
        }

        public DatabaseCheckConnectionBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_DatabaseCheckConnection;
            sdkComponentHelp =
                "Проверяет доступность БД через ADO.NET provider.\n\n" +
                "Открывает подключение, читает ServerVersion и сразу закрывает его.";
            sdkComponentIcon = ActivityIcons.Base;
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_ProviderInvariantName", "ADO.NET provider invariant name"),
                PropertyBuilder.String("Prop_ConnectionString", "Строка подключения к БД"),
                PropertyBuilder.Int("Prop_CommandTimeoutSeconds", "Таймаут подключения в секундах"),
                PropertyBuilder.Variable<bool>("Prop_IsAvailable", "Подключение к БД успешно"),
                PropertyBuilder.Variable<string>("Prop_ServerVersion", "Версия сервера БД"),
                PropertyBuilder.Variable<int>("Prop_ElapsedMs", "Длительность проверки в миллисекундах"),
                PropertyBuilder.Variable<string>("Prop_ErrorText", "Текст последней ошибки")
            };
            InitClass(container);
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var provider = GetPropertyValue<string>(Prop_ProviderInvariantName, nameof(Prop_ProviderInvariantName), sd);
                var connectionString = GetPropertyValue<string>(Prop_ConnectionString, nameof(Prop_ConnectionString), sd);

                using (var connection = DatabaseHelper.OpenConnection(provider, connectionString))
                {
                    stopwatch.Stop();
                    SetVariableValue(Prop_IsAvailable, true, sd);
                    SetVariableValue(Prop_ServerVersion, connection.ServerVersion, sd);
                    SetVariableValue(Prop_ElapsedMs, (int)stopwatch.ElapsedMilliseconds, sd);
                    SetVariableValue(Prop_ErrorText, string.Empty, sd);

                    return new ExecutionResult
                    {
                        IsSuccess = true,
                        SuccessMessage = $"Подключение к БД успешно. Версия сервера: {connection.ServerVersion}"
                    };
                }
            }
            catch (System.Data.Common.DbException ex)
            {
                stopwatch.Stop();
                SetVariableValue(Prop_IsAvailable, false, sd);
                SetVariableValue(Prop_ServerVersion, string.Empty, sd);
                SetVariableValue(Prop_ElapsedMs, (int)stopwatch.ElapsedMilliseconds, sd);
                SetVariableValue(Prop_ErrorText, ex.Message, sd);
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Ошибка БД: {ex.Message}" };
            }
            catch (InvalidOperationException ex)
            {
                stopwatch.Stop();
                SetVariableValue(Prop_IsAvailable, false, sd);
                SetVariableValue(Prop_ServerVersion, string.Empty, sd);
                SetVariableValue(Prop_ElapsedMs, (int)stopwatch.ElapsedMilliseconds, sd);
                SetVariableValue(Prop_ErrorText, ex.Message, sd);
                return new ExecutionResult { IsSuccess = false, ErrorMessage = $"Недопустимая операция: {ex.Message}" };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                SetVariableValue(Prop_IsAvailable, false, sd);
                SetVariableValue(Prop_ServerVersion, string.Empty, sd);
                SetVariableValue(Prop_ElapsedMs, (int)stopwatch.ElapsedMilliseconds, sd);
                SetVariableValue(Prop_ErrorText, ex.Message, sd);

                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка подключения к БД: {ex.Message}"
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
