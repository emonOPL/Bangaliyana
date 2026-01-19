using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Bangaliyana.Data;
using Bangaliyana.Models;

namespace Bangaliyana.Areas.Identity.Pages.Account.Manage
{
    public class OrderReturnsModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB per file
        private const int MaxFiles = 5;

        public OrderReturnsModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public List<Order> ReturnedOrders { get; set; } = new();
        public List<Order> EligibleOrders { get; set; } = new();

        // Stats
        public int TotalReturns { get; set; }
        public int PendingReturns { get; set; }
        public int CompletedReturns { get; set; }
        public decimal TotalRefunded { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        [BindProperty]
        public ReturnRequestInput Input { get; set; } = new();

        [BindProperty]
        public List<IFormFile>? ReturnImages { get; set; }

        public class ReturnRequestInput
        {
            [Required]
            public int OrderId { get; set; }

            [Required]
            [Display(Name = "Reason for Return")]
            public ReturnReason Reason { get; set; }

            [StringLength(1000)]
            [Display(Name = "Additional Details")]
            public string? Details { get; set; }

            [Display(Name = "Prefer Replacement")]
            public bool PreferReplacement { get; set; }

            [Display(Name = "Preferred Refund Method")]
            public RefundMethod RefundMethod { get; set; } = RefundMethod.WalletCredit;

            [StringLength(100)]
            [Display(Name = "Account Number")]
            public string? AccountNumber { get; set; }

            [StringLength(100)]
            [Display(Name = "Account Holder Name")]
            public string? AccountHolderName { get; set; }

            [StringLength(100)]
            [Display(Name = "Bank Name")]
            public string? BankName { get; set; }
        }

        public enum ReturnReason
        {
            [Display(Name = "Damaged Product")]
            Damaged,
            [Display(Name = "Wrong Item Received")]
            WrongItem,
            [Display(Name = "Product Not as Described")]
            NotAsDescribed,
            [Display(Name = "Quality Not Satisfactory")]
            QualityIssue,
            [Display(Name = "Size/Fit Issue")]
            SizeIssue,
            [Display(Name = "Changed Mind")]
            ChangedMind,
            [Display(Name = "Other")]
            Other
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            // Get orders with return status or return reason
            ReturnedOrders = await _context.Orders
                .Where(o => o.UserId == user.Id &&
                    (o.Status == OrderStatus.Returned ||
                     o.Status == OrderStatus.Refunded ||
                     o.ReturnReason != null))
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.ReturnedAt ?? o.UpdatedAt)
                .ToListAsync();

            // Get delivered orders eligible for return (within 7 days)
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
            EligibleOrders = await _context.Orders
                .Where(o => o.UserId == user.Id &&
                    o.Status == OrderStatus.Delivered &&
                    o.DeliveredAt >= sevenDaysAgo &&
                    o.ReturnReason == null)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.DeliveredAt)
                .ToListAsync();

            // Calculate stats
            TotalReturns = ReturnedOrders.Count;
            PendingReturns = ReturnedOrders.Count(o => o.Status == OrderStatus.Returned && o.PaymentStatus != PaymentStatus.Refunded);
            CompletedReturns = ReturnedOrders.Count(o => o.PaymentStatus == PaymentStatus.Refunded || o.PaymentStatus == PaymentStatus.PartiallyRefunded);
            TotalRefunded = ReturnedOrders
                .Where(o => o.PaymentStatus == PaymentStatus.Refunded)
                .Sum(o => o.TotalAmount);

