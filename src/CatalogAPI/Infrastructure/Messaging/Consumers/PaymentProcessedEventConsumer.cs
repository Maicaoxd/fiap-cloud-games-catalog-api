using CatalogAPI.Application.Payments.ProcessPaymentResult;
using FiapCloudGames.Contracts.Events;
using MassTransit;

namespace CatalogAPI.Infrastructure.Messaging.Consumers
{
    public sealed class PaymentProcessedEventConsumer : IConsumer<PaymentProcessedEvent>
    {
        private readonly ILogger<PaymentProcessedEventConsumer> _logger;
        private readonly ProcessPaymentResultUseCase _processPaymentResultUseCase;

        public PaymentProcessedEventConsumer(
            ILogger<PaymentProcessedEventConsumer> logger,
            ProcessPaymentResultUseCase processPaymentResultUseCase)
        {
            _logger = logger;
            _processPaymentResultUseCase = processPaymentResultUseCase;
        }

        public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
        {
            var message = context.Message;
            var command = new ProcessPaymentResultCommand(
                message.OrderId,
                message.UserId,
                message.TotalPrice,
                message.Status,
                message.ProcessedAt);

            await _processPaymentResultUseCase.ExecuteAsync(command, context.CancellationToken);

            _logger.LogInformation(
                "PaymentProcessedEvent consumido. OrderId: {OrderId}, UserId: {UserId}, Jogos: {GameCount}, Status: {Status}",
                message.OrderId,
                message.UserId,
                message.Games.Count,
                message.Status);
        }
    }
}
