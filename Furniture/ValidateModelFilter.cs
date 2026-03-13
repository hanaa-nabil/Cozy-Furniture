using Furniture.Application.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Furniture
{
    public class ValidateModelFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);

                context.Result = new BadRequestObjectResult(new AuthResponseDto
                {
                    Success = false,
                    Message = string.Join(", ", errors)
                });
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
