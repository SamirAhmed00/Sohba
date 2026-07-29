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
            if (context.ModelState.IsValid) return;
            var errors = context.ModelState
                .Where(e => e.Value.Errors.Count > 0)
                .SelectMany(e => e.Value.Errors.Select(x => x.ErrorMessage))
                .ToList();

            var errorMessage = string.Join("; ", errors);

            var request = context.HttpContext.Request;
            bool isAjax = request.Headers["X-Requested-With"] == "XMLHttpRequest";
            bool expectsJson = request.Headers["Accept"].ToString().Contains("application/json")
                               || request.ContentType?.Contains("application/json") == true;

            if (isAjax || expectsJson)
            {
                context.Result = new JsonResult(BaseResponseDto<object>.FailureResponse(errorMessage));
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Do nothing
        }
    }
}
