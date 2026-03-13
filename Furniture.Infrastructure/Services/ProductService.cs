using Furniture.Application.DTOs.Common;
using Furniture.Application.DTOs.Product;
using Furniture.Application.Interfaces;
using Furniture.Domain.Entities;
using Furniture.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Furniture.Infrastructure.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICacheService _cache;
        private readonly IConfiguration _configuration; 
        private readonly IImageService   _imageService;
        
        private const string ProductPrefix = "product_";
        private const string ProductListKey = "product_all";

        private TimeSpan ImageCacheExpiry => TimeSpan.FromMinutes(
            Convert.ToDouble(_configuration["CacheSettings:ProductImageExpiryMinutes"] ?? "60"));

        private TimeSpan ListCacheExpiry => TimeSpan.FromMinutes(
            Convert.ToDouble(_configuration["CacheSettings:ProductListExpiryMinutes"] ?? "10"));


        public ProductService(
            ApplicationDbContext context,
            ICacheService cache,
            IConfiguration configuration,
            IImageService imageService
            )
        {
            _context      = context;
            _cache        = cache;
            _configuration = configuration;
            _imageService  = imageService;
        }

        public async Task<PagedResult<ProductResponseDto>> GetAllAsync(ProductQueryParameters query)
        {
            var cacheKey = $"products:{query.Search}:{query.CategoryId}:{query.MinPrice}:" +
                           $"{query.MaxPrice}:{query.InStock}:{query.SortBy}:" +
                           $"{query.SortOrder}:{query.Page}:{query.PageSize}";

            var cached = await _cache.GetAsync<PagedResult<ProductResponseDto>>(cacheKey);
            if (cached != null) return cached;

            var q = _context.Products
                            .Include(p => p.Category)
                            .AsQueryable();

            // Search
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.ToLower();
                q = q.Where(p => p.Name.ToLower().Contains(term) ||
                                  p.Description.ToLower().Contains(term));
            }

            // Filters
            if (query.CategoryId.HasValue)
                q = q.Where(p => p.CategoryId == query.CategoryId);

            if (query.MinPrice.HasValue)
                q = q.Where(p => p.Price >= query.MinPrice);

            if (query.MaxPrice.HasValue)
                q = q.Where(p => p.Price <= query.MaxPrice);

            if (query.InStock.HasValue)
                q = q.Where(p => query.InStock.Value ? p.Stock > 0 : p.Stock == 0);

            // Sorting
            q = query.SortBy?.ToLower() switch
            {
                "price" => query.SortOrder == "desc"
                                ? q.OrderByDescending(p => p.Price)
                                : q.OrderBy(p => p.Price),
                "name" => query.SortOrder == "desc"
                                ? q.OrderByDescending(p => p.Name)
                                : q.OrderBy(p => p.Name),
                "stock" => query.SortOrder == "desc"
                                ? q.OrderByDescending(p => p.Stock)
                                : q.OrderBy(p => p.Stock),
                "created" => query.SortOrder == "desc"
                                ? q.OrderByDescending(p => p.CreatedAt)
                                : q.OrderBy(p => p.CreatedAt),
                _ => q.OrderByDescending(p => p.CreatedAt)
            };

            var totalCount = await q.CountAsync();

            var items = await q
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(p => new ProductResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    Stock = p.Stock,
                    ImageUrl = p.ImageUrl,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category.Name
                })
                .ToListAsync();

            var result = new PagedResult<ProductResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };

            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
            return result;
        }
        public async Task<ProductResponseDto> GetByIdAsync(int id)
        {
            var cacheKey = $"{ProductPrefix}{id}";

            // Try cache first
            var cached = await _cache.GetAsync<ProductResponseDto>(cacheKey);
            if (cached != null) return cached;

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                throw new KeyNotFoundException("Product not found.");

            var result = MapToDto(product);

            // Cache the product and register its key for prefix-based invalidation
            await _cache.SetAsync(cacheKey, result, ImageCacheExpiry);

            return result;
        }

        public async Task<List<ProductResponseDto>> GetByCategoryAsync(int categoryId)
        {
            var cacheKey = $"{ProductPrefix}category_{categoryId}";

            var cached = await _cache.GetAsync<List<ProductResponseDto>>(cacheKey);
            if (cached != null) return cached;

            var products = await _context.Products
                .Where(p => p.CategoryId == categoryId)
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var result = products.Select(MapToDto).ToList();

            await _cache.SetAsync(cacheKey, result, ListCacheExpiry);

            return result;
        }

        public async Task<ProductResponseDto> CreateAsync(CreateProductDto dto)
        {
            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId);
            if (!categoryExists)
                throw new KeyNotFoundException("Category not found.");

            string? imageUrl = null;
            if (dto.Image != null && dto.Image.Length > 0)
                imageUrl = await _imageService.UploadImageAsync(dto.Image, "products");

            var product = new Product
            {
                Name        = dto.Name,
                Description = dto.Description,
                Price       = dto.Price,
                Stock       = dto.Stock,
                ImageUrl    = imageUrl,
                CategoryId  = dto.CategoryId
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            await InvalidateProductCaches(product.CategoryId);

            return await GetByIdAsync(product.Id);
        }

        public async Task<ProductResponseDto> UpdateAsync(int id, UpdateProductDto dto)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                throw new KeyNotFoundException("Product not found.");

            if (dto.CategoryId.HasValue)
            {
                var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId);
                if (!categoryExists)
                    throw new KeyNotFoundException("Category not found.");

                product.CategoryId = dto.CategoryId.Value;
            }

            if (dto.Name != null) product.Name = dto.Name;
            if (dto.Description != null) product.Description = dto.Description;
            if (dto.Price != null) product.Price = dto.Price.Value;
            if (dto.Stock != null) product.Stock = dto.Stock.Value;
            if (dto.ImageUrl != null) product.ImageUrl = dto.ImageUrl;

            await _context.SaveChangesAsync();

            // Invalidate this product's cache and list caches
            await InvalidateProductCaches(product.CategoryId, id);

            return await GetByIdAsync(product.Id);
        }

        public async Task DeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                throw new KeyNotFoundException("Product not found.");

            var categoryId = product.CategoryId;

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            // Invalidate caches
            await InvalidateProductCaches(categoryId, id);
        }

        // -------------------------------------------------------------------------
        // Private helpers
        // -------------------------------------------------------------------------

        private async Task InvalidateProductCaches(int categoryId, int? productId = null)
        {
            // Invalidate specific product cache
            if (productId.HasValue)
                await _cache.RemoveAsync($"{ProductPrefix}{productId}");

            // Invalidate list caches
            await _cache.RemoveAsync(ProductListKey);
            await _cache.RemoveAsync($"{ProductPrefix}category_{categoryId}");
        }

        private static ProductResponseDto MapToDto(Product p) => new()
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            Stock = p.Stock,
            ImageUrl = p.ImageUrl,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name,
            CreatedAt = p.CreatedAt
        };
    }
}
