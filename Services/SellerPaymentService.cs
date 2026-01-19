using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.EntityFrameworkCore;

namespace Bangaliyana.Services
{
    public class SellerPaymentService : ISellerPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<SellerPaymentService> _logger;

        public SellerPaymentService(
            ApplicationDbContext context,
            INotificationService notificationService,
            ILogger<SellerPaymentService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        public string GeneratePaymentNumber()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            return $"PAY-{timestamp}-{random}";
        }

        public async Task<SellerPayment?> CreateMonthlyPaymentAsync(int sellerId)
        {
            var seller = await _context.Sellers
                .Include(s => s.BankAccounts)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == sellerId);

            if (seller == null)
            {
                _logger.LogWarning("Seller {SellerId} not found for payment creation", sellerId);
                return null;
            }

            // Check if seller has positive balance
            if (seller.AccountBalance <= 0)
            {
                _logger.LogInformation("Seller {SellerId} has no balance to pay", sellerId);
                return null;
            }

            // Get payment settings
            var settings = await GetSettingsAsync();

            // Check minimum payout amount
            if (seller.AccountBalance < settings.MinimumPayoutAmount)
            {
                _logger.LogInformation("Seller {SellerId} balance {Balance} is below minimum payout {MinPayout}",
                    sellerId, seller.AccountBalance, settings.MinimumPayoutAmount);
                return null;
            }

            // Determine payment method and get account details
            var bankAccount = seller.BankAccounts.FirstOrDefault(b => b.AccountType == BankAccountType.Bank);
            var hasMobileBanking = seller.MobileBankingProvider != MobileBankingProvider.None &&
                                   !string.IsNullOrEmpty(seller.MobileBankingNumber);

            if (bankAccount == null && !hasMobileBanking)
            {
                _logger.LogWarning("Seller {SellerId} has no payment account configured", sellerId);
                return null;
            }

            // Prefer bank account over mobile banking
            var paymentMethod = bankAccount != null ? SellerPaymentMethod.BankTransfer : SellerPaymentMethod.MobileBanking;

            // Calculate period
            var now = DateTime.UtcNow;
            var periodEnd = new DateTime(now.Year, now.Month, 1).AddDays(-1); // Last day of previous month
            var periodStart = periodEnd.AddMonths(-1).AddDays(1); // First day of previous month

