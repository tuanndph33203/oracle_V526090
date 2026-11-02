using AnFood.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace AnFood.Persistence.Context
{
    public class ReadOnlyDataContext : DataContext
    {
        public ReadOnlyDataContext(DbContextOptions<ReadOnlyDataContext> options)
            : base(options)
        { }
    }
}
