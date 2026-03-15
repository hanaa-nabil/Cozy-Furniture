using Furniture.Application.DTOs.Category;
using Furniture.Application.DTOs.Common;
using Furniture.Application.Interfaces;
using Furniture.Domain.Entities;
using Furniture.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Furniture.Infrastructure.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly IImageService _imageService;
        public CategoryService(ApplicationDbContext context, IImageService imageService)
        {
            _context = context;
            _imageService = imageService;
        }
        public async Task<PagedResult<CategoryResponseDto>> GetAllAsync(CategoryQueryParameters query)
        {
            var q = _context.Categories
                .Include(c => c.Products)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.ToLower();
                q = q.Where(c => c.Name.ToLower().Contains(term));
            }

            if (query.IsActive.HasValue)
                q = q.Where(c => c.IsActive == query.IsActive);

            q = query.SortBy?.ToLower() switch
            {
                "id" => query.SortOrder == "desc"
                                ? q.OrderByDescending(c => c.Id)
                                : q.OrderBy(c => c.Id),
                "name" => query.SortOrder == "desc"
                                ? q.OrderByDescending(c => c.Name)
                                : q.OrderBy(c => c.Name),
                "created" => query.SortOrder == "desc"
                                ? q.OrderByDescending(c => c.CreatedAt)
                                : q.OrderBy(c => c.CreatedAt),
                _ => q.OrderBy(c => c.Name)
            };

            var totalCount = await q.CountAsync();

            var items = await q
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(c => new CategoryResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    ImageUrl = c.ImageUrl,
                    IsActive = c.IsActive,
                    ProductCount = c.Products.Count
                })
                .ToListAsync();

            return new PagedResult<CategoryResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        public async Task<CategoryResponseDto> GetByIdAsync(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                throw new KeyNotFoundException("Category not found.");

            return MapToDto(category);
        }

        public async Task<CategoryResponseDto> CreateAsync(CreateCategoryDto dto)
        {
            var nameExists = await _context.Categories
                .AnyAsync(c => c.Name.ToLower() == dto.Name.ToLower());

            if (nameExists)
                throw new InvalidOperationException("A category with this name already exists.");
            string? imageUrl = null;
            if (dto.Image != null && dto.Image.Length > 0)
                imageUrl = await _imageService.UploadImageAsync(dto.Image, "categories");
            else if (dto.ImageUrl != null)
                imageUrl = dto.ImageUrl;

            var category = new Category
            {
                Name = dto.Name,
                Description = dto.Description,
                ImageUrl = imageUrl, 

                IsActive = dto.IsActive
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(category.Id);
        }

        public async Task<CategoryResponseDto> UpdateAsync(int id, UpdateCategoryDto dto)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                throw new KeyNotFoundException("Category not found.");

            // Check name uniqueness only if name is being changed
            if (dto.Name != null && dto.Name.ToLower() != category.Name.ToLower())
            {
                var nameExists = await _context.Categories
                    .AnyAsync(c => c.Name.ToLower() == dto.Name.ToLower());

                if (nameExists)
                    throw new InvalidOperationException("A category with this name already exists.");

                category.Name = dto.Name;
            }

            if (dto.Description != null) category.Description = dto.Description;
            if (dto.ImageUrl != null) category.ImageUrl = dto.ImageUrl;
            category.IsActive = dto.IsActive;


            if (dto.Image != null && dto.Image.Length > 0)
                category.ImageUrl = await _imageService.UploadImageAsync(dto.Image, "categories");
            else if (dto.ImageUrl != null)
                category.ImageUrl = dto.ImageUrl;

            await _context.SaveChangesAsync();

            return await GetByIdAsync(category.Id);
        }

        public async Task DeleteAsync(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                throw new KeyNotFoundException("Category not found.");

            // Prevent deleting a category that still has products
            if (category.Products.Any())
                throw new InvalidOperationException(
                    $"Cannot delete category '{category.Name}' because it has {category.Products.Count} product(s) assigned to it.");

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
        }

        private static CategoryResponseDto MapToDto(Category c) => new()
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            ImageUrl = c.ImageUrl,
            IsActive = c.IsActive,        
            ProductCount = c.Products?.Count ?? 0
        };
    }
}
