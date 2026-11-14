using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace Primo.MIA.Tests.Common
{
    /// <summary>
    /// Сверка заголовков активностей с именами из палитры (issue #38).
    ///
    /// UI-лейблы в XAML дублируют константы ActivityStrings.cs inline —
    /// полная сверка невозможна без ложных срабатываний (выходные переменные
    /// намеренно не имеют полей в XAML), поэтому тест защищает самый видимый
    /// слой: заголовок ActivityHeader в XAML обязан совпадать с константой
    /// Activity_*, которую Back-файл отдаёт в Studio (sdkComponentName).
    /// XAML с динамическими заголовками (&lt;Run/&gt;) пропускаются.
    /// Тест читает исходники из дерева репозитория (CI/локальная сборка
    /// всегда идут из checkout).
    /// </summary>
    [Trait("Category", "Fast")]
    public class ActivityHeaderConsistencyTests
    {
        private static readonly string RepoRoot = FindRepoRoot();

        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Primo.MIA.csproj")))
                dir = dir.Parent;
            if (dir == null)
                throw new InvalidOperationException(
                    "Корень репозитория не найден от " + AppContext.BaseDirectory);
            return dir.FullName;
        }

        private static Dictionary<string, string> ReadActivityConstants()
        {
            var text = File.ReadAllText(
                Path.Combine(RepoRoot, "Common", "ActivityStrings.cs"));
            var map = new Dictionary<string, string>();
            foreach (Match m in Regex.Matches(
                text, @"public const string (Activity_\w+)\s*=\s*""([^""]*)"""))
                map[m.Groups[1].Value] = m.Groups[2].Value;
            return map;
        }

        public static IEnumerable<object[]> ActivityHeaderFiles()
        {
            var files = Directory.GetFiles(RepoRoot, "*Back.cs", SearchOption.AllDirectories);
            var result = new List<object[]>();
            foreach (var back in files)
            {
                if (back.Contains(@"\obj\") || back.Contains(@"\bin\"))
                    continue;
                var xaml = back.Substring(0, back.Length - "Back.cs".Length) + ".xaml";
                if (!File.Exists(xaml))
                    continue;
                var backText = File.ReadAllText(back);
                var nameMatch = Regex.Match(
                    backText, @"sdkComponentName\s*=\s*ActivityStrings\.(\w+)");
                if (!nameMatch.Success)
                    continue;
                var xamlText = File.ReadAllText(xaml);
                // Только inline-заголовки: Text="..." рядом со стилем ActivityHeader
                var header = Regex.Match(
                    xamlText,
                    @"<TextBlock[^>]*Style=""\{StaticResource ActivityHeader\}""[^>]*\bText=""([^""]*)""",
                    RegexOptions.Singleline);
                if (!header.Success)
                    continue; // динамические (<Run/>) или отсутствующие — не сверяем
                result.Add(new object[]
                {
                    Path.GetFileName(back),
                    nameMatch.Groups[1].Value,
                    header.Groups[1].Value
                });
            }
            return result;
        }

        [Theory]
        [MemberData(nameof(ActivityHeaderFiles))]
        public void XamlHeader_Matches_PanelName(string backFile, string constantName, string xamlHeader)
        {
            var constants = ReadActivityConstants();
            constants.Should().ContainKey(constantName,
                "константа {0} должна существовать в ActivityStrings.cs", constantName);

            xamlHeader.Should().Be(
                constants[constantName],
                "заголовок в {0} должен совпадать с именем элемента в палитре Studio",
                backFile);
        }

        [Fact(DisplayName = "Страж: сверка покрывает все inline-заголовки, а не ноль")]
        public void Test_Finds_Coverage()
        {
            // Если конвенция заголовков изменится и проверок станет 0 —
            // тест молча выродится; минимальная планка это исключает
            ActivityHeaderFiles().Should().HaveCountGreaterOrEqualTo(9);
        }
    }
}
