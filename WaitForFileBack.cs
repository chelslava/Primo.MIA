using LTools.Common.Model;
using LTools.Common.UIElements;
using LTools.SDK;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static LTools.Common.Helpers.WFHelper.PropertiesItem;

namespace Primo.MIA
{
    /// <summary>
    /// Timed-out element
    /// </summary>
    public class WaitForFileBack : PrimoComponentSimple<WaitForFile>
    {
        /// <summary>
        /// Group name
        /// </summary>
        private const string CGroupName = "MIA";

        /// <summary>
        /// Group name
        /// </summary>
        public override string GroupName
        {
            get => CGroupName;
            protected set { }
        }

        private string prop_PathFile;
        /// <summary>
        /// Property My Prop 1
        /// </summary>
        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(string))]
        [System.ComponentModel.Category("Input"), System.ComponentModel.DisplayName("Путь к файлу")]
        public string Prop_PathFile
        {
            get { return this.prop_PathFile; }
            set { this.prop_PathFile = value; this.InvokePropertyChanged(this, "Prop_PathFile"); }
        }

        private string prop_FileExists;

        [LTools.Common.Model.Serialization.StoringProperty]
        [LTools.Common.Model.Studio.ValidateReturnScript(DataType = typeof(bool))]
        [System.ComponentModel.Category("Output"), System.ComponentModel.DisplayName("Флаг присутствия файла")]
        public string Prop_FileExists
        {
            get => prop_FileExists;
            set
            {
                prop_FileExists = value; this.InvokePropertyChanged(this, "Prop_FileExists");
            }
        }

        public WaitForFileBack(IWFContainer container) : base(container)
        {
            sdkComponentName = "Ожидание файла";
            sdkComponentHelp = "Активность ожидает файл в заданной папке";
            sdkComponentIcon = "pack://application:,,/Primo.SDKSample;component/Images/sample.png";
            sdkProperties = new List<LTools.Common.Helpers.WFHelper.PropertiesItem>()
            {
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_PathFile",
                    PropertyType = LTools.Common.Helpers.WFHelper.PropertiesItem.PropertyTypes.SCRIPT,
                    EditorType = ScriptEditorTypes.FILE_SELECTOR,
                    DataType = typeof(string), ToolTip = "Путь к файлу", IsReadOnly = false
                },
                new LTools.Common.Helpers.WFHelper.PropertiesItem()
                {
                    PropName = "Prop_FileExists",
                    PropertyType = LTools.Common.Helpers.WFHelper.PropertiesItem.PropertyTypes.VARIABLE,
                    EditorType = ScriptEditorTypes.NONE,
                    DataType = typeof(bool), ToolTip = "SDK Tooltip2", IsReadOnly = false
                }
            };
            InitClass(container);
            //this.Prop1 = this.IsNoCode("Prop1") ? "test text" : "\"test text\"";
        }

        /// <summary>
        /// Main action
        /// </summary>
        /// <param name="sd"></param>
        /// <returns></returns>
        public override ExecutionResult SimpleAction(ScriptingData sd)
        {
            try
            {
                string path = GetPropertyValue<string>(this.Prop_PathFile, "Prop_PathFile", sd);
                int timeout = 30000; // 30 секунд

                if (WaitForFile(path, timeout))
                {
                    SetVariableValue(this.Prop_FileExists, true, sd);
                }
                else
                {
                    SetVariableValue(this.Prop_FileExists, false, sd);
                }

                return new ExecutionResult() { IsSuccess = true, SuccessMessage = "Done" };
            }
            catch (Exception ex)
            {
                return new ExecutionResult() { IsSuccess = false, ErrorMessage = ex?.Message };
            }
        }

        /// <summary>
        /// Ожидает существования файла по указанному пути в течение заданного времени.
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <param name="timeoutMs">Таймаут в миллисекундах</param>
        /// <returns>True, если файл существует в течение таймаута, иначе false</returns>
        public static bool WaitForFile(string filePath, int timeoutMs)
        {
            var start = DateTime.UtcNow;
            var fileInfo = new FileInfo(filePath);

            while (!fileInfo.Exists)
            {
                if ((DateTime.UtcNow - start).TotalMilliseconds > timeoutMs)
                {
                    return false; // Таймаут истёк
                }
                Thread.Sleep(100); // Ждём 100 мс перед следующей проверкой
            }

            return true;
        }

        /// <summary>
        /// Syntax check
        /// </summary>
        /// <returns></returns>
        public override ValidationResult Validate()
        {
            ValidationResult ret = new ValidationResult();
            if (String.IsNullOrEmpty(this.Prop_PathFile)) ret.Items.Add(new ValidationResult.ValidationItem() { PropertyName = "Путь к файлу", Error = "Text not specified" });
            return ret;
        }
    }
}
