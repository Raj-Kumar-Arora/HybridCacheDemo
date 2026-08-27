using HybridCacheDemo.Data;
using HybridCacheDemo.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace HybridCacheDemo.Services;

public class ProductService(AppDbContext db, HybridCache cache)
{
    private readonly AppDbContext _db = db;
    private readonly HybridCache _cache = cache;

    public async Task<Product?> GetProductAsync(int id, CancellationToken token = default)
    {
        return await _cache.GetOrCreateAsync(
            $"product-{id}",
            async cancel => await _db.Products.FirstOrDefaultAsync(p => p.Id == id, cancel),
            cancellationToken: token,
            options: new HybridCacheEntryOptions
            {
                LocalCacheExpiration = TimeSpan.FromMinutes(2),     // L1 - IMemoryCache
                Expiration = TimeSpan.FromMinutes(10)               // L2 - Redis
            });
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        // Invalidate cache for this product
        await _cache.RemoveAsync($"product-{product.Id}");

        return product;
    }
}
