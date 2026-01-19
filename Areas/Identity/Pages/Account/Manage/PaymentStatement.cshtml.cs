using Bangaliyana.Data;
using Bangaliyana.Models;
using Bangaliyana.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Bangaliyana.Areas.Identity.Pages.Account.Manage
{
    [Authorize(Roles = "Seller")]
    public class PaymentStatementModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ISellerPaymentService _paymentService;
        private readonly IFileValidationService _fileValidationService;

        public PaymentStatementModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            ISellerPaymentService paymentService,
            IFileValidationService fileValidationService)
        {
            _userManager = userManager;
            _context = context;
            _paymentService = paymentService;
            _fileValidationService = fileValidationService;
        }

        [TempData]
        public string? StatusMessage { get; set; }

        public Models.Seller? CurrentSeller { get; set; }
        public IEnumerable<SellerPayment> Payments { get; set; } = new List<SellerPayment>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public SellerPaymentStatus? FilterStatus { get; set; }

        // For message modal
        public SellerPayment? SelectedPayment { get; set; }
        public IEnumerable<SellerPaymentMessage> Messages { get; set; } = new List<SellerPaymentMessage>();

        // For order breakdown
        public List<SellerTransaction> PaymentTransactions { get; set; } = new List<SellerTransaction>();
        public decimal TotalProductPayout { get; set; }
        public decimal TotalCommission { get; set; }

        public async Task<IActionResult> OnGetAsync(SellerPaymentStatus? status, int page = 1, int? viewMessages = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            CurrentSeller = await _context.Sellers
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            if (CurrentSeller == null)
            {
                return RedirectToPage("/Account/BecomeSeller", new { area = "Identity" });
            }

            FilterStatus = status;
            CurrentPage = page;
            var pageSize = 10;

            var query = _context.SellerPayments
                .Where(p => p.SellerId == CurrentSeller.Id)
                .Include(p => p.Messages)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }

            var totalCount = await query.CountAsync();
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            Payments = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Load messages for a specific payment if requested
            if (viewMessages.HasValue)
            {
                SelectedPayment = await _paymentService.GetPaymentByIdAsync(viewMessages.Value);
                if (SelectedPayment != null && SelectedPayment.SellerId == CurrentSeller.Id)
                {
                    Messages = await _paymentService.GetPaymentMessagesAsync(viewMessages.Value);
                    await _paymentService.MarkMessagesAsReadAsync(viewMessages.Value, forStaff: false);

                    // Load transactions for this payment period
                    PaymentTransactions = await _context.SellerTransactions
                        .Include(t => t.Order)
                        .Where(t => t.SellerId == CurrentSeller.Id &&
                                   t.OrderId != null &&
                                   (t.Type == SellerTransactionType.OrderPayout || t.Type == SellerTransactionType.Commission) &&
                                   t.CreatedAt >= SelectedPayment.PeriodStart &&
                                   t.CreatedAt <= SelectedPayment.PeriodEnd.AddDays(1))
                        .OrderByDescending(t => t.CreatedAt)
                        .ToListAsync();

                    TotalProductPayout = PaymentTransactions
                        .Where(t => t.Type == SellerTransactionType.OrderPayout)
                        .Sum(t => t.Amount);
                    TotalCommission = PaymentTransactions
                        .Where(t => t.Type == SellerTransactionType.Commission)
                        .Sum(t => Math.Abs(t.Amount));
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSendMessageAsync(int paymentId, string message, IFormFile? attachment)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                StatusMessage = "Error: Message cannot be empty.";
                return RedirectToPage(new { viewMessages = paymentId });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (seller == null) return NotFound();

            // Verify the payment belongs to this seller
            var payment = await _context.SellerPayments.FirstOrDefaultAsync(p => p.Id == paymentId && p.SellerId == seller.Id);
            if (payment == null)
            {
                StatusMessage = "Error: Payment not found.";
                return RedirectToPage();
            }

            var paymentMessage = await _paymentService.AddPaymentMessageAsync(paymentId, user.Id, message, isStaffReply: false);

            // Handle attachment
            if (attachment != null && attachment.Length > 0)
            {
                if (_fileValidationService.ValidateImage(attachment, 5, out var error) ||
                    _fileValidationService.ValidateDocument(attachment, 10, out error))
                {
                    var fileUrl = await _fileValidationService.SaveFileAsync(attachment, "payment-messages", $"msg_{paymentMessage.Id}_");
                    await _paymentService.AddMessageAttachmentAsync(
                        paymentMessage.Id,
                        attachment.FileName,
                        fileUrl,
                        attachment.ContentType,
                        attachment.Length);
                }
            }

            StatusMessage = "Message sent successfully.";
            return RedirectToPage(new { viewMessages = paymentId });
        }
    }
}
