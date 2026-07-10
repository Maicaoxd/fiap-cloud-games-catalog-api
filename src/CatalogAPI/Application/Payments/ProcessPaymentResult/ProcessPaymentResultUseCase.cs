using CatalogAPI.Application.Abstractions.Persistence;
using CatalogAPI.Application.Common.Exceptions;
using CatalogAPI.Domain.Libraries;
using CatalogAPI.Domain.Orders;

namespace CatalogAPI.Application.Payments.ProcessPaymentResult
{
    public sealed class ProcessPaymentResultUseCase
    {
        private readonly ILibraryRepository _libraryRepository;
        private readonly IOrderRepository _orderRepository;

        public ProcessPaymentResultUseCase(
            ILibraryRepository libraryRepository,
            IOrderRepository orderRepository)
        {
            _libraryRepository = libraryRepository;
            _orderRepository = orderRepository;
        }

        public async Task ExecuteAsync(
            ProcessPaymentResultCommand command,
            CancellationToken cancellationToken = default)
        {
            var status = ParseStatus(command.Status);
            var order = await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken);

            if (order is null || order.Status is not PaymentStatus.Pending)
                return;

            if (status is PaymentStatus.Approved)
            {
                foreach (var item in order.Items)
                {
                    if (await _libraryRepository.ExistsByUserAndGameAsync(
                            order.UserId,
                            item.GameId,
                            cancellationToken))
                        continue;

                    var library = Library.Create(order.UserId, item.GameId);
                    await _libraryRepository.AddAsync(library, cancellationToken);
                }
            }

            order.MarkPaymentProcessed(status);
            await _orderRepository.UpdateAsync(order, cancellationToken);
        }

        private static PaymentStatus ParseStatus(string status)
        {
            if (!Enum.TryParse<PaymentStatus>(status, ignoreCase: true, out var parsedStatus) ||
                parsedStatus is PaymentStatus.Pending)
                throw new InvalidPaymentStatusException();

            return parsedStatus;
        }
    }
}
