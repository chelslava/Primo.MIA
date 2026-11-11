// =============================================================================
// PgpLogic.cs — чистая логика PGP-шифрования файлов (issue #35).
//
// OpenPGP через BouncyCastle.Cryptography (уже транзитивная зависимость
// через SSH.NET — новый пакет не добавлен). ASCII-armored и binary.
// Ключи: файлы .asc/.gpg (публичный для шифрования, приватный + passphrase
// для расшифровки). Подписи — вне v1.
//
// Структура PGP-сообщения: PgpEncryptedDataList (по пакету на получателя)
// → для нашего KeyId GetDataStream(privateKey) → (PgpCompressedData)?
// → PgpLiteralData (файл). Вход читается дважды (поиск конверта + расшифровка)
// — для файловых объёмов RPA это приемлемая цена за простоту и надёжность.
// =============================================================================

using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Security;
using Primo.MIA.Common;
using System;
using System.IO;
using System.Linq;

namespace Primo.MIA
{
    /// <summary>
    /// Логика PGP-шифрования и расшифровки файлов.
    /// </summary>
    public class PgpLogic
    {
        // ── Шифрование ─────────────────────────────────────────────────

        /// <summary>
        /// Шифрует файл публичным ключом получателя (AES-256 + integrity).
        /// Выход пересоздаётся при существовании.
        /// </summary>
        /// <param name="inputPath">Исходный файл.</param>
        /// <param name="outputPath">Путь зашифрованного файла (.pgp/.asc).</param>
        /// <param name="publicKeyPath">Путь к публичному ключу (.asc/.gpg).</param>
        /// <param name="armor">ASCII-armored выход (для почты/файлового обмена).</param>
        public long EncryptFile(string inputPath, string outputPath, string publicKeyPath, bool armor)
        {
            Guard.NotNullOrWhiteSpace(inputPath, nameof(inputPath));
            Guard.NotNullOrWhiteSpace(outputPath, nameof(outputPath));
            Guard.NotNullOrWhiteSpace(publicKeyPath, nameof(publicKeyPath));

            if (!File.Exists(inputPath))
                throw new FileNotFoundException($"Исходный файл не найден: {inputPath}", inputPath);

            var publicKey = ReadPublicKey(publicKeyPath);
            var inputLength = new FileInfo(inputPath).Length;

            EnsureOutputFolder(outputPath);
            if (File.Exists(outputPath))
                File.Delete(outputPath);

            using (var output = File.Create(outputPath))
            using (var armored = armor ? new ArmoredOutputStream(output) : null)
            {
                var target = armor ? (Stream)armored : output;

                var encryptor = new PgpEncryptedDataGenerator(
                    SymmetricKeyAlgorithmTag.Aes256, withIntegrityPacket: true, new SecureRandom());
                encryptor.AddMethod(publicKey);

                using (var encryptedStream = encryptor.Open(target, new byte[1 << 16]))
                {
                    // BC 2.x: сами генераторы не IDisposable — в using только их потоки
                    var compressor = new PgpCompressedDataGenerator(CompressionAlgorithmTag.Zip);
                    using (var compressedStream = compressor.Open(encryptedStream))
                    {
                        var literal = new PgpLiteralDataGenerator();
                        using (var literalStream = literal.Open(
                            compressedStream, PgpLiteralData.Binary,
                            Path.GetFileName(inputPath), inputLength, File.GetLastWriteTimeUtc(inputPath)))
                        using (var input = File.OpenRead(inputPath))
                        {
                            input.CopyTo(literalStream);
                        }
                    }
                }
            }

            return new FileInfo(outputPath).Length;
        }

        // ── Расшифровка ────────────────────────────────────────────────

