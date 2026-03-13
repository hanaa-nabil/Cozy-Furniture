using Furniture.Application.DTOs.Category;
using Furniture.Application.DTOs.Common;
using Furniture.Application.Interfaces;
using Furniture.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Furniture.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService) => _categoryService = categoryService;

        // GET api/category
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] CategoryQueryParameters query)
        {
            var result = await _categoryService.GetAllAsync(query);
            return Ok(new { Success = true, Data = result });
        }

        // GET api/category/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _categoryService.GetByIdAsync(id);
            return Ok(result);
        }

        // POST api/category  (Admin only)
        [HttpPost]
        [Authorize(Roles = AuthConstants.Roles.Admin)]
        public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
        {
            var result = await _categoryService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        // PUT api/category/{id}  (Admin only)
        [HttpPut("{id}")]
        [Authorize(Roles = AuthConstants.Roles.Admin)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryDto dto)
        {
            var result = await _categoryService.UpdateAsync(id, dto);
            return Ok(result);
        }

        // DELETE api/category/{id}  (Admin only)
        [HttpDelete("{id}")]
        [Authorize(Roles = AuthConstants.Roles.Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            await _categoryService.DeleteAsync(id);
            return NoContent();
        }
    }
}
