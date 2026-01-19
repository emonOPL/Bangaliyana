// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Bangaliyana.Models;
using Bangaliyana.Data;
using System.Text.Json;
using System.IO;

namespace Bangaliyana.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ApplicationDbContext _context;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IWebHostEnvironment webHostEnvironment,
            ApplicationDbContext context,
            IStringLocalizer<SharedResources> localizer)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _webHostEnvironment = webHostEnvironment;
            _context = context;
            _localizer = localizer;
        }

        public string Email { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsPhoneConfirmed { get; set; }
        public string AvatarUrl { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [StringLength(50)]
            [Display(Name = "First Name")]
            public string FirstName { get; set; }
            
            [Required]
            [StringLength(50)]
            [Display(Name = "Last Name")]
            public string LastName { get; set; }
            
            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }

            [Display(Name = "Date of Birth")]
            [DataType(DataType.Date)]
            public DateTime? DateOfBirth { get; set; }

            [Display(Name = "Gender")]
            public Gender? Gender { get; set; }

            // Address Information - Division/District/Upazila/Union based
            [Display(Name = "Division")]
            public int? DivisionId { get; set; }
            
            [Display(Name = "District")]
            public int? DistrictId { get; set; }
            
            [Display(Name = "Upazila")]
            public int? UpazilaId { get; set; }

            [StringLength(200)]
            [Display(Name = "Textual Address")]
            public string Address { get; set; }
            
            [StringLength(20)]
            [Display(Name = "Postal Code")]
            public string PostalCode { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var email = await _userManager.GetEmailAsync(user);
            Email = email;

            IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
            PhoneNumber = user.PhoneNumber;
            IsPhoneConfirmed = user.PhoneNumberConfirmed;
            AvatarUrl = user.AvatarUrl;

            Input = new InputModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                DivisionId = user.DivisionId,
                DistrictId = user.DistrictId,
                UpazilaId = user.UpazilaId,
                Address = user.Address,
                PostalCode = user.PostalCode
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            // Update user properties
            var hasChanges = false;
            
            if (Input.FirstName != user.FirstName)
            {
                user.FirstName = Input.FirstName;
                hasChanges = true;
            }
            
            if (Input.LastName != user.LastName)
            {
                user.LastName = Input.LastName;
                hasChanges = true;
            }
            
            if (Input.PhoneNumber != user.PhoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = _localizer["UnexpectedErrorSettingPhoneNumber"].Value;
                    return RedirectToPage();
                }
                // Reset phone confirmation if phone number changed
                user.PhoneNumberConfirmed = false;
                user.PhoneVerificationCode = null;
                user.PhoneVerificationExpiry = null;
                hasChanges = true;
            }
            
            if (Input.DateOfBirth != user.DateOfBirth)
            {
                user.DateOfBirth = Input.DateOfBirth;
                hasChanges = true;
            }

            if (Input.Gender != user.Gender)
            {
                user.Gender = Input.Gender;
                hasChanges = true;
            }

            // Validate and set Division
            if (Input.DivisionId != user.DivisionId)
            {
                if (Input.DivisionId.HasValue)
                {
                    var divisionExists = await _context.Divisions.AnyAsync(d => d.Id == Input.DivisionId.Value);
                    user.DivisionId = divisionExists ? Input.DivisionId : null;
                }
                else
                {
                    user.DivisionId = null;
                }
                hasChanges = true;
            }

            // Validate and set District
            if (Input.DistrictId != user.DistrictId)
            {
                if (Input.DistrictId.HasValue)
                {
                    var districtExists = await _context.Districts.AnyAsync(d => d.Id == Input.DistrictId.Value);
                    user.DistrictId = districtExists ? Input.DistrictId : null;
                }
                else
                {
                    user.DistrictId = null;
                }
                hasChanges = true;
            }

            // Validate and set Upazila
            if (Input.UpazilaId != user.UpazilaId)
            {
                if (Input.UpazilaId.HasValue)
                {
                    var upazilaExists = await _context.Upazilas.AnyAsync(u => u.Id == Input.UpazilaId.Value);
                    user.UpazilaId = upazilaExists ? Input.UpazilaId : null;
                }
                else
                {
                    user.UpazilaId = null;
                }
                hasChanges = true;
            }

            if (Input.Address != user.Address)
            {
                user.Address = Input.Address;
                hasChanges = true;
            }
            
            if (Input.PostalCode != user.PostalCode)
            {
                user.PostalCode = Input.PostalCode;
                hasChanges = true;
            }

            if (hasChanges)
            {
                user.UpdatedAt = DateTime.Now;
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    StatusMessage = _localizer["UnexpectedErrorUpdatingProfile"].Value;
                    return RedirectToPage();
                }
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = _localizer["ProfileUpdated"].Value;
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSendVerificationCodeAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (string.IsNullOrEmpty(user.PhoneNumber))
            {
                return new JsonResult(new { success = false, message = "No phone number found" });
            }

            // Generate a 6-digit verification code
            var random = new Random();
            var code = random.Next(100000, 999999).ToString();
            
            // Store the code and expiry time in the user record
            user.PhoneVerificationCode = code;
            user.PhoneVerificationExpiry = DateTime.UtcNow.AddMinutes(10); // Code expires in 10 minutes
            
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return new JsonResult(new { success = false, message = "Failed to generate verification code" });
            }

            // In a production environment, you would send the code via SMS
            // You can implement actual SMS sending using services like Twilio, AWS SNS, etc.

            return new JsonResult(new {
                success = true,
                message = $"Verification code sent to {user.PhoneNumber}"
            });
        }

        public async Task<IActionResult> OnPostVerifyPhoneAsync([FromBody] VerifyPhoneRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            // Check if the code is valid and not expired
            if (string.IsNullOrEmpty(user.PhoneVerificationCode) || 
                user.PhoneVerificationExpiry == null ||
                user.PhoneVerificationExpiry < DateTime.UtcNow)
            {
                return new JsonResult(new { success = false, message = "Verification code expired or invalid" });
            }

            if (user.PhoneVerificationCode != request.Code)
            {
                return new JsonResult(new { success = false, message = "Invalid verification code" });
            }

            // Mark phone as confirmed
            user.PhoneNumberConfirmed = true;
            user.PhoneVerificationCode = null;
            user.PhoneVerificationExpiry = null;
            user.UpdatedAt = DateTime.Now;
            
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return new JsonResult(new { success = false, message = "Failed to verify phone number" });
            }

            await _signInManager.RefreshSignInAsync(user);
            
            return new JsonResult(new { success = true, message = "Phone number verified successfully!" });
        }

        public class VerifyPhoneRequest
        {
            public string Code { get; set; }
        }

        public async Task<IActionResult> OnPostUploadAvatarAsync(IFormFile avatar)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return new JsonResult(new { success = false, message = "User not found" });
            }

            if (avatar == null || avatar.Length == 0)
            {
                return new JsonResult(new { success = false, message = "No file uploaded" });
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(avatar.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return new JsonResult(new { success = false, message = "Invalid file type. Only JPG, PNG, GIF, and WebP are allowed." });
            }

            // Validate file size (max 5MB)
            if (avatar.Length > 5 * 1024 * 1024)
            {
                return new JsonResult(new { success = false, message = "File size must be less than 5MB" });
            }

            try
            {
                // Create avatars directory if it doesn't exist
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "avatars");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Delete old avatar if exists
                if (!string.IsNullOrEmpty(user.AvatarUrl))
                {
                    var oldAvatarPath = Path.Combine(_webHostEnvironment.WebRootPath, user.AvatarUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldAvatarPath))
                    {
                        System.IO.File.Delete(oldAvatarPath);
                    }
                }

                // Generate unique filename
                var uniqueFileName = $"{user.Id}_{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await avatar.CopyToAsync(stream);
                }

                // Update user's avatar URL
                user.AvatarUrl = $"/images/avatars/{uniqueFileName}";
                user.UpdatedAt = DateTime.Now;
                var updateResult = await _userManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                {
                    return new JsonResult(new { success = false, message = "Failed to update profile picture" });
                }

                await _signInManager.RefreshSignInAsync(user);

                return new JsonResult(new { success = true, message = "Profile picture updated successfully", avatarUrl = user.AvatarUrl });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = "An error occurred while uploading the file" });
            }
        }

        public async Task<IActionResult> OnPostRemoveAvatarAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return new JsonResult(new { success = false, message = "User not found" });
            }

            try
            {
                // Delete avatar file if exists
                if (!string.IsNullOrEmpty(user.AvatarUrl))
                {
                    var avatarPath = Path.Combine(_webHostEnvironment.WebRootPath, user.AvatarUrl.TrimStart('/'));
                    if (System.IO.File.Exists(avatarPath))
                    {
                        System.IO.File.Delete(avatarPath);
                    }
                }

                // Clear avatar URL
                user.AvatarUrl = null;
                user.UpdatedAt = DateTime.Now;
                var updateResult = await _userManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                {
                    return new JsonResult(new { success = false, message = "Failed to remove profile picture" });
                }

                await _signInManager.RefreshSignInAsync(user);

                return new JsonResult(new { success = true, message = "Profile picture removed successfully" });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = "An error occurred while removing the file" });
            }
        }
    }
}