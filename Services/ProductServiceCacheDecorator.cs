using System.Collections.Concurrent;
using HybridCacheDemo.Models;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;

namespace HybridCacheDemo.Services;

public class ProductServiceCacheDecorator : IProductService
{
    private readonly IProductService _inner;
    private readonly HybridCache _cache;
    private readonly IMemoryCache _memory;

    // Per-key semaphores to prevent in-process cache stampede
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public ProductServiceCacheDecorator(IProductService inner, HybridCache cache, IMemoryCache memory)
    {
        _inner = inner;
        _cache = cache;
        _memory = memory;
    }

    public async Task<Product?> GetProductAsync(int id, CancellationToken token = default)
    {
        var key = $"product-{id}";

        // Fast-path: check local (L1) cache first
        if (_memory.TryGetValue<Product?>(key, out var cached) && cached is not null)
            return cached;

        var sem = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(token);
        try
        {
            // Re-check local cache after acquiring lock (double-checked locking)
            if (_memory.TryGetValue<Product?>(key, out cached) && cached is not null)
                return cached;

            // Miss: use hybrid cache GetOrCreate which will attempt L2 then call factory
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
        finally
        {
            sem.Release();
            // Optionally keep semaphores to avoid races when removing; leaving them is fine
        }
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        var created = await _inner.CreateProductAsync(product);
        try
        {
            await _cache.RemoveAsync($"product-{created.Id}");
            // Also remove from local cache if present
            _memory.Remove($"product-{created.Id}");
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
                _memory.Remove($"product-{id}");
            }
            catch (Exception)
            {
                // Best-effort invalidation. In production, log this.
            }
        }

        return updated;
    }
}
