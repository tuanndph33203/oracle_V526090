using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AnFood.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IDatabaseFactory _databaseFactory;
        private DbContext _dataContext;
        private bool _disposed;
        private string Prefix;
        public List<IRepositoryBase> ListRepository = new List<IRepositoryBase>();

        /// <summary>
        /// Noi qu?n l� c�c repository, N� gi? 1 instance c?a DbContext d�ng chung cho m?i Repository..
        /// UnitOfWork n� g?i savechange d? luu t?t c? c�c thay d?i c?a c�c repository v�o database.
        /// </summary>
        /// <param name="databaseFactory"></param>
        /// <param name="prefix"></param>

        public UnitOfWork(IDatabaseFactory databaseFactory, string prefix = "")
        {

            Prefix = prefix;
            _databaseFactory = databaseFactory;
            _dataContext = _databaseFactory.GetDbContext();
        }

        public DbContext DataContext => _dataContext ?? (_dataContext = _databaseFactory.GetDbContext());


        public IRepository<T> GetRepository<T>() where T : class
        {
            var repository = new Repository<T>(_dataContext, Prefix);
            ListRepository.Add(repository);
            return repository;
        }

        public int Save()
        {
            var listTask = new List<Task>();
            foreach (var repository in ListRepository)
            {
                listTask.AddRange(repository.GetAllTask());
            }
            if (listTask.Count > 0)
            {
                Task.WaitAll(listTask.ToArray());
            }
            return DataContext.SaveChanges();
        }

        public bool CheckConnection()
        {
            return DataContext.Database.GetDbConnection().State == System.Data.ConnectionState.Connecting;
        }

        public async Task<int> SaveAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var listTask = new List<Task>();
            foreach (var repository in ListRepository)
            {
                listTask.AddRange(repository.GetAllTask());
            }
            Console.WriteLine(listTask);
            if (listTask.Count > 0)
            {
                await Task.WhenAll(listTask);
            }

            return await DataContext.SaveChangesAsync();
        }
        public System.Data.Common.DbConnection Connection => DataContext.Database.GetDbConnection();

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await DataContext.Database.BeginTransactionAsync();
        }
        // Khi using k?t th�c th� s? t? d?ng g?i Dispose d? gi?i ph�ng t�i nguy�n
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Gi?i ph�ng db context 
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
                if (disposing)
                {
                    _dataContext.Dispose();
                    _disposed = true;
                }

            _disposed = false;
        }
    }
}