using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Primo.MIA
{
    /// <summary>
    /// Чистая логика поиска файлов и папок без зависимости от SDK.
    /// Используется как production-кодом, так и unit-тестами.
    /// </summary>
    public class SearchFilesLogic
    {
        public List<string> SearchFiles(
            string directoryPath,
            string pattern,
            SearchFilterType filterType,
            SearchType searchType,
            bool searchInSubfolders)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("Путь к директории не может быть пустым");

            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"Директория не найдена: {directoryPath}");

            if (string.IsNullOrWhiteSpace(pattern))
                throw new ArgumentException("Паттерн поиска не может быть пустым");

            var results = new List<string>();
            var searchOption = searchInSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            if (searchType == SearchType.FilesOnly || searchType == SearchType.FilesAndFolders)
            {
                results.AddRange(FindFiles(directoryPath, pattern, filterType, searchOption));
            }

            if (searchType == SearchType.FoldersOnly || searchType == SearchType.FilesAndFolders)
            {
                results.AddRange(FindFolders(directoryPath, pattern, filterType, searchOption));
            }

            return results.OrderBy(x => x).ToList();
        }

        private List<string> FindFiles(string directoryPath, string pattern, SearchFilterType filterType, SearchOption searchOption)
        {
            var dirInfo = new DirectoryInfo(directoryPath);

            if (filterType == SearchFilterType.Wildcard)
            {
                return dirInfo.GetFiles(pattern, searchOption)
                    .Select(f => f.FullName)
                    .ToList();
            }

            var regex = CreateRegex(pattern);
            return dirInfo.GetFiles("*", searchOption)
                .Where(f => regex.IsMatch(f.Name))
                .Select(f => f.FullName)
                .ToList();
        }

        private List<string> FindFolders(string directoryPath, string pattern, SearchFilterType filterType, SearchOption searchOption)
        {
            var dirInfo = new DirectoryInfo(directoryPath);

            if (filterType == SearchFilterType.Wildcard)
            {
                return dirInfo.GetDirectories(pattern, searchOption)
                    .Select(d => d.FullName + "\\")
                    .ToList();
            }

            var regex = CreateRegex(pattern);
            return dirInfo.GetDirectories("*", searchOption)
                .Where(d => regex.IsMatch(d.Name))
                .Select(d => d.FullName + "\\")
                .ToList();
        }

        private static Regex CreateRegex(string pattern)
        {
            try
            {
                return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException($"Некорректный regex паттерн: {pattern}. Ошибка: {ex.Message}");
            }
        }
    }
}
