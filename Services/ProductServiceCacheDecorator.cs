using HybridCacheDemo.Models;
using Microsoft.Extensions.Caching.Hybrid;

namespace HybridCacheDemo.Services;

public class ProductServiceCacheDecorator : IProductService
{
    private readonly IProductService _inner;
    private readonly HybridCache _cache;

    public ProductServiceCacheDecorator(IProductService inner, HybridCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<Product?> GetProductAsync(int id, CancellationToken token = default)
    {
        var key = $"product-{id}";
        var result = await _cache.GetOrCreateAsync(
            key,
            async ct => await _inner.GetProductAsync(id, ct),
            cancellationToken: token,
            options: new HybridCacheEntryOptions
            {
                LocalCacheExpiration = TimeSpan.FromMinutes(2),
                Expiration = TimeSpan.FromMinutes(10)
            });

        return result;
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        var created = await _inner.CreateProductAsync(product);
        try
        {
            await _cache.RemoveAsync($"product-{created.Id}");
        }
        catch (Exception)
        {
            // Best-effort invalidation. In production, log this.
        }

        return created;
    }

    public async Task<Product?> UpdateProductAsync(int id, Product updatedProduct)
    {
        var updated = await _inner.UpdateProductAsync(id, updatedProduct);
        if (updated is not null)
        {
            try
            {
                await _cache.RemoveAsync($"product-{id}");
            }
            catch (Exception)
            {
                // Best-effort invalidation. In production, log this.
            }
        }

        return updated;
    }
}
