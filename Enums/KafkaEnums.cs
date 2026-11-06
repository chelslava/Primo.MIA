using System;

namespace Primo.MIA
{
    /// <summary>
    /// Протокол безопасности подключения к Kafka (issue #19).
    /// Значения отображаются в комбобоксе через [RuName]; сериализация —
    /// по имени члена.
    /// </summary>
    public enum KafkaSecurityProtocol
    {
        /// <summary>Обычное TCP-соединение без шифрования (порт 9092).</summary>
        [RuName("Без шифрования (Plaintext)")]
        Plaintext,

        /// <summary>TLS-шифрование без аутентификации клиента (порт 9093).</summary>
        [RuName("TLS")]
        Ssl,

        /// <summary>SASL-аутентификация поверх обычного TCP.</summary>
        [RuName("SASL (без TLS)")]
        SaslPlaintext,

        /// <summary>SASL-аутентификация поверх TLS — типовая прод-конфигурация.</summary>
        [RuName("SASL + TLS")]
        SaslSsl
    }

    /// <summary>
    /// SASL-механизм аутентификации (issue #19). Используется при
    /// <see cref="KafkaSecurityProtocol.SaslPlaintext"/> / <see cref="KafkaSecurityProtocol.SaslSsl"/>.
    /// </summary>
    public enum KafkaSaslMechanism
    {
        /// <summary>SASL не используется (для Plaintext/Ssl).</summary>
        [RuName("Не используется")]
        None,

        /// <summary>PLAIN — логин/пароль открытым текстом внутри защищённого канала.</summary>
        [RuName("PLAIN")]
        Plain,

        /// <summary>SCRAM-SHA-256 — логин/пароль через SCRAM-вызов (рекомендуется).</summary>
        [RuName("SCRAM-SHA-256")]
        ScramSha256,

        /// <summary>SCRAM-SHA-512 — усиленный SCRAM.</summary>
        [RuName("SCRAM-SHA-512")]
        ScramSha512
    }

    /// <summary>
    /// Начальное смещение новой группы консьюмеров (issue #19):
    /// откуда читать, если у группы ещё нет закоммиченного offset.
    /// </summary>
    public enum KafkaAutoOffsetReset
    {
        /// <summary>С самого начала топика (все накопленные сообщения).</summary>
        [RuName("С начала (Earliest)")]
        Earliest,

        /// <summary>Только новые сообщения (с конца топика).</summary>
        [RuName("Только новые (Latest)")]
        Latest
    }

    /// <summary>
    /// Формат сообщения (issue #19): влияет на ContentType метаданные;
    /// и ключ, и значение передаются строками UTF-8.
    /// </summary>
    public enum KafkaMessageFormat
    {
        /// <summary>Обычный текст (ContentType: text/plain; charset=utf-8).</summary>
        [RuName("Строка")]
        String,

        /// <summary>JSON-документ (ContentType: application/json).</summary>
        [RuName("JSON")]
        Json
    }
}
