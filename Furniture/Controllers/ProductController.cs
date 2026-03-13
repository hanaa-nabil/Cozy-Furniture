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
        private readonly IImageService _imageService;

        public ProductController(IProductService productService, IImageService imageService)
        {
            _productService = productService;
            _imageService = imageService;
        }

        // GET api/product
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] ProductQueryParameters query)
        {
            var result = await _productService.GetAllAsync(query);
            return Ok(new { Success = true, Data = result });
        }

        // GET api/product/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _productService.GetByIdAsync(id);
            return Ok(result);
        }

        // GET api/product/category/{categoryId}
        [HttpGet("category/{categoryId}")]
        public async Task<IActionResult> GetByCategory(int categoryId)
        {
            var result = await _productService.GetByCategoryAsync(categoryId);
            return Ok(result);
        }

        // POST api/product  (Admin only)
        [HttpPost]
        [Authorize(Roles = AuthConstants.Roles.Admin)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] CreateProductDto dto)
        {
            var result = await _productService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        
        
        // PUT api/product/{id}  (Admin only)
        [HttpPut("{id}")]
        [Authorize(Roles = AuthConstants.Roles.Admin)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto)
        {
            var result = await _productService.UpdateAsync(id, dto);
            return Ok(result);
        }
        

        // DELETE api/product/{id}  (Admin only)
        [HttpDelete("{id}")]
        [Authorize(Roles = AuthConstants.Roles.Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            await _productService.DeleteAsync(id);
            return NoContent();
        }
    }
}