        /// <summary>
        /// Расшифровывает файл приватным ключом отправителя.
        /// </summary>
        /// <param name="inputPath">Зашифрованный файл (.pgp/.asc).</param>
        /// <param name="outputPath">Путь расшифрованного файла.</param>
        /// <param name="privateKeyPath">Путь к приватному ключу (.asc/.gpg).</param>
        /// <param name="passphrase">Парольная фраза приватного ключа (может быть пустой).</param>
        /// <returns>Число расшифрованных байт.</returns>
        public long DecryptFile(
            string inputPath, string outputPath, string privateKeyPath, string passphrase)
        {
            Guard.NotNullOrWhiteSpace(inputPath, nameof(inputPath));
            Guard.NotNullOrWhiteSpace(outputPath, nameof(outputPath));
            Guard.NotNullOrWhiteSpace(privateKeyPath, nameof(privateKeyPath));

            if (!File.Exists(inputPath))
                throw new FileNotFoundException($"Зашифрованный файл не найден: {inputPath}", inputPath);
            if (!File.Exists(privateKeyPath))
                throw new FileNotFoundException($"Приватный ключ не найден: {privateKeyPath}", privateKeyPath);

            EnsureOutputFolder(outputPath);
            if (File.Exists(outputPath))
                File.Delete(outputPath);

            // BC 2.7: passphrase конвертируется в байты посимвольно —
            // не-ASCII (например кириллица) даёт OverflowException в недрах BC;
            // отсекаем заранее с понятным сообщением
            ValidatePassphraseAscii(passphrase);

            // 1. парсим список шифрованных пакетов сообщения
            PgpEncryptedDataList encryptedList;
            using (var input = File.OpenRead(inputPath))
                encryptedList = FindEncryptedDataList(input);

            // 2. грузим секретные ключи
            PgpSecretKeyRingBundle bundle;
            using (var keyInput = File.OpenRead(privateKeyPath))
                bundle = new PgpSecretKeyRingBundle(PgpUtilities.GetDecoderStream(keyInput));

            // 3. выбираем пакет, для которого у нас есть ключ
            PgpPublicKeyEncryptedData target;
            PgpSecretKey secretKey;
            using (var keyInput = File.OpenRead(privateKeyPath))
            {
                var freshBundle = new PgpSecretKeyRingBundle(PgpUtilities.GetDecoderStream(keyInput));
                (target, secretKey) = SelectRecipientPacket(encryptedList, freshBundle);
            }

            // 4. извлекаем приватный ключ (passphrase)
            var privateKey = ExtractPrivateKey(secretKey, passphrase);

            // 5. расшифровка: encrypted → (compressed)? → literal
            using (var input = File.OpenRead(inputPath))
            {
                var freshList = FindEncryptedDataList(input);
                var targetPacket = freshList.GetEncryptedDataObjects()
                    .Cast<PgpPublicKeyEncryptedData>()
                    .First(p => p.KeyId == target.KeyId);

                using (var plainStream = targetPacket.GetDataStream(privateKey))
                {
                    var message = new PgpObjectFactory(plainStream).NextPgpObject();

                    if (message is PgpCompressedData compressed)
                    {
                        using (var compressedStream = compressed.GetDataStream())
                        {
                            message = new PgpObjectFactory(compressedStream).NextPgpObject();
                        }
                    }

                    if (!(message is PgpLiteralData literal))
                        throw new InvalidOperationException(
                            "PGP-сообщение не содержит файловых данных (literal data)");

                    using (var content = literal.GetInputStream())
                    using (var output = File.Create(outputPath))
                    {
                        content.CopyTo(output);
                        return output.Length;
                    }
                }
            }
        }

