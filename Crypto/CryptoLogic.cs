// =============================================================================
// CryptoLogic.cs — чистая (тестируемая) логика крипто-утилит.
//
// Хэширование строк и файлов (MD5/SHA1/SHA256/SHA384/SHA512), симметричное
// шифрование AES-CBC с ключом из пароля (PBKDF2-SHA256, 100 000 итераций),
// Base64 кодирование/декодирование.
//
// Формат зашифрованной строки: Base64([salt 16 байт][IV 16 байт][шифртекст]).
// Соль генерируется случайно при каждом шифровании — вывод неоднозначен,
// но детерминированно расшифровывается тем же паролем.
// =============================================================================

using Primo.MIA.Common;
using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Primo.MIA
{
    /// <summary>
    /// Бизнес-логика крипто-активностей: хэширование, AES-шифрование, Base64.
    /// </summary>
    public class CryptoLogic
    {
        /// <summary>Размер соли PBKDF2 (байт).</summary>
        private const int SaltSizeBytes = 16;

        /// <summary>Размер IV AES (байт).</summary>
        private const int IvSizeBytes = 16;

        /// <summary>Размер ключа AES-256 (байт).</summary>
        private const int KeySizeBytes = 32;

        /// <summary>Итерации PBKDF2 (OWASP рекомендует не менее 100 000 для SHA-256).</summary>
        private const int Pbkdf2Iterations = 100_000;

        // ── Хэширование ───────────────────────────────────────────────────

        /// <summary>
        /// Вычисляет хэш строки.
        /// </summary>
        public string HashString(string input, CryptoHashAlgorithm algorithm, CryptoHashOutputFormat format)
        {
            Guard.NotNull(input, nameof(input));
            using (var hasher = CreateHasher(algorithm))
            {
                var hash = hasher.ComputeHash(Encoding.UTF8.GetBytes(input));
                return FormatHash(hash, format);
            }
        }

        /// <summary>
        /// Вычисляет хэш содержимого файла (потоково, без загрузки в память целиком).
        /// </summary>
        public string HashFile(string filePath, CryptoHashAlgorithm algorithm, CryptoHashOutputFormat format)
        {
            Guard.NotNullOrWhiteSpace(filePath, nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Файл не найден: {filePath}");

            using (var hasher = CreateHasher(algorithm))
            using (var stream = File.OpenRead(filePath))
            {
                var hash = hasher.ComputeHash(stream);
                return FormatHash(hash, format);
            }
        }

        private static HashAlgorithm CreateHasher(CryptoHashAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case CryptoHashAlgorithm.MD5: return MD5.Create();
                case CryptoHashAlgorithm.SHA1: return SHA1.Create();
                case CryptoHashAlgorithm.SHA256: return SHA256.Create();
                case CryptoHashAlgorithm.SHA384: return SHA384.Create();
                case CryptoHashAlgorithm.SHA512: return SHA512.Create();
                default:
                    throw new ArgumentException($"Неподдерживаемый алгоритм хэширования: {algorithm}", nameof(algorithm));
            }
        }

        private static string FormatHash(byte[] hash, CryptoHashOutputFormat format)
        {
            switch (format)
            {
                case CryptoHashOutputFormat.HexUpper:
                    return ToHex(hash, uppercase: true);
                case CryptoHashOutputFormat.Base64:
                    return Convert.ToBase64String(hash);
                case CryptoHashOutputFormat.HexLower:
                default:
                    return ToHex(hash, uppercase: false);
            }
        }

        private static string ToHex(byte[] bytes, bool uppercase)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                sb.Append(b.ToString(uppercase ? "X2" : "x2", CultureInfo.InvariantCulture));
            return sb.ToString();
        }

        // ── AES-шифрование строки паролем ────────────────────────────────

        /// <summary>
        /// Шифрует строку паролем (AES-256-CBC, PBKDF2-SHA256).
        /// Возвращает Base64([salt][IV][шифртекст]).
        /// </summary>
        public string EncryptString(string plaintext, string password)
        {
            Guard.NotNull(plaintext, nameof(plaintext));
            Guard.NotNullOrWhiteSpace(password, nameof(password));

            var salt = new byte[SaltSizeBytes];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }

            using (var derive = new Rfc2898DeriveBytes(password, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256))
            {
                var key = derive.GetBytes(KeySizeBytes);
                var iv = derive.GetBytes(IvSizeBytes);

                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var encryptor = aes.CreateEncryptor())
                    {
                        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
                        var ciphertext = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

                        // [salt][iv][ciphertext] → один Base64
                        var result = new byte[SaltSizeBytes + IvSizeBytes + ciphertext.Length];
                        Buffer.BlockCopy(salt, 0, result, 0, SaltSizeBytes);
                        Buffer.BlockCopy(iv, 0, result, SaltSizeBytes, IvSizeBytes);
                        Buffer.BlockCopy(ciphertext, 0, result, SaltSizeBytes + IvSizeBytes, ciphertext.Length);
                        return Convert.ToBase64String(result);
                    }
                }
            }
        }

        /// <summary>
        /// Расшифровывает строку, зашифрованную EncryptString тем же паролем.
        /// </summary>
        public string DecryptString(string encryptedBase64, string password)
        {
            Guard.NotNullOrWhiteSpace(encryptedBase64, nameof(encryptedBase64));
            Guard.NotNullOrWhiteSpace(password, nameof(password));

            byte[] payload;
            try
            {
                payload = Convert.FromBase64String(encryptedBase64);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException("Входные данные не являются корректной Base64-строкой", ex);
            }

            if (payload.Length < SaltSizeBytes + IvSizeBytes + 1)
                throw new InvalidOperationException("Зашифрованные данные слишком короткие");

            var salt = new byte[SaltSizeBytes];
            var iv = new byte[IvSizeBytes];
            Buffer.BlockCopy(payload, 0, salt, 0, SaltSizeBytes);
            Buffer.BlockCopy(payload, SaltSizeBytes, iv, 0, IvSizeBytes);

            using (var derive = new Rfc2898DeriveBytes(password, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256))
            {
                var key = derive.GetBytes(KeySizeBytes);

                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var decryptor = aes.CreateDecryptor())
                    {
                        var ciphertextLength = payload.Length - SaltSizeBytes - IvSizeBytes;
                        try
                        {
                            var plaintextBytes = decryptor.TransformFinalBlock(payload, SaltSizeBytes + IvSizeBytes, ciphertextLength);
                            return Encoding.UTF8.GetString(plaintextBytes);
                        }
                        catch (CryptographicException ex)
                        {
                            throw new InvalidOperationException(
                                "Не удалось расшифровать данные — вероятно, неверный пароль", ex);
                        }
                    }
                }
            }
        }

        // ── Base64 ────────────────────────────────────────────────────────

        /// <summary>Кодирует строку в Base64 (UTF-8).</summary>
        public string Base64Encode(string input)
        {
            Guard.NotNull(input, nameof(input));
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
        }

        /// <summary>Декодирует Base64-строку (UTF-8).</summary>
        public string Base64Decode(string input)
        {
            Guard.NotNullOrWhiteSpace(input, nameof(input));

            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(input));
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException("Входные данные не являются корректной Base64-строкой", ex);
            }
        }
    }
}
