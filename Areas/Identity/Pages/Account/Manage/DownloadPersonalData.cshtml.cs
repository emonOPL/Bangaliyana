// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Bangaliyana.Models;
using Bangaliyana.Data;
using Bangaliyana.Services;

namespace Bangaliyana.Areas.Identity.Pages.Account.Manage
{
    public class DownloadPersonalDataModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DownloadPersonalDataModel> _logger;
        private readonly ApplicationDbContext _context;
        private readonly PdfGeneratorService _pdfGenerator;

        public DownloadPersonalDataModel(
            UserManager<ApplicationUser> userManager,
            ILogger<DownloadPersonalDataModel> logger,
            ApplicationDbContext context,
            PdfGeneratorService pdfGenerator)
        {
            _userManager = userManager;
            _logger = logger;
            _context = context;
            _pdfGenerator = pdfGenerator;
        }

        public IActionResult OnGet()
        {
            return NotFound();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = _userManager.GetUserId(User);

            // Load user with address navigation properties
            var user = await _context.Users
                .Include(u => u.Division)
                .Include(u => u.District)
                .Include(u => u.Upazila)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            _logger.LogInformation("User with ID '{UserId}' asked for their personal data.", userId);

            // Get user's orders
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.Email == user.Email)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // Get user's reward points transactions
            var pointsTransactions = await _context.RewardPointsTransactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            // Get user's redeemed rewards
            var redeemedRewards = await _context.UserRedeemedRewards
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.RedeemedAt)
                .ToListAsync();

            // Get user's coupon usages
            var couponUsages = await _context.CouponUsages
                .Include(c => c.Coupon)
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UsedAt)
                .ToListAsync();

            // Get user's tickets
            var tickets = await _context.Tickets
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            // Get user's saved addresses
            var savedAddresses = await _context.UserAddresses
                .Include(a => a.Division)
                .Include(a => a.District)
                .Include(a => a.Upazila)
                .Where(a => a.UserId == userId)
                .ToListAsync();

            // Generate PDF with all data
            var pdfBytes = _pdfGenerator.GeneratePersonalDataPdf(
                user, orders, pointsTransactions, redeemedRewards,
                couponUsages, tickets, savedAddresses);

            // Return PDF file
            Response.Headers.TryAdd("Content-Disposition", $"attachment; filename=PersonalData_{DateTime.Now:yyyyMMdd}.pdf");
            return new FileContentResult(pdfBytes, "application/pdf");
        }
    }
}