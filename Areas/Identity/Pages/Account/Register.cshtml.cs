// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using Bangaliyana.Models;
using Bangaliyana.Services;

namespace Bangaliyana.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IMenuPermissionService _menuPermissionService;
        private readonly IRewardService _rewardService;
        private readonly INotificationService _notificationService;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            IMenuPermissionService menuPermissionService,
            IRewardService rewardService,
            INotificationService notificationService,
            IStringLocalizer<SharedResources> localizer)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _roleManager = roleManager;
            _logger = logger;
            _emailSender = emailSender;
            _menuPermissionService = menuPermissionService;
            _rewardService = rewardService;
            _notificationService = notificationService;
            _localizer = localizer;
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
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            
            [Required]
            [Display(Name = "First Name")]
            [StringLength(50, ErrorMessage = "The {0} must be at max {1} characters.")]
            public string FirstName { get; set; }
            
            [Required]
            [Display(Name = "Last Name")]
            [StringLength(50, ErrorMessage = "The {0} must be at max {1} characters.")]
            public string LastName { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Phone]
            [Display(Name = "Phone Number")]
            [RegularExpression(@"^(\+?[0-9]{1,3})?([0-9]{10,14})$", ErrorMessage = "Please enter a valid phone number.")]
            public string PhoneNumber { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
            
            [Display(Name = "I agree to the Terms and Conditions")]
            public bool AgreeToTerms { get; set; }

            [StringLength(8, ErrorMessage = "Referral code must be 8 characters.")]
            [Display(Name = "Referral Code")]
            public string ReferralCode { get; set; }
        }


        public async Task<IActionResult> OnGetAsync(string returnUrl = null)
        {
            // If user is already authenticated, redirect them to home page
            if (User.Identity.IsAuthenticated)
            {
                return LocalRedirect(returnUrl ?? Url.Content("~/"));
            }

            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            
            if (!Input.AgreeToTerms)
            {
                ModelState.AddModelError(string.Empty, _localizer["MustAgreeToTerms"].Value);
            }
            
            if (ModelState.IsValid)
            {
                // Check if email is already registered
                var existingEmail = await _userManager.FindByEmailAsync(Input.Email);
                if (existingEmail != null)
                {
                    ModelState.AddModelError(string.Empty, _localizer["EmailAlreadyRegistered"].Value);
                    return Page();
                }
                
                var user = CreateUser();

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);  // Use email as username
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                user.PhoneNumber = Input.PhoneNumber;
                
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");
                    
                    // Get user ID for subsequent operations
                    var userId = await _userManager.GetUserIdAsync(user);

                    // Assign default "User" role to new registrations
                    if (await _roleManager.RoleExistsAsync("User"))
                    {
                        await _userManager.AddToRoleAsync(user, "User");
                        _logger.LogInformation("User assigned to 'User' role.");

                        // Auto-assign all general and required Customer permissions
                        await _menuPermissionService.AssignRolePermissionsAsync(userId, "User");
                        _logger.LogInformation("User assigned default menu permissions.");
                    }

                    // Handle referral code - apply if provided
                    if (!string.IsNullOrWhiteSpace(Input.ReferralCode))
                    {
                        var result2 = await _rewardService.ApplyReferralCodeAsync(userId, Input.ReferralCode);
                        if (result2.Success)
                        {
                            _logger.LogInformation("Applied referral code {Code} for user {UserId}", Input.ReferralCode, userId);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to apply referral code {Code} for user {UserId}: {Message}",
                                Input.ReferralCode, userId, result2.Message);
                        }
                    }

                    // Generate user's own referral code
                    await _rewardService.GenerateReferralCodeAsync(userId);

                    // Create welcome notification and promo code for new user
                    try
                    {
                        await _notificationService.CreateWelcomeNotificationAsync(userId);
                        await _notificationService.CreateFirstOrderPromoAsync(userId);
                        // Also check for any active festival promos
                        await _notificationService.CheckAndCreateFestivalNotificationsAsync(userId);
                        _logger.LogInformation("Welcome notification and promo code created for user {UserId}", userId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create welcome notification/promo for user {UserId}", userId);
                    }

                    // Additional user info is now stored directly in ApplicationUser model
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    // Send confirmation email if email service is configured
                    try
                    {
                        await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                            $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send confirmation email.");
                    }

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return new ApplicationUser
                {
                    FirstName = Input.FirstName,
                    LastName = Input.LastName,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}