        /// <summary>
        /// Ищет PgpEncryptedDataList в сообщении; поддерживаются также
        /// нешифрованные compressed-конверты (обход одного уровня).
        /// </summary>
        private PgpEncryptedDataList FindEncryptedDataList(Stream input)
        {
            try
            {
                var factory = new PgpObjectFactory(PgpUtilities.GetDecoderStream(input));
                var obj = factory.NextPgpObject();

                // редкий случай: compressed без шифрования, данные внутри
                if (obj is PgpCompressedData preCompressed)
                {
                    using (var compressedStream = preCompressed.GetDataStream())
                        obj = new PgpObjectFactory(compressedStream).NextPgpObject();
                }

                if (obj is PgpEncryptedDataList list)
                    return list;

                throw new InvalidOperationException(
                    "Файл не содержит шифрованного PGP-конверта (PgpEncryptedDataList)");
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Не удалось разобрать PGP-файл: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Выбирает пакет получателя, для которого в связке есть секретный ключ.
        /// </summary>
        private (PgpPublicKeyEncryptedData Packet, PgpSecretKey Key) SelectRecipientPacket(
            PgpEncryptedDataList encryptedList, PgpSecretKeyRingBundle bundle)
        {
            foreach (var packet in encryptedList.GetEncryptedDataObjects().Cast<PgpPublicKeyEncryptedData>())
            {
                var key = bundle.GetSecretKey(packet.KeyId);
                if (key != null)
                    return (packet, key);
            }

            var keyIds = encryptedList.GetEncryptedDataObjects()
                .Cast<PgpPublicKeyEncryptedData>()
                .Select(p => $"0x{p.KeyId:X}");
            throw new InvalidOperationException(
                "В файле приватного ключа нет ключа для этого сообщения. " +
                $"Ключи получателей сообщения: {string.Join(", ", keyIds)}");
        }

        /// <summary>
        /// Извлекает приватный ключ; неверная passphrase → понятная ошибка.
        /// </summary>
        private PgpPrivateKey ExtractPrivateKey(PgpSecretKey secretKey, string passphrase)
        {
            try
            {
                var key = secretKey.ExtractPrivateKey(passphrase?.ToCharArray() ?? Array.Empty<char>());
                if (key == null)
                    throw new InvalidOperationException(
                        "Не удалось извлечь приватный ключ (ключ без расшифруемого материала?)");
                return key;
            }
            catch (Exception ex) when (ex is PgpException || ex is InvalidOperationException)
            {
                throw new InvalidOperationException(
                    $"Не удалось расшифровать приватный ключ — проверьте парольную фразу: {ex.Message}", ex);
            }
        }

        // ── Публичный ключ ─────────────────────────────────────────────

        /// <summary>
        /// Читает публичный ключ (armored или binary) — первый ключ,
        /// пригодный для шифрования (иначе первый имеющийся).
        /// </summary>
        public PgpPublicKey ReadPublicKey(string publicKeyPath)
        {
            Guard.NotNullOrWhiteSpace(publicKeyPath, nameof(publicKeyPath));
            if (!File.Exists(publicKeyPath))
                throw new FileNotFoundException($"Публичный ключ не найден: {publicKeyPath}", publicKeyPath);

            try
            {
                using (var stream = File.OpenRead(publicKeyPath))
                using (var decoder = PgpUtilities.GetDecoderStream(stream))
                {
                    var bundle = new PgpPublicKeyRingBundle(decoder);
                    var allKeys = bundle.GetKeyRings()
                        .Cast<PgpPublicKeyRing>()
                        .SelectMany(r => r.GetPublicKeys().Cast<PgpPublicKey>())
                        .ToList();

                    if (allKeys.Count == 0)
                        throw new InvalidOperationException("В файле ключа нет ни одного публичного ключа");

                    return allKeys.FirstOrDefault(k => k.IsEncryptionKey) ?? allKeys.First();
                }
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Не удалось разобрать публичный ключ: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Парольная фраза должна быть ASCII: BC 2.7 конвертирует её в байты
        /// посимвольно (не-ASCII даёт OverflowException в недрах библиотеки).
        /// </summary>
        private static void ValidatePassphraseAscii(string passphrase)
        {
            if (passphrase == null)
                return;
            if (passphrase.Any(c => c > 255))
                throw new InvalidOperationException(
                    "Парольная фраза приватного ключа должна содержать только ASCII-символы " +
                    "(ограничение BouncyCastle 2.x: посимвольная конвертация в байты)");
        }

        private static void EnsureOutputFolder(string outputPath)
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(outputPath));
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
        }
    }
}