            return Page();
        }

        public async Task<IActionResult> OnPostRequestReturnAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == Input.OrderId && o.UserId == user.Id);

            if (order == null)
            {
                StatusMessage = "Error: Order not found.";
                return RedirectToPage();
            }

            // Verify order is eligible for return
            if (order.Status != OrderStatus.Delivered)
            {
                StatusMessage = "Error: Only delivered orders can be returned.";
                return RedirectToPage();
            }

            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
            if (order.DeliveredAt < sevenDaysAgo)
            {
                StatusMessage = "Error: Return period has expired (7 days from delivery).";
                return RedirectToPage();
            }

            // Validate refund method requirements
            if (Input.RefundMethod != RefundMethod.WalletCredit &&
                Input.RefundMethod != RefundMethod.OriginalMethod &&
                !Input.PreferReplacement)
            {
                if (string.IsNullOrWhiteSpace(Input.AccountNumber))
                {
                    StatusMessage = "Error: Account number is required for the selected refund method.";
                    return RedirectToPage();
                }

                if (Input.RefundMethod == RefundMethod.BankTransfer)
                {
                    if (string.IsNullOrWhiteSpace(Input.AccountHolderName))
                    {
                        StatusMessage = "Error: Account holder name is required for bank transfer.";
                        return RedirectToPage();
                    }
                    if (string.IsNullOrWhiteSpace(Input.BankName))
                    {
                        StatusMessage = "Error: Bank name is required for bank transfer.";
                        return RedirectToPage();
                    }
                }
            }

            // Handle image uploads
            var uploadedImages = new List<string>();
            if (ReturnImages != null && ReturnImages.Count > 0)
            {
                if (ReturnImages.Count > MaxFiles)
                {
                    StatusMessage = $"Error: Maximum {MaxFiles} images allowed.";
                    return RedirectToPage();
                }

                // Create returns folder if it doesn't exist
                var returnsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "returns");
                if (!Directory.Exists(returnsFolder))
                {
                    Directory.CreateDirectory(returnsFolder);
                }

                foreach (var file in ReturnImages)
                {
                    if (file.Length > 0)
                    {
                        // Validate file size
                        if (file.Length > MaxFileSize)
                        {
                            StatusMessage = $"Error: File {file.FileName} exceeds maximum size of 5MB.";
                            return RedirectToPage();
                        }

                        // Validate file extension
                        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                        if (!_allowedExtensions.Contains(extension))
                        {
                            StatusMessage = $"Error: File type {extension} is not allowed. Allowed types: JPG, PNG, GIF, WebP.";
                            return RedirectToPage();
                        }

                        // Generate unique filename
                        var fileName = $"return_{order.Id}_{Guid.NewGuid()}{extension}";
                        var filePath = Path.Combine(returnsFolder, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        uploadedImages.Add($"/images/returns/{fileName}");
                    }
                }
            }

            // Build return reason text with proper display name
            var reasonText = GetReturnReasonDisplayName(Input.Reason);
            if (!string.IsNullOrWhiteSpace(Input.Details))
            {
                reasonText += $": {Input.Details}";
            }

            // Update order with return details
            order.ReturnReason = reasonText;
            order.Status = OrderStatus.Returned;
            order.ReturnedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;
            order.PrefersReplacement = Input.PreferReplacement;

            // Save refund method details
            order.PreferredRefundMethod = Input.RefundMethod;
            if (Input.RefundMethod != RefundMethod.WalletCredit &&
                Input.RefundMethod != RefundMethod.OriginalMethod)
            {
                order.RefundAccountNumber = Input.AccountNumber;
                order.RefundAccountHolderName = Input.AccountHolderName;
                order.RefundBankName = Input.BankName;
            }

            // Save uploaded images as JSON array
            if (uploadedImages.Count > 0)
            {
                order.ReturnImages = JsonSerializer.Serialize(uploadedImages);
            }

            await _context.SaveChangesAsync();

            StatusMessage = $"Return request submitted for Order #{order.OrderNumber}. We'll process your request within 2-3 business days.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCancelReturnAsync(int orderId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == user.Id);

            if (order == null)
            {
                StatusMessage = "Error: Order not found.";
                return RedirectToPage();
            }

            // Can only cancel if not yet refunded
            if (order.PaymentStatus == PaymentStatus.Refunded)
            {
                StatusMessage = "Error: Cannot cancel return - refund already processed.";
                return RedirectToPage();
            }

            // Delete uploaded return images
            if (!string.IsNullOrEmpty(order.ReturnImages))
            {
                try
                {
                    var imagePaths = JsonSerializer.Deserialize<List<string>>(order.ReturnImages);
                    if (imagePaths != null)
                    {
                        foreach (var imagePath in imagePaths)
                        {
                            var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                            if (System.IO.File.Exists(fullPath))
                            {
                                System.IO.File.Delete(fullPath);
                            }
                        }
                    }
                }
                catch { /* Ignore errors when deleting images */ }
            }

            // Clear return details
            order.ReturnReason = null;
            order.Status = OrderStatus.Delivered;
            order.ReturnedAt = null;
            order.UpdatedAt = DateTime.UtcNow;
            order.PrefersReplacement = false;
            order.PreferredRefundMethod = null;
            order.RefundAccountNumber = null;
            order.RefundAccountHolderName = null;
            order.RefundBankName = null;
            order.ReturnImages = null;

            await _context.SaveChangesAsync();

            StatusMessage = "Return request cancelled successfully.";
            return RedirectToPage();
        }

        // Helper method to get return images as list
        public List<string> GetReturnImages(Order order)
        {
            if (string.IsNullOrEmpty(order.ReturnImages))
                return new List<string>();

            try
            {
                return JsonSerializer.Deserialize<List<string>>(order.ReturnImages) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        // Helper method to get display name for refund method
        public string GetRefundMethodDisplay(RefundMethod? method)
        {
            if (method == null) return "Not specified";

            return method switch
            {
                RefundMethod.WalletCredit => "Wallet Credit",
                RefundMethod.Bkash => "bKash",
                RefundMethod.Nagad => "Nagad",
                RefundMethod.Upay => "Upay",
                RefundMethod.Rocket => "Rocket",
                RefundMethod.BankTransfer => "Bank Transfer",
                RefundMethod.OriginalMethod => "Original Payment Method",
                _ => method.ToString() ?? "Unknown"
            };
        }

        // Helper method to get display name for return reason
        private string GetReturnReasonDisplayName(ReturnReason reason)
        {
            return reason switch
            {
                ReturnReason.Damaged => "Damaged Product",
                ReturnReason.WrongItem => "Wrong Item Received",
                ReturnReason.NotAsDescribed => "Product Not as Described",
                ReturnReason.QualityIssue => "Quality Not Satisfactory",
                ReturnReason.SizeIssue => "Size/Fit Issue",
                ReturnReason.ChangedMind => "Changed Mind",
                ReturnReason.Other => "Other",
                _ => reason.ToString()
            };
        }

        // Helper method to format stored return reason for display (handles old enum-style data)
        public string FormatReturnReasonForDisplay(string? returnReason)
        {
            if (string.IsNullOrEmpty(returnReason))
                return "No reason specified";

            // Check if it starts with an enum value (no spaces) and convert to display name
            var reasonMappings = new Dictionary<string, string>
            {
                { "Damaged", "Damaged Product" },
                { "WrongItem", "Wrong Item Received" },
                { "NotAsDescribed", "Product Not as Described" },
                { "QualityIssue", "Quality Not Satisfactory" },
                { "SizeIssue", "Size/Fit Issue" },
                { "ChangedMind", "Changed Mind" },
                { "Other", "Other" }
            };

            foreach (var mapping in reasonMappings)
            {
                // Check if reason starts with enum value (with or without colon for details)
                if (returnReason.StartsWith(mapping.Key + ":"))
                {
                    // Replace enum with display name and keep the details
                    return mapping.Value + returnReason.Substring(mapping.Key.Length);
                }
                else if (returnReason == mapping.Key)
                {
                    return mapping.Value;
                }
            }

            // Already properly formatted or custom reason
            return returnReason;
        }
    }
}
