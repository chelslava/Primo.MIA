namespace Primo.MIA
{
    /// <summary>
    /// Действие над полученным сообщением в RabbitMQ (issue #49).
    /// </summary>
    public enum RabbitMqAckAction
    {
        /// <summary>Подтвердить: сообщение удаляется из очереди (успешная обработка).</summary>
        [RuName("Ack (подтвердить)")]
        Ack,

        /// <summary>Вернуть в очередь с инкрементом x-retry-count (копия + ack оригинала).</summary>
        [RuName("Requeue (повтор с счётчиком)")]
        Requeue,

        /// <summary>Отправить в очередь мёртвых писем и подтвердить оригинал.</summary>
        [RuName("Nack → DLQ (очередь отказов)")]
        NackDlq
    }
}
