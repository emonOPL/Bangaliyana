using Bangaliyana.Data;
using Bangaliyana.Models;
using Bangaliyana.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Bangaliyana.Areas.Identity.Pages.Account.Manage
{
    [Authorize(Roles = "Seller")]
    public class PaymentAccountsModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IFileValidationService _fileValidationService;
        private readonly INotificationService _notificationService;

        public PaymentAccountsModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IFileValidationService fileValidationService,
            INotificationService notificationService)
        {
            _userManager = userManager;
            _context = context;
            _fileValidationService = fileValidationService;
            _notificationService = notificationService;
        }

        [TempData]
        public string? StatusMessage { get; set; }

        public Models.Seller? CurrentSeller { get; set; }
        public SellerBankAccount? BankAccount { get; set; }
        public SellerBankAccountChangeRequest? PendingRequest { get; set; }

        [BindProperty]
        public BankChangeInput BankInput { get; set; } = new();

        [BindProperty]
        public MobileBankingInput MobileInput { get; set; } = new();

        public class BankChangeInput
        {
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

            [Required(ErrorMessage = "Checkbook page photo is required")]
            [Display(Name = "Checkbook Page Photo")]
            public IFormFile? CheckbookPhoto { get; set; }

            [StringLength(500)]
            [Display(Name = "Reason for Change")]
            public string? ChangeReason { get; set; }
        }

        public class MobileBankingInput : IValidatableObject
        {
            [Display(Name = "Mobile Banking Provider")]
            public MobileBankingProvider Provider { get; set; } = MobileBankingProvider.None;

            [StringLength(20)]
            [Phone]
            [Display(Name = "Mobile Number")]
            public string? MobileNumber { get; set; }

            [StringLength(100)]
            [Display(Name = "Account Name")]
            public string? AccountName { get; set; }

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                if (Provider != MobileBankingProvider.None && string.IsNullOrWhiteSpace(MobileNumber))
                {
                    yield return new ValidationResult(
                        "Mobile number is required when a provider is selected",
                        new[] { nameof(MobileNumber) });
                }
            }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            CurrentSeller = await _context.Sellers
                .Include(s => s.BankAccounts)
                .Include(s => s.BankAccountChangeRequests.Where(r => r.Status == BankChangeRequestStatus.Pending))
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            if (CurrentSeller == null)
            {
                return RedirectToPage("/Account/BecomeSeller", new { area = "Identity" });
            }

            BankAccount = CurrentSeller.BankAccounts.FirstOrDefault(b => b.AccountType == BankAccountType.Bank);
            PendingRequest = CurrentSeller.BankAccountChangeRequests.FirstOrDefault(r => r.Status == BankChangeRequestStatus.Pending);

            // Pre-fill mobile banking input
            MobileInput.Provider = CurrentSeller.MobileBankingProvider;
            MobileInput.MobileNumber = CurrentSeller.MobileBankingNumber;
            MobileInput.AccountName = CurrentSeller.MobileBankingAccountName;

            return Page();
        }

        public async Task<IActionResult> OnPostRequestBankChangeAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Validate checkbook photo is required
            if (BankInput.CheckbookPhoto == null)
            {
                ModelState.AddModelError("BankInput.CheckbookPhoto", "Checkbook page photo is required");
            }

            // Check ModelState for all form validations
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            CurrentSeller = await _context.Sellers
                .Include(s => s.BankAccounts)
                .Include(s => s.BankAccountChangeRequests)
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            if (CurrentSeller == null) return NotFound();

            // Check if there's already a pending request
            if (CurrentSeller.BankAccountChangeRequests.Any(r => r.Status == BankChangeRequestStatus.Pending))
            {
                StatusMessage = "Error: You already have a pending bank account change request.";
                return RedirectToPage();
            }

            // Validate checkbook photo
            string? checkbookPhotoUrl = null;
            if (BankInput.CheckbookPhoto != null)
            {
                if (!_fileValidationService.ValidateImage(BankInput.CheckbookPhoto, 5, out var error))
                {
                    ModelState.AddModelError("BankInput.CheckbookPhoto", error);
                    await OnGetAsync();
                    return Page();
                }
                checkbookPhotoUrl = await _fileValidationService.SaveFileAsync(
                    BankInput.CheckbookPhoto, "sellers/bank", "checkbook_");
            }

            var existingAccount = CurrentSeller.BankAccounts.FirstOrDefault(b => b.AccountType == BankAccountType.Bank);

            var changeRequest = new SellerBankAccountChangeRequest
            {
                SellerId = CurrentSeller.Id,
                ExistingBankAccountId = existingAccount?.Id,
                NewAccountType = BankAccountType.Bank,
                NewBankName = BankInput.BankName,
                NewBranchName = BankInput.BranchName,
                NewAccountHolderName = BankInput.AccountHolderName,
                NewAccountNumber = BankInput.AccountNumber,
                NewRoutingNumber = BankInput.RoutingNumber,
                NewCheckbookPhotoUrl = checkbookPhotoUrl ?? existingAccount?.CheckbookPhotoUrl,
                ChangeReason = BankInput.ChangeReason,
                Status = BankChangeRequestStatus.Pending,
                RequestedAt = DateTime.UtcNow
            };

            _context.SellerBankAccountChangeRequests.Add(changeRequest);
            await _context.SaveChangesAsync();

            // Notify admins
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var superAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");
            foreach (var admin in admins.Concat(superAdmins).DistinctBy(a => a.Id))
            {
                await _notificationService.CreateNotificationAsync(new UserNotification
                {
                    UserId = admin.Id,
                    Type = "admin",
                    Icon = "fa-university",
                    IconColor = "text-warning",
                    Title = "Bank Account Change Request",
                    Message = $"Seller '{CurrentSeller.ShopName}' has requested a bank account change.",
                    Link = $"/Admin/SellerManagement/BankChangeRequestDetails/{changeRequest.Id}"
                });
            }

            StatusMessage = "Bank account change request submitted successfully. It will be reviewed by admin.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateMobileBankingAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (seller == null) return NotFound();

            seller.MobileBankingProvider = MobileInput.Provider;
            seller.MobileBankingNumber = MobileInput.MobileNumber;
            seller.MobileBankingAccountName = MobileInput.AccountName;
            seller.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            StatusMessage = "Mobile banking details updated successfully.";
            return RedirectToPage();
        }
    }
}
