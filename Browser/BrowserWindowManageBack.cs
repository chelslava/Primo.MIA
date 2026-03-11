// =============================================================================
// BrowserWindowManageBack.cs — активность «Управление окном браузера».
//
// Управляет размером, позицией и состоянием окна браузера.
// Поддерживает развёртывание, свёртывание, изменение размера и позиции.
//
// Используется для:
//   - Развёртывания окна на весь экран
//   - Установки конкретного размера окна
//   - Позиционирования окна на экране
//   - Получения текущих параметров окна
// =============================================================================

using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using Primo.MIA.Common;
using System;
using System.Collections.Generic;

namespace Primo.MIA
{
    /// <summary>
    /// Активность для управления окном браузера.
    /// </summary>
    public class BrowserWindowManageBack : BrowserActivityBase<BrowserWindowManage>
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

        private WindowOperation _propOperation;
        /// <summary>Тип операции с окном.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Операция")]
        public WindowOperation Prop_Operation
        {
            get => _propOperation;
            set { _propOperation = value; InvokePropertyChanged(this, "Prop_Operation"); }
        }

        private string _propWidth;
        /// <summary>Ширина окна (для SetSize).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Ширина окна")]
        public string Prop_Width
        {
            get => _propWidth;
            set { _propWidth = value; InvokePropertyChanged(this, "Prop_Width"); }
        }

        private string _propHeight;
        /// <summary>Высота окна (для SetSize).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Высота окна")]
        public string Prop_Height
        {
            get => _propHeight;
            set { _propHeight = value; InvokePropertyChanged(this, "Prop_Height"); }
        }

        private string _propX;
        /// <summary>Позиция X (для SetPosition).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Позиция X")]
        public string Prop_X
        {
            get => _propX;
            set { _propX = value; InvokePropertyChanged(this, "Prop_X"); }
        }

