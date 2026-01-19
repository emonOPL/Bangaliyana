// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Bangaliyana.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using Microsoft.EntityFrameworkCore;
using Bangaliyana.Services;
using Bangaliyana.Extensions;
using Microsoft.AspNetCore.RateLimiting;

namespace Bangaliyana.Areas.Identity.Pages.Account
{
    [EnableRateLimiting("login")]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly ICartService _cartService;
        private readonly IOtpService _otpService;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<LoginModel> logger,
            ICartService cartService,
            IOtpService otpService,
            IStringLocalizer<SharedResources> localizer)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _cartService = cartService;
            _otpService = otpService;
            _localizer = localizer;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        [BindProperty]
        public PhoneInputModel PhoneInput { get; set; }

        [BindProperty]
        public OtpInputModel OtpInput { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        [TempData]
        public string SuccessMessage { get; set; }

        public string ActiveTab { get; set; } = "email";

        public bool ShowOtpField { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public class PhoneInputModel
        {
            [Required(ErrorMessage = "Phone number is required")]
            [Phone(ErrorMessage = "Please enter a valid phone number")]
            [Display(Name = "Phone Number")]
            public string PhoneNumber { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public class OtpInputModel
        {
            [Required(ErrorMessage = "Email or Phone is required")]
            [Display(Name = "Email or Phone")]
            public string Identifier { get; set; }

            [Display(Name = "OTP Code")]
            [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits")]
            public string OtpCode { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string returnUrl = null, string tab = null)
        {
            // If user is already authenticated, redirect them to home page
            if (User.Identity.IsAuthenticated)
            {
                return LocalRedirect(returnUrl ?? Url.Content("~/"));
            }

            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");
            ActiveTab = tab ?? "email";

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
            return Page();
        }

        // Email + Password Login
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ActiveTab = "email";

            // Clear ModelState errors for all properties NOT belonging to Input model
            // This is necessary because all BindProperty models (Input, PhoneInput, OtpInput) are validated together
            // and the error keys may not have the expected prefix
            foreach (var key in ModelState.Keys.Where(k => !k.StartsWith("Input")).ToList())
            {
                ModelState.Remove(key);
            }

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // Find user by email
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user != null)
                {
                    var result = await _signInManager.PasswordSignInAsync(
                        user.UserName,
                        Input.Password,
                        Input.RememberMe,
                        lockoutOnFailure: false);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User logged in with email.");

                        // Merge session cart with user's persistent cart
                        await _cartService.MergeCartsAsync(user.Id);
                        _logger.LogInformation($"Cart merged for user {user.Email}");

                        return LocalRedirect(returnUrl);
                    }
                    if (result.RequiresTwoFactor)
                    {
                        return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                    }
                    if (result.IsLockedOut)
                    {
                        _logger.LogWarning("User account locked out.");
                        return RedirectToPage("./Lockout");
                    }
                }

                ModelState.AddModelError(string.Empty, _localizer["InvalidEmailOrPassword"].Value);
                return Page();
            }

            return Page();
        }

        // Phone + Password Login
        public async Task<IActionResult> OnPostPhoneLoginAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ActiveTab = "phone";
            ReturnUrl = returnUrl;

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            // Clear other model errors
            ModelState.Clear();

            if (string.IsNullOrEmpty(PhoneInput?.PhoneNumber))
            {
                ModelState.AddModelError("PhoneInput.PhoneNumber", _localizer["PhoneNumberRequired"].Value);
                return Page();
            }

            if (string.IsNullOrEmpty(PhoneInput?.Password))
            {
                ModelState.AddModelError("PhoneInput.Password", _localizer["PasswordRequired"].Value);
                return Page();
            }

            // Find user by phone number
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == PhoneInput.PhoneNumber);

            if (user != null)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    user.UserName,
                    PhoneInput.Password,
                    PhoneInput.RememberMe,
                    lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in with phone.");

                    await _cartService.MergeCartsAsync(user.Id);

                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = PhoneInput.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
            }

            ModelState.AddModelError(string.Empty, _localizer["InvalidPhoneOrPassword"].Value);
            return Page();
        }

        // Request OTP
        public async Task<IActionResult> OnPostRequestOtpAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ActiveTab = "otp";
            ReturnUrl = returnUrl;
            ShowOtpField = false;

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ModelState.Clear();

            if (string.IsNullOrEmpty(OtpInput?.Identifier))
            {
                ModelState.AddModelError("OtpInput.Identifier", _localizer["EmailOrPhoneRequired"].Value);
                return Page();
            }

            var identifier = OtpInput.Identifier.Trim();
            ApplicationUser user = null;
            bool isPhone = identifier.StartsWith("+") || identifier.All(c => char.IsDigit(c) || c == '-' || c == ' ');

            if (isPhone)
            {
                user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == identifier);
            }
            else
            {
                user = await _userManager.FindByEmailAsync(identifier);
            }

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, _localizer["NoAccountFoundWithEmailOrPhone"].Value);
                return Page();
            }

            // Generate and send OTP
            var result = await _otpService.GenerateAndSendOtpAsync(identifier, OtpType.Login, isPhone);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage);
                return Page();
            }

            ShowOtpField = true;
            SuccessMessage = $"OTP sent successfully! Valid until {(result.ExpiresAt.HasValue ? result.ExpiresAt.Value.ToBangladeshString("HH:mm") : "")}";
            TempData["OtpSent"] = true;
            TempData["OtpIdentifier"] = identifier;

            return Page();
        }

        // Verify OTP and Login
        public async Task<IActionResult> OnPostVerifyOtpAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ActiveTab = "otp";
            ReturnUrl = returnUrl;
            ShowOtpField = true;

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ModelState.Clear();

            if (string.IsNullOrEmpty(OtpInput?.Identifier))
            {
                ModelState.AddModelError("OtpInput.Identifier", _localizer["EmailOrPhoneRequired"].Value);
                return Page();
            }

            if (string.IsNullOrEmpty(OtpInput?.OtpCode))
            {
                ModelState.AddModelError("OtpInput.OtpCode", _localizer["OtpCodeRequired"].Value);
                return Page();
            }

            var identifier = OtpInput.Identifier.Trim();

            // Validate OTP
            var validationResult = await _otpService.ValidateOtpAsync(identifier, OtpInput.OtpCode, OtpType.Login);

            if (!validationResult.IsValid)
            {
                ModelState.AddModelError(string.Empty, validationResult.ErrorMessage);
                return Page();
            }

            // Find user
            bool isPhone = identifier.StartsWith("+") || identifier.All(c => char.IsDigit(c) || c == '-' || c == ' ');
            ApplicationUser user = isPhone
                ? await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == identifier)
                : await _userManager.FindByEmailAsync(identifier);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, _localizer["UserNotFound"].Value);
                return Page();
            }

            // Sign in the user
            await _signInManager.SignInAsync(user, OtpInput.RememberMe);

            _logger.LogInformation("User logged in with OTP.");

            await _cartService.MergeCartsAsync(user.Id);

            return LocalRedirect(returnUrl);
        }
    }
}
