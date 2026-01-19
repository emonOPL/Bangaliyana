// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Bangaliyana.Models;

namespace Bangaliyana.Areas.Identity.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(SignInManager<ApplicationUser> signInManager, ILogger<LogoutModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            // Sign out from Identity - this properly clears the authentication cookie
            await _signInManager.SignOutAsync();
            
            // Clear all session data
            HttpContext.Session.Clear();
            
            // Determine if we're using HTTPS
            var isHttps = HttpContext.Request.IsHttps;
            
            // Clear authentication cookies explicitly
            if (Request.Cookies.ContainsKey("Bangaliyana.Auth"))
            {
                Response.Cookies.Delete("Bangaliyana.Auth", new CookieOptions
                {
                    Path = "/",
                    Secure = isHttps,
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax
                });
            }
            
            // Clear session cookie
            if (Request.Cookies.ContainsKey("Bangaliyana.Session"))
            {
                Response.Cookies.Delete("Bangaliyana.Session", new CookieOptions
                {
                    Path = "/",
                    Secure = isHttps,
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax
                });
            }
            
            // Clear antiforgery cookie
            if (Request.Cookies.ContainsKey("Bangaliyana.Antiforgery"))
            {
                Response.Cookies.Delete("Bangaliyana.Antiforgery", new CookieOptions
                {
                    Path = "/",
                    Secure = isHttps,
                    HttpOnly = true,
                    SameSite = SameSiteMode.Strict
                });
            }
            
            // Clear any Identity cookies that might still exist
            if (Request.Cookies.ContainsKey(".AspNetCore.Identity.Application"))
            {
                Response.Cookies.Delete(".AspNetCore.Identity.Application", new CookieOptions
                {
                    Path = "/",
                    Secure = true,
                    HttpOnly = true
                });
            }
            
            _logger.LogInformation("User logged out successfully.");
            
            // Add cache control headers to prevent back button login
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            
            returnUrl = returnUrl ?? Url.Content("~/");
            return LocalRedirect(returnUrl);
        }
        
        // Add OnGet method to handle GET requests to logout page
        public IActionResult OnGet()
        {
            // Redirect to home page if accessed via GET
            return RedirectToPage("/Index");
        }
    }
}