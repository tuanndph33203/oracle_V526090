using Microsoft.EntityFrameworkCore;

namespace AnFood.Persistence
{
    public interface IDatabaseFactory
    {
        DbContext GetDbContext();
        
    }
}