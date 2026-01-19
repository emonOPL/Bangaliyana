# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Bangaliyana** is an ASP.NET Core 8 e-commerce web application built with Entity Framework Core, featuring user authentication, product management, shopping cart functionality, and order processing. The application includes comprehensive support for Bangladesh's administrative divisions (Division/District/Upazila/Union) for address management.

Key features include:
- Multi-vendor marketplace with seller management
- Real-time notifications via SignalR
- Biometric authentication with WebAuthn/Fido2
- Background job processing with Hangfire
- Advanced search with history tracking
- Reward points and premium membership system
- Dynamic CMS and menu management
- File upload validation and PDF generation

## Development Commands

### Build and Run
```bash
dotnet build              # Build the project
dotnet run                # Run in development mode
dotnet clean              # Clean build artifacts
```

### Database Management
```bash
dotnet ef migrations add [MigrationName]  # Add new migration
dotnet ef database update                  # Update database
dotnet ef migrations script                # Generate SQL script
dotnet ef database drop                    # Drop database (dev only)
```

## Architecture Overview

### Project Structure
- **Areas/**: Feature-based organization
  - `Admin/`: Administrative functionality (products, categories, orders, users, roles, site settings, menus, promotional campaigns)
  - `Customer/`: Customer-facing features (shopping, orders, wishlist, payments, seller messaging)
  - `Seller/`: Seller dashboard and management
  - `Moderator/`: Content moderation tools
  - `Identity/`: ASP.NET Identity UI customizations
- **Data/**: Entity Framework DbContext and migrations
- **Models/**: Domain entities and view models (ApplicationUser, Products, Orders, etc.)
- **Services/**: Business logic layer (30+ services for different features)
- **Utilities/**: Helper classes (SessionExtensions)
- **Extensions/**: Utility extensions (EnumExtensions, DateTimeExtensions)
- **Hubs/**: SignalR hubs for real-time communication
- **Middleware/**: Custom middleware for security headers
- **Filters/**: Custom action filters

### Key Architectural Patterns

#### Area-Based Organization
- Customer area: public-facing e-commerce (HomeController, PaymentController, WishlistController)
- Admin area: backend operations (ProductController, OrdersController, UserManagementController, SiteSettingsController, MenuManagementController)
- Identity area: authentication/authorization UI

#### Hybrid Cart System
Cart implementation supports both authenticated and guest users:
- **Guest users**: Cart stored in session using `SessionExtensions`
- **Authenticated users**: Cart persisted in database via `PersistentCart` entity
- **Cart merging**: Session cart automatically merges with database when user logs in

#### Bangladesh Administrative Structure
Complete hierarchical address system:
- `Division` → `District` → `Upazila` → `Union`
- Foreign key relationships with `DeleteBehavior.Restrict`
- ApplicationUser model includes all levels for precise addressing

#### Identity and Authorization
- Custom `ApplicationUser` extends IdentityUser with Bangladesh-specific fields, reward points, premium membership
- **Roles**: SuperAdmin, Admin, Moderator, Seller, User
- **Authorization Policies**: RequireSuperAdminRole, RequireAdminRole, RequireModeratorRole, RequireSellerRole, RequireUserRole
- **WebAuthn Support**: Biometric authentication via Fido2
- **Advanced Features**: COD restrictions, phone verification, wallet balance
- Cookie configuration optimized for development/production environments
- Security headers middleware (X-Content-Type-Options, X-Frame-Options, X-XSS-Protection)

#### Dynamic CMS System
Site content managed through `ISiteSettingsService`:
- **SiteSettings**: Global site configuration
- **SocialLinks**: Social media links in footer
- **Features**: Feature highlights on login/home pages
- **Banners**: Hero and promotional banners
- **PageContent**: Dynamic pages (about, terms, contact)
- **MenuItems**: Role-based navigation menus via `IMenuService`

### Service Layer
| Service | Purpose |
|---------|---------|
| `ICartService` | Unified cart operations (session/database) |
| `IEmailService` | Email notifications |
| `ISiteSettingsService` | CMS and site configuration |
| `ISearchService` | Product search with history |
| `IOtpService` | OTP verification |
| `IMenuService` | Dynamic menu management |
| `IMenuPermissionService` | Role-based menu access |
| `PdfGeneratorService` | Order receipts (QuestPDF) |
| `IRewardService` | Reward points and premium membership |
| `INotificationService` | User notifications management |
| `ISellerPaymentService` | Seller payment processing |
| `IFileValidationService` | File upload validation |
| `IPriceAlertService` | Price change notifications |
| `IPromotionalCampaignService` | Marketing campaigns |
| `IShopRatingService` | Shop rating system |
| `IShopFollowService` | Shop following functionality |
| `IRealTimeNotificationService` | SignalR notifications |

### Database Seeding
Three seed classes run at startup (Program.cs):
1. `SeedData.InitializeAsync()` - Roles and admin user
2. `SeedDynamicContent.InitializeAsync()` - Site settings, social links, features, banners, pages
3. `SeedProductData.InitializeAsync()` - Product types, special tags, categories, products, testimonials

### Key NuGet Packages
- **QuestPDF**: PDF generation for order receipts
- **X.PagedList.Mvc.Core**: Pagination support
- **Hangfire**: Background job processing for payments and notifications
- **Fido2.AspNet**: WebAuthn/biometric authentication
- **Microsoft.AspNetCore.SignalR**: Real-time communication
- **AlertifyJS**: Client-side notifications

## Development Notes

### Database Connection
SQL Server Express: `Server=OPL\\SQLEXPRESS;Database=Bangaliyana`

### Admin Account
Default: `admin@bangaliyana.com` / `Admin@123` (created on first run)

### Cart Behavior
Always use `CartService` for cart operations - it handles guest/authenticated user switching automatically.

### Payment Integration
SSLCommerz payment gateway for online payments. Manual payments also supported (bKash, Nagad, Upay, COD).

### Image Uploads
Product images stored in `wwwroot/images/products/` with GUID-based filenames. Default fallback: `noimage.jpg`.

### Background Jobs
Hangfire dashboard available at `/hangfire` for monitoring background jobs including:
- Seller payment processing
- Festival promotions
- Price alerts
- Notification cleanup

### Real-time Features
SignalR hubs provide real-time functionality:
- Instant notifications
- Live chat with sellers
- Order status updates
- Price change alerts

### Security Features
- CSRF protection with custom cookie configuration
- File upload validation for security
- Rate limiting on sensitive operations
- Biometric authentication support

## Key File Locations

| Purpose | Location |
|---------|----------|
| DbContext | `Data/ApplicationDbContext.cs` |
| Custom User Model | `Models/ApplicationUser.cs` |
| Cart Logic | `Services/CartService.cs` |
| Session Helpers | `Utilities/SessionExtensions.cs` |
| Site Settings Service | `Services/SiteSettingsService.cs` |
| Customer Controller | `Areas/Customer/Controllers/HomeController.cs` |
| Admin Controllers | `Areas/Admin/Controllers/` |
| Identity Pages | `Areas/Identity/Pages/Account/` |

## Developer Preferences (MUST FOLLOW)

These are mandatory instructions that must be followed for every prompt:

1. **Deep Analysis Required**: যেকোনো prompt বা screenshot পেলে deeply analyze করতে হবে। Surface-level দেখে কাজ করা যাবে না।

2. **Related Files Analysis**: Prompt এ mentioned file/issue এর সাথে related এবং connected সব files analyze করতে হবে। যেমন: Edit page fix করতে হলে Index, Details, Create pages এও কিভাবে same data handle হয়েছে দেখতে হবে।

3. **Follow Existing Patterns**: কোনো logic বা UI implement/fix করার আগে project এ existing similar logic/UI খুঁজে বের করে সেটা follow করতে হবে। নতুন pattern introduce করা যাবে না যদি existing pattern থাকে।

4. **No Unauthorized Changes**: Prompt এ explicitly mention নেই এমন কোনো logic বা UI modify করা যাবে না। শুধুমাত্র যা বলা হয়েছে সেটাই করতে হবে।

5. **Auto Rebuild (.NET)**: প্রতিটা prompt এর modification সম্পূর্ণ হলে project rebuild এবং restart করতে হবে।