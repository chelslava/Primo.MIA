// =============================================================================
// CryptoLogic.cs — чистая (тестируемая) логика крипто-утилит.
//
// Хэширование строк и файлов (MD5/SHA1/SHA256/SHA384/SHA512), симметричное
// шифрование строки паролем, Base64 кодирование/декодирование.
//
// Шифрование строки (v2, по умолчанию) — аутентифицированное AES-256-GCM
// (BouncyCastle): неверный пароль или изменение данных всегда обнаруживаются
// (GCM-тег), а не дают мусор при случайно валидном паддинге — дефект старого
// CBC-формата (flaky 1/256, найден в CI). Формат: MIA2.<Base64(
// [salt 16][nonce 12][шифртекст+тег 16])>; соль и nonce случайны.
// Старый формат CBC ([salt][IV][шифртекст], без маркера) читается для
// совместимости — ранее зашифрованные строки продолжают расшифровываться.
// =============================================================================

using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
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

        /// <summary>Размер IV AES в старом CBC-формате (байт).</summary>
        private const int IvSizeBytes = 16;

        /// <summary>Размер ключа AES-256 (байт).</summary>
        private const int KeySizeBytes = 32;

        /// <summary>Размер nonce GCM (байт, стандарт 96 бит).</summary>
        private const int NonceSizeBytes = 12;

        /// <summary>Размер тега аутентификации GCM (байт).</summary>
        private const int TagSizeBytes = 16;

        /// <summary>Маркер нового аутентифицированного формата.</summary>
        private const string V2Prefix = "MIA2.";

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
        /// Шифрует строку паролем — аутентифицированное AES-256-GCM,
        /// ключ из пароля (PBKDF2-SHA256, 100 000 итераций).
        /// Возвращает MIA2.&lt;Base64([salt 16][nonce 12][шифртекст+тег])&gt;.
        /// </summary>
        public string EncryptString(string plaintext, string password)
        {
            Guard.NotNull(plaintext, nameof(plaintext));
            Guard.NotNullOrWhiteSpace(password, nameof(password));

            var salt = new byte[SaltSizeBytes];
            var nonce = new byte[NonceSizeBytes];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
                rng.GetBytes(nonce);
            }

            var key = DeriveKey(password, salt);
            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

            var cipher = new GcmBlockCipher(new AesEngine());
            cipher.Init(true, new AeadParameters(new KeyParameter(key), TagSizeBytes * 8, nonce));
            var combined = new byte[cipher.GetOutputSize(plaintextBytes.Length)];
            cipher.DoFinal(combined, cipher.ProcessBytes(plaintextBytes, 0, plaintextBytes.Length, combined, 0));

            // [salt][nonce][шифртекст+тег] → Base64 с маркером версии
            var payload = new byte[SaltSizeBytes + NonceSizeBytes + combined.Length];
            Buffer.BlockCopy(salt, 0, payload, 0, SaltSizeBytes);
            Buffer.BlockCopy(nonce, 0, payload, SaltSizeBytes, NonceSizeBytes);
            Buffer.BlockCopy(combined, 0, payload, SaltSizeBytes + NonceSizeBytes, combined.Length);
            return V2Prefix + Convert.ToBase64String(payload);
        }

        /// <summary>
        /// Расшифровывает строку, зашифрованную EncryptString тем же паролем.
        /// Новый формат (MIA2., GCM): неверный пароль или изменение данных
        /// всегда дают ошибку. Старый формат (CBC, без маркера) читается
        /// для совместимости.
        /// </summary>
        public string DecryptString(string encryptedBase64, string password)
        {
            Guard.NotNullOrWhiteSpace(encryptedBase64, nameof(encryptedBase64));
            Guard.NotNullOrWhiteSpace(password, nameof(password));

            if (encryptedBase64.StartsWith(V2Prefix, StringComparison.Ordinal))
                return DecryptStringV2(encryptedBase64.Substring(V2Prefix.Length), password);
            return DecryptStringLegacy(encryptedBase64, password);
        }

        /// <summary>v2: GCM-расшифровка.</summary>
        private string DecryptStringV2(string base64Payload, string password)
        {
            var payload = FromBase64OrThrow(base64Payload);
            if (payload.Length < SaltSizeBytes + NonceSizeBytes + TagSizeBytes)
                throw new InvalidOperationException("Зашифрованные данные слишком короткие");

            var salt = new byte[SaltSizeBytes];
            var nonce = new byte[NonceSizeBytes];
            Buffer.BlockCopy(payload, 0, salt, 0, SaltSizeBytes);
            Buffer.BlockCopy(payload, SaltSizeBytes, nonce, 0, NonceSizeBytes);

            var key = DeriveKey(password, salt);
            var cipher = new GcmBlockCipher(new AesEngine());
            cipher.Init(false, new AeadParameters(new KeyParameter(key), TagSizeBytes * 8, nonce));

            var combined = new byte[payload.Length - SaltSizeBytes - NonceSizeBytes];
            Buffer.BlockCopy(payload, SaltSizeBytes + NonceSizeBytes, combined, 0, combined.Length);

            try
            {
                var plaintextBytes = new byte[cipher.GetOutputSize(combined.Length)];
                cipher.DoFinal(plaintextBytes, cipher.ProcessBytes(combined, 0, combined.Length, plaintextBytes, 0));
                return Encoding.UTF8.GetString(plaintextBytes);
            }
            catch (Org.BouncyCastle.Crypto.InvalidCipherTextException ex)
            {
                throw new InvalidOperationException(
                    "Не удалось расшифровать данные — неверный пароль или данные изменены", ex);
            }
        }

        /// <summary>
        /// Legacy (до v2): AES-256-CBC, Base64([salt][IV][шифртекст]).
        /// Только чтение — без аутентификации, неверный паддинг (~1/256)
        /// даёт мусор без ошибки; для новых данных используйте EncryptString.
        /// </summary>
        private string DecryptStringLegacy(string encryptedBase64, string password)
        {
            var payload = FromBase64OrThrow(encryptedBase64);

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

        /// <summary>PBKDF2-SHA256 из пароля и соли → ключ AES-256.</summary>
        private static byte[] DeriveKey(string password, byte[] salt)
        {
            using (var derive = new Rfc2898DeriveBytes(password, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256))
                return derive.GetBytes(KeySizeBytes);
        }

        /// <summary>Base64-декодирование с понятной ошибкой.</summary>
        private static byte[] FromBase64OrThrow(string base64)
        {
            try
            {
                return Convert.FromBase64String(base64);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException("Входные данные не являются корректной Base64-строкой", ex);
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
