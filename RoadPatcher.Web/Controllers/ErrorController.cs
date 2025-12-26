// ...existing code...
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace RoadPatcher.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/{statusCode}")]
        public IActionResult Code(int statusCode)
        {
            Response.StatusCode = statusCode;

            // You can add logging or custom handling per-status-code here
            if (statusCode == 404)
            {
                return View("404");
            }

            // Fallback to generic view that displays the status code
            ViewData["StatusCode"] = statusCode;
            return View("Index");
        }

        [Route("Error/500")]
        public IActionResult Error()
        {
            // Capture exception details if available (for logging)
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (exceptionHandlerPathFeature != null)
            {
                // Here you can log exceptionHandlerPathFeature.Error
            }

            Response.StatusCode = 500;
            return View("500");
        }
    }
}
// ...existing code...
