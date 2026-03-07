using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Primo.MIA;

namespace Primo.MIA.Tests.Logic
{
    /// <summary>
    /// Бизнес-логика поиска файлов
    /// </summary>
    public class SearchFilesLogic
    {
        /// <summary>
        /// Поиск файлов в директории
        /// </summary>
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

            // Поиск файлов
            if (searchType == SearchType.FilesOnly || searchType == SearchType.FilesAndFolders)
            {
                var files = FindFiles(directoryPath, pattern, filterType, searchOption);
                results.AddRange(files);
            }

            // Поиск папок
            if (searchType == SearchType.FoldersOnly || searchType == SearchType.FilesAndFolders)
            {
                var folders = FindFolders(directoryPath, pattern, filterType, searchOption);
                results.AddRange(folders);
            }

            return results.OrderBy(x => x).ToList();
        }

        /// <summary>
        /// Поиск файлов
        /// </summary>
        private List<string> FindFiles(string directoryPath, string pattern, SearchFilterType filterType, SearchOption searchOption)
        {
            var dirInfo = new DirectoryInfo(directoryPath);

            if (filterType == SearchFilterType.Wildcard)
            {
                return dirInfo.GetFiles(pattern, searchOption)
                    .Select(f => f.FullName)
                    .ToList();
            }
            else // Regex
            {
                var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                return dirInfo.GetFiles("*", searchOption)
                    .Where(f => regex.IsMatch(f.Name))
                    .Select(f => f.FullName)
                    .ToList();
            }
        }

        /// <summary>
        /// Поиск папок
        /// </summary>
        private List<string> FindFolders(string directoryPath, string pattern, SearchFilterType filterType, SearchOption searchOption)
        {
            var dirInfo = new DirectoryInfo(directoryPath);

            if (filterType == SearchFilterType.Wildcard)
            {
                return dirInfo.GetDirectories(pattern, searchOption)
                    .Select(d => d.FullName + "\\")
                    .ToList();
            }
            else // Regex
            {
                var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                return dirInfo.GetDirectories("*", searchOption)
                    .Where(d => regex.IsMatch(d.Name))
                    .Select(d => d.FullName + "\\")
                    .ToList();
            }
        }
    }
}
