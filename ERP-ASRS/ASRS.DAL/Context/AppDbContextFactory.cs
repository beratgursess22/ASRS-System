using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ASRS.DAL.Context;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        optionsBuilder.UseMySql(
            "Server=localhost;Database=asrs_db;User=root;Password=123456;",
            new MySqlServerVersion(new Version(8, 0, 36))
        );

        return new AppDbContext(optionsBuilder.Options);
    }
}