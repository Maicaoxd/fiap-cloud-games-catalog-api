using CatalogAPI.Application.Abstractions.Persistence;
using CatalogAPI.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace CatalogAPI.Infrastructure.Persistence.Repositories
{
    public sealed class OrderRepository : IOrderRepository
    {
        private readonly CatalogDbContext _dbContext;

        public OrderRepository(CatalogDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            return _dbContext.Orders
                .Include(order => order.Items)
                .SingleOrDefaultAsync(order => order.Id == orderId, cancellationToken);
        }

        public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
        {
            await _dbContext.Orders.AddAsync(order, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
        {
            _dbContext.Orders.Update(order);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
