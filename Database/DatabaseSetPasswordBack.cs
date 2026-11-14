// =============================================================================
// DatabaseSetPasswordBack.cs — активность «БД: Задать пароль подключения»
// (issue #26).
//
// Пароль хранится в RepoDict.SecDict как SecureString и подставляется в
// строку подключения в рантайме вместо плейсхолдера {password}
// (DatabaseConnectionHelper.ResolveConnectionString — единая точка для всех
// DB-активностей). Строка подключения в workflow остаётся без пароля.
// Пустой пароль снимает ранее заданный.
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using System.Security;

namespace Primo.MIA
{
    /// <summary>Активность задания пароля для строк подключения БД.</summary>
    public class DatabaseSetPasswordBack : PrimoComponentTO<DatabaseSetPassword>
    {
        public override string GroupName { get => ActivityCategories.Database; protected set { } }

        protected override int sdkTimeOut { get => 300000; set { } }

        private string _propPassword;
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main), System.ComponentModel.DisplayName(ActivityStrings.Field_DbPassword)]
        /// <summary>Пароль подключения (подставляется вместо {password}).</summary>
        public string Prop_Password
        {
            get => _propPassword;
            set { _propPassword = value; InvokePropertyChanged(this, nameof(Prop_Password)); }
        }

        public DatabaseSetPasswordBack(IWFContainer container) : base(container)
        {
            sdkComponentName = ActivityStrings.Activity_DatabaseSetPassword;
            sdkComponentHelp = @"Компонент ""БД: Задать пароль подключения""
Задаёт пароль, который подставляется в строку подключения вместо плейсхолдера {password}.

Использование:
1. Перед активностью с подключением к БД добавьте этот элемент и укажите пароль.
2. В строке подключения (в любой DB-активности) впишите {password} на месте пароля:
   Server=srv;Database=db;User Id=sa;Password={password};
3. Пароль хранится в SecureString (RepoDict.SecDict) и собирается в строку только в рантайме.

Безопасность:
Рекомендуется передавать пароль переменной из защищённого источника
(TOML-конфиг, хранилище секретов), а не литералом в поле.
Пустой пароль снимает ранее заданный.

Строка подключения без плейсхолдера {password} работает как раньше —
задавать пароль не обязательно.";
            sdkComponentIcon = ActivityIcons.Base;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.String("Prop_Password", "Пароль подключения ({password} в строке)")
            };

            InitClass(container);

            this.Prop_Password = "\"\"";
        }

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                var password = GetPropertyValue<string>(this.Prop_Password, nameof(Prop_Password), sd) ?? string.Empty;

                if (string.IsNullOrEmpty(password))
                {
                    // Пустой пароль — снять ранее заданный
                    RepoDict.SecDict.TryRemove(DatabaseConnectionHelper.PasswordKey, out _);
                    return new ExecutionResult
                    {
                        IsSuccess = true,
                        SuccessMessage = "[БД] Пароль подключения снят"
                    };
                }

                var secure = new SecureString();
                foreach (var c in password)
                    secure.AppendChar(c);
                secure.MakeReadOnly();

                RepoDict.SecDict[DatabaseConnectionHelper.PasswordKey] = secure;

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = $"[БД] Пароль подключения задан ({password.Length} символов); " +
                                     "подставляйте {password} в строке подключения"
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка задания пароля БД: {ex.Message}"
                };
            }
        }
    }
}
