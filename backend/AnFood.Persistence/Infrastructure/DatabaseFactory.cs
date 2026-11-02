using AnFood.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data.SqlClient;

namespace  AnFood.Persistence.Context
{
    public class DatabaseFactory : IDatabaseFactory
    {
        private readonly DataContext _context;

        public DatabaseFactory(DataContext context)
        {
            _context = context;
        }

        public DbContext GetDbContext()
        {
            return _context;
        }
    }
}