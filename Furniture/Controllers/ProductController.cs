using Furniture.Application.DTOs.Common;
using Furniture.Application.DTOs.Product;
using Furniture.Application.Interfaces;
using Furniture.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Furniture.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] ProductQueryParameters query)
        {
            var result = await _productService.GetAllAsync(query);
            return Ok(new { Success = true, Data = result });
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _productService.GetByIdAsync(id);
            return Ok(new { Success = true, Data = result });
        }

        [HttpGet("category/{categoryId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByCategory(int categoryId)
        {
            var result = await _productService.GetByCategoryAsync(categoryId);
            return Ok(new { Success = true, Data = result });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager,Seller")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] CreateProductDto dto)
        {
            var result = await _productService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, new { Success = true, Data = result });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Manager,Seller")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateProductDto dto)
        {
            var result = await _productService.UpdateAsync(id, dto);
            return Ok(new { Success = true, Data = result });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            await _productService.DeleteAsync(id);
            return Ok(new { Success = true, Message = "Product deleted successfully." });
        }
    }
}