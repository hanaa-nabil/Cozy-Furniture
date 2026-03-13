using Furniture.Application.DTOs.Common;
using Furniture.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Furniture.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchService;
        private readonly ICacheService _cacheService;

        public SearchController(ISearchService searchService, ICacheService cacheService)
        {
            _searchService = searchService;
            _cacheService = cacheService;
        }

        [HttpGet]
        public async Task<IActionResult> Search(
            [FromQuery] string query,
            [FromQuery] int limit = 5)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return BadRequest(new { Success = false, Message = "Query must be at least 2 characters." });

            var cacheKey = $"search:{query.ToLower().Trim()}:{limit}";
            var cached = await _cacheService.GetAsync<GlobalSearchResult>(cacheKey);

            if (cached != null)
                return Ok(new { Success = true, Data = cached, FromCache = true });

            var result = await _searchService.SearchAsync(query, limit);
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

            return Ok(new { Success = true, Data = result });
        }
    }
}

