// =============================================================================
// BrowserStorageManageBack.cs — активность «Управление Web Storage».
//
// Управляет localStorage и sessionStorage браузера.
// Поддерживает чтение, запись, удаление данных из Web Storage.
//
// Используется для:
//   - Работы с данными SPA приложений
//   - Сохранения и чтения настроек
//   - Управления сессионными данными
//   - Очистки хранилища
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using OpenQA.Selenium;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;
using static LTools.Common.Helpers.WFHelper.PropertiesItem;

namespace Primo.MIA
{
    /// <summary>
    /// Активность для управления Web Storage браузера.
    /// </summary>
    public class BrowserStorageManageBack : PrimoComponentTO<BrowserStorageManage>
    {
        public override string GroupName
        {
            get => ActivityCategories.Browser;
            protected set { }
        }

        protected override int sdkTimeOut
        {
            get => 30000;
            set { }
        }

        // ── Входные параметры ──────────────────────────────────────────

        private string _propSessionId;
        /// <summary>ID сессии браузера.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName(ActivityStrings.Field_SessionId)]
        public string Prop_SessionId
        {
            get => _propSessionId;
            set { _propSessionId = value; InvokePropertyChanged(this, "Prop_SessionId"); }
        }

        private StorageType _propStorageType;
        /// <summary>Тип хранилища.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Тип хранилища")]
        public StorageType Prop_StorageType
        {
            get => _propStorageType;
            set { _propStorageType = value; InvokePropertyChanged(this, "Prop_StorageType"); }
        }

        private StorageOperation _propOperation;
        /// <summary>Операция с хранилищем.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Операция")]
        public StorageOperation Prop_Operation
        {
            get => _propOperation;
            set { _propOperation = value; InvokePropertyChanged(this, "Prop_Operation"); }
        }

        private string _propKey;
        /// <summary>Ключ для операций.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Ключ")]
        public string Prop_Key
        {
            get => _propKey;
            set { _propKey = value; InvokePropertyChanged(this, "Prop_Key"); }
        }

        private string _propValue;
        /// <summary>Значение для SetItem.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Значение")]
        public string Prop_Value
        {
            get => _propValue;
            set { _propValue = value; InvokePropertyChanged(this, "Prop_Value"); }
        }

        // ── Выходные параметры ─────────────────────────────────────────

        private string _propOutValue;
        /// <summary>Значение из хранилища.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Значение (результат)")]
        public string Prop_OutValue
        {
            get => _propOutValue;
            set { _propOutValue = value; InvokePropertyChanged(this, "Prop_OutValue"); }
        }

        private string _propOutKeys;
        /// <summary>Список ключей.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(List<string>))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Список ключей")]
        public string Prop_OutKeys
        {
            get => _propOutKeys;
            set { _propOutKeys = value; InvokePropertyChanged(this, "Prop_OutKeys"); }
        }

