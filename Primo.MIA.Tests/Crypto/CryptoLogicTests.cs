using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Primo.MIA;
using Xunit;

namespace Primo.MIA.Tests.Crypto
{
    /// <summary>
    /// Тесты крипто-логики: хэширование (известные эталоны), AES roundtrip,
    /// Base64, обработка невалидных входных данных.
    /// </summary>
    [Trait("Category", "Fast")]
    public class CryptoLogicTests
    {
        private readonly CryptoLogic _logic;

        public CryptoLogicTests()
        {
            _logic = new CryptoLogic();
        }

        #region HashString Tests

        [Fact]
        public void HashString_Sha256_KnownVector()
        {
            // Известный эталон: SHA-256("abc")
            _logic.HashString("abc", CryptoHashAlgorithm.SHA256, CryptoHashOutputFormat.HexLower)
                .Should().Be("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
        }

        [Fact]
        public void HashString_Md5_KnownVector()
        {
            // Известный эталон: MD5("hello")
            _logic.HashString("hello", CryptoHashAlgorithm.MD5, CryptoHashOutputFormat.HexLower)
                .Should().Be("5d41402abc4b2a76b9719d911017c592");
        }

        [Fact]
        public void HashString_Sha1_KnownVector()
        {
            _logic.HashString("hello", CryptoHashAlgorithm.SHA1, CryptoHashOutputFormat.HexLower)
                .Should().Be("aaf4c61ddcc5e8a2dabede0f3b482cd9aea9434d");
        }

        [Fact]
        public void HashString_Sha512_KnownVector()
        {
            _logic.HashString("hello", CryptoHashAlgorithm.SHA512, CryptoHashOutputFormat.HexLower)
                .Should().StartWith("9b71d224bd62f3785d96d46ad3ea3d73");
        }

        [Fact]
        public void HashString_HexUpper_ReturnsUppercase()
        {
            var result = _logic.HashString("hello", CryptoHashAlgorithm.MD5, CryptoHashOutputFormat.HexUpper);
            result.Should().Be("5D41402ABC4B2A76B9719D911017C592");
        }

        [Fact]
        public void HashString_Base64Format_ReturnsBase64()
        {
            var result = _logic.HashString("hello", CryptoHashAlgorithm.MD5, CryptoHashOutputFormat.Base64);
            // Эталон вычисляем независимо: Base64 от байтов MD5("hello")
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var expected = Convert.ToBase64String(md5.ComputeHash(Encoding.UTF8.GetBytes("hello")));
                result.Should().Be(expected);
            }
            result.Should().EndWith("==").And.HaveLength(24);
        }

        [Fact]
        public void HashString_Deterministic_SameInputSameHash()
        {
            var a = _logic.HashString("данные", CryptoHashAlgorithm.SHA256, CryptoHashOutputFormat.HexLower);
            var b = _logic.HashString("данные", CryptoHashAlgorithm.SHA256, CryptoHashOutputFormat.HexLower);
            a.Should().Be(b);
        }

        [Fact]
        public void HashString_DifferentInputs_DifferentHashes()
        {
            var a = _logic.HashString("abc", CryptoHashAlgorithm.SHA256, CryptoHashOutputFormat.HexLower);
            var b = _logic.HashString("abd", CryptoHashAlgorithm.SHA256, CryptoHashOutputFormat.HexLower);
            a.Should().NotBe(b);
        }

        [Fact]
        public void HashString_NullInput_ThrowsArgumentNull()
        {
            Action act = () => _logic.HashString(null, CryptoHashAlgorithm.SHA256, CryptoHashOutputFormat.HexLower);
            act.Should().Throw<ArgumentNullException>();
        }

        #endregion

        #region HashFile Tests

