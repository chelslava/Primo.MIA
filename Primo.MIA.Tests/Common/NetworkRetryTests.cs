using System;
using System.Net;
using System.Net.Sockets;
using FluentAssertions;
using Primo.MIA.Common;
using Xunit;

namespace Primo.MIA.Tests.Common
{
    /// <summary>
    /// Тесты Common/NetworkRetry (issue #43): разбор retry-параметров
    /// и классификация транзиентных ошибок сетевых модулей.
    /// </summary>
    [Trait("Category", "Fast")]
    public class NetworkRetryTests
    {
        #region ParseRetryCount

        [Fact(DisplayName = "ParseRetryCount: пустое/нечисловое → 0 (без повторов)")]
        public void ParseRetryCount_Defaults()
        {
            NetworkRetry.ParseRetryCount(null).Should().Be(0);
            NetworkRetry.ParseRetryCount("").Should().Be(0);
            NetworkRetry.ParseRetryCount("  ").Should().Be(0);
            NetworkRetry.ParseRetryCount("много").Should().Be(0);
        }

        [Fact(DisplayName = "ParseRetryCount: валидное значение сохраняется")]
        public void ParseRetryCount_Valid()
        {
            NetworkRetry.ParseRetryCount("3").Should().Be(3);
        }

        [Fact(DisplayName = "ParseRetryCount: отрицательное → 0, за верхнюю границу → Max")]
        public void ParseRetryCount_Clamped()
        {
            NetworkRetry.ParseRetryCount("-5").Should().Be(0);
            NetworkRetry.ParseRetryCount("9999").Should().Be(NetworkRetry.MaxRetryCount);
        }

        #endregion

        #region ParseRetryDelayMs

        [Fact(DisplayName = "ParseRetryDelayMs: пустое/нечисловое → 1000")]
        public void ParseRetryDelayMs_Defaults()
        {
            NetworkRetry.ParseRetryDelayMs(null).Should().Be(1000);
            NetworkRetry.ParseRetryDelayMs("abc").Should().Be(1000);
        }

        [Fact(DisplayName = "ParseRetryDelayMs: 0 допустим (без паузы), границы клампятся")]
        public void ParseRetryDelayMs_Clamped()
        {
            NetworkRetry.ParseRetryDelayMs("0").Should().Be(0);
            NetworkRetry.ParseRetryDelayMs("500").Should().Be(500);
            NetworkRetry.ParseRetryDelayMs("-100").Should().Be(0);
            NetworkRetry.ParseRetryDelayMs("999999").Should().Be(NetworkRetry.MaxRetryDelayMs);
        }

        #endregion

        #region IsTransient: сетевые — да

        [Fact(DisplayName = "IsTransient: WebException транспортных статусов → true")]
        public void IsTransient_WebException_TransientStatuses()
        {
            foreach (var status in new[]
            {
                WebExceptionStatus.ConnectFailure,
                WebExceptionStatus.Timeout,
                WebExceptionStatus.NameResolutionFailure,
                WebExceptionStatus.KeepAliveFailure,
                WebExceptionStatus.ConnectionClosed,
                WebExceptionStatus.ReceiveFailure,
                WebExceptionStatus.SendFailure,
                WebExceptionStatus.PipelineFailure
            })
            {
                NetworkRetry.IsTransient(new WebException("сеть", null, status, null))
                    .Should().BeTrue($"статус {status} транзиентен");
            }
        }

        [Fact(DisplayName = "IsTransient: WebException ProtocolError (530/550) → false")]
        public void IsTransient_WebException_ProtocolError()
        {
            NetworkRetry.IsTransient(new WebException("ошибка", null, WebExceptionStatus.ProtocolError, null))
                .Should().BeFalse();
        }

        [Fact(DisplayName = "IsTransient: SocketException → true")]
        public void IsTransient_SocketException()
        {
            NetworkRetry.IsTransient(new SocketException((int)SocketError.ConnectionRefused)).Should().BeTrue();
        }

        [Fact(DisplayName = "IsTransient: SshConnectionException/SshOperationTimeoutException → true")]
        public void IsTransient_Ssh_ConnectionLevel()
        {
            NetworkRetry.IsTransient(new Renci.SshNet.Common.SshConnectionException("разрыв")).Should().BeTrue();
            NetworkRetry.IsTransient(new Renci.SshNet.Common.SshOperationTimeoutException("таймаут")).Should().BeTrue();
        }

        [Fact(DisplayName = "IsTransient: BrokerUnreachableException → true")]
        public void IsTransient_RabbitMq_BrokerUnreachable()
        {
            NetworkRetry.IsTransient(
                new RabbitMQ.Client.Exceptions.BrokerUnreachableException(new Exception("inner")))
                .Should().BeTrue();
        }

        [Fact(DisplayName = "IsTransient: KafkaException → true")]
        public void IsTransient_Kafka()
        {
            NetworkRetry.IsTransient(new Confluent.Kafka.KafkaException(
                new Confluent.Kafka.Error(Confluent.Kafka.ErrorCode.Local_TimedOut, "таймаут")))
                .Should().BeTrue();
        }

        #endregion

        #region IsTransient: детерминированные — нет

        [Fact(DisplayName = "IsTransient: SshAuthenticationException → false")]
        public void IsTransient_Ssh_Authentication()
        {
            NetworkRetry.IsTransient(new Renci.SshNet.Common.SshAuthenticationException("неверный пароль"))
                .Should().BeFalse();
        }

        [Fact(DisplayName = "IsTransient: SftpPathNotFoundException → false")]
        public void IsTransient_Ssh_PathNotFound()
        {
            NetworkRetry.IsTransient(new Renci.SshNet.Common.SftpPathNotFoundException("нет файла"))
                .Should().BeFalse();
        }

        [Fact(DisplayName = "IsTransient: OperationInterruptedException (закрытие канала) → false")]
        public void IsTransient_RabbitMq_ChannelClosed()
        {
            NetworkRetry.IsTransient(new RabbitMQ.Client.Exceptions.OperationInterruptedException(
                new RabbitMQ.Client.ShutdownEventArgs(
                    RabbitMQ.Client.ShutdownInitiator.Peer, 404, "NOT_FOUND")))
                .Should().BeFalse();
        }

        [Fact(DisplayName = "IsTransient: TimeoutException и OperationCanceledException → false")]
        public void IsTransient_TimeoutAndCancel()
        {
            NetworkRetry.IsTransient(new TimeoutException()).Should().BeFalse();
            NetworkRetry.IsTransient(new OperationCanceledException()).Should().BeFalse();
        }

        [Fact(DisplayName = "IsTransient: null и прочие исключения → false")]
        public void IsTransient_NullAndOthers()
        {
            NetworkRetry.IsTransient(null).Should().BeFalse();
            NetworkRetry.IsTransient(new InvalidOperationException("логика")).Should().BeFalse();
            NetworkRetry.IsTransient(new UnauthorizedAccessException("доступ")).Should().BeFalse();
        }

        #endregion
    }
}
