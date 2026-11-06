namespace Primo.MIA
{
    /// <summary>Алгоритм хэширования.</summary>
    public enum CryptoHashAlgorithm
    {
        /// <summary>MD5 (128 бит; не использовать для безопасности).</summary>
        [RuName("MD5")]
        MD5 = 0,

        /// <summary>SHA-1 (160 бит).</summary>
        [RuName("SHA-1")]
        SHA1 = 1,

        /// <summary>SHA-256 (рекомендуется).</summary>
        [RuName("SHA-256")]
        SHA256 = 2,

        /// <summary>SHA-384.</summary>
        [RuName("SHA-384")]
        SHA384 = 3,

        /// <summary>SHA-512.</summary>
        [RuName("SHA-512")]
        SHA512 = 4
    }

    /// <summary>Формат вывода хэша.</summary>
    public enum CryptoHashOutputFormat
    {
        /// <summary>Hex-строка в нижнем регистре (по умолчанию).</summary>
        [RuName("Hex (нижний регистр)")]
        HexLower = 0,

        /// <summary>Hex-строка в верхнем регистре.</summary>
        [RuName("Hex (верхний регистр)")]
        HexUpper = 1,

        /// <summary>Base64.</summary>
        [RuName("Base64")]
        Base64 = 2
    }

    /// <summary>Источник данных для хэширования.</summary>
    public enum CryptoHashSource
    {
        /// <summary>Хэшировать входную строку.</summary>
        [RuName("Строка")]
        String = 0,

        /// <summary>Хэшировать содержимое файла.</summary>
        [RuName("Файл")]
        File = 1
    }
}
