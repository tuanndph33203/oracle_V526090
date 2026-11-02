using AnFood.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnFood.Persistence.Infrastructure
{
    public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
    {
        public DataContext CreateDbContext(string[] args)
        {
            var basePath = Directory.GetCurrentDirectory();

            // tr? t?i project API
            var apiPath = Path.Combine(basePath, "../AnFood.API");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.GetFullPath(apiPath))
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var connectionString = configuration["ConnectionString:SqlServerDatabase"];

            var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new DataContext(optionsBuilder.Options);
        }
    }

}
