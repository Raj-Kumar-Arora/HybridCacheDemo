using HybridCacheDemo.Models;
using Microsoft.EntityFrameworkCore;

namespace HybridCacheDemo.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
}
