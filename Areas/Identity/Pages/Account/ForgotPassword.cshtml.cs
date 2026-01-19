// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.RateLimiting;
using Bangaliyana.Models;

namespace Bangaliyana.Areas.Identity.Pages.Account
{
    [EnableRateLimiting("password-reset")]
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ForgotPasswordModel> _logger;

        public ForgotPasswordModel(
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            ILogger<ForgotPasswordModel> logger)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            [Display(Name = "Email Address")]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                
                // Always redirect to confirmation page regardless of user existence
                // This prevents user enumeration attacks
                if (user == null)
                {
                    _logger.LogInformation($"Password reset requested for non-existent email: {Input.Email}");
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                // Generate password reset token
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                
                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code, email = Input.Email },
                    protocol: Request.Scheme);

                try
                {
                    await _emailSender.SendEmailAsync(
                        Input.Email,
                        "Reset Your Password - Bangaliyana",
                        $@"<h2>Reset Password</h2>
                        <p>Hello {user.UserName},</p>
                        <p>You have requested to reset your password for your Bangaliyana account.</p>
                        <p>Please reset your password by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.</p>
                        <p>If you did not request this password reset, please ignore this email.</p>
                        <p>This link will expire in 24 hours for security reasons.</p>
                        <br>
                        <p>Best regards,<br>The Bangaliyana Team</p>");
                    
                    _logger.LogInformation($"Password reset email sent to {Input.Email}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send password reset email to {Input.Email}");
                    
                    // Still redirect to confirmation to prevent user enumeration
                    TempData["EmailError"] = "If an account exists with this email, you will receive password reset instructions. Please check your email.";
                }

                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            return Page();
        }
    }
}