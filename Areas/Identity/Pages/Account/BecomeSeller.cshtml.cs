using Bangaliyana.Data;
using Bangaliyana.Models;
using Bangaliyana.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Bangaliyana.Areas.Identity.Pages.Account
{
    [Authorize]
    public class BecomeSellerModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IFileValidationService _fileValidationService;

        public BecomeSellerModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            INotificationService notificationService,
            IFileValidationService fileValidationService)
        {
            _userManager = userManager;
            _context = context;
            _notificationService = notificationService;
            _fileValidationService = fileValidationService;
        }

        [TempData]
        public string? StatusMessage { get; set; }

        public bool AlreadySeller { get; set; }
        public bool HasPendingApplication { get; set; }
        public bool RequiresModification { get; set; }
        public Models.Seller? ExistingSeller { get; set; }
        public int CurrentStep { get; set; } = 1;
        public List<SelectListItem> BusinessTypes { get; set; } = new();

        [BindProperty]
        public Step1Model Step1 { get; set; } = new();

        [BindProperty]
        public Step2Model Step2 { get; set; } = new();

        [BindProperty]
        public Step3Model Step3 { get; set; } = new();

        [BindProperty]
        public Step4Model Step4 { get; set; } = new();

        // Step 1: Personal Information
        public class Step1Model
        {
            [Display(Name = "Passport Size Photo")]
            public IFormFile? PassportPhoto { get; set; }

            public string? ExistingPassportPhotoUrl { get; set; }

            [Required(ErrorMessage = "Full name is required")]
            [StringLength(200, MinimumLength = 2)]
            [Display(Name = "Full Name (as per document)")]
            public string FullName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Father's name is required")]
            [StringLength(200)]
            [Display(Name = "Father's Name")]
            public string FatherName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Mother's name is required")]
            [StringLength(200)]
            [Display(Name = "Mother's Name")]
            public string MotherName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Permanent address is required")]
            [StringLength(500)]
            [Display(Name = "Permanent Address")]
            public string PermanentAddress { get; set; } = string.Empty;

            [StringLength(500)]
            [Display(Name = "Present Address")]
            public string? PresentAddress { get; set; }

            public bool SameAsPermament { get; set; }
        }

        // Step 2: Identity Verification
        public class Step2Model
        {
            [Required(ErrorMessage = "Document type is required")]
            [Display(Name = "Document Type")]
            public DocumentType DocumentType { get; set; } = DocumentType.NID;

            [Required(ErrorMessage = "Document number is required")]
            [StringLength(50)]
            [Display(Name = "Document Number")]
            public string DocumentNumber { get; set; } = string.Empty;

            [Display(Name = "Document Photo")]
            public IFormFile? DocumentPhoto { get; set; }

            public string? ExistingDocumentPhotoUrl { get; set; }
        }

        // Step 3: Shop Details
        public class Step3Model
        {
            [Required(ErrorMessage = "Shop name is required")]
            [StringLength(200, MinimumLength = 3)]
            [Display(Name = "Shop Name")]
            public string ShopName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Business type is required")]
            [Display(Name = "Business Type")]
            public int BusinessTypeId { get; set; }

            [Required(ErrorMessage = "Shop address is required")]
            [StringLength(500)]
            [Display(Name = "Shop Address")]
            public string BusinessAddress { get; set; } = string.Empty;

            [Required(ErrorMessage = "Mobile number is required")]
            [Phone]
            [StringLength(20)]
            [Display(Name = "Mobile Number")]
            public string BusinessPhone { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email is required")]
            [EmailAddress]
            [StringLength(100)]
            [Display(Name = "Email Address")]
            public string BusinessEmail { get; set; } = string.Empty;

            [StringLength(50)]
            [Display(Name = "Trade License Number")]
            public string? TradeLicenseNumber { get; set; }

            [Display(Name = "Trade License Document")]
            public IFormFile? TradeLicenseDocument { get; set; }

            public string? ExistingTradeLicenseUrl { get; set; }
        }

        // Step 4: Bank Details
        public class Step4Model
        {
            // ============ BANK ACCOUNT (Required) ============
            [Required(ErrorMessage = "Bank name is required")]
            [StringLength(100)]
            [Display(Name = "Bank Name")]
            public string BankName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Branch name is required")]
            [StringLength(100)]
            [Display(Name = "Branch Name")]
            public string BranchName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Account holder name is required")]
            [StringLength(100)]
            [Display(Name = "Account Holder Name")]
            public string AccountHolderName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Account number is required")]
            [StringLength(50)]
            [Display(Name = "Account Number")]
            public string AccountNumber { get; set; } = string.Empty;

            [StringLength(20)]
            [Display(Name = "Routing Number")]
            public string? RoutingNumber { get; set; }

            [StringLength(20)]
            [Display(Name = "SWIFT/BIC Code")]
            public string? SwiftBicCode { get; set; }

            [Display(Name = "Checkbook Page Photo")]
            public IFormFile? CheckbookPhoto { get; set; }

            public string? ExistingCheckbookPhotoUrl { get; set; }

            public BankAccountType AccountType { get; set; } = BankAccountType.Bank;

            // ============ MOBILE BANKING (Optional) ============
            [Display(Name = "Mobile Banking Provider")]
            public MobileBankingProvider MobileBankingProvider { get; set; } = MobileBankingProvider.None;

            [StringLength(20)]
            [Phone]
            [Display(Name = "Mobile Banking Number")]
            public string? MobileBankingNumber { get; set; }

            [StringLength(100)]
            [Display(Name = "Mobile Banking Account Name")]
            public string? MobileBankingAccountName { get; set; }

            // ============ TERMS ============
            [Required(ErrorMessage = "You must accept the terms and conditions")]
            [Display(Name = "I accept the seller terms and conditions")]
            public bool AcceptTerms { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? step)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            await LoadBusinessTypesAsync();

            // Check if already a seller with approved status
            if (await _userManager.IsInRoleAsync(user, "Seller"))
            {
                AlreadySeller = true;
                ExistingSeller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == user.Id);
                return Page();
            }

            // Check for existing application
            ExistingSeller = await _context.Sellers
                .Include(s => s.BankAccounts)
                .Include(s => s.BusinessTypeEntity)
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            if (ExistingSeller != null)
            {
                // Check if pending and not requiring modification
                if (ExistingSeller.ApplicationStatus == ApplicationStatus.Pending)
                {
                    HasPendingApplication = true;
                    return Page();
                }

                // Check if requires modification - allow editing
                if (ExistingSeller.ApplicationStatus == ApplicationStatus.ModificationRequired)
                {
                    RequiresModification = true;
                    await LoadExistingDataAsync();
                    CurrentStep = step ?? ExistingSeller.ApplicationStep;
                    return Page();
                }

                // If draft, continue from where left off
                if (ExistingSeller.ApplicationStatus == ApplicationStatus.Draft)
                {
                    await LoadExistingDataAsync();
                    CurrentStep = step ?? ExistingSeller.ApplicationStep;
                    return Page();
                }

                // Approved or Rejected - show appropriate message
                if (ExistingSeller.ApplicationStatus == ApplicationStatus.Approved)
                {
                    AlreadySeller = true;
                    return Page();
                }

                // Rejected - show rejection reason
                return Page();
            }

            // New application
            CurrentStep = 1;

            // Pre-fill from user profile
            Step1.FullName = user.FullName ?? "";
            Step3.BusinessPhone = user.PhoneNumber ?? "";
            Step3.BusinessEmail = user.Email ?? "";

            return Page();
        }

        public async Task<IActionResult> OnPostStep1Async()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            await LoadBusinessTypesAsync();
            CurrentStep = 1;

            // Check if seller has existing passport photo
            var existingSeller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == user.Id);
            var hasExistingPhoto = existingSeller != null && !string.IsNullOrEmpty(existingSeller.PassportPhotoUrl);

            // Validate passport photo - required if no existing photo
            if (Step1.PassportPhoto == null && !hasExistingPhoto)
            {
                ModelState.AddModelError("Step1.PassportPhoto", "Passport size photo is required");
            }
            else if (Step1.PassportPhoto != null)
            {
                if (!_fileValidationService.ValidateImage(Step1.PassportPhoto, 2, out var error))
                {
                    ModelState.AddModelError("Step1.PassportPhoto", error);
                }
            }

            // Clear validation errors for other steps
            ClearOtherStepErrors(1);

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Get or create seller
            var seller = await GetOrCreateSellerAsync(user);

            // Save passport photo
            if (Step1.PassportPhoto != null)
            {
                // Delete old photo if exists
                _fileValidationService.DeleteFile(seller.PassportPhotoUrl);
                seller.PassportPhotoUrl = await _fileValidationService.SaveFileAsync(
                    Step1.PassportPhoto, "sellers", "passport_");
            }

            seller.FullName = Step1.FullName;
            seller.FatherName = Step1.FatherName;
            seller.MotherName = Step1.MotherName;
            seller.PermanentAddress = Step1.PermanentAddress;
            seller.PresentAddress = Step1.SameAsPermament ? Step1.PermanentAddress : Step1.PresentAddress;
            seller.ApplicationStep = Math.Max(seller.ApplicationStep, 2);
            seller.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return RedirectToPage(new { step = 2 });
        }

        public async Task<IActionResult> OnPostStep2Async()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            await LoadBusinessTypesAsync();
            CurrentStep = 2;

            // Validate document photo
            if (Step2.DocumentPhoto != null)
            {
                if (!_fileValidationService.ValidateDocument(Step2.DocumentPhoto, 5, out var error))
                {
                    ModelState.AddModelError("Step2.DocumentPhoto", error);
                }
            }

            // Validate unique document number
            var existingWithDocument = await _context.Sellers
                .AnyAsync(s => s.DocumentNumber == Step2.DocumentNumber && s.UserId != user.Id);
            if (existingWithDocument)
            {
                ModelState.AddModelError("Step2.DocumentNumber",
                    "This document number is already registered with another shop. One document can only be used for one shop.");
            }

            ClearOtherStepErrors(2);

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (seller == null) return RedirectToPage(new { step = 1 });

            // Save document photo
            if (Step2.DocumentPhoto != null)
            {
                _fileValidationService.DeleteFile(seller.DocumentPhotoUrl);
                seller.DocumentPhotoUrl = await _fileValidationService.SaveFileAsync(
                    Step2.DocumentPhoto, "sellers/documents", "doc_");
            }

            seller.DocumentType = Step2.DocumentType;
            seller.DocumentNumber = Step2.DocumentNumber;
            seller.ApplicationStep = Math.Max(seller.ApplicationStep, 3);
            seller.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return RedirectToPage(new { step = 3 });
        }

        public async Task<IActionResult> OnPostStep3Async()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            await LoadBusinessTypesAsync();
            CurrentStep = 3;

            // Validate trade license if provided
            if (Step3.TradeLicenseDocument != null)
            {
                if (!_fileValidationService.ValidateDocument(Step3.TradeLicenseDocument, 5, out var error))
                {
                    ModelState.AddModelError("Step3.TradeLicenseDocument", error);
                }
            }

            ClearOtherStepErrors(3);

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (seller == null) return RedirectToPage(new { step = 1 });

            // Generate slug
            var slug = GenerateSlug(Step3.ShopName);
            var slugBase = slug;
            var counter = 1;
            while (await _context.Sellers.AnyAsync(s => s.ShopSlug == slug && s.Id != seller.Id))
            {
                slug = $"{slugBase}-{counter++}";
            }

            // Save trade license
            if (Step3.TradeLicenseDocument != null)
            {
                _fileValidationService.DeleteFile(seller.TradeLicenseDocUrl);
                seller.TradeLicenseDocUrl = await _fileValidationService.SaveFileAsync(
                    Step3.TradeLicenseDocument, "sellers/documents", "license_");
            }

            seller.ShopName = Step3.ShopName;
            seller.ShopSlug = slug;
            seller.BusinessTypeId = Step3.BusinessTypeId;
            seller.BusinessAddress = Step3.BusinessAddress;
            seller.BusinessPhone = Step3.BusinessPhone;
            seller.BusinessEmail = Step3.BusinessEmail;
            seller.TradeLicenseNumber = Step3.TradeLicenseNumber;
            seller.ApplicationStep = Math.Max(seller.ApplicationStep, 4);
            seller.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return RedirectToPage(new { step = 4 });
        }

        public async Task<IActionResult> OnPostStep4Async()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            await LoadBusinessTypesAsync();
            CurrentStep = 4;

            // Validate checkbook photo - required if no existing photo
            if (Step4.CheckbookPhoto == null && string.IsNullOrEmpty(Step4.ExistingCheckbookPhotoUrl))
            {
                ModelState.AddModelError("Step4.CheckbookPhoto", "Checkbook page photo is required");
            }
            else if (Step4.CheckbookPhoto != null)
            {
                if (!_fileValidationService.ValidateImage(Step4.CheckbookPhoto, 5, out var error))
                {
                    ModelState.AddModelError("Step4.CheckbookPhoto", error);
                }
            }

            if (!Step4.AcceptTerms)
            {
                ModelState.AddModelError("Step4.AcceptTerms", "You must accept the terms and conditions.");
            }

            ClearOtherStepErrors(4);

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var seller = await _context.Sellers
                .Include(s => s.BankAccounts)
                .FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (seller == null) return RedirectToPage(new { step = 1 });

            // Get or create bank account
            var bankAccount = seller.BankAccounts.FirstOrDefault() ?? new SellerBankAccount
            {
                SellerId = seller.Id,
                CreatedAt = DateTime.UtcNow
            };

            // Save checkbook photo
            if (Step4.CheckbookPhoto != null)
            {
                _fileValidationService.DeleteFile(bankAccount.CheckbookPhotoUrl);
                bankAccount.CheckbookPhotoUrl = await _fileValidationService.SaveFileAsync(
                    Step4.CheckbookPhoto, "sellers/bank", "checkbook_");
            }

            bankAccount.AccountType = BankAccountType.Bank;
            bankAccount.BankName = Step4.BankName;
            bankAccount.BranchName = Step4.BranchName;
            bankAccount.AccountHolderName = Step4.AccountHolderName;
            bankAccount.AccountNumber = Step4.AccountNumber;
            bankAccount.RoutingNumber = Step4.RoutingNumber;
            bankAccount.IsPrimary = true;
            bankAccount.UpdatedAt = DateTime.UtcNow;

            if (bankAccount.Id == 0)
            {
                _context.SellerBankAccounts.Add(bankAccount);
            }

            // Save mobile banking to seller (optional)
            seller.MobileBankingProvider = Step4.MobileBankingProvider;
            seller.MobileBankingNumber = Step4.MobileBankingNumber;
            seller.MobileBankingAccountName = Step4.MobileBankingAccountName;

            // Submit application
            seller.ApplicationStatus = ApplicationStatus.Pending;
            seller.Status = SellerStatus.Pending;
            seller.AppliedAt = DateTime.UtcNow;
            seller.ModificationNotes = null;
            seller.ModificationRequestedAt = null;
            seller.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Notify admins
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var superAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");
            var allAdmins = admins.Concat(superAdmins).DistinctBy(a => a.Id);

            foreach (var admin in allAdmins)
            {
                await _notificationService.CreateNotificationAsync(new UserNotification
                {
                    UserId = admin.Id,
                    Type = "admin",
                    Icon = "fa-user-plus",
                    IconColor = "text-info",
                    Title = "New Seller Application",
                    Message = $"{seller.FullName ?? user.Email} has applied to become a seller with shop name '{seller.ShopName}'.",
                    Link = "/Admin/SellerManagement/Details/" + seller.Id
                });
            }

            StatusMessage = "Your seller application has been submitted successfully! We will review it and get back to you soon.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSaveDraftAsync(int currentStep)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Save current step data as draft without validation
            var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (seller == null)
            {
                seller = new Models.Seller
                {
                    UserId = user.Id,
                    ApplicationStatus = ApplicationStatus.Draft,
                    Status = SellerStatus.Pending,
                    CommissionRate = 5m,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Sellers.Add(seller);
            }

            seller.ApplicationStep = currentStep;
            seller.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            StatusMessage = "Your progress has been saved as draft.";
            return RedirectToPage(new { step = currentStep });
        }

        private async Task<Models.Seller> GetOrCreateSellerAsync(ApplicationUser user)
        {
            var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (seller == null)
            {
                seller = new Models.Seller
                {
                    UserId = user.Id,
                    ShopName = "",
                    ShopSlug = Guid.NewGuid().ToString(),
                    ApplicationStatus = ApplicationStatus.Draft,
                    Status = SellerStatus.Pending,
                    CommissionRate = 5m,
                    ApplicationStep = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Sellers.Add(seller);
                await _context.SaveChangesAsync();
            }
            return seller;
        }

        private async Task LoadExistingDataAsync()
        {
            if (ExistingSeller == null) return;

            // Step 1
            Step1.ExistingPassportPhotoUrl = ExistingSeller.PassportPhotoUrl;
            Step1.FullName = ExistingSeller.FullName ?? "";
            Step1.FatherName = ExistingSeller.FatherName ?? "";
            Step1.MotherName = ExistingSeller.MotherName ?? "";
            Step1.PermanentAddress = ExistingSeller.PermanentAddress ?? "";
            Step1.PresentAddress = ExistingSeller.PresentAddress;

            // Step 2
            Step2.DocumentType = ExistingSeller.DocumentType;
            Step2.DocumentNumber = ExistingSeller.DocumentNumber ?? "";
            Step2.ExistingDocumentPhotoUrl = ExistingSeller.DocumentPhotoUrl;

            // Step 3
            Step3.ShopName = ExistingSeller.ShopName ?? "";
            Step3.BusinessTypeId = ExistingSeller.BusinessTypeId ?? 0;
            Step3.BusinessAddress = ExistingSeller.BusinessAddress ?? "";
            Step3.BusinessPhone = ExistingSeller.BusinessPhone ?? "";
            Step3.BusinessEmail = ExistingSeller.BusinessEmail ?? "";
            Step3.TradeLicenseNumber = ExistingSeller.TradeLicenseNumber;
            Step3.ExistingTradeLicenseUrl = ExistingSeller.TradeLicenseDocUrl;

            // Step 4 - Bank Account
            var bankAccount = ExistingSeller.BankAccounts.FirstOrDefault(b => b.AccountType == BankAccountType.Bank);
            if (bankAccount != null)
            {
                Step4.AccountType = bankAccount.AccountType;
                Step4.BankName = bankAccount.BankName;
                Step4.BranchName = bankAccount.BranchName ?? "";
                Step4.AccountHolderName = bankAccount.AccountHolderName;
                Step4.AccountNumber = bankAccount.AccountNumber;
                Step4.RoutingNumber = bankAccount.RoutingNumber;
                Step4.ExistingCheckbookPhotoUrl = bankAccount.CheckbookPhotoUrl;
            }

            // Step 4 - Mobile Banking
            Step4.MobileBankingProvider = ExistingSeller.MobileBankingProvider;
            Step4.MobileBankingNumber = ExistingSeller.MobileBankingNumber;
            Step4.MobileBankingAccountName = ExistingSeller.MobileBankingAccountName;
        }

        private async Task LoadBusinessTypesAsync()
        {
            BusinessTypes = await _context.BusinessTypes
                .Where(bt => bt.IsActive)
                .OrderBy(bt => bt.DisplayOrder)
                .Select(bt => new SelectListItem
                {
                    Value = bt.Id.ToString(),
                    Text = bt.Name
                })
                .ToListAsync();
        }

        private void ClearOtherStepErrors(int currentStep)
        {
            var keysToRemove = ModelState.Keys
                .Where(k => !k.StartsWith($"Step{currentStep}"))
                .ToList();

            foreach (var key in keysToRemove)
            {
                ModelState.Remove(key);
            }
        }

        private string GenerateSlug(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return Guid.NewGuid().ToString();

            return name.ToLower()
                .Replace(" ", "-")
                .Replace("@", "-")
                .Replace(".", "-")
                .Replace("_", "-")
                .Replace("--", "-")
                .Trim('-');
        }
    }
}
