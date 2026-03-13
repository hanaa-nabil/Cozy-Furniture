using Furniture.Application.DTOs.Category;
using Furniture.Application.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Furniture.Application.Interfaces
{
    public interface ICategoryService
    {
        Task<PagedResult<CategoryResponseDto>> GetAllAsync(CategoryQueryParameters query);
        Task<CategoryResponseDto> GetByIdAsync(int id);
        Task<CategoryResponseDto> CreateAsync(CreateCategoryDto dto);
        Task<CategoryResponseDto> UpdateAsync(int id, UpdateCategoryDto dto);
        Task DeleteAsync(int id);
    }
}
