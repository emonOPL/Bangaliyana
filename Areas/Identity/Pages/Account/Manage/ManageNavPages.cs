// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bangaliyana.Areas.Identity.Pages.Account.Manage
{
    public static class ManageNavPages
    {
        // Dashboard tabs
        public static string Index => "Index";
        public static string Orders => "Orders";
        public static string Wishlist => "Wishlist";
        public static string SavedAddresses => "SavedAddresses";
        public static string WalletRewards => "WalletRewards";
        public static string Referrals => "Referrals";
        public static string PaymentMethods => "PaymentMethods";
        public static string SupportTickets => "SupportTickets";
        public static string OrderReturns => "OrderReturns";
        public static string Notifications => "Notifications";

        // Seller tabs
        public static string Shop => "Shop";
        public static string PaymentAccounts => "PaymentAccounts";
        public static string PaymentStatement => "PaymentStatement";

        // Account settings tabs
        public static string Email => "Email";
        public static string ChangePassword => "ChangePassword";
        public static string BiometricLogin => "BiometricLogin";
        public static string ExternalLogins => "ExternalLogins";
        public static string PersonalData => "PersonalData";
        public static string DownloadPersonalData => "DownloadPersonalData";
        public static string DeletePersonalData => "DeletePersonalData";
        public static string PlatformReview => "PlatformReview";

        // Navigation class helpers - Dashboard
        public static string IndexNavClass(ViewContext viewContext) => PageNavClass(viewContext, Index);
        public static string OrdersNavClass(ViewContext viewContext) => PageNavClass(viewContext, Orders);
        public static string WishlistNavClass(ViewContext viewContext) => PageNavClass(viewContext, Wishlist);
        public static string SavedAddressesNavClass(ViewContext viewContext) => PageNavClass(viewContext, SavedAddresses);
        public static string WalletRewardsNavClass(ViewContext viewContext) => PageNavClass(viewContext, WalletRewards);
        public static string ReferralsNavClass(ViewContext viewContext) => PageNavClass(viewContext, Referrals);
        public static string PaymentMethodsNavClass(ViewContext viewContext) => PageNavClass(viewContext, PaymentMethods);
        public static string SupportTicketsNavClass(ViewContext viewContext) => PageNavClass(viewContext, SupportTickets);
        public static string OrderReturnsNavClass(ViewContext viewContext) => PageNavClass(viewContext, OrderReturns);
        public static string NotificationsNavClass(ViewContext viewContext) => PageNavClass(viewContext, Notifications);

        // Navigation class helpers - Seller
        public static string ShopNavClass(ViewContext viewContext) => PageNavClass(viewContext, Shop);
        public static string PaymentAccountsNavClass(ViewContext viewContext) => PageNavClass(viewContext, PaymentAccounts);
        public static string PaymentStatementNavClass(ViewContext viewContext) => PageNavClass(viewContext, PaymentStatement);

        // Navigation class helpers - Account settings
        public static string EmailNavClass(ViewContext viewContext) => PageNavClass(viewContext, Email);
        public static string ChangePasswordNavClass(ViewContext viewContext) => PageNavClass(viewContext, ChangePassword);
        public static string BiometricLoginNavClass(ViewContext viewContext) => PageNavClass(viewContext, BiometricLogin);
        public static string ExternalLoginsNavClass(ViewContext viewContext) => PageNavClass(viewContext, ExternalLogins);
        public static string PersonalDataNavClass(ViewContext viewContext) => PageNavClass(viewContext, PersonalData);
        public static string DownloadPersonalDataNavClass(ViewContext viewContext) => PageNavClass(viewContext, DownloadPersonalData);
        public static string DeletePersonalDataNavClass(ViewContext viewContext) => PageNavClass(viewContext, DeletePersonalData);
        public static string PlatformReviewNavClass(ViewContext viewContext) => PageNavClass(viewContext, PlatformReview);

        public static string PageNavClass(ViewContext viewContext, string page)
        {
            var activePage = viewContext.ViewData["ActivePage"] as string
                ?? System.IO.Path.GetFileNameWithoutExtension(viewContext.ActionDescriptor.DisplayName);
            return string.Equals(activePage, page, StringComparison.OrdinalIgnoreCase) ? "active" : null;
        }
    }
}
