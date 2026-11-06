namespace Primo.MIA
{
    /// <summary>Алгоритм хэширования.</summary>
    public enum CryptoHashAlgorithm
    {
        /// <summary>MD5 (128 бит; не использовать для безопасности).</summary>
        MD5 = 0,

        /// <summary>SHA-1 (160 бит).</summary>
        SHA1 = 1,

        /// <summary>SHA-256 (рекомендуется).</summary>
        SHA256 = 2,

        /// <summary>SHA-384.</summary>
        SHA384 = 3,

        /// <summary>SHA-512.</summary>
        SHA512 = 4
    }

    /// <summary>Формат вывода хэша.</summary>
    public enum CryptoHashOutputFormat
    {
        /// <summary>Hex-строка в нижнем регистре (по умолчанию).</summary>
        HexLower = 0,

        /// <summary>Hex-строка в верхнем регистре.</summary>
        HexUpper = 1,

        /// <summary>Base64.</summary>
        Base64 = 2
    }

    /// <summary>Источник данных для хэширования.</summary>
    public enum CryptoHashSource
    {
        /// <summary>Хэшировать входную строку.</summary>
        String = 0,

        /// <summary>Хэшировать содержимое файла.</summary>
        File = 1
    }
}
