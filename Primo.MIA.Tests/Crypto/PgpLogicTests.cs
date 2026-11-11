using System;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Xunit;

namespace Primo.MIA.Tests.Crypto
{
    /// <summary>
    /// Тесты PGP-шифрования файлов (issue #35): roundtrip бинарный/armored,
    /// неверная passphrase, Guard. Ключи RSA-2048 генерируются один раз
    /// в фикстуре (общие для всех тестов класса).
    /// </summary>
    [Trait("Category", "Fast")]
    public class PgpLogicTests : IClassFixture<PgpKeyFixture>
    {
        private readonly PgpKeyFixture _keys;
        private readonly PgpLogic _logic = new PgpLogic();

        public PgpLogicTests(PgpKeyFixture keys)
        {
            _keys = keys;
        }

        private string WriteTemp(string name, byte[] content)
        {
            var path = Path.Combine(_keys.WorkDir, name);
            File.WriteAllBytes(path, content);
            return path;
        }

        // ── Roundtrip ──────────────────────────────────────────────────

        [Fact(DisplayName = "Roundtrip бинарный: содержимое совпадает")]
        public void Roundtrip_Binary()
        {
            var original = Encoding.UTF8.GetBytes("Отчёт №42 — содержимое файла для PGP roundtrip");
            var input = WriteTemp("plain.bin", original);
            var encrypted = Path.Combine(_keys.WorkDir, "out.pgp");
            var decrypted = Path.Combine(_keys.WorkDir, "decrypted.bin");

            var encBytes = _logic.EncryptFile(input, encrypted, _keys.PublicKeyPath, armor: false);
            encBytes.Should().BeGreaterThan(0);

            var decBytes = _logic.DecryptFile(encrypted, decrypted, _keys.PrivateKeyPath, PgpKeyFixture.Passphrase);
            decBytes.Should().Be(original.Length);
            File.ReadAllBytes(decrypted).Should().BeEquivalentTo(original);
        }

        [Fact(DisplayName = "Roundtrip armored (.asc): ASCII-выход декодируется")]
        public void Roundtrip_Armored()
        {
            var original = Encoding.UTF8.GetBytes("armored payload");
            var input = WriteTemp("plain2.txt", original);
            var encrypted = Path.Combine(_keys.WorkDir, "out.asc");
            var decrypted = Path.Combine(_keys.WorkDir, "decrypted2.txt");

            _logic.EncryptFile(input, encrypted, _keys.PublicKeyPath, armor: true);

            // armored выход — печатный ASCII с заголовками
            File.ReadAllText(encrypted).Should().Contain("-----BEGIN PGP MESSAGE-----");

            _logic.DecryptFile(encrypted, decrypted, _keys.PrivateKeyPath, PgpKeyFixture.Passphrase);
            File.ReadAllBytes(decrypted).Should().BeEquivalentTo(original);
        }

        [Fact(DisplayName = "Roundtrip: пустой файл и большой объём")]
        public void Roundtrip_EmptyAndLarge()
        {
            // пустой файл
            var empty = WriteTemp("empty.bin", Array.Empty<byte>());
            var encEmpty = Path.Combine(_keys.WorkDir, "empty.pgp");
            var decEmpty = Path.Combine(_keys.WorkDir, "empty-out.bin");
            _logic.EncryptFile(empty, encEmpty, _keys.PublicKeyPath, armor: false);
            _logic.DecryptFile(encEmpty, decEmpty, _keys.PrivateKeyPath, PgpKeyFixture.Passphrase);
            new FileInfo(decEmpty).Length.Should().Be(0);

            // ~1 МБ
            var large = new byte[1_000_000];
            new Random(42).NextBytes(large);
            var big = WriteTemp("big.bin", large);
            var encBig = Path.Combine(_keys.WorkDir, "big.pgp");
            var decBig = Path.Combine(_keys.WorkDir, "big-out.bin");
            _logic.EncryptFile(big, encBig, _keys.PublicKeyPath, armor: false);
            _logic.DecryptFile(encBig, decBig, _keys.PrivateKeyPath, PgpKeyFixture.Passphrase);
            File.ReadAllBytes(decBig).Should().BeEquivalentTo(large);
        }

