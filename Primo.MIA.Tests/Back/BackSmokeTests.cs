using FluentAssertions;
using LTools.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Primo.MIA.Tests.Back
{
    /// <summary>
    /// Смоук-тесты слоя Back (issue #46): для каждого Back-класса модулей
    /// Archive/Calendar/Crypto/Database/Browser проверяется, что
    ///   1) Validate() на экземпляре с пустыми свойствами НЕ бросает исключений
    ///      и возвращает ValidationResult;
    ///   2) там, где класс на пустых свойствах обязан найти ошибки
    ///      (expectValidationErrors), они действительно возвращаются;
    ///   3) GroupName начинается с категории MIA.
    /// Конструктор Back-классов требует реальный IWFContainer от Primo Studio,
    /// поэтому экземпляр создаётся через FormatterServices.GetUninitializedObject
    /// (как в LogMessageBackTests): Validate() читает только строковые свойства
    /// и enum-поля (обнуляются в дефолт).
    ///
    /// expectValidationErrors=false — для классов, у которых конфигурация по
    /// умолчанию легитимно валидна (нет обязательных полей: BrowserOpen/Close/
    /// GetInfo/Screenshot; дефолтная операция без обязательных вводов:
    /// AlertHandle.Accept, TabManage.OpenNewTab, WindowManage.Maximize).
    /// </summary>
    [Trait("Category", "Fast")]
    public class BackSmokeTests
    {
        public static IEnumerable<object[]> AllBackTypes()
        {
            yield return new object[] { typeof(ZipArchiveCreateBack), true };
            yield return new object[] { typeof(ZipArchiveExtractBack), true };

            yield return new object[] { typeof(BusinessCalendarBack), true };
            yield return new object[] { typeof(CalendarLoadBack), true };
            yield return new object[] { typeof(CalendarNextDayOfWeekBack), true };
            yield return new object[] { typeof(CalendarNthWorkdayBack), true };
            yield return new object[] { typeof(CalendarWorkdaysListBack), true };

            yield return new object[] { typeof(Base64DecodeBack), true };
            yield return new object[] { typeof(Base64EncodeBack), true };
            yield return new object[] { typeof(DecryptStringBack), true };
            yield return new object[] { typeof(EncryptStringBack), true };
            yield return new object[] { typeof(HashCalculateBack), true };
            yield return new object[] { typeof(PgpDecryptFileBack), true };
            yield return new object[] { typeof(PgpEncryptFileBack), true };

            yield return new object[] { typeof(DatabaseBulkInsertBack), true };
            yield return new object[] { typeof(DatabaseCheckConnectionBack), true };
            yield return new object[] { typeof(DatabaseGetTableSchemaBack), true };
            yield return new object[] { typeof(DatabaseListColumnsBack), true };
            yield return new object[] { typeof(DatabaseListTablesBack), true };
            yield return new object[] { typeof(DatabaseNonQueryBack), true };
            yield return new object[] { typeof(DatabaseQueryBack), true };
            yield return new object[] { typeof(DatabaseQueryPagedBack), true };
            yield return new object[] { typeof(DatabaseScalarBack), true };
            yield return new object[] { typeof(DatabaseSetPasswordBack), true };
            yield return new object[] { typeof(DatabaseStoredProcedureBack), true };
            yield return new object[] { typeof(DatabaseTableExistsBack), true };
            yield return new object[] { typeof(DatabaseTransactionBeginBack), true };
            yield return new object[] { typeof(DatabaseTransactionCommitBack), true };
            yield return new object[] { typeof(DatabaseTransactionRollbackBack), true };
            yield return new object[] { typeof(DatabaseUpsertBack), true };

            // Browser: 23 класса обязаны найти ошибки на пустых свойствах
            yield return new object[] { typeof(AlertHandleBack), false };
            yield return new object[] { typeof(BrowserCloseBack), false };
            yield return new object[] { typeof(BrowserExecuteJavaScriptBack), true };
            yield return new object[] { typeof(BrowserGetInfoBack), false };
            yield return new object[] { typeof(BrowserGetLogsBack), true };
            yield return new object[] { typeof(BrowserManageCookiesBack), true };
            yield return new object[] { typeof(BrowserNavigateBack), true };
            yield return new object[] { typeof(BrowserOpenBack), false };
            yield return new object[] { typeof(BrowserScreenshotBack), false };
            yield return new object[] { typeof(BrowserStorageManageBack), true };
            yield return new object[] { typeof(BrowserSwitchToBack), true };
            yield return new object[] { typeof(BrowserTabManageBack), false };
            yield return new object[] { typeof(BrowserWaitForBack), true };
            yield return new object[] { typeof(BrowserWindowManageBack), false };
            yield return new object[] { typeof(ElementClickBack), true };
            yield return new object[] { typeof(ElementDragDropBack), true };
            yield return new object[] { typeof(ElementFindBack), true };
            yield return new object[] { typeof(ElementGetComputedStyleBack), true };
            yield return new object[] { typeof(ElementGetInfoBack), true };
            yield return new object[] { typeof(ElementGetPropertyBack), true };
            yield return new object[] { typeof(ElementGetRectBack), true };
            yield return new object[] { typeof(ElementGetScreenshotBack), true };
            yield return new object[] { typeof(ElementHoverBack), true };
            yield return new object[] { typeof(ElementInputBack), true };
            yield return new object[] { typeof(ElementScrollToBack), true };
            yield return new object[] { typeof(ElementSearchBack), true };
            yield return new object[] { typeof(ElementSelectBack), true };
            yield return new object[] { typeof(ElementSelectMultipleBack), true };
            yield return new object[] { typeof(ElementSubmitBack), true };
            yield return new object[] { typeof(ElementUploadFileBack), true };
        }

        [Theory(DisplayName = "Смоук Back: Validate() на пустых свойствах не бросает и валидирует")]
        [MemberData(nameof(AllBackTypes))]
        public void Validate_EmptyProperties_NoThrow(Type backType, bool expectValidationErrors)
        {
            var instance = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(backType);

            var validateMethod = backType.GetMethod("Validate", Type.EmptyTypes);
            validateMethod.Should().NotBeNull($"{backType.Name} должен иметь Validate()");

            ValidationResult result = null;
            Action act = () => result = (ValidationResult)validateMethod.Invoke(instance, null);
            act.Should().NotThrow($"{backType.Name}.Validate() не должен бросать на пустых свойствах");

            bool overridesValidate = validateMethod.DeclaringType == backType;
            if (overridesValidate)
            {
                result.Should().NotBeNull($"{backType.Name} переопределяет Validate()");

                if (expectValidationErrors)
                {
                    result.Items.Should().NotBeEmpty(
                        $"{backType.Name}.Validate() обязан пометить пустые обязательные поля");
                }
            }
        }

        [Theory(DisplayName = "Смоук Back: GroupName начинается с категории MIA")]
        [MemberData(nameof(AllBackTypes))]
        public void GroupName_StartsWithMiaCategory(Type backType, bool expectValidationErrors)
        {
            var instance = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(backType);

            var property = backType.GetProperty("GroupName");
            property.Should().NotBeNull($"{backType.Name} должен иметь GroupName");

            var value = property.GetValue(instance) as string;
            value.Should().StartWith("MIA", $"{backType.Name}.GroupName в категории MIA");
        }

        /// <summary>
        /// Instance-поле с проходом по иерархии (sdkComponentName объявлен
        /// в базовом классе SDK).
        /// </summary>
        private static object GetInstanceFieldValue(object instance, string fieldName)
        {
            for (var type = instance.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(fieldName,
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (field != null)
                    return field.GetValue(instance);
            }
            return null;
        }
    }
}