        [Fact]
        public void HashFile_KnownContent_MatchesStringHash()
        {
            var path = Path.Combine(Path.GetTempPath(), $"pmia_hash_{Guid.NewGuid():N}.txt");
            try
            {
                // UTF-8 без BOM: File.WriteAllText(Encoding.UTF8) добавляет BOM,
                // который изменил бы содержимое файла относительно строки
                File.WriteAllText(path, "abc", new UTF8Encoding(false));
                var fileHash = _logic.HashFile(path, CryptoHashAlgorithm.SHA256, CryptoHashOutputFormat.HexLower);
                var stringHash = _logic.HashString("abc", CryptoHashAlgorithm.SHA256, CryptoHashOutputFormat.HexLower);
                fileHash.Should().Be(stringHash);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        public void HashFile_MissingFile_ThrowsFileNotFound()
        {
            Action act = () => _logic.HashFile("Z:/no/such/file.bin", CryptoHashAlgorithm.SHA256, CryptoHashOutputFormat.HexLower);
            act.Should().Throw<FileNotFoundException>();
        }

        [Fact]
        public void HashFile_EmptyPath_ThrowsArgumentNull()
        {
            Action act = () => _logic.HashFile("", CryptoHashAlgorithm.SHA256, CryptoHashOutputFormat.HexLower);
            act.Should().Throw<ArgumentException>();
        }

        #endregion

        #region Encrypt/Decrypt Roundtrip Tests

        [Fact]
        public void EncryptDecrypt_Roundtrip_ReturnsOriginal()
        {
            var original = "Секретное сообщение 123!";
            var encrypted = _logic.EncryptString(original, "пароль");
            encrypted.Should().NotContain(original);
            _logic.DecryptString(encrypted, "пароль").Should().Be(original);
        }

        [Fact]
        public void EncryptString_SameInputTwice_ProducesDifferentCiphertext()
        {
            var e1 = _logic.EncryptString("data", "pass");
            var e2 = _logic.EncryptString("data", "pass");
            e1.Should().NotBe(e2, "соль генерируется случайно при каждом шифровании");
        }

        [Fact]
        public void DecryptString_WrongPassword_ThrowsInvalidOperation()
        {
            var encrypted = _logic.EncryptString("data", "correct-password");
            Action act = () => _logic.DecryptString(encrypted, "wrong-password");
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*пароль*");
        }

        [Fact]
        public void EncryptString_UsesAuthenticatedV2Format()
        {
            // GCM (аутентифицированный) вместо старого CBC: неверный пароль
            // или изменение данных всегда обнаруживаются (defect 1/256, CI)
            var encrypted = _logic.EncryptString("data", "pass");
            encrypted.Should().StartWith("MIA2.");
        }

        [Fact]
        public void DecryptString_TamperedCiphertext_ThrowsInvalidOperation()
        {
            var encrypted = _logic.EncryptString("authenticate me", "пароль");
            var payload = Convert.FromBase64String(encrypted.Substring("MIA2.".Length));
            payload[payload.Length - 1] ^= 0xFF; // повреждение тега аутентификации
            var tampered = "MIA2." + Convert.ToBase64String(payload);

            Action act = () => _logic.DecryptString(tampered, "пароль");
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*изменены*");
        }

        [Fact]
        public void DecryptString_LegacyCbcFormat_StillDecrypts()
        {
            // Обратная совместимость: строка в старом формате (до v2)
            // [salt 16][IV 16][AES-256-CBC PKCS7] расшифровывается
            const string password = "old-secret";
            const string original = "старый формат";

            var salt = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
                rng.GetBytes(salt);

            byte[] iv, ciphertext;
            using (var derive = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256))
            {
                var key = derive.GetBytes(32);
                iv = derive.GetBytes(16);
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    using (var encryptor = aes.CreateEncryptor())
                    {
                        var plain = Encoding.UTF8.GetBytes(original);
                        ciphertext = encryptor.TransformFinalBlock(plain, 0, plain.Length);
                    }
                }
            }

            var payload = new byte[salt.Length + iv.Length + ciphertext.Length];
            Buffer.BlockCopy(salt, 0, payload, 0, salt.Length);
            Buffer.BlockCopy(iv, 0, payload, salt.Length, iv.Length);
            Buffer.BlockCopy(ciphertext, 0, payload, salt.Length + iv.Length, ciphertext.Length);

            _logic.DecryptString(Convert.ToBase64String(payload), password)
                .Should().Be(original);
        }

        [Fact]
        public void DecryptString_GarbageBase64_ThrowsInvalidOperation()
        {
            Action act = () => _logic.DecryptString("не-base64!!", "pass");
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void DecryptString_TooShortPayload_ThrowsInvalidOperation()
        {
            var shortPayload = Convert.ToBase64String(new byte[10]);
            Action act = () => _logic.DecryptString(shortPayload, "pass");
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*короткие*");
        }

        [Fact]
        public void EncryptString_EmptyPlaintext_RoundtripReturnsEmpty()
        {
            var encrypted = _logic.EncryptString("", "pass");
            _logic.DecryptString(encrypted, "pass").Should().BeEmpty();
        }

        [Fact]
        public void EncryptString_EmptyPassword_ThrowsArgument()
        {
            Action act = () => _logic.EncryptString("data", "");
            act.Should().Throw<ArgumentException>();
        }

        #endregion

        #region Base64 Tests

        [Fact]
        public void Base64Encode_KnownVector()
        {
            _logic.Base64Encode("hello").Should().Be("aGVsbG8=");
        }

        [Fact]
        public void Base64Encode_Cyrillic_Roundtrip()
        {
            var original = "Привет, мир!";
            _logic.Base64Decode(_logic.Base64Encode(original)).Should().Be(original);
        }

        [Fact]
        public void Base64Decode_Invalid_ThrowsInvalidOperation()
        {
            Action act = () => _logic.Base64Decode("!!не-base64!!");
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Base64Encode_Null_ThrowsArgumentNull()
        {
            Action act = () => _logic.Base64Encode(null);
            act.Should().Throw<ArgumentNullException>();
        }

        #endregion
    }
}
