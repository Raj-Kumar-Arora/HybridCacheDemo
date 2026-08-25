namespace HybridCacheDemo.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public decimal Price { get; set; }
    public string Category { get; set; } = default!;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}