        // ── Ошибки ─────────────────────────────────────────────────────

        [Fact(DisplayName = "Неверная passphrase — InvalidOperationException с подсказкой")]
        public void Decrypt_WrongPassphrase_FailsWithHint()
        {
            var original = Encoding.UTF8.GetBytes("secret");
            var input = WriteTemp("wrong-pass.bin", original);
            var encrypted = Path.Combine(_keys.WorkDir, "wrong-pass.pgp");
            var decrypted = Path.Combine(_keys.WorkDir, "wrong-pass-out.bin");
            _logic.EncryptFile(input, encrypted, _keys.PublicKeyPath, armor: false);

            Action act = () => _logic.DecryptFile(encrypted, decrypted, _keys.PrivateKeyPath, "wrong-phrase");

            act.Should().Throw<InvalidOperationException>().WithMessage("*парольн*");
        }

        [Fact(DisplayName = "Не-ASCII passphrase — понятная ошибка до вызова BC (issue #35)")]
        public void Decrypt_NonAsciiPassphrase_ClearError()
        {
            var original = Encoding.UTF8.GetBytes("secret");
            var input = WriteTemp("cyr-pass.bin", original);
            var encrypted = Path.Combine(_keys.WorkDir, "cyr-pass.pgp");
            var decrypted = Path.Combine(_keys.WorkDir, "cyr-pass-out.bin");
            _logic.EncryptFile(input, encrypted, _keys.PublicKeyPath, armor: false);

            Action act = () => _logic.DecryptFile(encrypted, decrypted, _keys.PrivateKeyPath, "кириллица-фраза");

            act.Should().Throw<InvalidOperationException>().WithMessage("*ASCII*");
            act.Should().NotThrow<OverflowException>("ошибка отсекается до вызова BouncyCastle");
        }

        [Fact(DisplayName = "Чужой ключ (нет в связке) — сообщение о несовпадении KeyId")]
        public void Decrypt_ForeignKey_FailsWithKeyIdHint()
        {
            var other = PgpKeyFixture.Generate("other@key", "other-passphrase", Path.Combine(_keys.WorkDir, "other-key"));

            var original = Encoding.UTF8.GetBytes("для другого получателя");
            var input = WriteTemp("foreign.bin", original);
            var encrypted = Path.Combine(_keys.WorkDir, "foreign.pgp");
            var decrypted = Path.Combine(_keys.WorkDir, "foreign-out.bin");
            _logic.EncryptFile(input, encrypted, other.PublicKeyPath, armor: false);

            Action act = () => _logic.DecryptFile(encrypted, decrypted, _keys.PrivateKeyPath, PgpKeyFixture.Passphrase);

            act.Should().Throw<InvalidOperationException>().WithMessage("*ключа для этого сообщения*");
        }

        [Fact(DisplayName = "Не-PGP файл — InvalidOperationException о разборе")]
        public void Decrypt_NotPgpFile_Fails()
        {
            var notPgp = WriteTemp("not-pgp.pgp", Encoding.UTF8.GetBytes("просто текст"));
            var decrypted = Path.Combine(_keys.WorkDir, "not-pgp-out.bin");

            Action act = () => _logic.DecryptFile(notPgp, decrypted, _keys.PrivateKeyPath, PgpKeyFixture.Passphrase);

            // BC декодер даёт «Premature end of stream» — оборачиваемся в понятное
            act.Should().Throw<InvalidOperationException>().WithMessage("*разобрать PGP-файл*");
        }

        [Fact(DisplayName = "Guard: отсутствующие файлы — FileNotFoundException")]
        public void Guard_MissingFiles()
        {
            Action noInput = () => _logic.EncryptFile(
                Path.Combine(_keys.WorkDir, "missing.in"), "x.pgp", _keys.PublicKeyPath, false);
            Action noPubKey = () => _logic.EncryptFile("a.txt", "x.pgp",
                Path.Combine(_keys.WorkDir, "missing.asc"), false);
            Action noEncInput = () => _logic.DecryptFile(
                Path.Combine(_keys.WorkDir, "missing.pgp"), "x.bin", _keys.PrivateKeyPath, "");
            Action noPrivKey = () => _logic.DecryptFile("a.pgp", "x.bin",
                Path.Combine(_keys.WorkDir, "missing-priv.asc"), "");

            noInput.Should().Throw<FileNotFoundException>();
            noPubKey.Should().Throw<FileNotFoundException>();
            noEncInput.Should().Throw<FileNotFoundException>();
            noPrivKey.Should().Throw<FileNotFoundException>();
        }

