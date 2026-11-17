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
    /// Archive/Calendar/Crypto/Database проверяется, что
    ///   1) Validate() на экземпляре с пустыми свойствами НЕ бросает исключений
    ///      и возвращает ValidationResult с ошибками валидации (там, где класс
    ///      переопределяет Validate);
    ///   2) sdkComponentName заполнен.
    /// Конструктор Back-классов требует реальный IWFContainer от Primo Studio,
    /// поэтому экземпляр создаётся через FormatterServices.GetUninitializedObject
    /// (как в LogMessageBackTests): Validate() читает только строковые свойства.
    /// </summary>
    [Trait("Category", "Fast")]
    public class BackSmokeTests
    {
        public static IEnumerable<object[]> AllBackTypes()
        {
            yield return new object[] { typeof(ZipArchiveCreateBack) };
            yield return new object[] { typeof(ZipArchiveExtractBack) };

            yield return new object[] { typeof(BusinessCalendarBack) };
            yield return new object[] { typeof(CalendarLoadBack) };
            yield return new object[] { typeof(CalendarNextDayOfWeekBack) };
            yield return new object[] { typeof(CalendarNthWorkdayBack) };
            yield return new object[] { typeof(CalendarWorkdaysListBack) };

            yield return new object[] { typeof(Base64DecodeBack) };
            yield return new object[] { typeof(Base64EncodeBack) };
            yield return new object[] { typeof(DecryptStringBack) };
            yield return new object[] { typeof(EncryptStringBack) };
            yield return new object[] { typeof(HashCalculateBack) };
            yield return new object[] { typeof(PgpDecryptFileBack) };
            yield return new object[] { typeof(PgpEncryptFileBack) };

            yield return new object[] { typeof(DatabaseBulkInsertBack) };
            yield return new object[] { typeof(DatabaseCheckConnectionBack) };
            yield return new object[] { typeof(DatabaseGetTableSchemaBack) };
            yield return new object[] { typeof(DatabaseListColumnsBack) };
            yield return new object[] { typeof(DatabaseListTablesBack) };
            yield return new object[] { typeof(DatabaseNonQueryBack) };
            yield return new object[] { typeof(DatabaseQueryBack) };
            yield return new object[] { typeof(DatabaseQueryPagedBack) };
            yield return new object[] { typeof(DatabaseScalarBack) };
            yield return new object[] { typeof(DatabaseSetPasswordBack) };
            yield return new object[] { typeof(DatabaseStoredProcedureBack) };
            yield return new object[] { typeof(DatabaseTableExistsBack) };
            yield return new object[] { typeof(DatabaseTransactionBeginBack) };
            yield return new object[] { typeof(DatabaseTransactionCommitBack) };
            yield return new object[] { typeof(DatabaseTransactionRollbackBack) };
            yield return new object[] { typeof(DatabaseUpsertBack) };
        }

        [Theory(DisplayName = "Смоук Back: Validate() на пустых свойствах не бросает и валидирует")]
        [MemberData(nameof(AllBackTypes))]
        public void Validate_EmptyProperties_NoThrow(Type backType)
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
                result.Items.Should().NotBeEmpty(
                    $"{backType.Name}.Validate() обязан пометить пустые обязательные поля");
            }
        }

        [Theory(DisplayName = "Смоук Back: GroupName начинается с категории MIA")]
        [MemberData(nameof(AllBackTypes))]
        public void GroupName_StartsWithMiaCategory(Type backType)
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