        private string _propY;
        /// <summary>Позиция Y (для SetPosition).</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Main),
         System.ComponentModel.DisplayName("Позиция Y")]
        public string Prop_Y
        {
            get => _propY;
            set { _propY = value; InvokePropertyChanged(this, "Prop_Y"); }
        }

        // ── Выходные параметры ─────────────────────────────────────────

        private string _propOutWidth;
        /// <summary>Текущая ширина окна.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Ширина (результат)")]
        public string Prop_OutWidth
        {
            get => _propOutWidth;
            set { _propOutWidth = value; InvokePropertyChanged(this, "Prop_OutWidth"); }
        }

        private string _propOutHeight;
        /// <summary>Текущая высота окна.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Высота (результат)")]
        public string Prop_OutHeight
        {
            get => _propOutHeight;
            set { _propOutHeight = value; InvokePropertyChanged(this, "Prop_OutHeight"); }
        }

        private string _propOutX;
        /// <summary>Текущая позиция X.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Позиция X (результат)")]
        public string Prop_OutX
        {
            get => _propOutX;
            set { _propOutX = value; InvokePropertyChanged(this, "Prop_OutX"); }
        }

        private string _propOutY;
        /// <summary>Текущая позиция Y.</summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(int))]
        [System.ComponentModel.Category(ActivityStrings.Category_Output),
         System.ComponentModel.DisplayName("Позиция Y (результат)")]
        public string Prop_OutY
        {
            get => _propOutY;
            set { _propOutY = value; InvokePropertyChanged(this, "Prop_OutY"); }
        }

        // ── Конструктор ────────────────────────────────────────────────

        public BrowserWindowManageBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Браузер: Управление окном";
            sdkComponentHelp =
                "Управляет размером, позицией и состоянием окна браузера.\n" +
                "\n" +
                "── Основные параметры ────────────────────────\n" +
                "ID сессии — идентификатор сессии браузера\n" +
                "Операция — тип операции с окном\n" +
                "\n" +
                "── Операции ──────────────────────────────────\n" +
                "Maximize — развернуть на весь экран\n" +
                "Minimize — свернуть окно\n" +
                "FullScreen — полноэкранный режим (F11)\n" +
                "SetSize — установить размер (ширина, высота)\n" +
                "SetPosition — установить позицию (X, Y)\n" +
                "GetSize — получить текущий размер\n" +
                "GetPosition — получить текущую позицию\n" +
                "\n" +
                "── Выходные данные ───────────────────────────\n" +
                "Для GetSize и GetPosition возвращаются текущие значения.";

            sdkComponentIcon = ActivityIcons.Browser;

            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                PropertyBuilder.Variable<string>("Prop_SessionId", "ID сессии браузера"),
                PropertyBuilder.Enum<WindowOperation>("Prop_Operation", "Операция"),
                PropertyBuilder.Int("Prop_Width", "Ширина окна"),
                PropertyBuilder.Int("Prop_Height", "Высота окна"),
                PropertyBuilder.Int("Prop_X", "Позиция X"),
                PropertyBuilder.Int("Prop_Y", "Позиция Y"),
                PropertyBuilder.Variable<int>("Prop_OutWidth", "Ширина (результат)"),
                PropertyBuilder.Variable<int>("Prop_OutHeight", "Высота (результат)"),
                PropertyBuilder.Variable<int>("Prop_OutX", "Позиция X (результат)"),
                PropertyBuilder.Variable<int>("Prop_OutY", "Позиция Y (результат)")
            };

            InitClass(container);

            Prop_SessionId = "\"\"";
            Prop_Operation = WindowOperation.Maximize;
            Prop_Width = "1024";
            Prop_Height = "768";
            Prop_X = "0";
            Prop_Y = "0";
            Prop_OutWidth = "";
            Prop_OutHeight = "";
            Prop_OutX = "";
            Prop_OutY = "";
        }

        // ── TimedAction — точка входа ──────────────────────────────────

        public override ExecutionResult TimedAction(ScriptingData sd)
        {
            try
            {
                string sessionId = GetPropertyValue<string>(Prop_SessionId, nameof(Prop_SessionId), sd);
                var driver = GetDriverFromContext(sessionId);
                string resultMsg;

                switch (Prop_Operation)
                {
                    case WindowOperation.Maximize:
                        SeleniumHelper.MaximizeWindow(driver);
                        resultMsg = "[Управление окном] Окно развёрнуто";
                        break;

                    case WindowOperation.Minimize:
                        SeleniumHelper.MinimizeWindow(driver);
                        resultMsg = "[Управление окном] Окно свёрнуто";
                        break;

                    case WindowOperation.FullScreen:
                        SeleniumHelper.FullScreenWindow(driver);
                        resultMsg = "[Управление окном] Полноэкранный режим";
                        break;

                    case WindowOperation.SetSize:
                        {
                            string widthStr = GetPropertyValue<string>(Prop_Width, "Prop_Width", sd) ?? "1024";
                            string heightStr = GetPropertyValue<string>(Prop_Height, "Prop_Height", sd) ?? "768";
                            int width = int.TryParse(widthStr, out int w) ? w : 1024;
                            int height = int.TryParse(heightStr, out int h) ? h : 768;

                            SeleniumHelper.SetWindowSize(driver, width, height);
                            resultMsg = $"[Управление окном] Размер установлен: {width}x{height}";
                        }
                        break;

                    case WindowOperation.SetPosition:
                        {
                            string xStr = GetPropertyValue<string>(Prop_X, "Prop_X", sd) ?? "0";
                            string yStr = GetPropertyValue<string>(Prop_Y, "Prop_Y", sd) ?? "0";
                            int x = int.TryParse(xStr, out int xVal) ? xVal : 0;
                            int y = int.TryParse(yStr, out int yVal) ? yVal : 0;

                            SeleniumHelper.SetWindowPosition(driver, x, y);
                            resultMsg = $"[Управление окном] Позиция установлена: ({x}, {y})";
                        }
                        break;

                    case WindowOperation.GetSize:
                        {
                            var (width, height) = SeleniumHelper.GetWindowSize(driver);
                            SetVariableValue(Prop_OutWidth, width, sd);
                            SetVariableValue(Prop_OutHeight, height, sd);
                            resultMsg = $"[Управление окном] Размер получен: {width}x{height}";
                        }
                        break;

                    case WindowOperation.GetPosition:
                        {
                            var (x, y) = SeleniumHelper.GetWindowPosition(driver);
                            SetVariableValue(Prop_OutX, x, sd);
                            SetVariableValue(Prop_OutY, y, sd);
                            resultMsg = $"[Управление окном] Позиция получена: ({x}, {y})";
                        }
                        break;

                    default:
                        throw new NotSupportedException($"Операция {Prop_Operation} не поддерживается");
                }

                return CreateSuccessResult(resultMsg);
            }
            catch (Exception ex)
            {
                return CreateErrorResult($"Ошибка [Управление окном]: {ex.Message}");
            }
        }

        // ── Валидация ──────────────────────────────────────────────────

        public override ValidationResult Validate()
        {
            var ret = new ValidationResult();


            switch (Prop_Operation)
            {
                case WindowOperation.SetSize:
                    if (string.IsNullOrWhiteSpace(Prop_Width))
                        ret.Items.Add(new ValidationResult.ValidationItem()
                        {
                            PropertyName = "Width",
                            Error = "Ширина обязательна для операции SetSize"
                        });
                    if (string.IsNullOrWhiteSpace(Prop_Height))
                        ret.Items.Add(new ValidationResult.ValidationItem()
                        {
                            PropertyName = "Height",
                            Error = "Высота обязательна для операции SetSize"
                        });
                    break;

                case WindowOperation.SetPosition:
                    if (string.IsNullOrWhiteSpace(Prop_X))
                        ret.Items.Add(new ValidationResult.ValidationItem()
                        {
                            PropertyName = "X",
                            Error = "Позиция X обязательна для операции SetPosition"
                        });
                    if (string.IsNullOrWhiteSpace(Prop_Y))
                        ret.Items.Add(new ValidationResult.ValidationItem()
                        {
                            PropertyName = "Y",
                            Error = "Позиция Y обязательна для операции SetPosition"
                        });
                    break;

                case WindowOperation.GetSize:
                    ret.ValidateRequired(Prop_OutWidth, "Ширина (результат)", "Переменная для ширины обязательна");
                    ret.ValidateRequired(Prop_OutHeight, "Высота (результат)", "Переменная для высоты обязательна");
                    break;

                case WindowOperation.GetPosition:
                    ret.ValidateRequired(Prop_OutX, "Позиция X (результат)", "Переменная для X обязательна");
                    ret.ValidateRequired(Prop_OutY, "Позиция Y (результат)", "Переменная для Y обязательна");
                    break;
            }

            return ret;
        }
    }
}
