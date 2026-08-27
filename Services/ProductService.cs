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
        // Ensure the DB generates the Id (clear any client-supplied Id)
        product.Id = 0;

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        // Invalidate cache for this product
        try
        {
            await _cache.RemoveAsync($"product-{product.Id}");
        }
        catch (Exception)
        {
            // If L2 (Redis) is unavailable, don't fail the create operation.
            // Consider logging this in real app.
        }

        return product;
    }

    public async Task<Product?> UpdateProductAsync(int id, Product updatedProduct)
    {
        var existing = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (existing is null)
            return null;

        // Update fields (adjust as needed)
        existing.Name = updatedProduct.Name;
        existing.Price = updatedProduct.Price;
        existing.Category = updatedProduct.Category;

        await _db.SaveChangesAsync();

        // Invalidate cache for this product (best-effort)
        try
        {
            await _cache.RemoveAsync($"product-{id}");
        }
        catch (Exception)
        {
            // Swallow cache errors to avoid failing the update when Redis is down.
            // Consider adding logging here.
        }

        return existing;
    }
}
