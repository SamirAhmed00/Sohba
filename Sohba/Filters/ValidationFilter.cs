using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Sohba.Application.DTOs.Common;
using System.Linq;

namespace Sohba.Filters
{
    public class ValidationFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState
                    .Where(e => e.Value.Errors.Count > 0)
                    .SelectMany(e => e.Value.Errors.Select(x => x.ErrorMessage))
                    .ToList();

                var errorMessage = string.Join("; ", errors);

                // For AJAX POST requests, return BaseResponseDto.
                // For normal form submissions, we could let it pass to the controller 
                // However, the rule specifies transforming ModelState errors into BaseResponseDto.
                // Let's check if the request expects JSON or is an AJAX request
                bool isAjax = context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                              context.HttpContext.Request.Headers["Accept"].ToString().Contains("application/json") ||
                              context.HttpContext.Request.ContentType?.Contains("application/json") == true;

                if (isAjax)
                {
                    context.Result = new JsonResult(BaseResponseDto<object>.FailureResponse(errorMessage));
                }
                else
                {
                    // If it's not strictly an AJAX request but the controller action expects a View,
                    // we typically shouldn't short-circuit with JSON.
                    // But to adhere strictly to the mandate without knowing the caller's Accept header perfectly,
                    // returning JSON for validation errors on modern stacks is common, or let it fall through.
                    // Given the instruction: "automatically transforms them into our BaseResponseDto.FailureResponse"
                    context.Result = new JsonResult(BaseResponseDto<object>.FailureResponse(errorMessage));
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Do nothing
        }
    }
}
