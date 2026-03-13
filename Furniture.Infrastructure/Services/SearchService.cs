using Furniture.Application.DTOs.Common;
using Furniture.Application.Interfaces;
using Furniture.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Furniture.Infrastructure.Services
{
    public class SearchService(ApplicationDbContext context) : ISearchService
    {
        public async Task<GlobalSearchResult> SearchAsync(string query, int limit = 5)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new GlobalSearchResult();

            var term = query.Trim().ToLower();

            var products = await context.Products
                .Where(p => p.Name.ToLower().Contains(term) ||
                            p.Description.ToLower().Contains(term))
                .Take(limit)
                .Select(p => new SearchResult
                {
                    Type = "product",
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    ImageUrl = p.ImageUrl
                })
                .ToListAsync();

            var categories = await context.Categories
                .Where(c => c.Name.ToLower().Contains(term))
                .Take(limit)
                .Select(c => new SearchResult
                {
                    Type = "category",
                    Id = c.Id,
                    Name = c.Name,
                    ImageUrl = c.ImageUrl
                })
                .ToListAsync();

            return new GlobalSearchResult
            {
                Products = products,
                Categories = categories
            };
        }
    }
}