        [Fact(DisplayName = "Encrypt: папка результата создаётся при отсутствии")]
        public void Encrypt_CreatesOutputFolder()
        {
            var input = WriteTemp("folder-test.txt", new byte[] { 1, 2, 3 });
            var output = Path.Combine(_keys.WorkDir, "deep", "nested", "out.pgp");

            _logic.EncryptFile(input, output, _keys.PublicKeyPath, armor: false);

            File.Exists(output).Should().BeTrue();
        }
    }

    /// <summary>
    /// Фикстура: одна пара ключей RSA-2048 на весь тестовый класс
    /// (генерация ~1-2 сек). Armored-файлы в общей временной папке.
    /// </summary>
    public class PgpKeyFixture : IDisposable
    {
        // BC 2.7 passphrase → байты посимвольно: только ASCII (кириллица даёт Overflow)
        public const string Passphrase = "test-passphrase-2026";
        public const string Identity = "test-robot@primo.mia";

        public string WorkDir { get; }
        public string PublicKeyPath { get; }
        public string PrivateKeyPath { get; }

        public PgpKeyFixture()
        {
            WorkDir = Path.Combine(Path.GetTempPath(), "mia-pgp-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(WorkDir);
            var generated = Generate(Identity, Passphrase, WorkDir);
            PublicKeyPath = generated.PublicKeyPath;
            PrivateKeyPath = generated.PrivateKeyPath;
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(WorkDir))
                    Directory.Delete(WorkDir, recursive: true);
            }
            catch
            {
                // временная папка может быть занята — не роняем уборкой
            }
        }

        /// <summary>
        /// Генерирует пару ключей RSA-2048 и сохраняет armored-файлы
        /// {prefix}-pub.asc / {prefix}-priv.asc в указанной папке.
        /// </summary>
        public static (string PublicKeyPath, string PrivateKeyPath) Generate(
            string identity, string passphrase, string folder)
        {
            Directory.CreateDirectory(folder);

            var keyGen = new RsaKeyPairGenerator();
            keyGen.Init(new RsaKeyGenerationParameters(
                BigInteger.ValueOf(0x10001), new SecureRandom(), 2048, 12));
            var pair = keyGen.GenerateKeyPair();

            var pgpPair = new PgpKeyPair(PublicKeyAlgorithmTag.RsaGeneral, pair, DateTime.UtcNow);

            // BC 2.7.0: 10-арговый конструктор — секретный ключ шифруется
            // AES-256 под парольной фразой, подпись SHA-256.
            // Уровень сертификации 0x10 = generic certification (RFC 4880);
            // enum PgpSignatureType в BC 2.7 удалён
            var ringGen = new PgpKeyRingGenerator(
                0x10,
                pgpPair, identity,
                SymmetricKeyAlgorithmTag.Aes256,
                HashAlgorithmTag.Sha256,
                passphrase.ToCharArray(),
                true,
                null,
                null,
                new SecureRandom());

            var publicKeyPath = Path.Combine(folder, $"{Sanitize(identity)}-pub.asc");
            var privateKeyPath = Path.Combine(folder, $"{Sanitize(identity)}-priv.asc");

            using (var pubStream = File.Create(publicKeyPath))
            using (var pubArmored = new ArmoredOutputStream(pubStream))
            {
                ringGen.GeneratePublicKeyRing().Encode(pubArmored);
            }

            using (var privStream = File.Create(privateKeyPath))
            using (var privArmored = new ArmoredOutputStream(privStream))
            {
                ringGen.GenerateSecretKeyRing().Encode(privArmored);
            }

            return (publicKeyPath, privateKeyPath);
        }

        private static string Sanitize(string s) =>
            new string(s.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
    }
}
