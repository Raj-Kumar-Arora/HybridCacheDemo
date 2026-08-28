using HybridCacheDemo.Data;
using HybridCacheDemo.Models;
using Microsoft.EntityFrameworkCore;

namespace HybridCacheDemo.Services;

public class ProductService : IProductService
{
    private readonly AppDbContext _db;

    public ProductService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Product?> GetProductAsync(int id, CancellationToken token = default)
    {
        return await _db.Products.FirstOrDefaultAsync(p => p.Id == id, token);
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        // Ensure the DB generates the Id (clear any client-supplied Id)
        product.Id = 0;

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

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

        return existing;
    }
}
