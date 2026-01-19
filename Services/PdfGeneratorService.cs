using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Bangaliyana.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bangaliyana.Services
{
    public class PdfGeneratorService
    {
        private readonly ISiteSettingsService _siteSettingsService;

        // Cache site settings for PDF generation
        private SiteSettings? _cachedSettings;

        static PdfGeneratorService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public PdfGeneratorService(ISiteSettingsService siteSettingsService)
        {
            _siteSettingsService = siteSettingsService;
        }

        private async Task<SiteSettings> GetSettingsAsync()
        {
            _cachedSettings ??= await _siteSettingsService.GetSiteSettingsAsync();
            return _cachedSettings ?? new SiteSettings { SiteName = "Bangaliyana", ContactEmail = "support@bangaliyana.com" };
        }

        public async Task<byte[]> GenerateOrderInvoicePdfAsync(Order order)
        {
            // Get site settings for dynamic branding
            var settings = await GetSettingsAsync();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // Invoice Header
                    page.Header().Element(c => InvoiceHeader(c, order, settings));

                    page.Content()
                        .PaddingVertical(0.5f, Unit.Centimetre)
                        .Column(x =>
                        {
                            x.Spacing(15);

                            // Customer & Order Info
                            x.Item().Element(c => InvoiceInfoSection(c, order));

                            // Order Items Table
                            x.Item().Element(c => InvoiceItemsSection(c, order));

                            // Totals
                            x.Item().Element(c => InvoiceTotalsSection(c, order));

                            // Payment & Delivery Info
                            x.Item().Element(c => InvoicePaymentSection(c, order));

                            // Thank You Note
                            x.Item().PaddingTop(20).Element(c => InvoiceFooterNote(c, settings));
                        });

                    page.Footer().Element(c => InvoiceFooter(c, settings));
                });
            });

            return document.GeneratePdf();
        }

        private void InvoiceHeader(IContainer container, Order order, SiteSettings settings)
        {
            var siteName = settings.SiteName?.ToUpperInvariant() ?? "BANGALIYANA";

            container.Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text(siteName).FontSize(24).Bold().FontColor(Colors.Green.Darken2);
                        c.Item().Text("Invoice").FontSize(14).FontColor(Colors.Grey.Darken1);
                    });
                    row.ConstantItem(150).AlignRight().Column(c =>
                    {
                        c.Item().Text("INVOICE").FontSize(20).Bold().FontColor(Colors.Grey.Darken2);
                        c.Item().Text($"#{order.OrderNumber ?? order.Id.ToString("D6")}").FontSize(12).FontColor(Colors.Grey.Medium);
                    });
                });

                col.Item().PaddingTop(10).BorderBottom(2).BorderColor(Colors.Green.Darken2);
            });
        }

        private void InvoiceInfoSection(IContainer container, Order order)
        {
            container.Row(row =>
            {
                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(c =>
                {
                    c.Item().Text("Bill To:").FontSize(10).Bold().FontColor(Colors.Grey.Darken1);
                    c.Item().PaddingTop(5).Text(order.CustomerName).FontSize(11).Bold();
                    c.Item().Text(order.Phone).FontSize(10);
                    c.Item().Text(order.Email).FontSize(10).FontColor(Colors.Blue.Darken1);
                    c.Item().PaddingTop(5).Text(order.Address).FontSize(10);
                    if (!string.IsNullOrEmpty(order.City))
                        c.Item().Text(order.City).FontSize(10);
                    if (!string.IsNullOrEmpty(order.PostalCode))
                        c.Item().Text($"Postal Code: {order.PostalCode}").FontSize(10);
                });

                row.ConstantItem(20);

                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(c =>
                {
                    c.Item().Text("Order Details:").FontSize(10).Bold().FontColor(Colors.Grey.Darken1);
                    c.Item().PaddingTop(5).Row(r =>
                    {
                        r.AutoItem().Text("Order Date:").FontSize(10).FontColor(Colors.Grey.Darken1);
                        r.AutoItem().PaddingLeft(5).Text(order.OrderDate.ToString("dd MMM yyyy")).FontSize(10).Bold();
                    });
                    c.Item().Row(r =>
                    {
                        r.AutoItem().Text("Invoice Date:").FontSize(10).FontColor(Colors.Grey.Darken1);
                        r.AutoItem().PaddingLeft(5).Text(DateTime.Now.ToString("dd MMM yyyy")).FontSize(10).Bold();
                    });
                    c.Item().Row(r =>
                    {
                        r.AutoItem().Text("Status:").FontSize(10).FontColor(Colors.Grey.Darken1);
                        r.AutoItem().PaddingLeft(5).Text(AddSpacesToEnum(order.Status.ToString())).FontSize(10).Bold()
                            .FontColor(GetOrderStatusColor(order.Status));
                    });
                    c.Item().Row(r =>
                    {
                        r.AutoItem().Text("Payment:").FontSize(10).FontColor(Colors.Grey.Darken1);
                        r.AutoItem().PaddingLeft(5).Text(order.IsPaymentReceived ? "Paid" : "Pending").FontSize(10).Bold()
                            .FontColor(order.IsPaymentReceived ? Colors.Green.Darken2 : Colors.Orange.Darken2);
                    });
                });
            });
        }

        private void InvoiceItemsSection(IContainer container, Order order)
        {
            container.Column(col =>
            {
                col.Item().PaddingBottom(8).Text("Order Items").FontSize(12).Bold().FontColor(Colors.Grey.Darken2);

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(40);
                        columns.RelativeColumn(3);
                        columns.ConstantColumn(70);
                        columns.ConstantColumn(50);
                        columns.ConstantColumn(80);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Green.Darken2).Padding(8).Text("#").FontSize(10).Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Green.Darken2).Padding(8).Text("Product").FontSize(10).Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Green.Darken2).Padding(8).AlignRight().Text("Unit Price").FontSize(10).Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Green.Darken2).Padding(8).AlignCenter().Text("Qty").FontSize(10).Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Green.Darken2).Padding(8).AlignRight().Text("Total").FontSize(10).Bold().FontColor(Colors.White);
                    });

                    var items = order.OrderItems?.ToList() ?? new List<OrderItem>();
                    for (int i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        var bgColor = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                        table.Cell().Background(bgColor).Padding(8).Text((i + 1).ToString()).FontSize(10);
                        table.Cell().Background(bgColor).Padding(8).Column(c =>
                        {
                            c.Item().Text(item.ProductName).FontSize(10).Bold();
                            if (!string.IsNullOrEmpty(item.VariantInfo))
                                c.Item().Text(item.VariantInfo).FontSize(9).FontColor(Colors.Grey.Darken1);
                        });
                        table.Cell().Background(bgColor).Padding(8).AlignRight().Text($"৳{item.UnitPrice:N0}").FontSize(10);
                        table.Cell().Background(bgColor).Padding(8).AlignCenter().Text(item.Quantity.ToString()).FontSize(10);
                        table.Cell().Background(bgColor).Padding(8).AlignRight().Text($"৳{item.TotalPrice:N0}").FontSize(10).Bold();
                    }
                });
            });
        }

        private void InvoiceTotalsSection(IContainer container, Order order)
        {
            container.AlignRight().Width(250).Column(col =>
            {
                col.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).Row(r =>
                {
                    r.RelativeItem().Text("Subtotal:").FontSize(10).FontColor(Colors.Grey.Darken1);
                    r.ConstantItem(100).AlignRight().Text($"৳{order.SubTotal:N0}").FontSize(10);
                });

                if (order.DiscountAmount > 0)
                {
                    col.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).Row(r =>
                    {
                        var discountLabel = !string.IsNullOrEmpty(order.CouponCode) ? $"Coupon ({order.CouponCode}):" : "Discount:";
                        r.RelativeItem().Text(discountLabel).FontSize(10).FontColor(Colors.Green.Darken2);
                        r.ConstantItem(100).AlignRight().Text($"-৳{order.DiscountAmount:N0}").FontSize(10).FontColor(Colors.Green.Darken2);
                    });
                }

                if (order.RewardDiscountAmount > 0)
                {
                    col.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).Row(r =>
                    {
                        r.RelativeItem().Text("Reward Discount:").FontSize(10).FontColor(Colors.Green.Darken2);
                        r.ConstantItem(100).AlignRight().Text($"-৳{order.RewardDiscountAmount:N0}").FontSize(10).FontColor(Colors.Green.Darken2);
                    });
                }

                if (order.PremiumDiscountAmount > 0)
                {
                    col.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).Row(r =>
                    {
                        r.RelativeItem().Text("Premium Discount:").FontSize(10).FontColor(Colors.Green.Darken2);
                        r.ConstantItem(100).AlignRight().Text($"-৳{order.PremiumDiscountAmount:N0}").FontSize(10).FontColor(Colors.Green.Darken2);
                    });
                }

                if (order.WalletAmountUsed > 0)
                {
                    col.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).Row(r =>
                    {
                        r.RelativeItem().Text("Wallet Used:").FontSize(10).FontColor(Colors.Green.Darken2);
                        r.ConstantItem(100).AlignRight().Text($"-৳{order.WalletAmountUsed:N0}").FontSize(10).FontColor(Colors.Green.Darken2);
                    });
                }

                col.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).Row(r =>
                {
                    r.RelativeItem().Text("Delivery:").FontSize(10).FontColor(Colors.Grey.Darken1);
                    r.ConstantItem(100).AlignRight().Text(order.FreeShippingApplied ? "FREE" : $"৳{order.DeliveryCharge:N0}").FontSize(10)
                        .FontColor(order.FreeShippingApplied ? Colors.Green.Darken2 : Colors.Grey.Darken2);
                });

                col.Item().Background(Colors.Green.Darken2).Padding(10).Row(r =>
                {
                    r.RelativeItem().Text("Grand Total:").FontSize(12).Bold().FontColor(Colors.White);
                    r.ConstantItem(100).AlignRight().Text($"৳{order.TotalAmount:N0}").FontSize(12).Bold().FontColor(Colors.White);
                });
            });
        }

        private void InvoicePaymentSection(IContainer container, Order order)
        {
            container.PaddingTop(15).Row(row =>
            {
                row.RelativeItem().Background(Colors.Grey.Lighten4).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                {
                    c.Item().Text("Payment Information").FontSize(10).Bold().FontColor(Colors.Grey.Darken2);
                    c.Item().PaddingTop(5).Text($"Method: {AddSpacesToEnum(order.PaymentMethod.ToString())}").FontSize(9);
                    c.Item().Text($"Status: {(order.IsPaymentReceived ? "Paid" : "Pending")}").FontSize(9)
                        .FontColor(order.IsPaymentReceived ? Colors.Green.Darken2 : Colors.Orange.Darken2);
                    if (!string.IsNullOrEmpty(order.TransactionId))
                        c.Item().Text($"Transaction ID: {order.TransactionId}").FontSize(9);
                    if (!string.IsNullOrEmpty(order.CouponCode))
                        c.Item().Text($"Coupon Applied: {order.CouponCode}").FontSize(9).FontColor(Colors.Green.Darken2);
                });

                row.ConstantItem(15);

                row.RelativeItem().Background(Colors.Grey.Lighten4).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                {
                    c.Item().Text("Delivery Information").FontSize(10).Bold().FontColor(Colors.Grey.Darken2);
                    c.Item().PaddingTop(5).Text($"Status: {AddSpacesToEnum(order.Status.ToString())}").FontSize(9);
                    if (!string.IsNullOrEmpty(order.CourierName))
                        c.Item().Text($"Courier: {order.CourierName}").FontSize(9);
                    if (!string.IsNullOrEmpty(order.TrackingNumber))
                        c.Item().Text($"Tracking: {order.TrackingNumber}").FontSize(9);
                    if (order.ShippedAt.HasValue)
                        c.Item().Text($"Shipped: {order.ShippedAt.Value:dd MMM yyyy}").FontSize(9);
                    if (order.DeliveredAt.HasValue)
                        c.Item().Text($"Delivered: {order.DeliveredAt.Value:dd MMM yyyy}").FontSize(9).FontColor(Colors.Green.Darken2);
                });
            });
        }

        private void InvoiceFooterNote(IContainer container, SiteSettings settings)
        {
            var siteName = settings.SiteName ?? "Bangaliyana";
            var contactEmail = settings.ContactEmail ?? "support@bangaliyana.com";
            var contactPhone = settings.ContactPhone ?? "";

            var contactInfo = !string.IsNullOrEmpty(contactPhone)
                ? $"Email: {contactEmail} | Phone: {contactPhone}"
                : $"Email: {contactEmail}";

            container.Background(Colors.Green.Lighten5).Border(1).BorderColor(Colors.Green.Lighten3).Padding(15).Column(c =>
            {
                c.Item().AlignCenter().Text($"Thank you for shopping with {siteName}!").FontSize(11).Bold().FontColor(Colors.Green.Darken2);
                c.Item().AlignCenter().PaddingTop(5).Text("If you have any questions about this invoice, please contact our support.").FontSize(9).FontColor(Colors.Grey.Darken1);
                c.Item().AlignCenter().Text(contactInfo).FontSize(9).FontColor(Colors.Grey.Darken1);
            });
        }

        private void InvoiceFooter(IContainer container, SiteSettings settings)
        {
            var websiteUrl = !string.IsNullOrEmpty(settings.ContactEmail)
                ? $"www.{settings.SiteName?.ToLowerInvariant().Replace(" ", "") ?? "bangaliyana"}.com"
                : "www.bangaliyana.com";

            container.BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span("Generated on ").FontSize(8).FontColor(Colors.Grey.Medium);
                    t.Span(DateTime.Now.ToString("dd MMM yyyy")).FontSize(8).FontColor(Colors.Grey.Medium);
                    t.Span(" at ").FontSize(8).FontColor(Colors.Grey.Medium);
                    t.Span(DateTime.Now.ToString("hh:mm tt")).FontSize(8).FontColor(Colors.Grey.Medium);
                });
                row.ConstantItem(120).AlignRight().Text(websiteUrl).FontSize(8).FontColor(Colors.Grey.Medium);
            });
        }

        private string GetOrderStatusColor(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => Colors.Orange.Darken2,
                OrderStatus.Confirmed => Colors.Blue.Darken1,
                OrderStatus.Paid => Colors.Green.Darken2,
                OrderStatus.Processing => Colors.Blue.Darken1,
                OrderStatus.Shipped => Colors.Indigo.Darken1,
                OrderStatus.OutForDelivery => Colors.Blue.Darken1,
                OrderStatus.Delivered => Colors.Green.Darken2,
                OrderStatus.Cancelled => Colors.Red.Darken2,
                OrderStatus.Returned => Colors.Orange.Darken2,
                OrderStatus.Refunded => Colors.Grey.Darken1,
                _ => Colors.Grey.Darken1
            };
        }

        public byte[] GeneratePersonalDataPdf(
            ApplicationUser user,
            List<Order> orders,
            List<RewardPointsTransaction> pointsTransactions = null,
            List<UserRedeemedReward> redeemedRewards = null,
            List<CouponUsage> couponUsages = null,
            List<Ticket> tickets = null,
            List<UserAddress> savedAddresses = null)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(HeaderSection);

                    page.Content()
                        .PaddingVertical(0.5f, Unit.Centimetre)
                        .Column(x =>
                        {
                            x.Spacing(15);

                            // 1. Personal Information
                            x.Item().Element(c => PersonalInfoSection(c, user));

                            // 2. Account Summary (Balance, Points, Premium)
                            x.Item().Element(c => AccountSummarySection(c, user));

                            // 3. Saved Addresses
                            if (savedAddresses != null && savedAddresses.Any())
                            {
                                x.Item().Element(c => SavedAddressesSection(c, savedAddresses));
                            }

                            // 4. Order History
                            if (orders != null && orders.Any())
                            {
                                x.Item().PageBreak();
                                x.Item().Element(c => OrdersSection(c, orders));
                            }

                            // 5. Reward Points History
                            if (pointsTransactions != null && pointsTransactions.Any())
                            {
                                x.Item().PageBreak();
                                x.Item().Element(c => PointsHistorySection(c, pointsTransactions));
                            }

                            // 6. Redeemed Rewards
                            if (redeemedRewards != null && redeemedRewards.Any())
                            {
                                x.Item().Element(c => RedeemedRewardsSection(c, redeemedRewards));
                            }

                            // 7. Coupon Usage History
                            if (couponUsages != null && couponUsages.Any())
                            {
                                x.Item().Element(c => CouponUsageSection(c, couponUsages));
                            }

                            // 8. Support Tickets
                            if (tickets != null && tickets.Any())
                            {
                                x.Item().PageBreak();
                                x.Item().Element(c => TicketsSection(c, tickets));
                            }
                        });

                    page.Footer().Element(FooterSection);
                });
            });

            return document.GeneratePdf();
        }

        private void HeaderSection(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("BANGALIYANA").FontSize(22).Bold().FontColor(Colors.Green.Darken2);
                    col.Item().Text("Personal Data Report").FontSize(14).FontColor(Colors.Grey.Darken1);
                });
                row.ConstantItem(130).AlignRight().Column(col =>
                {
                    col.Item().Text($"Generated: {DateTime.Now:dd MMM yyyy}").FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().Text($"Time: {DateTime.Now:hh:mm tt}").FontSize(9).FontColor(Colors.Grey.Medium);
                });
            });
        }

        private void FooterSection(IContainer container)
        {
            container.BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text("Confidential - For personal use only").FontSize(8).FontColor(Colors.Grey.Medium);
                row.ConstantItem(100).AlignRight().Text(x =>
                {
                    x.Span("Page ").FontSize(8);
                    x.CurrentPageNumber().FontSize(8);
                    x.Span(" of ").FontSize(8);
                    x.TotalPages().FontSize(8);
                });
            });
        }

        private void PersonalInfoSection(IContainer container, ApplicationUser user)
        {
            container.Column(col =>
            {
                col.Item().Element(c => SectionHeader(c, "Personal Information"));

                col.Item().Background(Colors.Grey.Lighten4).Border(1).BorderColor(Colors.Grey.Lighten2)
                    .Padding(12).Column(inner =>
                    {
                        inner.Spacing(6);

                        inner.Item().Row(r =>
                        {
                            r.RelativeItem().Element(c => InfoField(c, "Full Name", user.FullName));
                            r.RelativeItem().Element(c => InfoField(c, "Email", user.Email ?? "Not provided"));
                        });

                        inner.Item().Row(r =>
                        {
                            r.RelativeItem().Element(c => InfoField(c, "Phone", user.PhoneNumber ?? "Not provided"));
                            r.RelativeItem().Element(c => InfoField(c, "Date of Birth", user.DateOfBirth?.ToString("dd MMM yyyy") ?? "Not provided"));
                        });

                        inner.Item().Row(r =>
                        {
                            r.RelativeItem().Element(c => InfoField(c, "Email Verified", user.EmailConfirmed ? "Yes" : "No"));
                            r.RelativeItem().Element(c => InfoField(c, "Phone Verified", user.PhoneNumberConfirmed ? "Yes" : "No"));
                        });

                        var addressParts = new List<string>();
                        if (!string.IsNullOrEmpty(user.Address)) addressParts.Add(user.Address);
                        if (user.Upazila != null) addressParts.Add(user.Upazila.Name);
                        if (user.District != null) addressParts.Add(user.District.Name);
                        if (user.Division != null) addressParts.Add(user.Division.Name);
                        if (!string.IsNullOrEmpty(user.PostalCode)) addressParts.Add($"Postal: {user.PostalCode}");
                        var fullAddress = addressParts.Any() ? string.Join(", ", addressParts) : "Not provided";

                        inner.Item().Element(c => InfoField(c, "Address", fullAddress));

                        inner.Item().Row(r =>
                        {
                            r.RelativeItem().Element(c => InfoField(c, "Account Created", user.CreatedAt.ToString("dd MMM yyyy")));
                            r.RelativeItem().Element(c => InfoField(c, "Last Updated", user.UpdatedAt.ToString("dd MMM yyyy hh:mm tt")));
                        });

                        inner.Item().Row(r =>
                        {
                            r.RelativeItem().Element(c => InfoField(c, "Two-Factor Auth", user.TwoFactorEnabled ? "Enabled" : "Disabled"));
                            r.RelativeItem().Element(c => InfoField(c, "Referral Code", user.ReferralCode ?? "Not generated"));
                        });
                    });
            });
        }

        private void AccountSummarySection(IContainer container, ApplicationUser user)
        {
            container.Column(col =>
            {
                col.Item().Element(c => SectionHeader(c, "Account Summary"));

                col.Item().Row(row =>
                {
                    row.Spacing(10);

                    row.RelativeItem().Border(1).BorderColor(Colors.Green.Lighten2)
                        .Background(Colors.Green.Lighten5).Padding(10).Column(c =>
                        {
                            c.Item().Text("Wallet Balance").FontSize(9).FontColor(Colors.Grey.Darken1);
                            c.Item().Text($"৳{user.WalletBalance:N0}").FontSize(16).Bold().FontColor(Colors.Green.Darken2);
                        });

                    row.RelativeItem().Border(1).BorderColor(Colors.Orange.Lighten2)
                        .Background(Colors.Orange.Lighten5).Padding(10).Column(c =>
                        {
                            c.Item().Text("Reward Points").FontSize(9).FontColor(Colors.Grey.Darken1);
                            c.Item().Text($"{user.TotalRewardPoints:N0} pts").FontSize(16).Bold().FontColor(Colors.Orange.Darken2);
                        });

                    row.RelativeItem().Border(1).BorderColor(Colors.Purple.Lighten2)
                        .Background(Colors.Purple.Lighten5).Padding(10).Column(c =>
                        {
                            c.Item().Text("Premium Status").FontSize(9).FontColor(Colors.Grey.Darken1);
                            if (user.IsPremiumMember)
                            {
                                c.Item().Text("Active").FontSize(16).Bold().FontColor(Colors.Purple.Darken2);
                                if (user.PremiumExpiresAt.HasValue)
                                    c.Item().Text($"Expires: {user.PremiumExpiresAt:dd MMM yyyy}").FontSize(8).FontColor(Colors.Grey.Medium);
                            }
                            else
                            {
                                c.Item().Text("Not Active").FontSize(16).Bold().FontColor(Colors.Grey.Medium);
                            }
                        });

                    row.RelativeItem().Border(1).BorderColor(user.IsCODAllowed ? Colors.Blue.Lighten2 : Colors.Red.Lighten2)
                        .Background(user.IsCODAllowed ? Colors.Blue.Lighten5 : Colors.Red.Lighten5).Padding(10).Column(c =>
                        {
                            c.Item().Text("COD Status").FontSize(9).FontColor(Colors.Grey.Darken1);
                            c.Item().Text(user.IsCODAllowed ? "Allowed" : "Restricted").FontSize(16).Bold()
                                .FontColor(user.IsCODAllowed ? Colors.Blue.Darken2 : Colors.Red.Darken2);
                        });
                });
            });
        }

        private void SavedAddressesSection(IContainer container, List<UserAddress> addresses)
        {
            container.Column(col =>
            {
                col.Item().Element(c => SectionHeader(c, $"Saved Addresses ({addresses.Count})"));

                col.Item().Row(row =>
                {
                    row.Spacing(10);
                    foreach (var addr in addresses.Take(3))
                    {
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                        {
                            c.Item().Row(r =>
                            {
                                r.AutoItem().Text(addr.Label ?? "Address").Bold().FontSize(9);
                                if (addr.IsDefault)
                                    r.AutoItem().PaddingLeft(5).Text("(Default)").FontSize(8).FontColor(Colors.Green.Darken1);
                            });
                            c.Item().Text(addr.RecipientName).FontSize(9);
                            c.Item().Text(addr.Phone).FontSize(8).FontColor(Colors.Grey.Darken1);

                            var addrParts = new List<string>();
                            if (!string.IsNullOrEmpty(addr.FullAddress)) addrParts.Add(addr.FullAddress);
                            if (addr.Upazila != null) addrParts.Add(addr.Upazila.Name);
                            if (addr.District != null) addrParts.Add(addr.District.Name);
                            c.Item().Text(string.Join(", ", addrParts)).FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                    }
                });

                if (addresses.Count > 3)
                    col.Item().Text($"+{addresses.Count - 3} more addresses").FontSize(8).FontColor(Colors.Grey.Medium).Italic();
            });
        }

        private void OrdersSection(IContainer container, List<Order> orders)
        {
            container.Column(col =>
            {
                col.Item().Element(c => SectionHeader(c, $"Order History ({orders.Count} orders)"));

                var totalSpent = orders.Sum(o => o.TotalAmount);
                var avgOrder = orders.Count > 0 ? totalSpent / orders.Count : 0;
                var deliveredCount = orders.Count(o => o.Status == OrderStatus.Delivered);

                col.Item().Background(Colors.Blue.Lighten5).Border(1).BorderColor(Colors.Blue.Lighten3).Padding(8).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Total Spent").FontSize(8).FontColor(Colors.Grey.Darken1);
                        c.Item().Text($"৳{totalSpent:N2}").Bold().FontSize(12).FontColor(Colors.Blue.Darken2);
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Avg. Order Value").FontSize(8).FontColor(Colors.Grey.Darken1);
                        c.Item().Text($"৳{avgOrder:N2}").Bold().FontSize(12).FontColor(Colors.Blue.Darken2);
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Delivered Orders").FontSize(8).FontColor(Colors.Grey.Darken1);
                        c.Item().Text($"{deliveredCount}").Bold().FontSize(12).FontColor(Colors.Green.Darken2);
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Total Orders").FontSize(8).FontColor(Colors.Grey.Darken1);
                        c.Item().Text($"{orders.Count}").Bold().FontSize(12).FontColor(Colors.Grey.Darken2);
                    });
                });

                col.Item().PaddingTop(8);

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(55);
                        columns.ConstantColumn(70);
                        columns.RelativeColumn(2);
                        columns.ConstantColumn(70);
                        columns.ConstantColumn(60);
                        columns.ConstantColumn(55);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Darken2).Padding(5).Text("Order #").FontSize(9).Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Grey.Darken2).Padding(5).Text("Date").FontSize(9).Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Grey.Darken2).Padding(5).Text("Items").FontSize(9).Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Grey.Darken2).Padding(5).AlignRight().Text("Amount").FontSize(9).Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Grey.Darken2).Padding(5).AlignCenter().Text("Payment").FontSize(9).Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Grey.Darken2).Padding(5).AlignCenter().Text("Status").FontSize(9).Bold().FontColor(Colors.White);
                    });

                    var displayOrders = orders.Take(20).ToList();
                    var isAlternate = false;
                    foreach (var order in displayOrders)
                    {
                        var bgColor = isAlternate ? Colors.Grey.Lighten4 : Colors.White;
                        isAlternate = !isAlternate;

                        var itemNames = order.OrderItems?.Select(i => i.ProductName).ToList() ?? new List<string>();
                        var itemsText = itemNames.Any()
                            ? string.Join(", ", itemNames.Take(2).Select(n => n.Length > 18 ? n.Substring(0, 15) + "..." : n))
                            : "No items";
                        if (itemNames.Count > 2) itemsText += $" +{itemNames.Count - 2}";

                        table.Cell().Background(bgColor).Padding(4).Text($"#{order.Id}").FontSize(8);
                        table.Cell().Background(bgColor).Padding(4).Text(order.OrderDate.ToString("dd MMM yyyy")).FontSize(8);
                        table.Cell().Background(bgColor).Padding(4).Text(itemsText).FontSize(8);
                        table.Cell().Background(bgColor).Padding(4).AlignRight().Text($"৳{order.TotalAmount:N0}").FontSize(8);
                        table.Cell().Background(bgColor).Padding(4).AlignCenter().Text(AddSpacesToEnum(order.PaymentMethod.ToString())).FontSize(7);
                        table.Cell().Background(bgColor).Padding(4).AlignCenter()
                            .Text(AddSpacesToEnum(order.Status.ToString())).FontSize(8).FontColor(GetStatusColor(order.Status.ToString()));
                    }
                });

                if (orders.Count > 20)
                    col.Item().PaddingTop(5).Text($"+{orders.Count - 20} more orders not shown").FontSize(8).FontColor(Colors.Grey.Medium).Italic();
            });
        }

        private void PointsHistorySection(IContainer container, List<RewardPointsTransaction> transactions)
        {
            container.Column(col =>
            {
                col.Item().Element(c => SectionHeader(c, $"Reward Points History ({transactions.Count} transactions)"));

                var earned = transactions.Where(t => t.Points > 0).Sum(t => t.Points);
                var spent = transactions.Where(t => t.Points < 0).Sum(t => Math.Abs(t.Points));

                col.Item().Background(Colors.Orange.Lighten5).Border(1).BorderColor(Colors.Orange.Lighten3).Padding(8).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Total Earned").FontSize(8).FontColor(Colors.Grey.Darken1);
                        c.Item().Text($"+{earned:N0} pts").Bold().FontSize(12).FontColor(Colors.Green.Darken2);
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Total Spent").FontSize(8).FontColor(Colors.Grey.Darken1);
                        c.Item().Text($"-{spent:N0} pts").Bold().FontSize(12).FontColor(Colors.Red.Darken2);
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Current Balance").FontSize(8).FontColor(Colors.Grey.Darken1);
                        c.Item().Text($"{transactions.FirstOrDefault()?.BalanceAfter ?? 0:N0} pts").Bold().FontSize(12).FontColor(Colors.Orange.Darken2);
                    });
                });

                col.Item().PaddingTop(8);

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(75);
                        columns.RelativeColumn(2);
                        columns.ConstantColumn(70);
                        columns.ConstantColumn(70);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Orange.Darken1).Padding(5).Text("Date").FontSize(9).Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Orange.Darken1).Padding(5).Text("Type / Description").FontSize(9).Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Orange.Darken1).Padding(5).AlignRight().Text("Points").FontSize(9).Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Orange.Darken1).Padding(5).AlignRight().Text("Balance").FontSize(9).Bold().FontColor(Colors.White);
                    });

                    var displayTrans = transactions.Take(15).ToList();
                    var isAlternate = false;
                    foreach (var t in displayTrans)
                    {
                        var bgColor = isAlternate ? Colors.Orange.Lighten5 : Colors.White;
                        isAlternate = !isAlternate;

                        table.Cell().Background(bgColor).Padding(4).Text(t.CreatedAt.ToString("dd MMM yyyy")).FontSize(8);
                        table.Cell().Background(bgColor).Padding(4).Text(AddSpacesToEnum(t.TransactionType.ToString()) + (string.IsNullOrEmpty(t.Description) ? "" : $" - {t.Description}")).FontSize(8);
                        table.Cell().Background(bgColor).Padding(4).AlignRight()
                            .Text($"{(t.Points >= 0 ? "+" : "")}{t.Points}").FontSize(8)
                            .FontColor(t.Points >= 0 ? Colors.Green.Darken2 : Colors.Red.Darken2);
                        table.Cell().Background(bgColor).Padding(4).AlignRight().Text($"{t.BalanceAfter}").FontSize(8);
                    }
                });

                if (transactions.Count > 15)
                    col.Item().PaddingTop(5).Text($"+{transactions.Count - 15} more transactions not shown").FontSize(8).FontColor(Colors.Grey.Medium).Italic();
            });
        }

        private void RedeemedRewardsSection(IContainer container, List<UserRedeemedReward> rewards)
        {
            container.Column(col =>
            {
                col.Item().Element(c => SectionHeader(c, $"Redeemed Rewards ({rewards.Count})"));

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(75);
                        columns.RelativeColumn(2);
                        columns.ConstantColumn(60);
                        columns.ConstantColumn(70);
                        columns.ConstantColumn(55);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Purple.Darken1).Padding(5).Text("Date").FontSize(9).Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Purple.Darken1).Padding(5).Text("Reward").FontSize(9).Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Purple.Darken1).Padding(5).AlignRight().Text("Points").FontSize(9).Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Purple.Darken1).Padding(5).Text("Expires").FontSize(9).Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Purple.Darken1).Padding(5).AlignCenter().Text("Status").FontSize(9).Bold().FontColor(Colors.White);
                    });

                    var isAlternate = false;
                    foreach (var r in rewards.Take(10))
                    {
                        var bgColor = isAlternate ? Colors.Purple.Lighten5 : Colors.White;
                        isAlternate = !isAlternate;

                        table.Cell().Background(bgColor).Padding(4).Text(r.RedeemedAt.ToString("dd MMM yyyy")).FontSize(8);
                        table.Cell().Background(bgColor).Padding(4).Text(r.RewardName).FontSize(8);
                        table.Cell().Background(bgColor).Padding(4).AlignRight().Text($"{r.PointsSpent}").FontSize(8);
                        table.Cell().Background(bgColor).Padding(4).Text(r.ExpiresAt.ToString("dd MMM yyyy")).FontSize(8);
                        table.Cell().Background(bgColor).Padding(4).AlignCenter()
                            .Text(r.Status.ToString()).FontSize(8)
                            .FontColor(r.Status == RewardStatus.Available ? Colors.Green.Darken2 :
                                       r.Status == RewardStatus.Used ? Colors.Blue.Darken2 : Colors.Red.Darken2);
                    }
                });
            });
        }

        private void CouponUsageSection(IContainer container, List<CouponUsage> usages)
        {
            container.Column(col =>
            {
                col.Item().Element(c => SectionHeader(c, $"Coupon Usage History ({usages.Count})"));

                var totalSaved = usages.Sum(u => u.DiscountAmount);
                col.Item().Text($"Total Savings: ৳{totalSaved:N2}").FontSize(10).Bold().FontColor(Colors.Green.Darken2);
                col.Item().PaddingTop(5);

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(75);
                        columns.ConstantColumn(80);
                        columns.RelativeColumn();
                        columns.ConstantColumn(70);
                        columns.ConstantColumn(55);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Teal.Darken1).Padding(5).Text("Date").FontSize(9).Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Teal.Darken1).Padding(5).Text("Coupon Code").FontSize(9).Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Teal.Darken1).Padding(5).Text("Description").FontSize(9).Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Teal.Darken1).Padding(5).AlignRight().Text("Discount").FontSize(9).Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Teal.Darken1).Padding(5).Text("Order #").FontSize(9).Bold().FontColor(Colors.White);
                    });

                    var isAlternate = false;
                    foreach (var u in usages.Take(10))
                    {
                        var bgColor = isAlternate ? Colors.Teal.Lighten5 : Colors.White;
                        isAlternate = !isAlternate;

                        table.Cell().Background(bgColor).Padding(4).Text(u.UsedAt.ToString("dd MMM yyyy")).FontSize(8);
                        table.Cell().Background(bgColor).Padding(4).Text(u.Coupon?.Code ?? "N/A").FontSize(8).Bold();
                        table.Cell().Background(bgColor).Padding(4).Text(u.Coupon?.Description ?? "-").FontSize(8);
                        table.Cell().Background(bgColor).Padding(4).AlignRight().Text($"৳{u.DiscountAmount:N0}").FontSize(8).FontColor(Colors.Green.Darken2);
                        table.Cell().Background(bgColor).Padding(4).Text($"#{u.OrderId}").FontSize(8);
                    }
                });
            });
        }

        private void TicketsSection(IContainer container, List<Ticket> tickets)
        {
            container.Column(col =>
            {
                col.Item().Element(c => SectionHeader(c, $"Support Tickets ({tickets.Count})"));

                var openCount = tickets.Count(t => t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress);
                var resolvedCount = tickets.Count(t => t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed);

                col.Item().Row(row =>
                {
                    row.AutoItem().Padding(5).Background(Colors.Yellow.Lighten4).Border(1).BorderColor(Colors.Yellow.Lighten2)
                        .Padding(5).Text($"Open: {openCount}").FontSize(9).Bold();
                    row.AutoItem().PaddingLeft(10).Padding(5).Background(Colors.Green.Lighten4).Border(1).BorderColor(Colors.Green.Lighten2)
                        .Padding(5).Text($"Resolved: {resolvedCount}").FontSize(9).Bold();
                });

                col.Item().PaddingTop(8);

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(65);
                        columns.ConstantColumn(70);
                        columns.RelativeColumn(2);
                        columns.ConstantColumn(60);
                        columns.ConstantColumn(55);
                        columns.ConstantColumn(55);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Indigo.Darken1).Padding(5).Text("Ticket #").FontSize(9).Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Indigo.Darken1).Padding(5).Text("Date").FontSize(9).Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Indigo.Darken1).Padding(5).Text("Subject").FontSize(9).Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Indigo.Darken1).Padding(5).AlignCenter().Text("Category").FontSize(9).Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Indigo.Darken1).Padding(5).AlignCenter().Text("Priority").FontSize(9).Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Indigo.Darken1).Padding(5).AlignCenter().Text("Status").FontSize(9).Bold().FontColor(Colors.White);
                    });

                    var isAlternate = false;
                    foreach (var t in tickets.Take(10))
                    {
                        var bgColor = isAlternate ? Colors.Indigo.Lighten5 : Colors.White;
                        isAlternate = !isAlternate;

                        table.Cell().Background(bgColor).Padding(4).Text(t.TicketNumber).FontSize(8);
                        table.Cell().Background(bgColor).Padding(4).Text(t.CreatedAt.ToString("dd MMM yyyy")).FontSize(8);
                        table.Cell().Background(bgColor).Padding(4).Text(t.Subject.Length > 30 ? t.Subject.Substring(0, 27) + "..." : t.Subject).FontSize(8);
                        table.Cell().Background(bgColor).Padding(4).AlignCenter().Text(AddSpacesToEnum(t.Category.ToString())).FontSize(7);
                        table.Cell().Background(bgColor).Padding(4).AlignCenter()
                            .Text(AddSpacesToEnum(t.Priority.ToString())).FontSize(8)
                            .FontColor(t.Priority == TicketPriority.Urgent ? Colors.Red.Darken2 :
                                       t.Priority == TicketPriority.High ? Colors.Orange.Darken2 : Colors.Grey.Darken1);
                        table.Cell().Background(bgColor).Padding(4).AlignCenter()
                            .Text(AddSpacesToEnum(t.Status.ToString())).FontSize(8)
                            .FontColor(GetTicketStatusColor(t.Status));
                    }
                });

                if (tickets.Count > 10)
                    col.Item().PaddingTop(5).Text($"+{tickets.Count - 10} more tickets not shown").FontSize(8).FontColor(Colors.Grey.Medium).Italic();
            });
        }

        private void SectionHeader(IContainer container, string title)
        {
            container.PaddingBottom(8).BorderBottom(2).BorderColor(Colors.Grey.Lighten2).Row(row =>
            {
                row.AutoItem().Text(title).FontSize(14).Bold().FontColor(Colors.Grey.Darken3);
            });
        }

        private void InfoField(IContainer container, string label, string value)
        {
            container.Column(col =>
            {
                col.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Darken1);
                col.Item().Text(value ?? "N/A").FontSize(10).FontColor(Colors.Grey.Darken3);
            });
        }

        private string GetStatusColor(string status)
        {
            return status?.ToLower() switch
            {
                "pending" => Colors.Orange.Darken1,
                "confirmed" => Colors.Blue.Darken1,
                "processing" => Colors.Blue.Darken1,
                "shipped" => Colors.Indigo.Darken1,
                "delivered" => Colors.Green.Darken1,
                "cancelled" => Colors.Red.Darken1,
                "returned" => Colors.Red.Lighten1,
                _ => Colors.Grey.Darken1
            };
        }

        private string GetTicketStatusColor(TicketStatus status)
        {
            return status switch
            {
                TicketStatus.Open => Colors.Yellow.Darken2,
                TicketStatus.InProgress => Colors.Blue.Darken1,
                TicketStatus.WaitingForCustomer => Colors.Orange.Darken1,
                TicketStatus.Resolved => Colors.Green.Darken1,
                TicketStatus.Closed => Colors.Grey.Darken1,
                _ => Colors.Grey.Darken1
            };
        }

        private string AddSpacesToEnum(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var result = new System.Text.StringBuilder();
            result.Append(text[0]);
            for (int i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]) && !char.IsUpper(text[i - 1]))
                    result.Append(' ');
                result.Append(text[i]);
            }
            return result.ToString();
        }
    }
}
