using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AnFood.Persistence
{
    //IDisposable : cung c?p m?t phuong th?c d? gi?i ph�ng t�i nguy�n kh�ng qu?n l�.
    public interface IUnitOfWork : IDisposable
    {
        DbContext DataContext { get; }
        IRepository<T> GetRepository<T>() where T : class;
        int Save();
        Task<int> SaveAsync(CancellationToken cancellationToken = default(CancellationToken));
        bool CheckConnection();
        System.Data.Common.DbConnection Connection { get; }
        Task<IDbContextTransaction> BeginTransactionAsync();

    }
}