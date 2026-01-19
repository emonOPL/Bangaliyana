using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Diagnostics;

namespace Bangaliyana.Controllers
{
    public class ErrorController : Controller
    {
        private readonly ILogger<ErrorController> _logger;

        public ErrorController(ILogger<ErrorController> logger)
        {
            _logger = logger;
        }

        [Route("/Error/{statusCode?}")]
        public IActionResult Index(int? statusCode)
        {
            var code = statusCode ?? 500;

            // Get exception details for logging (not displayed to user in production)
            var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (exceptionFeature != null)
            {
                _logger.LogError(exceptionFeature.Error, "Unhandled exception at {Path}", exceptionFeature.Path);
            }

            var errorInfo = GetErrorInfo(code);

            ViewBag.StatusCode = code;
            ViewBag.Title = errorInfo.Title;
            ViewBag.Message = errorInfo.Message;
            ViewBag.Description = errorInfo.Description;
            ViewBag.Icon = errorInfo.Icon;
            ViewBag.Color = errorInfo.Color;

            Response.StatusCode = code;
            return View("Error");
        }

        private (string Title, string Message, string Description, string Icon, string Color) GetErrorInfo(int statusCode)
        {
            return statusCode switch
            {
                400 => ("Bad Request", "Oops! Something went wrong with your request.", "The server could not understand your request. Please check and try again.", "fa-exclamation-circle", "warning"),
                401 => ("Unauthorized", "Access Denied!", "You need to log in to access this page.", "fa-lock", "danger"),
                403 => ("Forbidden", "Access Forbidden!", "You don't have permission to access this resource.", "fa-ban", "danger"),
                404 => ("Page Not Found", "Oops! We can't find that page.", "The page you're looking for might have been moved, deleted, or never existed.", "fa-search", "primary"),
                405 => ("Method Not Allowed", "Method Not Allowed!", "The requested method is not supported for this resource.", "fa-hand-paper", "warning"),
                408 => ("Request Timeout", "Request Timed Out!", "The server took too long to respond. Please try again.", "fa-clock", "warning"),
                429 => ("Too Many Requests", "Slow Down!", "You've made too many requests. Please wait a moment and try again.", "fa-tachometer-alt", "warning"),
                500 => ("Server Error", "Something went wrong!", "We're experiencing technical difficulties. Our team has been notified.", "fa-server", "danger"),
                502 => ("Bad Gateway", "Bad Gateway!", "The server received an invalid response. Please try again later.", "fa-plug", "danger"),
                503 => ("Service Unavailable", "Service Unavailable!", "We're temporarily offline for maintenance. Please check back soon.", "fa-tools", "warning"),
                504 => ("Gateway Timeout", "Gateway Timeout!", "The server didn't respond in time. Please try again.", "fa-hourglass-half", "warning"),
                _ => ("Error", "Something Unexpected Happened!", "An unexpected error occurred. Please try again or contact support.", "fa-exclamation-triangle", "danger")
            };
        }
    }
}
