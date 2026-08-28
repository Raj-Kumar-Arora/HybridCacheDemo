using HybridCacheDemo.Models;

namespace HybridCacheDemo.Services;

public interface IProductService
{
    Task<Product?> GetProductAsync(int id, CancellationToken token = default);
    Task<Product> CreateProductAsync(Product product);
    Task<Product?> UpdateProductAsync(int id, Product updatedProduct);
}
