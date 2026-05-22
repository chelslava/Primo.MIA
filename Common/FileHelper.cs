// =============================================================================
// FileHelper.cs — вспомогательные методы для работы с файлами.
//
// Содержит общую логику работы с файловой системой.
// =============================================================================

using System;
using System.IO;

namespace Primo.MIA.Common
{
    /// <summary>
    /// Вспомогательные методы для работы с файлами.
    /// </summary>
    public static class FileHelper
    {
        /// <summary>
        /// Генерирует уникальный путь к файлу.
        /// Если файл directory\baseName+extension уже существует —
        /// перебирает суффиксы (1), (2)... до нахождения свободного имени.
        /// Возвращает полный путь к файлу (файл при этом НЕ создаётся).
        /// </summary>
        public static string GenerateUniqueFilePath(string directory, string baseName, string extension)
        {
            // Если директория пустая — работаем только с именем файла
            string Combine(string name) => string.IsNullOrEmpty(directory)
                ? name + extension
                : Path.Combine(directory, name + extension);

            string candidate = Combine(baseName);

            // Если файл не существует — сразу возвращаем
            if (!File.Exists(candidate)) return candidate;

            // Перебираем суффиксы пока не найдём свободное имя
            for (int counter = 1; counter <= 9999; counter++)
            {
                candidate = Combine($"{baseName}({counter})");
                if (!File.Exists(candidate)) return candidate;
            }

            // Если все 9999 имён заняты — добавляем GUID для гарантированной уникальности
            return Combine($"{baseName}_{Guid.NewGuid():N}");
        }

        /// <summary>
        /// Нормализует расширение файла: добавляет точку если отсутствует.
        /// </summary>
        public static string NormalizeExtension(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return string.Empty;

            return extension.StartsWith(".") ? extension : "." + extension;
        }

        /// <summary>
        /// Проверяет существование директории и выбрасывает исключение если не найдена.
        /// </summary>
        public static void ValidateDirectoryExists(string directoryPath, string parameterName = "Директория")
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException($"{parameterName} не может быть пустой");

            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"{parameterName} не найдена: {directoryPath}");
        }

        /// <summary>
        /// Проверяет существование файла и выбрасывает исключение если не найден.
        /// </summary>
        public static void ValidateFileExists(string filePath, string parameterName = "Файл")
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException($"{parameterName} не может быть пустым");

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"{parameterName} не найден: {filePath}");
        }
    }
}
