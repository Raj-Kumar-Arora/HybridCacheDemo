using HybridCacheDemo.Data;
using HybridCacheDemo.Services;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));

builder.Services.AddMemoryCache(); // register IMemoryCache (L1)

// Redis (L2 cache)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "hybrid-demo:";
});

// HybridCache (L1 + L2)
builder.Services.AddHybridCache(options =>
{
    options.DisableCompression = true;
    options.MaximumPayloadBytes = 1024 * 1024;
});

// Controllers
builder.Services.AddControllers();

// Register DB-only ProductService and then decorate it with caching behavior.
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<IProductService>(sp =>
    new ProductServiceCacheDecorator(
        sp.GetRequiredService<ProductService>(),
        sp.GetRequiredService<HybridCache>(),
        sp.GetRequiredService<IMemoryCache>()));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();
app.Run();