            // Create the payment record
            var payment = new SellerPayment
            {
                PaymentNumber = GeneratePaymentNumber(),
                SellerId = sellerId,
                Amount = seller.AccountBalance,
                PaymentMethod = paymentMethod,
                Status = SellerPaymentStatus.Pending,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Snapshot payment details based on method
            if (paymentMethod == SellerPaymentMethod.BankTransfer && bankAccount != null)
            {
                payment.BankName = bankAccount.BankName;
                payment.BranchName = bankAccount.BranchName;
                payment.AccountHolderName = bankAccount.AccountHolderName;
                payment.AccountNumber = bankAccount.AccountNumber;
                payment.RoutingNumber = bankAccount.RoutingNumber;
            }
            else if (paymentMethod == SellerPaymentMethod.MobileBanking)
            {
                payment.MobileBankingProvider = seller.MobileBankingProvider;
                payment.MobileNumber = seller.MobileBankingNumber;
                payment.MobileAccountName = seller.MobileBankingAccountName;
            }

            _context.SellerPayments.Add(payment);

            // Create withdrawal transaction
            var transaction = new SellerTransaction
            {
                SellerId = sellerId,
                Type = SellerTransactionType.Withdrawal,
                Amount = -seller.AccountBalance,
                Description = $"Monthly payment withdrawal - {payment.PaymentNumber}",
                BalanceAfter = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.SellerTransactions.Add(transaction);

            // Update seller balance
            seller.AccountBalance = 0;
            seller.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Notify admins
            await NotifyAdminsAboutNewPaymentAsync(payment, seller);

            // Notify seller
            if (seller.User != null)
            {
                await _notificationService.CreateNotificationAsync(new UserNotification
                {
                    UserId = seller.UserId,
                    Type = "payment",
                    Icon = "fa-money-bill-wave",
                    IconColor = "text-success",
                    Title = "Payment Initiated",
                    Message = $"Your monthly payment of BDT {payment.Amount:N2} has been initiated. Payment #: {payment.PaymentNumber}",
                    Link = "/Identity/Account/Manage/PaymentStatement"
                });
            }

            _logger.LogInformation("Created payment {PaymentNumber} for seller {SellerId}, amount: {Amount}",
                payment.PaymentNumber, sellerId, payment.Amount);

            return payment;
        }

        public async Task ProcessAllMonthlyPaymentsAsync(string? triggeredByUserId = null)
        {
            _logger.LogInformation("Starting monthly payment processing");

            // Log processing started
            var startLog = new SellerPaymentLog
            {
                LogType = PaymentLogType.ProcessingStarted,
                Message = "Monthly payment processing started",
                TriggeredByUserId = triggeredByUserId,
                CreatedAt = DateTime.UtcNow
            };
            _context.SellerPaymentLogs.Add(startLog);
            await _context.SaveChangesAsync();

            var settings = await GetSettingsAsync();
            if (!settings.AutoProcessPayments)
            {
                _logger.LogInformation("Auto-processing is disabled, skipping");

                // Log skipped
                _context.SellerPaymentLogs.Add(new SellerPaymentLog
                {
                    LogType = PaymentLogType.ProcessingCompleted,
                    Message = "Payment processing skipped - Auto-processing is disabled",
                    TriggeredByUserId = triggeredByUserId,
                    TotalSellersProcessed = 0,
                    SuccessCount = 0,
                    FailCount = 0,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
                return;
            }

            // Get all eligible sellers (approved with positive balance)
            var eligibleSellers = await _context.Sellers
                .Where(s => s.Status == SellerStatus.Approved &&
                           s.AccountBalance > 0 &&
                           s.AccountBalance >= settings.MinimumPayoutAmount)
                .Select(s => new { s.Id, s.AccountBalance })
                .ToListAsync();

            _logger.LogInformation("Found {Count} eligible sellers for payment", eligibleSellers.Count);

            var successCount = 0;
            var failCount = 0;
            decimal totalAmountProcessed = 0;

            foreach (var seller in eligibleSellers)
            {
                try
                {
                    var payment = await CreateMonthlyPaymentAsync(seller.Id);
                    if (payment != null)
                    {
                        successCount++;
                        totalAmountProcessed += payment.Amount;

                        // Log individual payment created
                        _context.SellerPaymentLogs.Add(new SellerPaymentLog
                        {
                            LogType = PaymentLogType.PaymentCreated,
                            Message = $"Payment #{payment.PaymentNumber} created for BDT {(int)Math.Round(payment.Amount)}",
                            SellerPaymentId = payment.Id,
                            SellerId = seller.Id,
                            TotalAmountProcessed = payment.Amount,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
                catch (Exception ex)
                {
                    failCount++;
                    _logger.LogError(ex, "Failed to create payment for seller {SellerId}", seller.Id);

                    // Log failure
                    _context.SellerPaymentLogs.Add(new SellerPaymentLog
                    {
                        LogType = PaymentLogType.PaymentFailed,
                        Message = $"Failed to create payment for seller",
                        Details = ex.Message,
                        SellerId = seller.Id,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // Log processing completed
            _context.SellerPaymentLogs.Add(new SellerPaymentLog
            {
                LogType = PaymentLogType.ProcessingCompleted,
                Message = $"Monthly payment processing completed",
                Details = $"Processed {eligibleSellers.Count} sellers. Success: {successCount}, Failed: {failCount}. Total amount: BDT {(int)Math.Round(totalAmountProcessed)}",
                TriggeredByUserId = triggeredByUserId,
                TotalSellersProcessed = eligibleSellers.Count,
                SuccessCount = successCount,
                FailCount = failCount,
                TotalAmountProcessed = totalAmountProcessed,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            _logger.LogInformation("Monthly payment processing completed. Success: {Success}, Failed: {Failed}",
                successCount, failCount);
        }

        public async Task<SellerPayment?> GetPaymentByIdAsync(int paymentId)
        {
            return await _context.SellerPayments
                .Include(p => p.Seller)
                    .ThenInclude(s => s!.User)
                .Include(p => p.ProcessedBy)
                .Include(p => p.Messages)
                    .ThenInclude(m => m.Attachments)
                .FirstOrDefaultAsync(p => p.Id == paymentId);
        }

        public async Task<SellerPayment?> GetPaymentByNumberAsync(string paymentNumber)
        {
            return await _context.SellerPayments
                .Include(p => p.Seller)
                    .ThenInclude(s => s!.User)
                .Include(p => p.ProcessedBy)
                .FirstOrDefaultAsync(p => p.PaymentNumber == paymentNumber);
        }

        public async Task<bool> UpdatePaymentStatusAsync(int paymentId, SellerPaymentStatus status, string? notes, string? processedByUserId)
        {
            var payment = await _context.SellerPayments
                .Include(p => p.Seller)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null) return false;

            var oldStatus = payment.Status;
            payment.Status = status;
            payment.AdminNotes = notes;
            payment.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(processedByUserId))
            {
                payment.ProcessedByUserId = processedByUserId;
            }

            // Update timestamps based on status
            switch (status)
            {
                case SellerPaymentStatus.UnderReview:
                    payment.ReviewedAt = DateTime.UtcNow;
                    break;
                case SellerPaymentStatus.Processing:
                    payment.ProcessedAt = DateTime.UtcNow;
                    break;
                case SellerPaymentStatus.Paid:
                    payment.CompletedAt = DateTime.UtcNow;
                    break;
            }

            await _context.SaveChangesAsync();

            // Notify seller of status change
            if (payment.Seller != null)
            {
                await _notificationService.CreateNotificationAsync(new UserNotification
                {
                    UserId = payment.Seller.UserId,
                    Type = "payment",
                    Icon = GetStatusIcon(status),
                    IconColor = GetStatusColor(status),
                    Title = "Payment Status Updated",
                    Message = $"Your payment #{payment.PaymentNumber} status changed from {oldStatus} to {status}",
                    Link = "/Identity/Account/Manage/PaymentStatement"
                });
            }

            return true;
        }

        public async Task<bool> MarkAsPaidAsync(int paymentId, string transactionReference, string processedByUserId)
        {
            var payment = await _context.SellerPayments
                .Include(p => p.Seller)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null) return false;

            payment.Status = SellerPaymentStatus.Paid;
            payment.TransactionReference = transactionReference;
            payment.ProcessedByUserId = processedByUserId;
            payment.CompletedAt = DateTime.UtcNow;
            payment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Notify seller
            if (payment.Seller != null)
            {
                await _notificationService.CreateNotificationAsync(new UserNotification
                {
                    UserId = payment.Seller.UserId,
                    Type = "payment",
                    Icon = "fa-check-circle",
                    IconColor = "text-success",
                    Title = "Payment Completed",
                    Message = $"Your payment #{payment.PaymentNumber} of BDT {payment.Amount:N2} has been paid. Ref: {transactionReference}",
                    Link = "/Identity/Account/Manage/PaymentStatement"
                });
            }

            return true;
        }

        public async Task<bool> MarkAsFailedAsync(int paymentId, string failureReason, string processedByUserId)
        {
            var payment = await _context.SellerPayments
                .Include(p => p.Seller)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null) return false;

            payment.Status = SellerPaymentStatus.Failed;
            payment.FailureReason = failureReason;
            payment.ProcessedByUserId = processedByUserId;
            payment.UpdatedAt = DateTime.UtcNow;

            // Refund the amount back to seller balance
            if (payment.Seller != null)
            {
                payment.Seller.AccountBalance += payment.Amount;
                payment.Seller.UpdatedAt = DateTime.UtcNow;

                // Create refund transaction
                var transaction = new SellerTransaction
                {
                    SellerId = payment.SellerId,
                    Type = SellerTransactionType.Adjustment,
                    Amount = payment.Amount,
                    Description = $"Payment refund - {payment.PaymentNumber} - {failureReason}",
                    BalanceAfter = payment.Seller.AccountBalance,
                    CreatedAt = DateTime.UtcNow
                };

                _context.SellerTransactions.Add(transaction);
            }

            await _context.SaveChangesAsync();

            // Notify seller
            if (payment.Seller != null)
            {
                await _notificationService.CreateNotificationAsync(new UserNotification
                {
                    UserId = payment.Seller.UserId,
                    Type = "payment",
                    Icon = "fa-times-circle",
                    IconColor = "text-danger",
                    Title = "Payment Failed",
                    Message = $"Your payment #{payment.PaymentNumber} failed: {failureReason}. Amount has been refunded to your balance.",
                    Link = "/Identity/Account/Manage/PaymentStatement"
                });
            }

            return true;
        }

        public async Task<IEnumerable<SellerPayment>> GetSellerPaymentsAsync(int sellerId, int page = 1, int pageSize = 10)
        {
            return await _context.SellerPayments
                .Where(p => p.SellerId == sellerId)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(p => p.Messages)
                .ToListAsync();
        }

        public async Task<IEnumerable<SellerPayment>> GetAllPaymentsAsync(SellerPaymentStatus? status = null, string? search = null, int page = 1, int pageSize = 20)
        {
            var query = _context.SellerPayments
                .Include(p => p.Seller)
                    .ThenInclude(s => s!.User)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(p =>
                    p.PaymentNumber.ToLower().Contains(search) ||
                    (p.Seller != null && p.Seller.ShopName.ToLower().Contains(search)) ||
                    (p.AccountHolderName != null && p.AccountHolderName.ToLower().Contains(search)));
            }

            return await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetPaymentsCountAsync(SellerPaymentStatus? status = null, string? search = null)
        {
            var query = _context.SellerPayments.AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(p =>
                    p.PaymentNumber.ToLower().Contains(search) ||
                    (p.Seller != null && p.Seller.ShopName.ToLower().Contains(search)));
            }

            return await query.CountAsync();
        }

        public async Task<Dictionary<SellerPaymentStatus, int>> GetPaymentStatusCountsAsync()
        {
            return await _context.SellerPayments
                .GroupBy(p => p.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);
        }

        // Messaging methods
        public async Task<SellerPaymentMessage> AddPaymentMessageAsync(int paymentId, string senderId, string message, bool isStaffReply)
        {
            var paymentMessage = new SellerPaymentMessage
            {
                SellerPaymentId = paymentId,
                SenderId = senderId,
                Message = message,
                IsStaffReply = isStaffReply,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.SellerPaymentMessages.Add(paymentMessage);
            await _context.SaveChangesAsync();

            // Get payment for notification
            var payment = await _context.SellerPayments
                .Include(p => p.Seller)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment != null)
            {
                if (isStaffReply && payment.Seller != null)
                {
                    // Notify seller
                    await _notificationService.CreateNotificationAsync(new UserNotification
                    {
                        UserId = payment.Seller.UserId,
                        Type = "message",
                        Icon = "fa-comment",
                        IconColor = "text-primary",
                        Title = "New Payment Support Reply",
                        Message = $"You have a new reply regarding payment #{payment.PaymentNumber}",
                        Link = "/Identity/Account/Manage/PaymentStatement"
                    });
                }
                else
                {
                    // Notify admins
                    await NotifyAdminsAboutNewMessageAsync(payment);
                }
            }

            return paymentMessage;
        }

        public async Task<IEnumerable<SellerPaymentMessage>> GetPaymentMessagesAsync(int paymentId)
        {
            return await _context.SellerPaymentMessages
                .Where(m => m.SellerPaymentId == paymentId)
                .OrderBy(m => m.CreatedAt)
                .Include(m => m.Sender)
                .Include(m => m.Attachments)
                .ToListAsync();
        }

        public async Task<int> GetUnreadMessagesCountAsync(int paymentId, bool forStaff)
        {
            return await _context.SellerPaymentMessages
                .Where(m => m.SellerPaymentId == paymentId &&
                           !m.IsRead &&
                           m.IsStaffReply != forStaff) // Staff sees unread from seller, seller sees unread from staff
                .CountAsync();
        }

        public async Task MarkMessagesAsReadAsync(int paymentId, bool forStaff)
        {
            var messages = await _context.SellerPaymentMessages
                .Where(m => m.SellerPaymentId == paymentId &&
                           !m.IsRead &&
                           m.IsStaffReply != forStaff)
                .ToListAsync();

            foreach (var message in messages)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<SellerPaymentMessageAttachment> AddMessageAttachmentAsync(int messageId, string fileName, string fileUrl, string? contentType, long? fileSize)
        {
            var attachment = new SellerPaymentMessageAttachment
            {
                MessageId = messageId,
                FileName = fileName,
                FileUrl = fileUrl,
                ContentType = contentType,
                FileSize = fileSize,
                CreatedAt = DateTime.UtcNow
            };

            _context.SellerPaymentMessageAttachments.Add(attachment);
            await _context.SaveChangesAsync();

            return attachment;
        }

        // Settings methods
        public async Task<SellerPaymentSettings> GetSettingsAsync()
        {
            var settings = await _context.SellerPaymentSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new SellerPaymentSettings();
                _context.SellerPaymentSettings.Add(settings);
                await _context.SaveChangesAsync();
            }
            return settings;
        }

        public async Task UpdateSettingsAsync(SellerPaymentSettings settings)
        {
            var existing = await _context.SellerPaymentSettings.FirstOrDefaultAsync();
            if (existing != null)
            {
                existing.MinimumPayoutAmount = settings.MinimumPayoutAmount;
                existing.PaymentDayOfMonth = settings.PaymentDayOfMonth;
                existing.PaymentHour = settings.PaymentHour;
                existing.AutoProcessPayments = settings.AutoProcessPayments;
                existing.SendEmailNotifications = settings.SendEmailNotifications;
                existing.RequireBankAccountVerification = settings.RequireBankAccountVerification;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.SellerPaymentSettings.Add(settings);
            }
            await _context.SaveChangesAsync();
        }

        // Log methods
        public async Task<IEnumerable<SellerPaymentLog>> GetPaymentLogsAsync(int page = 1, int pageSize = 50)
        {
            return await _context.SellerPaymentLogs
                .Include(l => l.SellerPayment)
                .Include(l => l.Seller)
                .Include(l => l.TriggeredBy)
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetPaymentLogsCountAsync()
        {
            return await _context.SellerPaymentLogs.CountAsync();
        }

        public async Task AddLogAsync(PaymentLogType logType, string message, string? details = null, int? paymentId = null, int? sellerId = null, string? userId = null)
        {
            var log = new SellerPaymentLog
            {
                LogType = logType,
                Message = message,
                Details = details,
                SellerPaymentId = paymentId,
                SellerId = sellerId,
                TriggeredByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.SellerPaymentLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        // Helper methods
        private async Task NotifyAdminsAboutNewPaymentAsync(SellerPayment payment, Seller seller)
        {
            var admins = await _context.UserRoles
                .Join(_context.Roles.Where(r => r.Name == "Admin" || r.Name == "SuperAdmin" || r.Name == "Moderator"),
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => ur.UserId)
                .Distinct()
                .ToListAsync();

            foreach (var adminId in admins)
            {
                await _notificationService.CreateNotificationAsync(new UserNotification
                {
                    UserId = adminId,
                    Type = "admin",
                    Icon = "fa-money-bill-wave",
                    IconColor = "text-warning",
                    Title = "New Seller Payment",
                    Message = $"New payment #{payment.PaymentNumber} for {seller.ShopName}, Amount: BDT {payment.Amount:N2}",
                    Link = $"/Admin/SellerPayment/Details/{payment.Id}"
                });
            }
        }

        private async Task NotifyAdminsAboutNewMessageAsync(SellerPayment payment)
        {
            var admins = await _context.UserRoles
                .Join(_context.Roles.Where(r => r.Name == "Admin" || r.Name == "SuperAdmin" || r.Name == "Moderator"),
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => ur.UserId)
                .Distinct()
                .ToListAsync();

            foreach (var adminId in admins)
            {
                await _notificationService.CreateNotificationAsync(new UserNotification
                {
                    UserId = adminId,
                    Type = "message",
                    Icon = "fa-comment",
                    IconColor = "text-primary",
                    Title = "New Payment Support Message",
                    Message = $"New message on payment #{payment.PaymentNumber}",
                    Link = $"/Admin/SellerPayment/Messages/{payment.Id}"
                });
            }
        }

        private string GetStatusIcon(SellerPaymentStatus status) => status switch
        {
            SellerPaymentStatus.Pending => "fa-clock",
            SellerPaymentStatus.UnderReview => "fa-search",
            SellerPaymentStatus.Processing => "fa-spinner",
            SellerPaymentStatus.Paid => "fa-check-circle",
            SellerPaymentStatus.Failed => "fa-times-circle",
            _ => "fa-info-circle"
        };

        private string GetStatusColor(SellerPaymentStatus status) => status switch
        {
            SellerPaymentStatus.Pending => "text-warning",
            SellerPaymentStatus.UnderReview => "text-info",
            SellerPaymentStatus.Processing => "text-primary",
            SellerPaymentStatus.Paid => "text-success",
            SellerPaymentStatus.Failed => "text-danger",
            _ => "text-muted"
        };
    }
}