        private string _propOutLength;
        /// <summary>Количество элементов.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Количество элементов")]
        public string Prop_OutLength
        {
            get => _propOutLength;
            set { _propOutLength = value; InvokePropertyChanged(this, "Prop_OutLength"); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public BrowserStorageManageBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Браузер: Управление Web Storage";
            sdkComponentHelp =
                "Управляет localStorage и sessionStorage браузера.\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "ID сессии — идентификатор сессии браузера\n" +
                "Тип хранилища — localStorage или sessionStorage\n" +
                "Операция — тип операции с хранилищем\n" +
                "\n" +
                "── Операции ──────────────────────────────────\n" +
                "GetItem — получить значение по ключу\n" +
                "SetItem — установить значение\n" +
                "RemoveItem — удалить ключ\n" +
                "Clear — очистить всё хранилище\n" +
                "GetAllKeys — получить все ключи\n" +
                "GetLength — получить количество элементов\n" +
                "\n" +
                "── Типы хранилищ ─────────────────────────────\n" +
                "LocalStorage — данные сохраняются между сессиями\n" +
                "SessionStorage — данные удаляются при закрытии вкладки\n" +
                "\n" +
                "ПРИМЕЧАНИЕ: Работает через JavaScript API браузера.";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Variable<string>("Prop_SessionId", "ID сессии браузера"),
                PropertyBuilder.Enum<StorageType>("Prop_StorageType", "Тип хранилища"),
                PropertyBuilder.Enum<StorageOperation>("Prop_Operation", "Операция"),
                PropertyBuilder.String("Prop_Key", "Ключ"),
                PropertyBuilder.String("Prop_Value", "Значение"),
                PropertyBuilder.Variable<string>("Prop_OutValue", "Значение (результат)"),
                PropertyBuilder.Variable<List<string>>("Prop_OutKeys", "Список ключей"),
                PropertyBuilder.Variable<int>("Prop_OutLength", "Количество элементов")
            };

            InitClass(container);

            this.Prop_SessionId = "\"\"";
            this.Prop_StorageType = StorageType.LocalStorage;
            this.Prop_Operation = StorageOperation.GetItem;
            this.Prop_Key = "\"\"";
            this.Prop_Value = "\"\"";
            this.Prop_OutValue = "";
            this.Prop_OutKeys = "";
            this.Prop_OutLength = "";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                string sessionId = SessionResolver.Resolve(
                    GetPropertyValue<string>(this.Prop_SessionId, nameof(Prop_SessionId), sd));

                var driver = SeleniumHelper.GetDriver(sessionId);
                string resultMsg;

                switch (this.Prop_Operation)
                {
                    case StorageOperation.GetItem:
                        {
                            string key = GetPropertyValue<string>(this.Prop_Key, "Prop_Key", sd);
                            if (string.IsNullOrWhiteSpace(key))
                                throw new ArgumentException("Ключ не может быть пустым для операции GetItem");

                            string value = SeleniumHelper.GetStorageItem(driver, this.Prop_StorageType, key);
                            SetVariableValue(this.Prop_OutValue, value ?? string.Empty, sd);
                            resultMsg = $"[Web Storage] Получено значение для ключа '{key}'";
                        }
                        break;

                    case StorageOperation.SetItem:
                        {
                            string key = GetPropertyValue<string>(this.Prop_Key, "Prop_Key", sd);
                            string value = GetPropertyValue<string>(this.Prop_Value, "Prop_Value", sd);

                            if (string.IsNullOrWhiteSpace(key))
                                throw new ArgumentException("Ключ не может быть пустым для операции SetItem");

                            SeleniumHelper.SetStorageItem(driver, this.Prop_StorageType, key, value ?? string.Empty);
                            resultMsg = $"[Web Storage] Установлено значение для ключа '{key}'";
                        }
                        break;

                    case StorageOperation.RemoveItem:
                        {
                            string key = GetPropertyValue<string>(this.Prop_Key, "Prop_Key", sd);
                            if (string.IsNullOrWhiteSpace(key))
                                throw new ArgumentException("Ключ не может быть пустым для операции RemoveItem");

                            SeleniumHelper.RemoveStorageItem(driver, this.Prop_StorageType, key);
                            resultMsg = $"[Web Storage] Удалён ключ '{key}'";
                        }
                        break;

                    case StorageOperation.Clear:
                        SeleniumHelper.ClearStorage(driver, this.Prop_StorageType);
                        resultMsg = $"[Web Storage] Хранилище {this.Prop_StorageType} очищено";
                        break;

                    case StorageOperation.GetAllKeys:
                        {
                            var keys = SeleniumHelper.GetStorageKeys(driver, this.Prop_StorageType);
                            SetVariableValue(this.Prop_OutKeys, keys, sd);
                            SetVariableValue(this.Prop_OutLength, keys.Count, sd);
                            resultMsg = $"[Web Storage] Получено ключей: {keys.Count}";
                        }
                        break;

                    case StorageOperation.GetLength:
                        {
                            int length = SeleniumHelper.GetStorageLength(driver, this.Prop_StorageType);
                            SetVariableValue(this.Prop_OutLength, length, sd);
                            resultMsg = $"[Web Storage] Количество элементов: {length}";
                        }
                        break;

                    default:
                        throw new NotSupportedException($"Операция {this.Prop_Operation} не поддерживается");
                }

                return new ExecutionResult
                {
                    IsSuccess = true,
                    SuccessMessage = resultMsg
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка [Web Storage]: {ex.Message}"
                };
            }
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();
             

            switch (this.Prop_Operation)
            {
                case StorageOperation.GetItem:
                    ret.ValidateRequired(this.Prop_Key, "Ключ", "Ключ обязателен для операции GetItem");
                    ret.ValidateRequired(this.Prop_OutValue, "Значение (результат)", "Переменная для значения обязательна");
                    break;

                case StorageOperation.SetItem:
                    ret.ValidateRequired(this.Prop_Key, "Ключ", "Ключ обязателен для операции SetItem");
                    ret.ValidateRequired(this.Prop_Value, "Значение", "Значение обязательно для операции SetItem");
                    break;

                case StorageOperation.RemoveItem:
                    ret.ValidateRequired(this.Prop_Key, "Ключ", "Ключ обязателен для операции RemoveItem");
                    break;

                case StorageOperation.GetAllKeys:
                    ret.ValidateRequired(this.Prop_OutKeys, "Список ключей", "Переменная для списка ключей обязательна");
                    break;

                case StorageOperation.GetLength:
                    ret.ValidateRequired(this.Prop_OutLength, "Количество элементов", "Переменная для количества обязательна");
                    break;
            }

            return ret;
        }
    }
}
