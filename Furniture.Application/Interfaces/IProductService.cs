using Furniture.Application.DTOs.Category;
using Furniture.Application.DTOs.Common;
using Furniture.Application.DTOs.Product;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Furniture.Application.Interfaces
{
    public interface IProductService
    {
        Task<PagedResult<ProductResponseDto>> GetAllAsync(ProductQueryParameters query);
        Task<ProductResponseDto> GetByIdAsync(int id);
        Task<List<ProductResponseDto>> GetByCategoryAsync(int categoryId);
        Task<ProductResponseDto> CreateAsync(CreateProductDto dto);
        Task<ProductResponseDto> UpdateAsync(int id, UpdateProductDto dto);
       // Task<ProductResponseDto> UploadImageAsync(int id, IFormFile file, IImageService imageService);
        Task DeleteAsync(int id);
    }
}
