using HybridCacheDemo.Models;
using HybridCacheDemo.Services;
using Microsoft.AspNetCore.Mvc;

namespace HybridCacheDemo.Controllers;

[ApiController]
[Route("api/products")]
public class ProductController : ControllerBase
{
    private readonly ProductService _service;

    public ProductController(ProductService service)
    {
        _service = service;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var product = await _service.GetProductAsync(id);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Product product)
    {
        var created = await _service.CreateProductAsync(product);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }
}
