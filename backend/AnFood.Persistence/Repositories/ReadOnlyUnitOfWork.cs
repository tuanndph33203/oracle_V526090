using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using AnFood.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AnFood.Persistence
{
    public class ReadOnlyUnitOfWork : IReadOnlyUnitOfWork
    {
        private readonly ReadOnlyDataContext _context;
        private readonly ConcurrentDictionary<Type, object> _repositories = new();

        public ReadOnlyUnitOfWork(ReadOnlyDataContext context)
        {
            _context = context;
        }

        public DbContext DataContext => _context;

        public DbConnection Connection => _context.Database.GetDbConnection();

        public IRepository<TEntity> GetRepository<TEntity>() where TEntity : class
        {
            return (IRepository<TEntity>)_repositories.GetOrAdd(
                typeof(TEntity),
                _ => new Repository<TEntity>(_context, "")
            );
        }

        public bool CheckConnection()
        {
            return _context.Database.CanConnect();
        }

        public int Save() => _context.SaveChanges();

        public Task<int> SaveAsync(CancellationToken cancellationToken = default) => _context.SaveChangesAsync(cancellationToken);

        public Task<IDbContextTransaction> BeginTransactionAsync() => _context.Database.BeginTransactionAsync();

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
