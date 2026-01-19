using Bangaliyana.Data;
using Bangaliyana.Filters;
using Bangaliyana.Hubs;
using Bangaliyana.Middleware;
using Bangaliyana.Models;
using Bangaliyana.Services;
using Fido2NetLib;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add environment variables for production secrets
// In production, set these environment variables:
// - ConnectionStrings__DefaultConnection
// - Smtp__Host, Smtp__Port, Smtp__User, Smtp__Pass
// - SuperAdminSettings__DefaultAdminEmail, SuperAdminSettings__DefaultAdminPassword
// - SuperAdminSettings__SuperAdminEmails__0, SuperAdminSettings__SuperAdminEmails__1, etc.
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configure Identity with proper cookie settings
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    // Sign-in settings
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
    
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;
    
    // Lockout settings (configurable via appsettings.json)
    var lockoutMinutes = builder.Configuration.GetValue<int>("IdentitySettings:LockoutTimeSpanMinutes", 5);
    var maxFailedAttempts = builder.Configuration.GetValue<int>("IdentitySettings:MaxFailedAccessAttempts", 5);
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(lockoutMinutes);
    options.Lockout.MaxFailedAccessAttempts = maxFailedAttempts;
    options.Lockout.AllowedForNewUsers = true;
    
    // User settings
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
})
.AddRoles<IdentityRole>()  // Enable roles support
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

// Configure cookie authentication
builder.Services.ConfigureApplicationCookie(options =>
{
    // Cookie settings
    options.Cookie.HttpOnly = true;
    // Use SameAsRequest in Development, Always in Production
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
        ? CookieSecurePolicy.SameAsRequest 
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Name = "Bangaliyana.Auth";
    
    // Timeout settings (configurable via appsettings.json)
    var cookieExpirationDays = builder.Configuration.GetValue<int>("AppSettings:CookieExpirationDays", 7);
    options.ExpireTimeSpan = TimeSpan.FromDays(cookieExpirationDays);
    options.SlidingExpiration = true;
    
    // Paths
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.ReturnUrlParameter = "ReturnUrl";
});

// Configure session
builder.Services.AddDistributedMemoryCache();
var sessionTimeoutMinutes = builder.Configuration.GetValue<int>("AppSettings:SessionTimeoutMinutes", 30);
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(sessionTimeoutMinutes);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "Bangaliyana.Session";
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
        ? CookieSecurePolicy.SameAsRequest 
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Configure data protection
builder.Services.AddDataProtection();

// Add security headers
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
        ? CookieSecurePolicy.SameAsRequest 
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax; // Lax required for cross-network/mobile access
    options.Cookie.Name = "Bangaliyana.Antiforgery";
});

// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("SuperAdmin", "Admin"));
    options.AddPolicy("RequireModeratorRole", policy => policy.RequireRole("SuperAdmin", "Admin", "Moderator"));
    options.AddPolicy("RequireUserRole", policy => policy.RequireRole("SuperAdmin", "User", "Admin"));
    options.AddPolicy("RequireSellerRole", policy => policy.RequireRole("SuperAdmin", "Seller", "Admin"));
    options.AddPolicy("RequireSuperAdminRole", policy => policy.RequireRole("SuperAdmin"));

    // Department-specific policies
    options.AddPolicy("RequireProductManagerRole", policy => policy.RequireRole("SuperAdmin", "Admin", "ProductManager"));
    options.AddPolicy("RequireOrderManagerRole", policy => policy.RequireRole("SuperAdmin", "Admin", "OrderManager"));
    options.AddPolicy("RequireSupportTeamRole", policy => policy.RequireRole("SuperAdmin", "Admin", "SupportTeam"));
    options.AddPolicy("RequireContentManagerRole", policy => policy.RequireRole("SuperAdmin", "Admin", "ContentManager"));
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ISiteSettingsService, SiteSettingsService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<IMenuPermissionService, MenuPermissionService>();
builder.Services.AddScoped<IRewardService, RewardService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ISellerMessagingService, SellerMessagingService>();
builder.Services.AddScoped<IFileValidationService, FileValidationService>();
builder.Services.AddScoped<IPriceAlertService, PriceAlertService>();
builder.Services.AddScoped<IPromotionalCampaignService, PromotionalCampaignService>();
builder.Services.AddScoped<IShopRatingService, ShopRatingService>();
builder.Services.AddScoped<IShopFollowService, ShopFollowService>();
builder.Services.AddScoped<ISellerPaymentService, SellerPaymentService>();
builder.Services.AddScoped<ISellerMonthlyReportService, SellerMonthlyReportService>();
builder.Services.AddScoped<IRealTimeNotificationService, RealTimeNotificationService>();
builder.Services.AddScoped<ISupportChatService, SupportChatService>();
builder.Services.AddScoped<IAIChatService, AIChatService>();
builder.Services.AddScoped<IBlogService, BlogService>();
builder.Services.AddScoped<IStylingGuideService, StylingGuideService>();
builder.Services.AddScoped<ISeasonalCollectionService, SeasonalCollectionService>();
builder.Services.AddScoped<ICompareService, CompareService>();
builder.Services.AddScoped<IProductBulkImportService, ProductBulkImportService>();
builder.Services.AddScoped<IStockAlertService, StockAlertService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ISellerSubscriptionService, SellerSubscriptionService>();
builder.Services.AddScoped<IImageOptimizationService, ImageOptimizationService>();
builder.Services.AddScoped<PdfGeneratorService>();
builder.Services.AddTransient<SellerPaymentBackgroundJobs>();
builder.Services.AddTransient<StockAlertBackgroundJobs>();

// Add FestivalPromoBackgroundService only if enabled (can be disabled for free hosting)
if (builder.Configuration.GetValue<bool>("BackgroundServices:Enabled", true))
{
    builder.Services.AddHostedService<FestivalPromoBackgroundService>();
}

// Add SignalR for real-time notifications
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// Configure Fido2 (WebAuthn) for biometric authentication
builder.Services.AddFido2(options =>
{
    options.ServerDomain = builder.Configuration["Fido2:ServerDomain"] ?? "localhost";
    options.ServerName = builder.Configuration["Fido2:ServerName"] ?? "Bangaliyana";
    options.Origins = builder.Configuration.GetSection("Fido2:Origins").Get<HashSet<string>>()
        ?? new HashSet<string> { "https://localhost:7195", "http://localhost:5106" };
    options.TimestampDriftTolerance = 300000; // 5 minutes
});

// Configure Hangfire (can be disabled for free hosting via appsettings)
var hangfireEnabled = builder.Configuration.GetValue<bool>("Hangfire:Enabled", true);
if (hangfireEnabled)
{
    builder.Services.AddHangfire(configuration => configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Zero,
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true
        }));
    builder.Services.AddHangfireServer();
}

builder.Services.AddLogging();

// Add localization services for multi-language support (EN + BN)
// ResourcesPath="" means ASP.NET looks for {RootNamespace}.{TypeName} = Bangaliyana.SharedResources
// This matches the default MSBuild resource naming convention
builder.Services.AddLocalization(options => options.ResourcesPath = "");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { "en", "bn" };
    options.SetDefaultCulture("en")
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
    options.RequestCultureProviders.Insert(0, new Microsoft.AspNetCore.Localization.CookieRequestCultureProvider
    {
        CookieName = "Bangaliyana.Culture"
    });
});

// Add rate limiting for sensitive endpoints (configurable via appsettings.json)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Rate limit for login attempts
    var loginLimit = builder.Configuration.GetValue<int>("RateLimiting:Login:PermitLimit", 5);
    var loginWindow = builder.Configuration.GetValue<int>("RateLimiting:Login:WindowMinutes", 1);
    options.AddFixedWindowLimiter("login", limiterOptions =>
    {
        limiterOptions.PermitLimit = loginLimit;
        limiterOptions.Window = TimeSpan.FromMinutes(loginWindow);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });

    // Rate limit for password reset
    var pwResetLimit = builder.Configuration.GetValue<int>("RateLimiting:PasswordReset:PermitLimit", 3);
    var pwResetWindow = builder.Configuration.GetValue<int>("RateLimiting:PasswordReset:WindowMinutes", 10);
    options.AddFixedWindowLimiter("password-reset", limiterOptions =>
    {
        limiterOptions.PermitLimit = pwResetLimit;
        limiterOptions.Window = TimeSpan.FromMinutes(pwResetWindow);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });

    // Rate limit for OTP verification
    var otpLimit = builder.Configuration.GetValue<int>("RateLimiting:OtpVerify:PermitLimit", 5);
    var otpWindow = builder.Configuration.GetValue<int>("RateLimiting:OtpVerify:WindowMinutes", 5);
    options.AddFixedWindowLimiter("otp-verify", limiterOptions =>
    {
        limiterOptions.PermitLimit = otpLimit;
        limiterOptions.Window = TimeSpan.FromMinutes(otpWindow);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });

    // Rate limit for payment endpoints
    var paymentLimit = builder.Configuration.GetValue<int>("RateLimiting:Payment:PermitLimit", 10);
    var paymentWindow = builder.Configuration.GetValue<int>("RateLimiting:Payment:WindowMinutes", 1);
    options.AddFixedWindowLimiter("payment", limiterOptions =>
    {
        limiterOptions.PermitLimit = paymentLimit;
        limiterOptions.Window = TimeSpan.FromMinutes(paymentWindow);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });

    // General API rate limit
    var apiLimit = builder.Configuration.GetValue<int>("RateLimiting:Api:PermitLimit", 100);
    var apiWindow = builder.Configuration.GetValue<int>("RateLimiting:Api:WindowMinutes", 1);
    options.AddFixedWindowLimiter("api", limiterOptions =>
    {
        limiterOptions.PermitLimit = apiLimit;
        limiterOptions.Window = TimeSpan.FromMinutes(apiWindow);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 2;
    });

    // Global rate limit fallback - partition by IP
    var globalLimit = builder.Configuration.GetValue<int>("RateLimiting:Global:PermitLimit", 200);
    var globalWindow = builder.Configuration.GetValue<int>("RateLimiting:Global:WindowMinutes", 1);
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                PermitLimit = globalLimit,
                Window = TimeSpan.FromMinutes(globalWindow),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

builder.Services.AddControllersWithViews()
    .AddViewLocalization(Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization();
builder.Services.AddRazorPages();

var app = builder.Build();

// Database migration and seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        // Auto-migrate database on startup (creates tables if not exist)
        logger.LogInformation("Applying database migrations...");
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully.");

        // Seed roles, admin user, and dynamic content
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var siteSettingsService = services.GetRequiredService<ISiteSettingsService>();
        var menuService = services.GetRequiredService<IMenuService>();
        var configuration = services.GetRequiredService<IConfiguration>();
        await SeedData.InitializeAsync(userManager, roleManager, configuration);
        await SeedDynamicContent.InitializeAsync(siteSettingsService);
        await SeedProductData.InitializeAsync(dbContext);
        await SeedBusinessTypes.InitializeAsync(dbContext);
        await SeedBlogContent.InitializeAsync(dbContext);
        await SeedAdminRoles.InitializeAsync(dbContext);
        await menuService.SeedDefaultMenuItemsAsync();
        var notificationService = services.GetRequiredService<INotificationService>();
        await notificationService.SeedFestivalDatesAsync();
        logger.LogInformation("Database seeding completed successfully.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating/seeding the database.");
    }
}

// Configure the HTTP request pipeline.
var showDetailedErrors = app.Environment.IsDevelopment() ||
    Environment.GetEnvironmentVariable("ASPNETCORE_DETAILEDERRORS") == "true";

if (showDetailedErrors)
{
    app.UseDeveloperExceptionPage();
    if (app.Environment.IsDevelopment())
    {
        app.UseMigrationsEndPoint();
    }
}
else
{
    app.UseExceptionHandler("/Error/500");
    app.UseHsts();
}

// Handle status code errors (404, 403, etc.) with custom error page
app.UseStatusCodePagesWithReExecute("/Error/{0}");

// Security headers middleware
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Use request localization for multi-language support
app.UseRequestLocalization();

// CORRECT ORDER: Authentication before Authorization
app.UseAuthentication();
app.UseAuthorization();

// Rate limiting middleware - must be after authentication/authorization
app.UseRateLimiter();

// Maintenance mode middleware - shows maintenance page when enabled (admins can bypass)
app.UseMaintenance();

// Map SignalR hubs for real-time features
app.MapHub<NotificationHub>("/notificationHub");
app.MapHub<SupportChatHub>("/supportChatHub");

// Hangfire Dashboard and recurring jobs (only if enabled)
if (app.Configuration.GetValue<bool>("Hangfire:Enabled", true))
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireAuthorizationFilter() },
        DashboardTitle = "Bangaliyana Background Jobs"
    });

    // Configure recurring jobs with error handling
    try
    {
        // Get Bangladesh timezone (works on both Windows and Linux)
        TimeZoneInfo bdTimeZone;
        try
        {
            bdTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Bangladesh Standard Time"); // Windows
        }
        catch
        {
            try
            {
                bdTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Dhaka"); // Linux
            }
            catch
            {
                bdTimeZone = TimeZoneInfo.CreateCustomTimeZone("BD", TimeSpan.FromHours(6), "Bangladesh", "Bangladesh Standard Time");
            }
        }

        // Configure recurring jobs - Monthly seller payment processing
        RecurringJob.AddOrUpdate<SellerPaymentBackgroundJobs>(
            "monthly-seller-payments",
            x => x.ProcessMonthlyPaymentsAsync(null),
            Cron.Monthly(1, 0, 0),
            new RecurringJobOptions { TimeZone = bdTimeZone });

        // Monthly report finalization
        RecurringJob.AddOrUpdate<SellerPaymentBackgroundJobs>(
            "monthly-seller-report-processing",
            x => x.ProcessMonthEndReportsAsync(null),
            "1 0 1 * *",
            new RecurringJobOptions { TimeZone = bdTimeZone });

        // Daily low stock alerts
        RecurringJob.AddOrUpdate<StockAlertBackgroundJobs>(
            "daily-low-stock-alerts",
            x => x.ProcessDailyLowStockAlertsAsync(),
            "0 9 * * *",
            new RecurringJobOptions { TimeZone = bdTimeZone });
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Failed to configure Hangfire recurring jobs.");
    }
}

app.UseSession();

// SuperAdmin role middleware - ensures SuperAdmin users always have all roles
app.UseSuperAdminRoleMiddleware();

// Daily login reward middleware - automatically awards points on first daily visit
app.UseDailyReward();

app.MapRazorPages();

// Custom routes for static pages (SEO-friendly URLs)
app.MapControllerRoute(
    name: "return-policy",
    pattern: "return-policy",
    defaults: new { area = "Customer", controller = "Home", action = "Page", slug = "return-policy" });
app.MapControllerRoute(
    name: "privacy-policy",
    pattern: "privacy-policy",
    defaults: new { area = "Customer", controller = "Home", action = "Page", slug = "privacy-policy" });
app.MapControllerRoute(
    name: "terms",
    pattern: "terms",
    defaults: new { area = "Customer", controller = "Home", action = "Page", slug = "terms" });
app.MapControllerRoute(
    name: "about",
    pattern: "about",
    defaults: new { area = "Customer", controller = "Home", action = "Page", slug = "about" });
app.MapControllerRoute(
    name: "contact",
    pattern: "contact",
    defaults: new { area = "Customer", controller = "Home", action = "Page", slug = "contact" });

app.MapControllerRoute(
    name: "areas",
    pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Seed data class for initial roles and admin user
public static class SeedData
{
    // Fallback SuperAdmin emails if not configured
    private static readonly string[] DefaultSuperAdminEmails = new[]
    {
        "admin@bangaliyana.com"
    };

    public static async Task InitializeAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
    {
        // Create roles - SuperAdmin has ALL permissions across the entire system
        // Department roles: ProductManager, OrderManager, SupportTeam, ContentManager
        string[] roleNames = { "SuperAdmin", "Admin", "Moderator", "Seller", "User", "ProductManager", "OrderManager", "SupportTeam", "ContentManager" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // Get admin credentials from configuration (with fallback defaults)
        var adminEmail = configuration["SuperAdminSettings:DefaultAdminEmail"] ?? "admin@bangaliyana.com";
        var adminPassword = configuration["SuperAdminSettings:DefaultAdminPassword"] ?? "Admin@123";

        // Create default SuperAdmin user
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "Super",
                LastName = "Admin",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await userManager.CreateAsync(adminUser, adminPassword);
        }

        // Ensure admin user has all SuperAdmin roles
        await EnsureSuperAdminRoles(userManager, adminUser);

        // Get SuperAdmin emails from configuration
        var superAdminEmails = configuration.GetSection("SuperAdminSettings:SuperAdminEmails").Get<string[]>() ?? DefaultSuperAdminEmails;

        // Promote any existing users with SuperAdmin emails to SuperAdmin role
        foreach (var email in superAdminEmails)
        {
            if (email.Equals(adminEmail, StringComparison.OrdinalIgnoreCase))
                continue; // Already handled above

            var existingUser = await userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                await EnsureSuperAdminRoles(userManager, existingUser);
            }
        }
    }

    /// <summary>
    /// Ensures a user has all SuperAdmin roles (SuperAdmin, Admin, Moderator, Seller, User)
    /// </summary>
    private static async Task EnsureSuperAdminRoles(UserManager<ApplicationUser> userManager, ApplicationUser user)
    {
        var allRoles = new[] { "SuperAdmin", "Admin", "Moderator", "Seller", "User" };
        foreach (var role in allRoles)
        {
            if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}

// Seed dynamic content
public static class SeedDynamicContent
{
    public static async Task InitializeAsync(ISiteSettingsService siteSettingsService)
    {
        // Get or create site settings (this will create defaults if none exist)
        var settings = await siteSettingsService.GetSiteSettingsAsync();

        // Seed social links if none exist
        var socialLinks = await siteSettingsService.GetAllSocialLinksAsync();
        if (!socialLinks.Any())
        {
            await siteSettingsService.CreateSocialLinkAsync(new SocialLink
            {
                PlatformName = "Facebook",
                Url = "https://facebook.com/bangaliyana",
                IconClass = "fab fa-facebook-f",
                IconColor = "#1877f2",
                DisplayOrder = 1,
                IsActive = true
            });

            await siteSettingsService.CreateSocialLinkAsync(new SocialLink
            {
                PlatformName = "Instagram",
                Url = "https://instagram.com/bangaliyana",
                IconClass = "fab fa-instagram",
                IconColor = "#e4405f",
                DisplayOrder = 2,
                IsActive = true
            });

            await siteSettingsService.CreateSocialLinkAsync(new SocialLink
            {
                PlatformName = "Twitter",
                Url = "https://twitter.com/bangaliyana",
                IconClass = "fab fa-twitter",
                IconColor = "#1da1f2",
                DisplayOrder = 3,
                IsActive = true
            });
        }

        // Seed features if none exist
        var features = await siteSettingsService.GetAllFeaturesAsync();
        if (!features.Any())
        {
            await siteSettingsService.CreateFeatureAsync(new Feature
            {
                Title = "Secure Shopping",
                Description = "Your data is protected with industry-leading security measures.",
                IconClass = "fas fa-shield-alt",
                IconColor = "#10b981",
                Section = FeatureSection.LoginPage,
                DisplayOrder = 1,
                IsActive = true
            });

            await siteSettingsService.CreateFeatureAsync(new Feature
            {
                Title = "Fast Delivery",
                Description = "Quick and reliable delivery across Bangladesh.",
                IconClass = "fas fa-truck",
                IconColor = "#6366f1",
                Section = FeatureSection.LoginPage,
                DisplayOrder = 2,
                IsActive = true
            });

            await siteSettingsService.CreateFeatureAsync(new Feature
            {
                Title = "24/7 Support",
                Description = "Our support team is always here to help you.",
                IconClass = "fas fa-headset",
                IconColor = "#f59e0b",
                Section = FeatureSection.LoginPage,
                DisplayOrder = 3,
                IsActive = true
            });
        }

        // Seed banner if none exist
        var banners = await siteSettingsService.GetAllBannersAsync();
        if (!banners.Any())
        {
            await siteSettingsService.CreateBannerAsync(new Banner
            {
                Title = "Tradition Meets Trend",
                Subtitle = "Your Home for Authentic Bangladeshi Fashion",
                Description = "Quality guaranteed, satisfaction delivered.",
                ButtonText = "Shop Now",
                LinkUrl = "/Customer/Home/Index",
                Location = BannerLocation.Hero,
                DisplayOrder = 1,
                IsActive = true
            });
        }

        // Seed pages if none exist OR seed missing default pages
        var pages = await siteSettingsService.GetAllPageContentsAsync();
        var existingSlugs = pages.Select(p => p.Slug.ToLower()).ToHashSet();

        // Seed About page if not exists
        if (!existingSlugs.Contains("about"))
        {
            await siteSettingsService.CreatePageContentAsync(new PageContent
            {
                Slug = "about",
                Title = "About Us",
                Content = @"<h2>Welcome to Bangaliyana</h2>
<p>Bangaliyana is your premier destination for authentic Bangladeshi fashion and products. We are dedicated to bringing you the finest quality traditional and modern Bengali attire.</p>
<h3>Our Mission</h3>
<p>To celebrate and preserve Bangladeshi culture through fashion while making it accessible to everyone around the world.</p>
<h3>Why Choose Us?</h3>
<ul>
<li>Authentic Bangladeshi products</li>
<li>Quality guaranteed</li>
<li>Fast and reliable delivery</li>
<li>Excellent customer support</li>
</ul>",
                MetaDescription = "Learn about Bangaliyana - your destination for authentic Bangladeshi fashion.",
                ShowInFooter = true,
                DisplayOrder = 1,
                IsActive = true
            });
        }

        // Seed Terms page if not exists
        if (!existingSlugs.Contains("terms"))
        {
            await siteSettingsService.CreatePageContentAsync(new PageContent
            {
                Slug = "terms",
                Title = "Terms & Conditions",
                Content = @"<h2>Terms of Service</h2>
<p>By using our website, you agree to the following terms and conditions.</p>
<h3>1. Use of Website</h3>
<p>You may use our website for lawful purposes only. You must not use our website to engage in any illegal or harmful activities.</p>
<h3>2. Orders and Payment</h3>
<p>All orders are subject to availability. We reserve the right to refuse any order.</p>
<h3>3. Returns and Refunds</h3>
<p>Please refer to our return policy for information about returns and refunds.</p>",
                MetaDescription = "Read our terms and conditions.",
                ShowInFooter = true,
                DisplayOrder = 2,
                IsActive = true
            });
        }

        // Seed Contact page if not exists
        if (!existingSlugs.Contains("contact"))
        {
            await siteSettingsService.CreatePageContentAsync(new PageContent
            {
                Slug = "contact",
                Title = "Contact Us",
                Content = @"<h2>Get in Touch</h2>
<p>We'd love to hear from you! Here's how you can reach us:</p>
<h3>Customer Support</h3>
<p>Email: support@bangaliyana.com<br>Phone: +880 1XXX-XXXXXX</p>
<h3>Business Hours</h3>
<p>Saturday - Thursday: 9:00 AM - 6:00 PM (BST)<br>Friday: Closed</p>",
                MetaDescription = "Contact Bangaliyana for any questions or support.",
                ShowInFooter = true,
                DisplayOrder = 3,
                IsActive = true
            });
        }

        // Seed Privacy Policy page if not exists
        if (!existingSlugs.Contains("privacy-policy"))
        {
            await siteSettingsService.CreatePageContentAsync(new PageContent
            {
                Slug = "privacy-policy",
                Title = "Privacy Policy",
                Content = @"<h2>Privacy Policy</h2>
<p>Your privacy is important to us. This Privacy Policy explains how we collect, use, disclose, and safeguard your information when you visit our website.</p>
<h3>Information We Collect</h3>
<p>We collect information you provide directly to us, such as when you create an account, make a purchase, or contact us for support.</p>
<h3>How We Use Your Information</h3>
<p>We use the information we collect to provide, maintain, and improve our services, process transactions, and communicate with you.</p>
<h3>Information Sharing</h3>
<p>We do not sell, trade, or otherwise transfer your personally identifiable information to outside parties except as described in this policy.</p>
<h3>Data Security</h3>
<p>We implement appropriate security measures to protect your personal information against unauthorized access, alteration, disclosure, or destruction.</p>
<h3>Contact Us</h3>
<p>If you have any questions about this Privacy Policy, please contact us.</p>",
                MetaDescription = "Read our privacy policy to understand how we collect, use, and protect your data.",
                ShowInFooter = true,
                DisplayOrder = 4,
                IsActive = true
            });
        }

        // Seed Return Policy page if not exists
        if (!existingSlugs.Contains("return-policy"))
        {
            await siteSettingsService.CreatePageContentAsync(new PageContent
            {
                Slug = "return-policy",
                Title = "রিটার্ন ও রিফান্ড পলিসি",
                Content = @"<div class='return-policy-content'>
<h2>রিটার্ন ও রিফান্ড পলিসি</h2>
<p>Bangaliyana তে আমরা গ্রাহক সন্তুষ্টিকে সর্বোচ্চ প্রাধান্য দিই। আপনার কেনাকাটার অভিজ্ঞতা যেন সুন্দর হয় সেজন্য আমরা সহজ রিটার্ন ও রিফান্ড পলিসি রেখেছি।</p>

<h3><i class='fas fa-undo-alt text-primary me-2'></i>রিটার্ন পলিসি</h3>
<ul>
<li><strong>রিটার্নের সময়সীমা:</strong> পণ্য ডেলিভারি পাওয়ার <strong>৭ দিনের</strong> মধ্যে রিটার্ন রিকোয়েস্ট করতে হবে।</li>
<li><strong>পণ্যের অবস্থা:</strong> পণ্যটি অব্যবহৃত, অরিজিনাল প্যাকেজিং সহ এবং সকল ট্যাগ অক্ষত থাকতে হবে।</li>
<li><strong>রিসিট/ইনভয়েস:</strong> অর্ডার আইডি বা ইনভয়েস থাকতে হবে।</li>
</ul>

<h3><i class='fas fa-times-circle text-danger me-2'></i>যেসব পণ্য রিটার্নযোগ্য নয়</h3>
<ul>
<li>পরিধান করা বা ব্যবহৃত পণ্য</li>
<li>ইনার ওয়্যার ও আন্ডারগার্মেন্টস</li>
<li>কাস্টমাইজড বা পার্সোনালাইজড পণ্য</li>
<li>সেল বা ক্লিয়ারেন্স পণ্য (যদি উল্লেখ থাকে)</li>
<li>পার্ফিউম ও বিউটি প্রোডাক্ট (সিল খোলা হলে)</li>
</ul>

<h3><i class='fas fa-exchange-alt text-success me-2'></i>এক্সচেঞ্জ পলিসি</h3>
<ul>
<li>সাইজ বা রং পরিবর্তনের জন্য এক্সচেঞ্জ করা যাবে (স্টক সাপেক্ষে)।</li>
<li>এক্সচেঞ্জের ক্ষেত্রে ডেলিভারি চার্জ গ্রাহক বহন করবেন।</li>
<li>দামের পার্থক্য থাকলে সেটি সমন্বয় করা হবে।</li>
</ul>

<h3><i class='fas fa-money-bill-wave text-warning me-2'></i>রিফান্ড পলিসি</h3>
<ul>
<li><strong>রিফান্ডের সময়:</strong> রিটার্ন অনুমোদনের <strong>৫-৭ কার্যদিবসের</strong> মধ্যে রিফান্ড প্রক্রিয়া সম্পন্ন হবে।</li>
<li><strong>রিফান্ড মাধ্যম:</strong> যে মাধ্যমে পেমেন্ট করা হয়েছে সেই মাধ্যমেই রিফান্ড করা হবে অথবা ওয়ালেট ব্যালেন্সে জমা হবে।</li>
<li><strong>ক্যাশ অন ডেলিভারি:</strong> COD অর্ডারের ক্ষেত্রে bKash/Nagad এ রিফান্ড করা হবে।</li>
</ul>

<h3><i class='fas fa-clipboard-list text-info me-2'></i>রিটার্ন প্রক্রিয়া</h3>
<ol>
<li>আপনার অ্যাকাউন্টে লগইন করুন এবং অর্ডার হিস্ট্রিতে যান।</li>
<li>যে অর্ডারটি রিটার্ন করতে চান সেটি সিলেক্ট করুন।</li>
<li>'রিটার্ন রিকোয়েস্ট' বাটনে ক্লিক করুন।</li>
<li>রিটার্নের কারণ উল্লেখ করুন এবং ছবি আপলোড করুন।</li>
<li>আমাদের টিম রিভিউ করে ২৪-৪৮ ঘন্টার মধ্যে জানাবে।</li>
</ol>

<h3><i class='fas fa-truck text-primary me-2'></i>ত্রুটিপূর্ণ পণ্য</h3>
<p>ডেলিভারির সময় পণ্য ত্রুটিপূর্ণ বা ভুল পণ্য পেলে অবশ্যই ডেলিভারি ম্যানের সামনে চেক করুন এবং সাথে সাথে আমাদের জানান। এক্ষেত্রে সম্পূর্ণ বিনামূল্যে রিপ্লেসমেন্ট বা রিফান্ড দেওয়া হবে।</p>

<div class='alert alert-info mt-4'>
<i class='fas fa-headset me-2'></i>
<strong>সাহায্য প্রয়োজন?</strong> আমাদের কাস্টমার সার্ভিসে যোগাযোগ করুন:
<a href='/Customer/HelpCenter/LiveChat'>লাইভ চ্যাট</a> অথবা ইমেইল করুন support@bangaliyana.com
</div>
</div>",
                MetaDescription = "Bangaliyana এর রিটার্ন, এক্সচেঞ্জ এবং রিফান্ড পলিসি সম্পর্কে বিস্তারিত জানুন।",
                MetaKeywords = "return policy, refund policy, exchange policy, রিটার্ন পলিসি, রিফান্ড",
                ShowInFooter = true,
                DisplayOrder = 5,
                IsActive = true
            });
        }

        // Seed Terms & Conditions page if not exists
        if (!existingSlugs.Contains("terms"))
        {
            await siteSettingsService.CreatePageContentAsync(new PageContent
            {
                Slug = "terms",
                Title = "Terms & Conditions",
                Content = @"<h2>Terms and Conditions</h2>
<p>Welcome to Bangaliyana. By accessing or using our website, you agree to be bound by these Terms and Conditions.</p>

<h3>Use of Website</h3>
<p>You may use our website for lawful purposes only. You must not use our website in any way that causes damage to the website or impairs the availability or accessibility of the website.</p>

<h3>Account Registration</h3>
<p>When you create an account with us, you must provide accurate and complete information. You are responsible for maintaining the confidentiality of your account and password.</p>

<h3>Orders and Payments</h3>
<p>All orders are subject to availability and confirmation of the order price. We reserve the right to refuse any order you place with us.</p>

<h3>Intellectual Property</h3>
<p>All content on this website, including text, graphics, logos, and images, is the property of Bangaliyana and is protected by copyright laws.</p>

<h3>Limitation of Liability</h3>
<p>Bangaliyana shall not be liable for any indirect, incidental, special, consequential, or punitive damages resulting from your use of our services.</p>

<h3>Changes to Terms</h3>
<p>We reserve the right to modify these terms at any time. Your continued use of the website following any changes indicates your acceptance of the new terms.</p>",
                MetaDescription = "Read our terms and conditions to understand the rules and regulations for using Bangaliyana.",
                ShowInFooter = true,
                DisplayOrder = 6,
                IsActive = true
            });
        }
    }
}

// Seed product data
public static class SeedProductData
{
    public static async Task InitializeAsync(ApplicationDbContext db)
    {
        // Always repair category hierarchy first
        await RepairCategoryHierarchyAsync(db);

        // Check if products already exist
        if (await db.Products.AnyAsync())
        {
            // Fix existing products that might have incorrect status or stock (ViewCount and SoldCount are dynamic and not seeded)
            var productsToFix = await db.Products
                .Where(p => p.Status != ProductStatus.Active || !p.IsAvailable || p.Stock <= 0)
                .ToListAsync();

            if (productsToFix.Any())
            {
                var random = new Random();
                foreach (var product in productsToFix)
                {
                    product.Status = ProductStatus.Active;
                    product.IsAvailable = true;
                    if (product.Stock <= 0)
                    {
                        product.Stock = random.Next(20, 100); // Set random stock between 20-100
                    }
                    // ViewCount and SoldCount are NOT seeded - they are dynamic and increment from actual usage
                }
                await db.SaveChangesAsync();
            }
            return;
        }

        // Seed Categories with Hierarchical Structure
        if (!await db.Categories.AnyAsync())
        {
            // Root Categories
            var rootClothing = new Category { Name = "Clothing", Slug = "clothing", Description = "Traditional and modern clothing", IconClass = "fas fa-tshirt", DisplayOrder = 1, IsActive = true, IsFeatured = true, Level = 0 };
            var rootAccessories = new Category { Name = "Accessories", Slug = "accessories", Description = "Fashion accessories and jewelry", IconClass = "fas fa-gem", DisplayOrder = 2, IsActive = true, IsFeatured = true, Level = 0 };
            var rootHomeLiving = new Category { Name = "Home & Living", Slug = "home-living", Description = "Home decor and living essentials", IconClass = "fas fa-home", DisplayOrder = 3, IsActive = true, IsFeatured = true, Level = 0 };

            db.Categories.AddRange(rootClothing, rootAccessories, rootHomeLiving);
            await db.SaveChangesAsync();

            // Clothing Subcategories
            var subSaree = new Category { Name = "Saree", Slug = "saree", Description = "Beautiful traditional sarees", IconClass = "fas fa-female", DisplayOrder = 1, IsActive = true, IsFeatured = true, ParentId = rootClothing.Id, Level = 1 };
            var subPanjabi = new Category { Name = "Panjabi", Slug = "panjabi", Description = "Traditional panjabi for men", IconClass = "fas fa-male", DisplayOrder = 2, IsActive = true, IsFeatured = true, ParentId = rootClothing.Id, Level = 1 };
            var subSalwar = new Category { Name = "Salwar Kameez", Slug = "salwar-kameez", Description = "Elegant salwar kameez sets", IconClass = "fas fa-vest", DisplayOrder = 3, IsActive = true, IsFeatured = true, ParentId = rootClothing.Id, Level = 1 };

            // Accessories Subcategories
            var subJewelry = new Category { Name = "Jewelry", Slug = "jewelry", Description = "Traditional Bengali jewelry", IconClass = "fas fa-gem", DisplayOrder = 1, IsActive = true, IsFeatured = true, ParentId = rootAccessories.Id, Level = 1 };
            var subBags = new Category { Name = "Bags & Purses", Slug = "bags-purses", Description = "Handcrafted bags and purses", IconClass = "fas fa-shopping-bag", DisplayOrder = 2, IsActive = true, ParentId = rootAccessories.Id, Level = 1 };

            // Home & Living Subcategories
            var subHandicrafts = new Category { Name = "Handicrafts", Slug = "handicrafts", Description = "Handmade Bangladeshi crafts", IconClass = "fas fa-palette", DisplayOrder = 1, IsActive = true, ParentId = rootHomeLiving.Id, Level = 1 };
            var subKantha = new Category { Name = "Nakshi Kantha", Slug = "nakshi-kantha", Description = "Traditional embroidered quilts", IconClass = "fas fa-blanket", DisplayOrder = 2, IsActive = true, ParentId = rootHomeLiving.Id, Level = 1 };

            db.Categories.AddRange(subSaree, subPanjabi, subSalwar, subJewelry, subBags, subHandicrafts, subKantha);
            await db.SaveChangesAsync();

            // Third level - Saree Types
            var subJamdani = new Category { Name = "Jamdani", Slug = "jamdani", Description = "Handwoven Jamdani sarees", IconClass = "fas fa-star", DisplayOrder = 1, IsActive = true, ParentId = subSaree.Id, Level = 2 };
            var subTangail = new Category { Name = "Tangail", Slug = "tangail", Description = "Traditional Tangail sarees", IconClass = "fas fa-circle", DisplayOrder = 2, IsActive = true, ParentId = subSaree.Id, Level = 2 };
            var subSilkSaree = new Category { Name = "Silk Saree", Slug = "silk-saree", Description = "Premium silk sarees", IconClass = "fas fa-ribbon", DisplayOrder = 3, IsActive = true, ParentId = subSaree.Id, Level = 2 };

            // Third level - Jewelry Types
            var subNecklace = new Category { Name = "Necklaces", Slug = "necklaces", Description = "Traditional necklaces", IconClass = "fas fa-circle-notch", DisplayOrder = 1, IsActive = true, ParentId = subJewelry.Id, Level = 2 };
            var subEarrings = new Category { Name = "Earrings", Slug = "earrings", Description = "Earrings and jhumkas", IconClass = "fas fa-plug", DisplayOrder = 2, IsActive = true, ParentId = subJewelry.Id, Level = 2 };
            var subBangles = new Category { Name = "Bangles", Slug = "bangles", Description = "Traditional bangles", IconClass = "fas fa-ring", DisplayOrder = 3, IsActive = true, ParentId = subJewelry.Id, Level = 2 };

            db.Categories.AddRange(subJamdani, subTangail, subSilkSaree, subNecklace, subEarrings, subBangles);
            await db.SaveChangesAsync();
        }

        // Reload to get IDs
        var savedCategories = await db.Categories.ToListAsync();

        var sareeCategory = savedCategories.First(c => c.Name == "Saree");
        var panjabiCategory = savedCategories.First(c => c.Name == "Panjabi");
        var salwarCategory = savedCategories.First(c => c.Name == "Salwar Kameez");
        var jewelryCategory = savedCategories.First(c => c.Name == "Jewelry");
        var handicraftsCategory = savedCategories.First(c => c.Name == "Handicrafts");
        var kanthaCategory = savedCategories.First(c => c.Name == "Nakshi Kantha");

        // Seed Products with placeholder images
        var products = new List<Products>
        {
            // Sarees
            new() { Name = "Jamdani Saree - Royal Blue", Description = "Exquisite handwoven Jamdani saree with intricate floral patterns. Made with pure cotton and traditional weaving techniques from Dhaka.", ShortDescription = "Handwoven Jamdani with floral motifs", Price = 15000, DiscountPrice = 12500, ImageUrl = "https://placehold.co/400x400/1a365d/ffffff?text=Jamdani+Saree", IsAvailable = true, Stock = 25, CategoryId = sareeCategory.Id, IsFeatured = true, ViewCount = 0, SoldCount = 0, Status = ProductStatus.Active, Tags = "best seller, traditional" },
            new() { Name = "Tangail Cotton Saree", Description = "Traditional Tangail cotton saree with classic border design. Perfect for daily wear and office.", ShortDescription = "Classic Tangail cotton saree", Price = 3500, ImageUrl = "https://placehold.co/400x400/c53030/ffffff?text=Tangail+Saree", IsAvailable = true, Stock = 50, CategoryId = sareeCategory.Id, ViewCount = 0, SoldCount = 0, Status = ProductStatus.Active, Tags = "trending, daily wear" },
            new() { Name = "Silk Katan Saree - Maroon", Description = "Premium silk Katan saree with gold zari work. Ideal for weddings and special occasions.", ShortDescription = "Premium silk with gold zari", Price = 25000, DiscountPrice = 22000, ImageUrl = "https://placehold.co/400x400/7b341e/ffffff?text=Silk+Katan", IsAvailable = true, Stock = 15, CategoryId = sareeCategory.Id, IsFeatured = true, ViewCount = 0, SoldCount = 0, Status = ProductStatus.Active, Tags = "featured, wedding" },
            new() { Name = "Muslin Saree - Off White", Description = "Authentic Dhaka muslin saree. Extremely lightweight and breathable, perfect for summer.", ShortDescription = "Authentic lightweight Dhaka muslin", Price = 35000, ImageUrl = "https://placehold.co/400x400/f7fafc/1a202c?text=Muslin+Saree", IsAvailable = true, Stock = 8, CategoryId = sareeCategory.Id, IsFeatured = true, ViewCount = 0, SoldCount = 0, Status = ProductStatus.Active, Tags = "new arrival, premium" },

            // Panjabis
            new() { Name = "Cotton Panjabi - White", Description = "Premium cotton panjabi with embroidered collar. Comfortable and stylish for any occasion.", ShortDescription = "Embroidered cotton panjabi", Price = 2800, DiscountPrice = 2400, ImageUrl = "https://placehold.co/400x400/f7fafc/2d3748?text=White+Panjabi", IsAvailable = true, Stock = 60, CategoryId = panjabiCategory.Id, ViewCount = 0, SoldCount = 0, Status = ProductStatus.Active, Tags = "best seller, eid" },
            new() { Name = "Silk Panjabi - Navy Blue", Description = "Elegant silk panjabi perfect for Eid and weddings. Features traditional button work.", ShortDescription = "Elegant silk for special occasions", Price = 5500, ImageUrl = "https://placehold.co/400x400/2c5282/ffffff?text=Silk+Panjabi", IsAvailable = true, Stock = 30, CategoryId = panjabiCategory.Id, IsFeatured = true, ViewCount = 0, SoldCount = 0, Status = ProductStatus.Active, Tags = "trending, wedding" },
            new() { Name = "Designer Panjabi - Black & Gold", Description = "Modern designer panjabi with gold accents. Perfect blend of tradition and contemporary style.", ShortDescription = "Modern designer with gold accents", Price = 7500, DiscountPrice = 6500, ImageUrl = "https://placehold.co/400x400/1a202c/d69e2e?text=Designer+Panjabi", IsAvailable = true, Stock = 20, CategoryId = panjabiCategory.Id, IsFeatured = true, ViewCount = 0, SoldCount = 0, Status = ProductStatus.Active, Tags = "new arrival, designer" },

            // Salwar Kameez
            new() { Name = "Lawn Three-Piece Set", Description = "Premium lawn cotton three-piece set with dupatta. Digital print with elegant design.", ShortDescription = "Premium lawn three-piece", Price = 4500, DiscountPrice = 3800, ImageUrl = "https://placehold.co/400x400/48bb78/ffffff?text=Lawn+Set", IsAvailable = true, Stock = 40, CategoryId = salwarCategory.Id, ViewCount = 0, SoldCount = 0, Status = ProductStatus.Active, Tags = "sale, summer" },
            new() { Name = "Khaddar Salwar Kameez", Description = "Warm khaddar fabric salwar kameez set. Perfect for winter season.", ShortDescription = "Warm khaddar for winter", Price = 3200, ImageUrl = "https://placehold.co/400x400/744210/ffffff?text=Khaddar+Set", IsAvailable = true, Stock = 35, CategoryId = salwarCategory.Id, ViewCount = 0, SoldCount = 0, Status = ProductStatus.Active, Tags = "new arrival, winter" },
            new() { Name = "Chiffon Party Wear", Description = "Elegant chiffon party wear with heavy embroidery and stone work.", ShortDescription = "Elegant chiffon with embroidery", Price = 8500, DiscountPrice = 7200, ImageUrl = "https://placehold.co/400x400/9f7aea/ffffff?text=Chiffon+Party", IsAvailable = true, Stock = 18, CategoryId = salwarCategory.Id, IsFeatured = true, ViewCount = 0, SoldCount = 0, Status = ProductStatus.Active, Tags = "featured, party" },

            // Jewelry
            new() { Name = "Gold Plated Necklace Set", Description = "Traditional Bengali gold plated necklace set with matching earrings. Perfect for weddings.", ShortDescription = "Traditional gold plated set", Price = 4500, DiscountPrice = 3900, ImageUrl = "https://placehold.co/400x400/d69e2e/ffffff?text=Gold+Necklace", IsAvailable = true, Stock = 45, CategoryId = jewelryCategory.Id, ViewCount = 0, SoldCount = 0, Status = ProductStatus.Active, Tags = "best seller, wedding" },
            new() { Name = "Silver Anklet Pair", Description = "Pure silver traditional anklets with bells. Handcrafted by local artisans.", ShortDescription = "Handcrafted silver anklets", Price = 2800, ImageUrl = "https://placehold.co/400x400/a0aec0/1a202c?text=Silver+Anklet", IsAvailable = true, Stock = 55, CategoryId = jewelryCategory.Id, ViewCount = 0, SoldCount = 0, Status = ProductStatus.Active, Tags = "trending, handmade" },
            new() { Name = "Oxidized Jhumka Earrings", Description = "Beautiful oxidized silver jhumka earrings. Ethnic design with peacock motif.", ShortDescription = "Ethnic oxidized jhumkas", Price = 850, DiscountPrice = 699, ImageUrl = "https://placehold.co/400x400/4a5568/ffffff?text=Jhumka", IsAvailable = true, Stock = 80, CategoryId = jewelryCategory.Id, ViewCount = 0, SoldCount = 0, Status = ProductStatus.Active, Tags = "sale, ethnic" },

            // Handicrafts
            new() { Name = "Terracotta Wall Hanging", Description = "Handmade terracotta wall hanging featuring rural Bengal scenery. Unique home decor piece.", ShortDescription = "Handmade terracotta art", Price = 1800, ImageUrl = "https://placehold.co/400x400/c05621/ffffff?text=Terracotta", IsAvailable = true, Stock = 25, CategoryId = handicraftsCategory.Id, ViewCount = 0, SoldCount = 0, Status = ProductStatus.Active, Tags = "new arrival, handmade" },
            new() { Name = "Bamboo Craft Basket Set", Description = "Set of 3 decorative bamboo baskets. Handwoven by skilled artisans from Sylhet.", ShortDescription = "Handwoven bamboo basket set", Price = 1200, DiscountPrice = 999, ImageUrl = "https://placehold.co/400x400/68d391/1a202c?text=Bamboo+Set", IsAvailable = true, Stock = 30, CategoryId = handicraftsCategory.Id, IsFeatured = true, ViewCount = 0, SoldCount = 0, Status = ProductStatus.Active, Tags = "featured, eco-friendly" },
            new() { Name = "Jute Wall Art", Description = "Eco-friendly jute wall art with traditional Bengali motifs. Perfect gift item.", ShortDescription = "Eco-friendly jute art", Price = 950, ImageUrl = "https://placehold.co/400x400/b7791f/ffffff?text=Jute+Art", IsAvailable = true, Stock = 40, CategoryId = handicraftsCategory.Id, ViewCount = 0, SoldCount = 0, Status = ProductStatus.Active, Tags = "trending, gift" },

            // Nakshi Kantha
            new() { Name = "Nakshi Kantha Bedspread - Queen", Description = "Authentic Nakshi Kantha bedspread with intricate hand embroidery. Tells stories through stitches.", ShortDescription = "Hand-embroidered kantha bedspread", Price = 8500, DiscountPrice = 7500, ImageUrl = "https://placehold.co/400x400/805ad5/ffffff?text=Nakshi+Kantha", IsAvailable = true, Stock = 12, CategoryId = kanthaCategory.Id, IsFeatured = true, ViewCount = 0, SoldCount = 0, Status = ProductStatus.Active, Tags = "featured, traditional" },
            new() { Name = "Kantha Cushion Covers Set", Description = "Set of 4 Nakshi Kantha cushion covers. Beautiful floral patterns.", ShortDescription = "Set of 4 kantha cushion covers", Price = 2400, ImageUrl = "https://placehold.co/400x400/ed64a6/ffffff?text=Kantha+Cushion", IsAvailable = true, Stock = 35, CategoryId = kanthaCategory.Id, ViewCount = 0, SoldCount = 0, Status = ProductStatus.Active, Tags = "best seller, home" },
            new() { Name = "Kantha Stole", Description = "Lightweight kantha embroidered stole. Perfect accessory for any outfit.", ShortDescription = "Lightweight embroidered stole", Price = 1500, DiscountPrice = 1299, ImageUrl = "https://placehold.co/400x400/38b2ac/ffffff?text=Kantha+Stole", IsAvailable = true, Stock = 50, CategoryId = kanthaCategory.Id, ViewCount = 0, SoldCount = 0, Status = ProductStatus.Active, Tags = "new arrival, accessory" }
        };

        db.Products.AddRange(products);
        await db.SaveChangesAsync();

        // Seed Testimonials
        if (!await db.Testimonials.AnyAsync())
        {
            var testimonials = new List<Testimonial>
            {
                new() { CustomerName = "Fatima Rahman", CustomerLocation = "Dhaka", Content = "The Jamdani saree I ordered was absolutely stunning! The quality exceeded my expectations. Will definitely order again.", Rating = 5, IsFeatured = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                new() { CustomerName = "Karim Ahmed", CustomerLocation = "Chittagong", Content = "Great collection of panjabis. The silk panjabi I bought for Eid was perfect. Fast delivery too!", Rating = 5, IsFeatured = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                new() { CustomerName = "Nusrat Jahan", CustomerLocation = "Sylhet", Content = "Love the handicraft items! The Nakshi Kantha bedspread is a beautiful piece of art. Authentic and high quality.", Rating = 5, IsFeatured = true, IsActive = true, CreatedAt = DateTime.UtcNow }
            };
            db.Testimonials.AddRange(testimonials);
            await db.SaveChangesAsync();
        }

        // Seed Home Features
        var homeFeatures = await db.Features.Where(f => f.Section == FeatureSection.HomePage).ToListAsync();
        if (!homeFeatures.Any())
        {
            var features = new List<Feature>
            {
                new() { Title = "Free Shipping", Description = "On orders over ৳3000", IconClass = "fas fa-truck", IconColor = "#198754", Section = FeatureSection.HomePage, DisplayOrder = 1, IsActive = true },
                new() { Title = "Secure Payment", Description = "100% secure payment", IconClass = "fas fa-shield-alt", IconColor = "#0d6efd", Section = FeatureSection.HomePage, DisplayOrder = 2, IsActive = true },
                new() { Title = "24/7 Support", Description = "Dedicated support", IconClass = "fas fa-headset", IconColor = "#dc3545", Section = FeatureSection.HomePage, DisplayOrder = 3, IsActive = true },
                new() { Title = "Easy Returns", Description = "7 days return policy", IconClass = "fas fa-undo", IconColor = "#ffc107", Section = FeatureSection.HomePage, DisplayOrder = 4, IsActive = true }
            };
            db.Features.AddRange(features);
            await db.SaveChangesAsync();
        }

        // Seed Testimonials (if none exist, add demo testimonials)
        if (!await db.Testimonials.AnyAsync())
        {
            var now = DateTime.UtcNow;
            var testimonials = new List<Testimonial>
            {
                new() { CustomerName = "Ayesha Begum", CustomerTitle = "Verified Customer", CustomerLocation = "Dhaka", Content = "Bangaliyana has the best collection of authentic Bengali products. The quality is amazing and delivery is always on time. Highly recommended!", Rating = 5, IsActive = true, IsApproved = true, IsUserSubmitted = false, IsVerifiedUser = true, CreatedAt = now.AddDays(-30), UpdatedAt = now.AddDays(-30) },
                new() { CustomerName = "Rabeya Khatun", CustomerTitle = "Verified Customer", CustomerLocation = "Chittagong", Content = "I love shopping here! The sarees are gorgeous and the customer service is excellent. They helped me choose the perfect outfit for my wedding.", Rating = 5, IsActive = true, IsApproved = true, IsUserSubmitted = false, IsVerifiedUser = true, CreatedAt = now.AddDays(-25), UpdatedAt = now.AddDays(-25) },
                new() { CustomerName = "Mohammad Hasan", CustomerTitle = "Customer", CustomerLocation = "Sylhet", Content = "Great platform for traditional Bengali clothing. The panjabi I ordered was exactly as shown in the pictures. Very satisfied!", Rating = 4, IsActive = true, IsApproved = true, IsUserSubmitted = false, IsVerifiedUser = false, CreatedAt = now.AddDays(-20), UpdatedAt = now.AddDays(-20) },
                new() { CustomerName = "Sultana Ahmed", CustomerTitle = "Verified Customer", CustomerLocation = "Rajshahi", Content = "The Nakshi Kantha collection is beautiful. Authentic handwork and the prices are reasonable. This is my go-to shop for Bengali handicrafts.", Rating = 5, IsActive = true, IsApproved = true, IsUserSubmitted = false, IsVerifiedUser = true, CreatedAt = now.AddDays(-15), UpdatedAt = now.AddDays(-15) },
                new() { CustomerName = "Nasreen Akter", CustomerTitle = "Verified Customer", CustomerLocation = "Khulna", Content = "Fast delivery and excellent packaging. The jewelry set I ordered was stunning. Will definitely shop again!", Rating = 5, IsActive = true, IsApproved = true, IsUserSubmitted = false, IsVerifiedUser = true, CreatedAt = now.AddDays(-10), UpdatedAt = now.AddDays(-10) },
                new() { CustomerName = "Kamal Uddin", CustomerTitle = "Customer", CustomerLocation = "Barisal", Content = "Good collection but delivery took a bit longer than expected. Product quality is excellent though.", Rating = 3, IsActive = true, IsApproved = true, IsUserSubmitted = false, IsVerifiedUser = false, CreatedAt = now.AddDays(-8), UpdatedAt = now.AddDays(-8) },
                new() { CustomerName = "Abdul Rahman", CustomerTitle = "Verified Customer", CustomerLocation = "Dhaka", Content = "The silk panjabi I bought for Eid was absolutely perfect! The fabric quality and stitching are top-notch. Thank you Bangaliyana!", Rating = 5, IsActive = true, IsApproved = true, IsUserSubmitted = false, IsVerifiedUser = true, CreatedAt = now.AddDays(-5), UpdatedAt = now.AddDays(-5) },
                new() { CustomerName = "Taslima Parvin", CustomerTitle = "Verified Customer", CustomerLocation = "Comilla", Content = "I'm a regular customer and have never been disappointed. The website is easy to use and the products are always authentic.", Rating = 4, IsActive = true, IsApproved = true, IsUserSubmitted = false, IsVerifiedUser = true, CreatedAt = now.AddDays(-3), UpdatedAt = now.AddDays(-3) },
                new() { CustomerName = "Shamima Islam", CustomerTitle = "Verified Customer", CustomerLocation = "Jessore", Content = "Best online shop for Bangladeshi traditional wear! The Jamdani saree I ordered is a masterpiece. Worth every taka!", Rating = 5, IsActive = true, IsApproved = true, IsUserSubmitted = false, IsVerifiedUser = true, CreatedAt = now.AddDays(-1), UpdatedAt = now.AddDays(-1) },
                new() { CustomerName = "Fahim Chowdhury", CustomerTitle = "Customer", CustomerLocation = "Rangpur", Content = "Excellent customer support! They answered all my queries promptly. The product arrived in perfect condition.", Rating = 4, IsActive = true, IsApproved = true, IsUserSubmitted = false, IsVerifiedUser = false, CreatedAt = now, UpdatedAt = now }
            };

            db.Testimonials.AddRange(testimonials);
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Seeds comprehensive category hierarchy or repairs existing categories
    /// </summary>
    private static async Task RepairCategoryHierarchyAsync(ApplicationDbContext db)
    {
        // If no categories exist, seed the comprehensive hierarchy
        if (!await db.Categories.AnyAsync())
        {
            await SeedComprehensiveCategoriesAsync(db);
            return;
        }

        // Check if the comprehensive hierarchy already exists (has our new root categories)
        var existingRoots = await db.Categories.Where(c => c.ParentId == null).Select(c => c.Name).ToListAsync();
        var newRoots = new[] { "Electronics & Media", "Fashion & Apparel", "Home, Furniture & Kitchen", "Beauty & Health", "Sports & Outdoors", "Toys, Kids & Hobbies", "Groceries & Household" };

        // If all new roots exist, hierarchy is already set up
        if (newRoots.All(r => existingRoots.Contains(r)))
        {
            return;
        }

        // Need to replace old hierarchy with comprehensive one
        // First, set all products' CategoryId to null to avoid FK constraint issues
        var productsWithCategory = await db.Products.Where(p => p.CategoryId != null).ToListAsync();
        foreach (var product in productsWithCategory)
        {
            product.CategoryId = null;
        }
        await db.SaveChangesAsync();

        // Delete all existing categories (children first, then parents)
        var allCategories = await db.Categories.OrderByDescending(c => c.Level).ToListAsync();
        db.Categories.RemoveRange(allCategories);
        await db.SaveChangesAsync();

        // Now seed the comprehensive hierarchy
        await SeedComprehensiveCategoriesAsync(db);
    }

    /// <summary>
    /// Seeds the comprehensive category hierarchy
    /// </summary>
    private static async Task SeedComprehensiveCategoriesAsync(ApplicationDbContext db)
    {
        // Helper to create category
        Category CreateCat(string name, string? desc = null, string? icon = null, int order = 0, bool featured = false) =>
            new() { Name = name, Slug = name.ToLower().Replace(" ", "-").Replace("&", "and").Replace(",", ""), Description = desc ?? name, IconClass = icon ?? "fas fa-folder", DisplayOrder = order, IsActive = true, IsFeatured = featured, Level = 0 };

        // ===========================================
        // 1. ELECTRONICS & MEDIA
        // ===========================================
        var electronics = CreateCat("Electronics & Media", "Electronics, gadgets, and media devices", "fas fa-laptop", 1, true);
        db.Categories.Add(electronics);
        await db.SaveChangesAsync();

        // Level 1: Computing
        var computing = new Category { Name = "Computing", Slug = "computing", Description = "Computers and computing devices", IconClass = "fas fa-desktop", DisplayOrder = 1, IsActive = true, Level = 1, ParentId = electronics.Id };
        var mobileWearables = new Category { Name = "Mobile & Wearables", Slug = "mobile-wearables", Description = "Smartphones, tablets, and wearable tech", IconClass = "fas fa-mobile-alt", DisplayOrder = 2, IsActive = true, Level = 1, ParentId = electronics.Id };
        var audioVideo = new Category { Name = "Audio & Video", Slug = "audio-video", Description = "TV, audio systems, and entertainment", IconClass = "fas fa-tv", DisplayOrder = 3, IsActive = true, Level = 1, ParentId = electronics.Id };
        var camerasPhoto = new Category { Name = "Cameras & Photography", Slug = "cameras-photography", Description = "Cameras, lenses, and photography equipment", IconClass = "fas fa-camera", DisplayOrder = 4, IsActive = true, Level = 1, ParentId = electronics.Id };
        db.Categories.AddRange(computing, mobileWearables, audioVideo, camerasPhoto);
        await db.SaveChangesAsync();

        // Level 2: Computing subcategories
        var laptops = new Category { Name = "Laptops", Slug = "laptops", Description = "Portable computing devices", IconClass = "fas fa-laptop", DisplayOrder = 1, IsActive = true, Level = 2, ParentId = computing.Id };
        var desktopPCs = new Category { Name = "Desktop PCs", Slug = "desktop-pcs", Description = "Desktop computers", IconClass = "fas fa-desktop", DisplayOrder = 2, IsActive = true, Level = 2, ParentId = computing.Id };
        var components = new Category { Name = "Components", Slug = "components", Description = "Computer components and parts", IconClass = "fas fa-microchip", DisplayOrder = 3, IsActive = true, Level = 2, ParentId = computing.Id };
        db.Categories.AddRange(laptops, desktopPCs, components);

        // Level 2: Mobile & Wearables subcategories
        var smartphones = new Category { Name = "Smartphones", Slug = "smartphones", Description = "Mobile phones", IconClass = "fas fa-mobile-alt", DisplayOrder = 1, IsActive = true, Level = 2, ParentId = mobileWearables.Id };
        var tablets = new Category { Name = "Tablets", Slug = "tablets", Description = "Tablet devices", IconClass = "fas fa-tablet-alt", DisplayOrder = 2, IsActive = true, Level = 2, ParentId = mobileWearables.Id };
        var wearableTech = new Category { Name = "Wearable Tech", Slug = "wearable-tech", Description = "Smartwatches and fitness trackers", IconClass = "fas fa-clock", DisplayOrder = 3, IsActive = true, Level = 2, ParentId = mobileWearables.Id };
        db.Categories.AddRange(smartphones, tablets, wearableTech);

        // Level 2: Audio & Video subcategories
        var television = new Category { Name = "Television", Slug = "television", Description = "TVs and displays", IconClass = "fas fa-tv", DisplayOrder = 1, IsActive = true, Level = 2, ParentId = audioVideo.Id };
        var headphones = new Category { Name = "Headphones", Slug = "headphones", Description = "Headphones and earphones", IconClass = "fas fa-headphones", DisplayOrder = 2, IsActive = true, Level = 2, ParentId = audioVideo.Id };
        var homeAudio = new Category { Name = "Home Audio", Slug = "home-audio", Description = "Speakers and audio systems", IconClass = "fas fa-volume-up", DisplayOrder = 3, IsActive = true, Level = 2, ParentId = audioVideo.Id };
        db.Categories.AddRange(television, headphones, homeAudio);

        // Level 2: Cameras subcategories
        var cameras = new Category { Name = "Cameras", Slug = "cameras", Description = "Digital cameras", IconClass = "fas fa-camera-retro", DisplayOrder = 1, IsActive = true, Level = 2, ParentId = camerasPhoto.Id };
        var lenses = new Category { Name = "Lenses", Slug = "lenses", Description = "Camera lenses", IconClass = "fas fa-bullseye", DisplayOrder = 2, IsActive = true, Level = 2, ParentId = camerasPhoto.Id };
        db.Categories.AddRange(cameras, lenses);
        await db.SaveChangesAsync();

        // Level 3: Laptops types
        db.Categories.AddRange(
            new Category { Name = "Gaming Laptops", Slug = "gaming-laptops", IconClass = "fas fa-gamepad", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = laptops.Id },
            new Category { Name = "Ultrabooks", Slug = "ultrabooks", IconClass = "fas fa-feather", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = laptops.Id },
            new Category { Name = "2-in-1 Convertibles", Slug = "2-in-1-convertibles", IconClass = "fas fa-sync", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = laptops.Id },
            new Category { Name = "Chromebooks", Slug = "chromebooks", IconClass = "fab fa-chrome", DisplayOrder = 4, IsActive = true, Level = 3, ParentId = laptops.Id },
            new Category { Name = "MacBooks", Slug = "macbooks", IconClass = "fab fa-apple", DisplayOrder = 5, IsActive = true, Level = 3, ParentId = laptops.Id }
        );

        // Level 3: Desktop PCs types
        db.Categories.AddRange(
            new Category { Name = "Tower Computers", Slug = "tower-computers", IconClass = "fas fa-server", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = desktopPCs.Id },
            new Category { Name = "All-in-One PCs", Slug = "all-in-one-pcs", IconClass = "fas fa-desktop", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = desktopPCs.Id },
            new Category { Name = "Mini PCs", Slug = "mini-pcs", IconClass = "fas fa-cube", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = desktopPCs.Id },
            new Category { Name = "Gaming Desktops", Slug = "gaming-desktops", IconClass = "fas fa-gamepad", DisplayOrder = 4, IsActive = true, Level = 3, ParentId = desktopPCs.Id }
        );

        // Level 3: Components types
        db.Categories.AddRange(
            new Category { Name = "Processors (CPUs)", Slug = "processors-cpus", IconClass = "fas fa-microchip", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = components.Id },
            new Category { Name = "Graphic Cards (GPUs)", Slug = "graphic-cards-gpus", IconClass = "fas fa-tachometer-alt", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = components.Id },
            new Category { Name = "Motherboards", Slug = "motherboards", IconClass = "fas fa-memory", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = components.Id },
            new Category { Name = "RAM", Slug = "ram", IconClass = "fas fa-memory", DisplayOrder = 4, IsActive = true, Level = 3, ParentId = components.Id },
            new Category { Name = "Internal SSDs", Slug = "internal-ssds", IconClass = "fas fa-hdd", DisplayOrder = 5, IsActive = true, Level = 3, ParentId = components.Id }
        );

        // Level 3: Smartphones types
        db.Categories.AddRange(
            new Category { Name = "iOS Devices", Slug = "ios-devices", IconClass = "fab fa-apple", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = smartphones.Id },
            new Category { Name = "Android Phones", Slug = "android-phones", IconClass = "fab fa-android", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = smartphones.Id },
            new Category { Name = "Rugged Phones", Slug = "rugged-phones", IconClass = "fas fa-shield-alt", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = smartphones.Id },
            new Category { Name = "Refurbished Phones", Slug = "refurbished-phones", IconClass = "fas fa-recycle", DisplayOrder = 4, IsActive = true, Level = 3, ParentId = smartphones.Id }
        );

        // Level 3: Tablets types
        db.Categories.AddRange(
            new Category { Name = "iPad", Slug = "ipad", IconClass = "fab fa-apple", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = tablets.Id },
            new Category { Name = "Android Tablets", Slug = "android-tablets", IconClass = "fab fa-android", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = tablets.Id },
            new Category { Name = "E-Readers", Slug = "e-readers", IconClass = "fas fa-book-reader", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = tablets.Id },
            new Category { Name = "Graphic Tablets", Slug = "graphic-tablets", IconClass = "fas fa-pen-fancy", DisplayOrder = 4, IsActive = true, Level = 3, ParentId = tablets.Id }
        );

        // Level 3: Wearable Tech types
        db.Categories.AddRange(
            new Category { Name = "Smartwatches", Slug = "smartwatches", IconClass = "fas fa-clock", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = wearableTech.Id },
            new Category { Name = "Fitness Trackers", Slug = "fitness-trackers", IconClass = "fas fa-heartbeat", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = wearableTech.Id },
            new Category { Name = "VR Headsets", Slug = "vr-headsets", IconClass = "fas fa-vr-cardboard", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = wearableTech.Id }
        );

        // Level 3: Television types
        db.Categories.AddRange(
            new Category { Name = "OLED TVs", Slug = "oled-tvs", IconClass = "fas fa-tv", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = television.Id },
            new Category { Name = "QLED TVs", Slug = "qled-tvs", IconClass = "fas fa-tv", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = television.Id },
            new Category { Name = "4K UHD TVs", Slug = "4k-uhd-tvs", IconClass = "fas fa-tv", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = television.Id },
            new Category { Name = "Smart TV Boxes", Slug = "smart-tv-boxes", IconClass = "fas fa-box", DisplayOrder = 4, IsActive = true, Level = 3, ParentId = television.Id }
        );

        // Level 3: Headphones types
        db.Categories.AddRange(
            new Category { Name = "Noise Cancelling", Slug = "noise-cancelling", IconClass = "fas fa-volume-mute", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = headphones.Id },
            new Category { Name = "Earbuds", Slug = "earbuds", IconClass = "fas fa-headphones-alt", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = headphones.Id },
            new Category { Name = "Over-Ear", Slug = "over-ear", IconClass = "fas fa-headphones", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = headphones.Id },
            new Category { Name = "Bone Conduction", Slug = "bone-conduction", IconClass = "fas fa-bone", DisplayOrder = 4, IsActive = true, Level = 3, ParentId = headphones.Id }
        );

        // Level 3: Home Audio types
        db.Categories.AddRange(
            new Category { Name = "Soundbars", Slug = "soundbars", IconClass = "fas fa-volume-up", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = homeAudio.Id },
            new Category { Name = "Hi-Fi Systems", Slug = "hi-fi-systems", IconClass = "fas fa-music", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = homeAudio.Id },
            new Category { Name = "Record Players", Slug = "record-players", IconClass = "fas fa-record-vinyl", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = homeAudio.Id },
            new Category { Name = "Bluetooth Speakers", Slug = "bluetooth-speakers", IconClass = "fab fa-bluetooth", DisplayOrder = 4, IsActive = true, Level = 3, ParentId = homeAudio.Id }
        );

        // Level 3: Camera types
        db.Categories.AddRange(
            new Category { Name = "DSLR", Slug = "dslr", IconClass = "fas fa-camera", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = cameras.Id },
            new Category { Name = "Mirrorless", Slug = "mirrorless", IconClass = "fas fa-camera-retro", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = cameras.Id },
            new Category { Name = "Point & Shoot", Slug = "point-shoot", IconClass = "fas fa-camera", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = cameras.Id },
            new Category { Name = "Action Cameras", Slug = "action-cameras", IconClass = "fas fa-video", DisplayOrder = 4, IsActive = true, Level = 3, ParentId = cameras.Id }
        );

        // Level 3: Lens types
        db.Categories.AddRange(
            new Category { Name = "Prime Lenses", Slug = "prime-lenses", IconClass = "fas fa-bullseye", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = lenses.Id },
            new Category { Name = "Zoom Lenses", Slug = "zoom-lenses", IconClass = "fas fa-search-plus", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = lenses.Id },
            new Category { Name = "Macro Lenses", Slug = "macro-lenses", IconClass = "fas fa-search", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = lenses.Id }
        );
        await db.SaveChangesAsync();

        // ===========================================
        // 2. FASHION & APPAREL
        // ===========================================
        var fashion = CreateCat("Fashion & Apparel", "Clothing, footwear, and fashion accessories", "fas fa-tshirt", 2, true);
        db.Categories.Add(fashion);
        await db.SaveChangesAsync();

        var womensWear = new Category { Name = "Women's Wear", Slug = "womens-wear", Description = "Women's clothing and fashion", IconClass = "fas fa-female", DisplayOrder = 1, IsActive = true, IsFeatured = true, Level = 1, ParentId = fashion.Id };
        var mensWear = new Category { Name = "Men's Wear", Slug = "mens-wear", Description = "Men's clothing and fashion", IconClass = "fas fa-male", DisplayOrder = 2, IsActive = true, IsFeatured = true, Level = 1, ParentId = fashion.Id };
        var kidsBaby = new Category { Name = "Kids & Baby", Slug = "kids-baby", Description = "Children's clothing", IconClass = "fas fa-baby", DisplayOrder = 3, IsActive = true, Level = 1, ParentId = fashion.Id };
        db.Categories.AddRange(womensWear, mensWear, kidsBaby);
        await db.SaveChangesAsync();

        // Women's Wear subcategories
        var womenClothing = new Category { Name = "Clothing", Slug = "women-clothing", IconClass = "fas fa-tshirt", DisplayOrder = 1, IsActive = true, Level = 2, ParentId = womensWear.Id };
        var womenFootwear = new Category { Name = "Footwear", Slug = "women-footwear", IconClass = "fas fa-shoe-prints", DisplayOrder = 2, IsActive = true, Level = 2, ParentId = womensWear.Id };
        var womenAccessories = new Category { Name = "Accessories", Slug = "women-accessories", IconClass = "fas fa-gem", DisplayOrder = 3, IsActive = true, Level = 2, ParentId = womensWear.Id };
        db.Categories.AddRange(womenClothing, womenFootwear, womenAccessories);

        // Men's Wear subcategories
        var menClothing = new Category { Name = "Clothing", Slug = "men-clothing", IconClass = "fas fa-tshirt", DisplayOrder = 1, IsActive = true, Level = 2, ParentId = mensWear.Id };
        var menFootwear = new Category { Name = "Footwear", Slug = "men-footwear", IconClass = "fas fa-shoe-prints", DisplayOrder = 2, IsActive = true, Level = 2, ParentId = mensWear.Id };
        var menAccessories = new Category { Name = "Accessories", Slug = "men-accessories", IconClass = "fas fa-gem", DisplayOrder = 3, IsActive = true, Level = 2, ParentId = mensWear.Id };
        db.Categories.AddRange(menClothing, menFootwear, menAccessories);

        // Kids & Baby subcategories
        var infants = new Category { Name = "Infants", Slug = "infants", IconClass = "fas fa-baby", DisplayOrder = 1, IsActive = true, Level = 2, ParentId = kidsBaby.Id };
        var toddlers = new Category { Name = "Toddlers", Slug = "toddlers", IconClass = "fas fa-child", DisplayOrder = 2, IsActive = true, Level = 2, ParentId = kidsBaby.Id };
        db.Categories.AddRange(infants, toddlers);
        await db.SaveChangesAsync();

        // Women's Clothing types
        db.Categories.AddRange(
            new Category { Name = "Dresses", Slug = "dresses", IconClass = "fas fa-tshirt", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = womenClothing.Id },
            new Category { Name = "Blouses", Slug = "blouses", IconClass = "fas fa-tshirt", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = womenClothing.Id },
            new Category { Name = "Leggings", Slug = "leggings", IconClass = "fas fa-tshirt", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = womenClothing.Id },
            new Category { Name = "Skirts", Slug = "skirts", IconClass = "fas fa-tshirt", DisplayOrder = 4, IsActive = true, Level = 3, ParentId = womenClothing.Id },
            new Category { Name = "Blazers", Slug = "blazers", IconClass = "fas fa-tshirt", DisplayOrder = 5, IsActive = true, Level = 3, ParentId = womenClothing.Id },
            new Category { Name = "Lingerie", Slug = "lingerie", IconClass = "fas fa-tshirt", DisplayOrder = 6, IsActive = true, Level = 3, ParentId = womenClothing.Id },
            new Category { Name = "Sarees", Slug = "sarees", IconClass = "fas fa-tshirt", DisplayOrder = 7, IsActive = true, IsFeatured = true, Level = 3, ParentId = womenClothing.Id },
            new Category { Name = "Salwar Kameez", Slug = "salwar-kameez", IconClass = "fas fa-tshirt", DisplayOrder = 8, IsActive = true, IsFeatured = true, Level = 3, ParentId = womenClothing.Id }
        );

        // Women's Footwear types
        db.Categories.AddRange(
            new Category { Name = "Stilettos", Slug = "stilettos", IconClass = "fas fa-shoe-prints", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = womenFootwear.Id },
            new Category { Name = "Flat Sandals", Slug = "flat-sandals", IconClass = "fas fa-shoe-prints", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = womenFootwear.Id },
            new Category { Name = "Running Shoes", Slug = "women-running-shoes", IconClass = "fas fa-running", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = womenFootwear.Id },
            new Category { Name = "Ankle Boots", Slug = "ankle-boots", IconClass = "fas fa-shoe-prints", DisplayOrder = 4, IsActive = true, Level = 3, ParentId = womenFootwear.Id }
        );

        // Women's Accessories types
        db.Categories.AddRange(
            new Category { Name = "Handbags", Slug = "handbags", IconClass = "fas fa-shopping-bag", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = womenAccessories.Id },
            new Category { Name = "Jewelry", Slug = "women-jewelry", IconClass = "fas fa-gem", DisplayOrder = 2, IsActive = true, IsFeatured = true, Level = 3, ParentId = womenAccessories.Id },
            new Category { Name = "Silk Scarves", Slug = "silk-scarves", IconClass = "fas fa-scarf", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = womenAccessories.Id },
            new Category { Name = "Sunglasses", Slug = "women-sunglasses", IconClass = "fas fa-glasses", DisplayOrder = 4, IsActive = true, Level = 3, ParentId = womenAccessories.Id }
        );

        // Men's Clothing types
        db.Categories.AddRange(
            new Category { Name = "Formal Shirts", Slug = "formal-shirts", IconClass = "fas fa-tshirt", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = menClothing.Id },
            new Category { Name = "T-Shirts", Slug = "t-shirts", IconClass = "fas fa-tshirt", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = menClothing.Id },
            new Category { Name = "Denim Jeans", Slug = "denim-jeans", IconClass = "fas fa-tshirt", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = menClothing.Id },
            new Category { Name = "Suits", Slug = "suits", IconClass = "fas fa-user-tie", DisplayOrder = 4, IsActive = true, Level = 3, ParentId = menClothing.Id },
            new Category { Name = "Chinos", Slug = "chinos", IconClass = "fas fa-tshirt", DisplayOrder = 5, IsActive = true, Level = 3, ParentId = menClothing.Id },
            new Category { Name = "Underwear", Slug = "underwear", IconClass = "fas fa-tshirt", DisplayOrder = 6, IsActive = true, Level = 3, ParentId = menClothing.Id },
            new Category { Name = "Panjabi", Slug = "panjabi", IconClass = "fas fa-tshirt", DisplayOrder = 7, IsActive = true, IsFeatured = true, Level = 3, ParentId = menClothing.Id }
        );

        // Men's Footwear types
        db.Categories.AddRange(
            new Category { Name = "Oxford Shoes", Slug = "oxford-shoes", IconClass = "fas fa-shoe-prints", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = menFootwear.Id },
            new Category { Name = "Loafers", Slug = "loafers", IconClass = "fas fa-shoe-prints", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = menFootwear.Id },
            new Category { Name = "Sneakers", Slug = "sneakers", IconClass = "fas fa-shoe-prints", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = menFootwear.Id },
            new Category { Name = "Work Boots", Slug = "work-boots", IconClass = "fas fa-shoe-prints", DisplayOrder = 4, IsActive = true, Level = 3, ParentId = menFootwear.Id }
        );

        // Men's Accessories types
        db.Categories.AddRange(
            new Category { Name = "Wallets", Slug = "wallets", IconClass = "fas fa-wallet", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = menAccessories.Id },
            new Category { Name = "Leather Belts", Slug = "leather-belts", IconClass = "fas fa-ribbon", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = menAccessories.Id },
            new Category { Name = "Ties", Slug = "ties", IconClass = "fas fa-user-tie", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = menAccessories.Id },
            new Category { Name = "Watches", Slug = "watches", IconClass = "fas fa-clock", DisplayOrder = 4, IsActive = true, Level = 3, ParentId = menAccessories.Id },
            new Category { Name = "Cufflinks", Slug = "cufflinks", IconClass = "fas fa-gem", DisplayOrder = 5, IsActive = true, Level = 3, ParentId = menAccessories.Id }
        );

        // Infants types
        db.Categories.AddRange(
            new Category { Name = "Onesies", Slug = "onesies", IconClass = "fas fa-baby", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = infants.Id },
            new Category { Name = "Swaddles", Slug = "swaddles", IconClass = "fas fa-baby", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = infants.Id },
            new Category { Name = "Baby Booties", Slug = "baby-booties", IconClass = "fas fa-baby", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = infants.Id }
        );

        // Toddlers types
        db.Categories.AddRange(
            new Category { Name = "Playwear", Slug = "playwear", IconClass = "fas fa-child", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = toddlers.Id },
            new Category { Name = "Pajamas", Slug = "pajamas", IconClass = "fas fa-bed", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = toddlers.Id },
            new Category { Name = "School Uniforms", Slug = "school-uniforms", IconClass = "fas fa-graduation-cap", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = toddlers.Id }
        );
        await db.SaveChangesAsync();

        // ===========================================
        // 3. HOME, FURNITURE & KITCHEN
        // ===========================================
        var homeFurniture = CreateCat("Home, Furniture & Kitchen", "Furniture, kitchen items, and home decor", "fas fa-home", 3, true);
        db.Categories.Add(homeFurniture);
        await db.SaveChangesAsync();

        var indoorFurniture = new Category { Name = "Indoor Furniture", Slug = "indoor-furniture", IconClass = "fas fa-couch", DisplayOrder = 1, IsActive = true, Level = 1, ParentId = homeFurniture.Id };
        var kitchenDining = new Category { Name = "Kitchen & Dining", Slug = "kitchen-dining", IconClass = "fas fa-utensils", DisplayOrder = 2, IsActive = true, Level = 1, ParentId = homeFurniture.Id };
        var homeDecor = new Category { Name = "Home Decor", Slug = "home-decor", IconClass = "fas fa-paint-roller", DisplayOrder = 3, IsActive = true, Level = 1, ParentId = homeFurniture.Id };
        db.Categories.AddRange(indoorFurniture, kitchenDining, homeDecor);
        await db.SaveChangesAsync();

        // Indoor Furniture subcategories
        var livingRoom = new Category { Name = "Living Room", Slug = "living-room", IconClass = "fas fa-couch", DisplayOrder = 1, IsActive = true, Level = 2, ParentId = indoorFurniture.Id };
        var bedroom = new Category { Name = "Bedroom", Slug = "bedroom", IconClass = "fas fa-bed", DisplayOrder = 2, IsActive = true, Level = 2, ParentId = indoorFurniture.Id };
        var office = new Category { Name = "Office", Slug = "office-furniture", IconClass = "fas fa-briefcase", DisplayOrder = 3, IsActive = true, Level = 2, ParentId = indoorFurniture.Id };
        db.Categories.AddRange(livingRoom, bedroom, office);

        // Kitchen & Dining subcategories
        var cookware = new Category { Name = "Cookware", Slug = "cookware", IconClass = "fas fa-utensils", DisplayOrder = 1, IsActive = true, Level = 2, ParentId = kitchenDining.Id };
        var smallAppliances = new Category { Name = "Small Appliances", Slug = "small-appliances", IconClass = "fas fa-blender", DisplayOrder = 2, IsActive = true, Level = 2, ParentId = kitchenDining.Id };
        var dinnerware = new Category { Name = "Dinnerware", Slug = "dinnerware", IconClass = "fas fa-utensil-spoon", DisplayOrder = 3, IsActive = true, Level = 2, ParentId = kitchenDining.Id };
        db.Categories.AddRange(cookware, smallAppliances, dinnerware);

        // Home Decor subcategories
        var lighting = new Category { Name = "Lighting", Slug = "lighting", IconClass = "fas fa-lightbulb", DisplayOrder = 1, IsActive = true, Level = 2, ParentId = homeDecor.Id };
        var textiles = new Category { Name = "Textiles", Slug = "textiles", IconClass = "fas fa-scroll", DisplayOrder = 2, IsActive = true, Level = 2, ParentId = homeDecor.Id };
        db.Categories.AddRange(lighting, textiles);
        await db.SaveChangesAsync();

        // Living Room types
        db.Categories.AddRange(
            new Category { Name = "Sectional Sofas", Slug = "sectional-sofas", IconClass = "fas fa-couch", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = livingRoom.Id },
            new Category { Name = "Recliners", Slug = "recliners", IconClass = "fas fa-couch", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = livingRoom.Id },
            new Category { Name = "Coffee Tables", Slug = "coffee-tables", IconClass = "fas fa-table", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = livingRoom.Id },
            new Category { Name = "TV Stands", Slug = "tv-stands", IconClass = "fas fa-tv", DisplayOrder = 4, IsActive = true, Level = 3, ParentId = livingRoom.Id }
        );

        // Bedroom types
        db.Categories.AddRange(
            new Category { Name = "Memory Foam Mattresses", Slug = "memory-foam-mattresses", IconClass = "fas fa-bed", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = bedroom.Id },
            new Category { Name = "Bed Frames", Slug = "bed-frames", IconClass = "fas fa-bed", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = bedroom.Id },
            new Category { Name = "Dressers", Slug = "dressers", IconClass = "fas fa-archive", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = bedroom.Id },
            new Category { Name = "Nightstands", Slug = "nightstands", IconClass = "fas fa-table", DisplayOrder = 4, IsActive = true, Level = 3, ParentId = bedroom.Id }
        );

        // Office types
        db.Categories.AddRange(
            new Category { Name = "Ergonomic Chairs", Slug = "ergonomic-chairs", IconClass = "fas fa-chair", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = office.Id },
            new Category { Name = "Standing Desks", Slug = "standing-desks", IconClass = "fas fa-desktop", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = office.Id },
            new Category { Name = "Bookshelves", Slug = "bookshelves", IconClass = "fas fa-book", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = office.Id }
        );

        // Cookware types
        db.Categories.AddRange(
            new Category { Name = "Pots & Pans", Slug = "pots-pans", IconClass = "fas fa-utensils", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = cookware.Id },
            new Category { Name = "Dutch Ovens", Slug = "dutch-ovens", IconClass = "fas fa-utensils", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = cookware.Id },
            new Category { Name = "Bakeware Sets", Slug = "bakeware-sets", IconClass = "fas fa-bread-slice", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = cookware.Id }
        );

        // Small Appliances types
        db.Categories.AddRange(
            new Category { Name = "Air Fryers", Slug = "air-fryers", IconClass = "fas fa-blender", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = smallAppliances.Id },
            new Category { Name = "Espresso Machines", Slug = "espresso-machines", IconClass = "fas fa-coffee", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = smallAppliances.Id },
            new Category { Name = "Blenders", Slug = "blenders", IconClass = "fas fa-blender", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = smallAppliances.Id },
            new Category { Name = "Toasters", Slug = "toasters", IconClass = "fas fa-bread-slice", DisplayOrder = 4, IsActive = true, Level = 3, ParentId = smallAppliances.Id }
        );

        // Dinnerware types
        db.Categories.AddRange(
            new Category { Name = "Ceramic Plates", Slug = "ceramic-plates", IconClass = "fas fa-utensil-spoon", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = dinnerware.Id },
            new Category { Name = "Glassware", Slug = "glassware", IconClass = "fas fa-wine-glass", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = dinnerware.Id },
            new Category { Name = "Cutlery Sets", Slug = "cutlery-sets", IconClass = "fas fa-utensils", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = dinnerware.Id }
        );

        // Lighting types
        db.Categories.AddRange(
            new Category { Name = "Floor Lamps", Slug = "floor-lamps", IconClass = "fas fa-lightbulb", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = lighting.Id },
            new Category { Name = "Chandeliers", Slug = "chandeliers", IconClass = "fas fa-lightbulb", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = lighting.Id },
            new Category { Name = "Smart Bulbs", Slug = "smart-bulbs", IconClass = "fas fa-lightbulb", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = lighting.Id }
        );

        // Textiles types
        db.Categories.AddRange(
            new Category { Name = "Area Rugs", Slug = "area-rugs", IconClass = "fas fa-scroll", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = textiles.Id },
            new Category { Name = "Blackout Curtains", Slug = "blackout-curtains", IconClass = "fas fa-scroll", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = textiles.Id },
            new Category { Name = "Decorative Pillows", Slug = "decorative-pillows", IconClass = "fas fa-scroll", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = textiles.Id },
            new Category { Name = "Nakshi Kantha", Slug = "nakshi-kantha", IconClass = "fas fa-scroll", DisplayOrder = 4, IsActive = true, IsFeatured = true, Level = 3, ParentId = textiles.Id }
        );
        await db.SaveChangesAsync();

        // ===========================================
        // 4. BEAUTY & HEALTH
        // ===========================================
        var beautyHealth = CreateCat("Beauty & Health", "Beauty products, cosmetics, and health essentials", "fas fa-spa", 4, true);
        db.Categories.Add(beautyHealth);
        await db.SaveChangesAsync();

        var personalCare = new Category { Name = "Personal Care", Slug = "personal-care", IconClass = "fas fa-shower", DisplayOrder = 1, IsActive = true, Level = 1, ParentId = beautyHealth.Id };
        var cosmetics = new Category { Name = "Cosmetics", Slug = "cosmetics", IconClass = "fas fa-magic", DisplayOrder = 2, IsActive = true, Level = 1, ParentId = beautyHealth.Id };
        var healthWellness = new Category { Name = "Health & Wellness", Slug = "health-wellness", IconClass = "fas fa-heartbeat", DisplayOrder = 3, IsActive = true, Level = 1, ParentId = beautyHealth.Id };
        db.Categories.AddRange(personalCare, cosmetics, healthWellness);
        await db.SaveChangesAsync();

        // Personal Care subcategories
        var skincare = new Category { Name = "Skincare", Slug = "skincare", IconClass = "fas fa-spa", DisplayOrder = 1, IsActive = true, Level = 2, ParentId = personalCare.Id };
        var hairCare = new Category { Name = "Hair Care", Slug = "hair-care", IconClass = "fas fa-cut", DisplayOrder = 2, IsActive = true, Level = 2, ParentId = personalCare.Id };
        var grooming = new Category { Name = "Grooming", Slug = "grooming", IconClass = "fas fa-cut", DisplayOrder = 3, IsActive = true, Level = 2, ParentId = personalCare.Id };
        db.Categories.AddRange(skincare, hairCare, grooming);

        // Cosmetics subcategories
        var faceMakeup = new Category { Name = "Face Makeup", Slug = "face-makeup", IconClass = "fas fa-paint-brush", DisplayOrder = 1, IsActive = true, Level = 2, ParentId = cosmetics.Id };
        var eyesLips = new Category { Name = "Eyes & Lips", Slug = "eyes-lips", IconClass = "fas fa-eye", DisplayOrder = 2, IsActive = true, Level = 2, ParentId = cosmetics.Id };
        db.Categories.AddRange(faceMakeup, eyesLips);

        // Health & Wellness subcategories
        var supplements = new Category { Name = "Supplements", Slug = "supplements", IconClass = "fas fa-pills", DisplayOrder = 1, IsActive = true, Level = 2, ParentId = healthWellness.Id };
        var medicalSupplies = new Category { Name = "Medical Supplies", Slug = "medical-supplies", IconClass = "fas fa-first-aid", DisplayOrder = 2, IsActive = true, Level = 2, ParentId = healthWellness.Id };
        db.Categories.AddRange(supplements, medicalSupplies);
        await db.SaveChangesAsync();

        // Skincare types
        db.Categories.AddRange(
            new Category { Name = "Face Serums", Slug = "face-serums", IconClass = "fas fa-tint", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = skincare.Id },
            new Category { Name = "Moisturizers", Slug = "moisturizers", IconClass = "fas fa-tint", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = skincare.Id },
            new Category { Name = "Cleansers", Slug = "cleansers", IconClass = "fas fa-soap", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = skincare.Id },
            new Category { Name = "Sheet Masks", Slug = "sheet-masks", IconClass = "fas fa-mask", DisplayOrder = 4, IsActive = true, Level = 3, ParentId = skincare.Id },
            new Category { Name = "Sunscreen", Slug = "sunscreen", IconClass = "fas fa-sun", DisplayOrder = 5, IsActive = true, Level = 3, ParentId = skincare.Id }
        );

        // Hair Care types
        db.Categories.AddRange(
            new Category { Name = "Shampoos", Slug = "shampoos", IconClass = "fas fa-pump-soap", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = hairCare.Id },
            new Category { Name = "Conditioners", Slug = "conditioners", IconClass = "fas fa-pump-soap", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = hairCare.Id },
            new Category { Name = "Hair Oils", Slug = "hair-oils", IconClass = "fas fa-tint", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = hairCare.Id },
            new Category { Name = "Hair Stylers", Slug = "hair-stylers", IconClass = "fas fa-wind", DisplayOrder = 4, IsActive = true, Level = 3, ParentId = hairCare.Id },
            new Category { Name = "Hair Dryers", Slug = "hair-dryers", IconClass = "fas fa-wind", DisplayOrder = 5, IsActive = true, Level = 3, ParentId = hairCare.Id }
        );

        // Grooming types
        db.Categories.AddRange(
            new Category { Name = "Electric Shavers", Slug = "electric-shavers", IconClass = "fas fa-cut", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = grooming.Id },
            new Category { Name = "Beard Trimmers", Slug = "beard-trimmers", IconClass = "fas fa-cut", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = grooming.Id },
            new Category { Name = "Epilators", Slug = "epilators", IconClass = "fas fa-cut", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = grooming.Id }
        );

        // Face Makeup types
        db.Categories.AddRange(
            new Category { Name = "Foundations", Slug = "foundations", IconClass = "fas fa-paint-brush", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = faceMakeup.Id },
            new Category { Name = "Concealers", Slug = "concealers", IconClass = "fas fa-paint-brush", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = faceMakeup.Id },
            new Category { Name = "Primers", Slug = "primers", IconClass = "fas fa-paint-brush", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = faceMakeup.Id },
            new Category { Name = "Setting Sprays", Slug = "setting-sprays", IconClass = "fas fa-spray-can", DisplayOrder = 4, IsActive = true, Level = 3, ParentId = faceMakeup.Id }
        );

        // Eyes & Lips types
        db.Categories.AddRange(
            new Category { Name = "Eyeshadow Palettes", Slug = "eyeshadow-palettes", IconClass = "fas fa-palette", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = eyesLips.Id },
            new Category { Name = "Mascara", Slug = "mascara", IconClass = "fas fa-eye", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = eyesLips.Id },
            new Category { Name = "Lipsticks", Slug = "lipsticks", IconClass = "fas fa-kiss", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = eyesLips.Id },
            new Category { Name = "Eyebrow Pencils", Slug = "eyebrow-pencils", IconClass = "fas fa-pencil-alt", DisplayOrder = 4, IsActive = true, Level = 3, ParentId = eyesLips.Id }
        );

        // Supplements types
        db.Categories.AddRange(
            new Category { Name = "Whey Protein", Slug = "whey-protein", IconClass = "fas fa-dumbbell", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = supplements.Id },
            new Category { Name = "Multivitamins", Slug = "multivitamins", IconClass = "fas fa-pills", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = supplements.Id },
            new Category { Name = "Collagen", Slug = "collagen", IconClass = "fas fa-pills", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = supplements.Id },
            new Category { Name = "Probiotics", Slug = "probiotics", IconClass = "fas fa-pills", DisplayOrder = 4, IsActive = true, Level = 3, ParentId = supplements.Id }
        );

        // Medical Supplies types
        db.Categories.AddRange(
            new Category { Name = "First Aid Kits", Slug = "first-aid-kits", IconClass = "fas fa-first-aid", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = medicalSupplies.Id },
            new Category { Name = "Blood Pressure Monitors", Slug = "blood-pressure-monitors", IconClass = "fas fa-heartbeat", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = medicalSupplies.Id },
            new Category { Name = "Face Masks", Slug = "face-masks-medical", IconClass = "fas fa-mask", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = medicalSupplies.Id }
        );
        await db.SaveChangesAsync();

        // ===========================================
        // 5. SPORTS & OUTDOORS
        // ===========================================
        var sportsOutdoors = CreateCat("Sports & Outdoors", "Sports equipment and outdoor gear", "fas fa-football-ball", 5, true);
        db.Categories.Add(sportsOutdoors);
        await db.SaveChangesAsync();

        var fitnessExercise = new Category { Name = "Fitness & Exercise", Slug = "fitness-exercise", IconClass = "fas fa-dumbbell", DisplayOrder = 1, IsActive = true, Level = 1, ParentId = sportsOutdoors.Id };
        var outdoorAdventure = new Category { Name = "Outdoor Adventure", Slug = "outdoor-adventure", IconClass = "fas fa-mountain", DisplayOrder = 2, IsActive = true, Level = 1, ParentId = sportsOutdoors.Id };
        var teamSports = new Category { Name = "Team Sports", Slug = "team-sports", IconClass = "fas fa-users", DisplayOrder = 3, IsActive = true, Level = 1, ParentId = sportsOutdoors.Id };
        db.Categories.AddRange(fitnessExercise, outdoorAdventure, teamSports);
        await db.SaveChangesAsync();

        // Fitness subcategories
        var cardio = new Category { Name = "Cardio", Slug = "cardio", IconClass = "fas fa-heartbeat", DisplayOrder = 1, IsActive = true, Level = 2, ParentId = fitnessExercise.Id };
        var strength = new Category { Name = "Strength", Slug = "strength", IconClass = "fas fa-dumbbell", DisplayOrder = 2, IsActive = true, Level = 2, ParentId = fitnessExercise.Id };
        db.Categories.AddRange(cardio, strength);

        // Outdoor Adventure subcategories
        var camping = new Category { Name = "Camping", Slug = "camping", IconClass = "fas fa-campground", DisplayOrder = 1, IsActive = true, Level = 2, ParentId = outdoorAdventure.Id };
        var hiking = new Category { Name = "Hiking", Slug = "hiking", IconClass = "fas fa-hiking", DisplayOrder = 2, IsActive = true, Level = 2, ParentId = outdoorAdventure.Id };
        db.Categories.AddRange(camping, hiking);

        // Team Sports subcategories
        var ballSports = new Category { Name = "Ball Sports", Slug = "ball-sports", IconClass = "fas fa-basketball-ball", DisplayOrder = 1, IsActive = true, Level = 2, ParentId = teamSports.Id };
        var waterSports = new Category { Name = "Water Sports", Slug = "water-sports", IconClass = "fas fa-swimmer", DisplayOrder = 2, IsActive = true, Level = 2, ParentId = teamSports.Id };
        db.Categories.AddRange(ballSports, waterSports);
        await db.SaveChangesAsync();

        // Cardio types
        db.Categories.AddRange(
            new Category { Name = "Treadmills", Slug = "treadmills", IconClass = "fas fa-running", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = cardio.Id },
            new Category { Name = "Exercise Bikes", Slug = "exercise-bikes", IconClass = "fas fa-biking", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = cardio.Id },
            new Category { Name = "Rowing Machines", Slug = "rowing-machines", IconClass = "fas fa-water", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = cardio.Id }
        );

        // Strength types
        db.Categories.AddRange(
            new Category { Name = "Dumbbells", Slug = "dumbbells", IconClass = "fas fa-dumbbell", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = strength.Id },
            new Category { Name = "Kettlebells", Slug = "kettlebells", IconClass = "fas fa-dumbbell", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = strength.Id },
            new Category { Name = "Power Racks", Slug = "power-racks", IconClass = "fas fa-dumbbell", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = strength.Id },
            new Category { Name = "Resistance Bands", Slug = "resistance-bands", IconClass = "fas fa-dumbbell", DisplayOrder = 4, IsActive = true, Level = 3, ParentId = strength.Id }
        );

        // Camping types
        db.Categories.AddRange(
            new Category { Name = "Tents", Slug = "tents", IconClass = "fas fa-campground", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = camping.Id },
            new Category { Name = "Sleeping Bags", Slug = "sleeping-bags", IconClass = "fas fa-bed", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = camping.Id },
            new Category { Name = "Camping Stoves", Slug = "camping-stoves", IconClass = "fas fa-fire", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = camping.Id },
            new Category { Name = "Lanterns", Slug = "lanterns", IconClass = "fas fa-lightbulb", DisplayOrder = 4, IsActive = true, Level = 3, ParentId = camping.Id }
        );

        // Hiking types
        db.Categories.AddRange(
            new Category { Name = "Hiking Boots", Slug = "hiking-boots", IconClass = "fas fa-shoe-prints", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = hiking.Id },
            new Category { Name = "Hydration Packs", Slug = "hydration-packs", IconClass = "fas fa-tint", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = hiking.Id },
            new Category { Name = "Trekking Poles", Slug = "trekking-poles", IconClass = "fas fa-hiking", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = hiking.Id }
        );

        // Ball Sports types
        db.Categories.AddRange(
            new Category { Name = "Soccer Balls", Slug = "soccer-balls", IconClass = "fas fa-futbol", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = ballSports.Id },
            new Category { Name = "Basketball Hoops", Slug = "basketball-hoops", IconClass = "fas fa-basketball-ball", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = ballSports.Id },
            new Category { Name = "Tennis Rackets", Slug = "tennis-rackets", IconClass = "fas fa-table-tennis", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = ballSports.Id },
            new Category { Name = "Cricket Equipment", Slug = "cricket-equipment", IconClass = "fas fa-baseball-ball", DisplayOrder = 4, IsActive = true, Level = 3, ParentId = ballSports.Id }
        );

        // Water Sports types
        db.Categories.AddRange(
            new Category { Name = "Kayaks", Slug = "kayaks", IconClass = "fas fa-water", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = waterSports.Id },
            new Category { Name = "Paddleboards", Slug = "paddleboards", IconClass = "fas fa-water", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = waterSports.Id },
            new Category { Name = "Swim Goggles", Slug = "swim-goggles", IconClass = "fas fa-swimmer", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = waterSports.Id }
        );
        await db.SaveChangesAsync();

        // ===========================================
        // 6. TOYS, KIDS & HOBBIES
        // ===========================================
        var toysHobbies = CreateCat("Toys, Kids & Hobbies", "Toys, games, and hobby supplies", "fas fa-puzzle-piece", 6, true);
        db.Categories.Add(toysHobbies);
        await db.SaveChangesAsync();

        var toysGames = new Category { Name = "Toys & Games", Slug = "toys-games", IconClass = "fas fa-gamepad", DisplayOrder = 1, IsActive = true, Level = 1, ParentId = toysHobbies.Id };
        var hobbiesCollectibles = new Category { Name = "Hobbies & Collectibles", Slug = "hobbies-collectibles", IconClass = "fas fa-palette", DisplayOrder = 2, IsActive = true, Level = 1, ParentId = toysHobbies.Id };
        db.Categories.AddRange(toysGames, hobbiesCollectibles);
        await db.SaveChangesAsync();

        // Toys & Games subcategories
        var learning = new Category { Name = "Learning", Slug = "learning-toys", IconClass = "fas fa-graduation-cap", DisplayOrder = 1, IsActive = true, Level = 2, ParentId = toysGames.Id };
        var play = new Category { Name = "Play", Slug = "play-toys", IconClass = "fas fa-puzzle-piece", DisplayOrder = 2, IsActive = true, Level = 2, ParentId = toysGames.Id };
        db.Categories.AddRange(learning, play);

        // Hobbies & Collectibles subcategories
        var artsCrafts = new Category { Name = "Arts & Crafts", Slug = "arts-crafts", IconClass = "fas fa-paint-brush", DisplayOrder = 1, IsActive = true, Level = 2, ParentId = hobbiesCollectibles.Id };
        var instruments = new Category { Name = "Instruments", Slug = "instruments", IconClass = "fas fa-guitar", DisplayOrder = 2, IsActive = true, Level = 2, ParentId = hobbiesCollectibles.Id };
        db.Categories.AddRange(artsCrafts, instruments);
        await db.SaveChangesAsync();

        // Learning types
        db.Categories.AddRange(
            new Category { Name = "STEM Kits", Slug = "stem-kits", IconClass = "fas fa-flask", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = learning.Id },
            new Category { Name = "Building Blocks (LEGO)", Slug = "building-blocks-lego", IconClass = "fas fa-cubes", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = learning.Id },
            new Category { Name = "Puzzles", Slug = "puzzles", IconClass = "fas fa-puzzle-piece", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = learning.Id }
        );

        // Play types
        db.Categories.AddRange(
            new Category { Name = "Action Figures", Slug = "action-figures", IconClass = "fas fa-robot", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = play.Id },
            new Category { Name = "Dolls", Slug = "dolls", IconClass = "fas fa-child", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = play.Id },
            new Category { Name = "Remote Control Cars", Slug = "remote-control-cars", IconClass = "fas fa-car", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = play.Id }
        );

        // Arts & Crafts types
        db.Categories.AddRange(
            new Category { Name = "Oil Paints", Slug = "oil-paints", IconClass = "fas fa-palette", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = artsCrafts.Id },
            new Category { Name = "Sketchbooks", Slug = "sketchbooks", IconClass = "fas fa-book", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = artsCrafts.Id },
            new Category { Name = "Sewing Machines", Slug = "sewing-machines", IconClass = "fas fa-cut", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = artsCrafts.Id },
            new Category { Name = "Yarn", Slug = "yarn", IconClass = "fas fa-circle", DisplayOrder = 4, IsActive = true, Level = 3, ParentId = artsCrafts.Id }
        );

        // Instruments types
        db.Categories.AddRange(
            new Category { Name = "Acoustic Guitars", Slug = "acoustic-guitars", IconClass = "fas fa-guitar", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = instruments.Id },
            new Category { Name = "Digital Pianos", Slug = "digital-pianos", IconClass = "fas fa-music", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = instruments.Id },
            new Category { Name = "Drum Kits", Slug = "drum-kits", IconClass = "fas fa-drum", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = instruments.Id },
            new Category { Name = "Tabla", Slug = "tabla", IconClass = "fas fa-drum", DisplayOrder = 4, IsActive = true, Level = 3, ParentId = instruments.Id },
            new Category { Name = "Harmonium", Slug = "harmonium", IconClass = "fas fa-music", DisplayOrder = 5, IsActive = true, Level = 3, ParentId = instruments.Id }
        );
        await db.SaveChangesAsync();

        // ===========================================
        // 7. GROCERIES & HOUSEHOLD
        // ===========================================
        var groceriesHousehold = CreateCat("Groceries & Household", "Food, beverages, and household supplies", "fas fa-shopping-basket", 7, true);
        db.Categories.Add(groceriesHousehold);
        await db.SaveChangesAsync();

        var freshFrozen = new Category { Name = "Fresh & Frozen", Slug = "fresh-frozen", IconClass = "fas fa-snowflake", DisplayOrder = 1, IsActive = true, Level = 1, ParentId = groceriesHousehold.Id };
        var pantryEssentials = new Category { Name = "Pantry Essentials", Slug = "pantry-essentials", IconClass = "fas fa-archive", DisplayOrder = 2, IsActive = true, Level = 1, ParentId = groceriesHousehold.Id };
        var householdSupplies = new Category { Name = "Household Supplies", Slug = "household-supplies", IconClass = "fas fa-broom", DisplayOrder = 3, IsActive = true, Level = 1, ParentId = groceriesHousehold.Id };
        db.Categories.AddRange(freshFrozen, pantryEssentials, householdSupplies);
        await db.SaveChangesAsync();

        // Fresh & Frozen subcategories
        var produce = new Category { Name = "Produce", Slug = "produce", IconClass = "fas fa-carrot", DisplayOrder = 1, IsActive = true, Level = 2, ParentId = freshFrozen.Id };
        var meatDairy = new Category { Name = "Meat & Dairy", Slug = "meat-dairy", IconClass = "fas fa-drumstick-bite", DisplayOrder = 2, IsActive = true, Level = 2, ParentId = freshFrozen.Id };
        db.Categories.AddRange(produce, meatDairy);

        // Pantry Essentials subcategories
        var snacks = new Category { Name = "Snacks", Slug = "snacks", IconClass = "fas fa-cookie", DisplayOrder = 1, IsActive = true, Level = 2, ParentId = pantryEssentials.Id };
        var beverages = new Category { Name = "Beverages", Slug = "beverages", IconClass = "fas fa-coffee", DisplayOrder = 2, IsActive = true, Level = 2, ParentId = pantryEssentials.Id };
        db.Categories.AddRange(snacks, beverages);

        // Household Supplies subcategories
        var cleaning = new Category { Name = "Cleaning", Slug = "cleaning", IconClass = "fas fa-broom", DisplayOrder = 1, IsActive = true, Level = 2, ParentId = householdSupplies.Id };
        var paperPlastic = new Category { Name = "Paper & Plastic", Slug = "paper-plastic", IconClass = "fas fa-toilet-paper", DisplayOrder = 2, IsActive = true, Level = 2, ParentId = householdSupplies.Id };
        db.Categories.AddRange(cleaning, paperPlastic);
        await db.SaveChangesAsync();

        // Produce types
        db.Categories.AddRange(
            new Category { Name = "Organic Vegetables", Slug = "organic-vegetables", IconClass = "fas fa-carrot", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = produce.Id },
            new Category { Name = "Seasonal Fruits", Slug = "seasonal-fruits", IconClass = "fas fa-apple-alt", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = produce.Id }
        );

        // Meat & Dairy types
        db.Categories.AddRange(
            new Category { Name = "Poultry", Slug = "poultry", IconClass = "fas fa-drumstick-bite", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = meatDairy.Id },
            new Category { Name = "Beef", Slug = "beef", IconClass = "fas fa-drumstick-bite", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = meatDairy.Id },
            new Category { Name = "Milk", Slug = "milk", IconClass = "fas fa-wine-bottle", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = meatDairy.Id },
            new Category { Name = "Greek Yogurt", Slug = "greek-yogurt", IconClass = "fas fa-cheese", DisplayOrder = 4, IsActive = true, Level = 3, ParentId = meatDairy.Id },
            new Category { Name = "Artisanal Cheese", Slug = "artisanal-cheese", IconClass = "fas fa-cheese", DisplayOrder = 5, IsActive = true, Level = 3, ParentId = meatDairy.Id },
            new Category { Name = "Fish & Seafood", Slug = "fish-seafood", IconClass = "fas fa-fish", DisplayOrder = 6, IsActive = true, Level = 3, ParentId = meatDairy.Id }
        );

        // Snacks types
        db.Categories.AddRange(
            new Category { Name = "Chips", Slug = "chips", IconClass = "fas fa-cookie", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = snacks.Id },
            new Category { Name = "Granola Bars", Slug = "granola-bars", IconClass = "fas fa-cookie", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = snacks.Id },
            new Category { Name = "Gourmet Chocolates", Slug = "gourmet-chocolates", IconClass = "fas fa-cookie-bite", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = snacks.Id },
            new Category { Name = "Traditional Sweets", Slug = "traditional-sweets", IconClass = "fas fa-cookie-bite", DisplayOrder = 4, IsActive = true, Level = 3, ParentId = snacks.Id }
        );

        // Beverages types
        db.Categories.AddRange(
            new Category { Name = "Coffee Beans", Slug = "coffee-beans", IconClass = "fas fa-coffee", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = beverages.Id },
            new Category { Name = "Specialty Teas", Slug = "specialty-teas", IconClass = "fas fa-mug-hot", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = beverages.Id },
            new Category { Name = "Sparkling Water", Slug = "sparkling-water", IconClass = "fas fa-tint", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = beverages.Id },
            new Category { Name = "Juices", Slug = "juices", IconClass = "fas fa-glass-whiskey", DisplayOrder = 4, IsActive = true, Level = 3, ParentId = beverages.Id }
        );

        // Cleaning types
        db.Categories.AddRange(
            new Category { Name = "Laundry Pods", Slug = "laundry-pods", IconClass = "fas fa-soap", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = cleaning.Id },
            new Category { Name = "Surface Cleaners", Slug = "surface-cleaners", IconClass = "fas fa-spray-can", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = cleaning.Id },
            new Category { Name = "Vacuum Cleaners", Slug = "vacuum-cleaners", IconClass = "fas fa-broom", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = cleaning.Id }
        );

        // Paper & Plastic types
        db.Categories.AddRange(
            new Category { Name = "Toilet Paper", Slug = "toilet-paper", IconClass = "fas fa-toilet-paper", DisplayOrder = 1, IsActive = true, Level = 3, ParentId = paperPlastic.Id },
            new Category { Name = "Trash Bags", Slug = "trash-bags", IconClass = "fas fa-trash", DisplayOrder = 2, IsActive = true, Level = 3, ParentId = paperPlastic.Id },
            new Category { Name = "Storage Containers", Slug = "storage-containers", IconClass = "fas fa-box", DisplayOrder = 3, IsActive = true, Level = 3, ParentId = paperPlastic.Id }
        );
        await db.SaveChangesAsync();
    }
}

// Seed business types for seller applications
public static class SeedBusinessTypes
{
    public static async Task InitializeAsync(ApplicationDbContext db)
    {
        // Check if business types already exist
        if (await db.BusinessTypes.AnyAsync())
        {
            return;
        }

        var businessTypes = new List<BusinessType>
        {
            new() { Name = "Fashion & Clothing", Description = "Clothing, apparel, and fashion accessories", IconClass = "fas fa-tshirt", DisplayOrder = 1, IsActive = true },
            new() { Name = "Electronics & Gadgets", Description = "Electronic devices, gadgets, and accessories", IconClass = "fas fa-laptop", DisplayOrder = 2, IsActive = true },
            new() { Name = "Groceries & Food", Description = "Food items, groceries, and beverages", IconClass = "fas fa-shopping-basket", DisplayOrder = 3, IsActive = true },
            new() { Name = "Handicrafts & Artisan", Description = "Handmade products, crafts, and artisan goods", IconClass = "fas fa-paint-brush", DisplayOrder = 4, IsActive = true },
            new() { Name = "Home & Decor", Description = "Home decoration, furniture, and household items", IconClass = "fas fa-home", DisplayOrder = 5, IsActive = true },
            new() { Name = "Beauty & Personal Care", Description = "Cosmetics, skincare, and personal care products", IconClass = "fas fa-spa", DisplayOrder = 6, IsActive = true },
            new() { Name = "Books & Stationery", Description = "Books, educational materials, and office supplies", IconClass = "fas fa-book", DisplayOrder = 7, IsActive = true },
            new() { Name = "Sports & Fitness", Description = "Sports equipment, fitness gear, and outdoor items", IconClass = "fas fa-running", DisplayOrder = 8, IsActive = true },
            new() { Name = "Jewelry & Accessories", Description = "Jewelry, watches, and fashion accessories", IconClass = "fas fa-gem", DisplayOrder = 9, IsActive = true },
            new() { Name = "Health & Wellness", Description = "Health products, supplements, and wellness items", IconClass = "fas fa-heartbeat", DisplayOrder = 10, IsActive = true },
            new() { Name = "Services", Description = "Professional services and consultations", IconClass = "fas fa-concierge-bell", DisplayOrder = 11, IsActive = true },
            new() { Name = "Other", Description = "Other types of businesses", IconClass = "fas fa-ellipsis-h", DisplayOrder = 99, IsActive = true }
        };

        db.BusinessTypes.AddRange(businessTypes);
        await db.SaveChangesAsync();
    }
}

public static class SeedBlogContent
{
    public static async Task InitializeAsync(ApplicationDbContext db)
    {
        // Seed Blog Categories if none exist
        if (!await db.BlogCategories.AnyAsync())
        {
            var blogCategories = new List<BlogCategory>
            {
                new() { Name = "Fashion Tips", Slug = "fashion-tips", Description = "Latest fashion tips and styling advice", IconClass = "fas fa-lightbulb", DisplayOrder = 1, IsActive = true },
                new() { Name = "Style Trends", Slug = "style-trends", Description = "Current fashion trends and what's in style", IconClass = "fas fa-fire", DisplayOrder = 2, IsActive = true },
                new() { Name = "Color Guide", Slug = "color-guide", Description = "Color matching and combination tips", IconClass = "fas fa-palette", DisplayOrder = 3, IsActive = true },
                new() { Name = "Occasion Wear", Slug = "occasion-wear", Description = "What to wear for different occasions", IconClass = "fas fa-calendar-alt", DisplayOrder = 4, IsActive = true },
                new() { Name = "Traditional Wear", Slug = "traditional-wear", Description = "Traditional Bengali and South Asian fashion", IconClass = "fas fa-star", DisplayOrder = 5, IsActive = true },
                new() { Name = "Lifestyle", Slug = "lifestyle", Description = "Lifestyle and fashion inspiration", IconClass = "fas fa-heart", DisplayOrder = 6, IsActive = true }
            };
            db.BlogCategories.AddRange(blogCategories);
            await db.SaveChangesAsync();
        }

        // Seed Blog Posts if none exist
        if (!await db.BlogPosts.AnyAsync())
        {
            var categories = await db.BlogCategories.ToListAsync();
            var fashionTips = categories.FirstOrDefault(c => c.Slug == "fashion-tips");
            var styleTrends = categories.FirstOrDefault(c => c.Slug == "style-trends");
            var colorGuide = categories.FirstOrDefault(c => c.Slug == "color-guide");
            var occasionWear = categories.FirstOrDefault(c => c.Slug == "occasion-wear");
            var traditionalWear = categories.FirstOrDefault(c => c.Slug == "traditional-wear");
            var lifestyle = categories.FirstOrDefault(c => c.Slug == "lifestyle");

            var blogPosts = new List<BlogPost>
            {
                new()
                {
                    Title = "10 Essential Fashion Tips for Every Wardrobe",
                    Slug = "10-essential-fashion-tips-for-every-wardrobe",
                    Excerpt = "Discover the top 10 fashion tips that will transform your wardrobe and elevate your style game instantly.",
                    Content = "<h2>Build a Solid Foundation</h2><p>A great wardrobe starts with versatile basics. Invest in quality pieces that can be mixed and matched.</p><h2>Know Your Colors</h2><p>Understanding which colors complement your skin tone is essential for looking your best.</p><h2>Fit is Everything</h2><p>No matter how expensive or trendy a piece is, it won't look good if it doesn't fit properly.</p><h2>Quality Over Quantity</h2><p>It's better to have fewer high-quality pieces than a closet full of items you never wear.</p><h2>Accessorize Wisely</h2><p>The right accessories can transform a simple outfit into something special.</p>",
                    AuthorName = "Bangaliyana Team",
                    BlogCategoryId = fashionTips?.Id,
                    Tags = "fashion tips, wardrobe essentials, style basics",
                    MetaDescription = "Learn 10 essential fashion tips to build a versatile and stylish wardrobe.",
                    IsActive = true,
                    IsFeatured = true,
                    ViewCount = 150,
                    DisplayOrder = 1,
                    PublishedAt = DateTime.UtcNow.AddDays(-5)
                },
                new()
                {
                    Title = "Trending Colors for This Season",
                    Slug = "trending-colors-for-this-season",
                    Excerpt = "Stay ahead of the fashion curve with our guide to the hottest colors trending this season.",
                    Content = "<h2>Earth Tones are In</h2><p>This season, earth tones like terracotta, olive green, and warm browns are dominating the fashion scene.</p><h2>Pop of Bright</h2><p>Don't be afraid to add a pop of bright color with accessories or statement pieces.</p><h2>Classic Neutrals</h2><p>Neutrals like beige, cream, and soft grey remain timeless choices that work with everything.</p>",
                    AuthorName = "Bangaliyana Team",
                    BlogCategoryId = styleTrends?.Id,
                    Tags = "color trends, seasonal fashion, style guide",
                    MetaDescription = "Discover the trending colors for this season and how to incorporate them into your wardrobe.",
                    IsActive = true,
                    IsFeatured = true,
                    ViewCount = 120,
                    DisplayOrder = 2,
                    PublishedAt = DateTime.UtcNow.AddDays(-3)
                },
                new()
                {
                    Title = "How to Mix and Match Colors Like a Pro",
                    Slug = "how-to-mix-and-match-colors-like-a-pro",
                    Excerpt = "Master the art of color coordination with these expert tips on mixing and matching colors.",
                    Content = "<h2>Understanding the Color Wheel</h2><p>The color wheel is your best friend when it comes to creating harmonious outfits.</p><h2>Complementary Colors</h2><p>Colors opposite each other on the wheel create bold, striking combinations.</p><h2>Analogous Colors</h2><p>Colors next to each other create a more subtle, sophisticated look.</p><h2>The 60-30-10 Rule</h2><p>Use 60% dominant color, 30% secondary color, and 10% accent color for balanced outfits.</p>",
                    AuthorName = "Bangaliyana Team",
                    BlogCategoryId = colorGuide?.Id,
                    Tags = "color matching, styling tips, fashion advice",
                    MetaDescription = "Learn professional techniques for mixing and matching colors in your outfits.",
                    IsActive = true,
                    IsFeatured = false,
                    ViewCount = 95,
                    DisplayOrder = 3,
                    PublishedAt = DateTime.UtcNow.AddDays(-7)
                },
                new()
                {
                    Title = "Wedding Guest Outfit Ideas",
                    Slug = "wedding-guest-outfit-ideas",
                    Excerpt = "Looking for the perfect wedding guest outfit? Here are our top picks and styling ideas.",
                    Content = "<h2>Know the Dress Code</h2><p>Always check the wedding invitation for dress code guidelines before planning your outfit.</p><h2>Traditional Options</h2><p>Sarees and lehengas are always elegant choices for traditional ceremonies.</p><h2>Contemporary Choices</h2><p>For more modern celebrations, consider stylish fusion wear or elegant dresses.</p><h2>Accessorize Thoughtfully</h2><p>Complete your look with appropriate jewelry, bags, and footwear.</p>",
                    AuthorName = "Bangaliyana Team",
                    BlogCategoryId = occasionWear?.Id,
                    Tags = "wedding outfit, occasion wear, party fashion",
                    MetaDescription = "Find the perfect wedding guest outfit with our comprehensive guide and styling ideas.",
                    IsActive = true,
                    IsFeatured = false,
                    ViewCount = 200,
                    DisplayOrder = 4,
                    PublishedAt = DateTime.UtcNow.AddDays(-10)
                },
                new()
                {
                    Title = "The Art of Draping a Saree",
                    Slug = "the-art-of-draping-a-saree",
                    Excerpt = "Learn different saree draping styles and find the one that suits you best.",
                    Content = "<h2>The Classic Nivi Style</h2><p>The most popular draping style, originating from Andhra Pradesh, is elegant and versatile.</p><h2>Bengali Style</h2><p>Known for its distinctive pleating pattern and the way the pallu is worn over the shoulder.</p><h2>Gujarati Style</h2><p>The pallu is brought from behind over the right shoulder, creating a unique front display.</p><h2>Modern Draping</h2><p>Experiment with contemporary draping styles for a fusion look.</p>",
                    AuthorName = "Bangaliyana Team",
                    BlogCategoryId = traditionalWear?.Id,
                    Tags = "saree draping, traditional wear, Bengali fashion",
                    MetaDescription = "Master different saree draping styles with our comprehensive guide.",
                    IsActive = true,
                    IsFeatured = true,
                    ViewCount = 180,
                    DisplayOrder = 5,
                    PublishedAt = DateTime.UtcNow.AddDays(-2)
                },
                new()
                {
                    Title = "Building a Capsule Wardrobe",
                    Slug = "building-a-capsule-wardrobe",
                    Excerpt = "Simplify your life with a capsule wardrobe that maximizes style and minimizes clutter.",
                    Content = "<h2>What is a Capsule Wardrobe?</h2><p>A capsule wardrobe is a collection of essential, timeless pieces that can be mixed and matched.</p><h2>The Benefits</h2><p>Less decision fatigue, more sustainable shopping habits, and a more cohesive style.</p><h2>Essential Pieces</h2><p>Start with basics like well-fitted jeans, white shirts, a blazer, and versatile dresses.</p><h2>Adding Personality</h2><p>Include a few statement pieces that reflect your personal style.</p>",
                    AuthorName = "Bangaliyana Team",
                    BlogCategoryId = lifestyle?.Id,
                    Tags = "capsule wardrobe, minimalism, sustainable fashion",
                    MetaDescription = "Learn how to build a capsule wardrobe for a simplified, stylish life.",
                    IsActive = true,
                    IsFeatured = false,
                    ViewCount = 110,
                    DisplayOrder = 6,
                    PublishedAt = DateTime.UtcNow.AddDays(-15)
                }
            };
            db.BlogPosts.AddRange(blogPosts);
            await db.SaveChangesAsync();
        }

        // Seed Styling Guides if none exist
        if (!await db.StylingGuides.AnyAsync())
        {
            var stylingGuides = new List<StylingGuide>
            {
                new()
                {
                    Title = "Color Matching Guide for Beginners",
                    Slug = "color-matching-guide-for-beginners",
                    Subtitle = "Master the basics of color coordination",
                    Content = "<h2>Understanding Color Theory</h2><p>Color theory is the foundation of great fashion choices. Learn about primary, secondary, and tertiary colors.</p><h2>Warm vs Cool Tones</h2><p>Identify whether you have warm or cool undertones to choose flattering colors.</p><h2>Creating Harmony</h2><p>Use the color wheel to create harmonious outfit combinations.</p>",
                    GuideType = StylingGuideType.ColorMatching,
                    TargetGender = "All",
                    Occasion = "Everyday",
                    Season = "All Season",
                    MetaDescription = "Learn the basics of color matching for fashion with this beginner's guide.",
                    IsActive = true,
                    IsFeatured = true,
                    ViewCount = 250,
                    DisplayOrder = 1
                },
                new()
                {
                    Title = "Dressing for Your Body Type",
                    Slug = "dressing-for-your-body-type",
                    Subtitle = "Find styles that flatter your unique shape",
                    Content = "<h2>Identify Your Body Type</h2><p>Understanding your body shape is the first step to dressing well.</p><h2>Pear Shape</h2><p>Balance wider hips with structured tops and A-line skirts.</p><h2>Apple Shape</h2><p>Create definition at the waist and choose v-necks to elongate.</p><h2>Hourglass</h2><p>Highlight your waist with fitted styles and wrap dresses.</p>",
                    GuideType = StylingGuideType.BodyType,
                    TargetGender = "Women",
                    Occasion = "All Occasions",
                    Season = "All Season",
                    MetaDescription = "Find the most flattering styles for your body type with our comprehensive guide.",
                    IsActive = true,
                    IsFeatured = true,
                    ViewCount = 300,
                    DisplayOrder = 2
                },
                new()
                {
                    Title = "Styling for Eid Celebrations",
                    Slug = "styling-for-eid-celebrations",
                    Subtitle = "Look your best during Eid festivities",
                    Content = "<h2>Traditional Elegance</h2><p>Embrace traditional wear like elegant sarees, lehengas, and panjabis.</p><h2>Color Choices</h2><p>Opt for festive colors like gold, royal blue, maroon, and pastels.</p><h2>Accessorizing</h2><p>Complete your look with traditional jewelry and elegant footwear.</p><h2>Comfort Matters</h2><p>Choose breathable fabrics for long celebration hours.</p>",
                    GuideType = StylingGuideType.OccasionBased,
                    TargetGender = "All",
                    Occasion = "Eid",
                    Season = "All Season",
                    MetaDescription = "Get ready for Eid celebrations with our complete styling guide.",
                    IsActive = true,
                    IsFeatured = true,
                    ViewCount = 180,
                    DisplayOrder = 3
                },
                new()
                {
                    Title = "Summer Fashion Essentials",
                    Slug = "summer-fashion-essentials",
                    Subtitle = "Stay cool and stylish in the heat",
                    Content = "<h2>Fabric Choices</h2><p>Choose breathable fabrics like cotton, linen, and light chiffon.</p><h2>Light Colors</h2><p>Light colors reflect heat and keep you cooler.</p><h2>Must-Have Pieces</h2><p>Stock up on flowy dresses, comfortable shorts, and light kurtas.</p><h2>Sun Protection</h2><p>Don't forget sunglasses and wide-brimmed hats.</p>",
                    GuideType = StylingGuideType.SeasonBased,
                    TargetGender = "All",
                    Occasion = "Casual",
                    Season = "Summer",
                    MetaDescription = "Beat the heat in style with our summer fashion essentials guide.",
                    IsActive = true,
                    IsFeatured = false,
                    ViewCount = 150,
                    DisplayOrder = 4
                },
                new()
                {
                    Title = "Current Fashion Trends to Try",
                    Slug = "current-fashion-trends-to-try",
                    Subtitle = "Stay ahead with these trending styles",
                    Content = "<h2>Oversized Blazers</h2><p>The oversized blazer trend continues to dominate street style.</p><h2>Sustainable Fashion</h2><p>Eco-friendly and sustainable fashion is more popular than ever.</p><h2>Bold Prints</h2><p>Don't shy away from bold patterns and prints this season.</p><h2>Athleisure</h2><p>Comfortable yet stylish athleisure remains a strong trend.</p>",
                    GuideType = StylingGuideType.TrendAlert,
                    TargetGender = "All",
                    Occasion = "Various",
                    Season = "All Season",
                    MetaDescription = "Discover the latest fashion trends and how to incorporate them into your style.",
                    IsActive = true,
                    IsFeatured = false,
                    ViewCount = 200,
                    DisplayOrder = 5
                },
                new()
                {
                    Title = "DIY Saree Styling Tips",
                    Slug = "diy-saree-styling-tips",
                    Subtitle = "Creative ways to style your saree",
                    Content = "<h2>Modern Draping Techniques</h2><p>Learn innovative ways to drape your saree for a contemporary look.</p><h2>Blouse Styling</h2><p>Experiment with different blouse styles to change your saree's entire look.</p><h2>Accessory Ideas</h2><p>Transform your saree look with creative accessory choices.</p><h2>Fusion Ideas</h2><p>Pair your saree with unconventional items like crop tops or jackets.</p>",
                    GuideType = StylingGuideType.DIYStyling,
                    TargetGender = "Women",
                    Occasion = "Various",
                    Season = "All Season",
                    MetaDescription = "Get creative with DIY saree styling tips and transform your traditional wear.",
                    IsActive = true,
                    IsFeatured = false,
                    ViewCount = 175,
                    DisplayOrder = 6
                }
            };
            db.StylingGuides.AddRange(stylingGuides);
            await db.SaveChangesAsync();
        }

        // Seed Seasonal Collections if none exist
        if (!await db.SeasonalCollections.AnyAsync())
        {
            var currentYear = DateTime.UtcNow.Year;
            var seasonalCollections = new List<SeasonalCollection>
            {
                new()
                {
                    Name = "Summer Breeze Collection",
                    Slug = "summer-breeze-collection",
                    Tagline = "Stay cool and stylish this summer",
                    Description = "Discover our curated collection of light, breathable fabrics and vibrant colors perfect for the summer season. From flowy dresses to comfortable kurtas, find everything you need to beat the heat in style.",
                    SeasonType = SeasonType.Summer,
                    Year = currentYear,
                    PrimaryColor = "#f97316",
                    SecondaryColor = "#fed7aa",
                    TextColor = "#ffffff",
                    ButtonText = "Shop Summer",
                    MetaDescription = "Shop our Summer Breeze Collection for stylish, breathable fashion.",
                    DisplayOrder = 1,
                    IsActive = true,
                    IsFeatured = true,
                    StartDate = new DateTime(currentYear, 3, 1),
                    EndDate = new DateTime(currentYear, 6, 30),
                    ShowCountdown = false
                },
                new()
                {
                    Name = "Monsoon Magic Collection",
                    Slug = "monsoon-magic-collection",
                    Tagline = "Embrace the rains in style",
                    Description = "Our Monsoon Magic Collection features waterproof accessories, quick-dry fabrics, and stylish rain wear. Stay fashionable even when it pours!",
                    SeasonType = SeasonType.Monsoon,
                    Year = currentYear,
                    PrimaryColor = "#3b82f6",
                    SecondaryColor = "#bfdbfe",
                    TextColor = "#ffffff",
                    ButtonText = "Shop Monsoon",
                    MetaDescription = "Shop our Monsoon Magic Collection for stylish rain-ready fashion.",
                    DisplayOrder = 2,
                    IsActive = true,
                    IsFeatured = false,
                    StartDate = new DateTime(currentYear, 6, 1),
                    EndDate = new DateTime(currentYear, 9, 30),
                    ShowCountdown = false
                },
                new()
                {
                    Name = "Eid Festive Collection",
                    Slug = "eid-festive-collection",
                    Tagline = "Celebrate Eid in elegance",
                    Description = "Make your Eid special with our exclusive festive collection. From elegant sarees to designer panjabis, find the perfect outfit for your celebrations.",
                    SeasonType = SeasonType.Eid,
                    Year = currentYear,
                    PrimaryColor = "#059669",
                    SecondaryColor = "#d1fae5",
                    TextColor = "#ffffff",
                    ButtonText = "Shop Eid Collection",
                    MetaDescription = "Shop our Eid Festive Collection for elegant celebration wear.",
                    DisplayOrder = 3,
                    IsActive = true,
                    IsFeatured = true,
                    StartDate = DateTime.UtcNow.AddDays(-30),
                    EndDate = DateTime.UtcNow.AddDays(60),
                    ShowCountdown = true
                },
                new()
                {
                    Name = "Puja Special Collection",
                    Slug = "puja-special-collection",
                    Tagline = "Traditional elegance for Puja celebrations",
                    Description = "Our Puja Special Collection brings you the finest traditional wear. Beautiful sarees, elegant jewelry, and festive accessories for your celebrations.",
                    SeasonType = SeasonType.Puja,
                    Year = currentYear,
                    PrimaryColor = "#dc2626",
                    SecondaryColor = "#fecaca",
                    TextColor = "#ffffff",
                    ButtonText = "Shop Puja Collection",
                    MetaDescription = "Shop our Puja Special Collection for traditional celebration wear.",
                    DisplayOrder = 4,
                    IsActive = true,
                    IsFeatured = true,
                    StartDate = new DateTime(currentYear, 9, 1),
                    EndDate = new DateTime(currentYear, 11, 30),
                    ShowCountdown = false
                },
                new()
                {
                    Name = "Winter Warmth Collection",
                    Slug = "winter-warmth-collection",
                    Tagline = "Cozy fashion for cold days",
                    Description = "Stay warm and stylish with our Winter Warmth Collection. Featuring cozy sweaters, elegant shawls, and comfortable winter wear.",
                    SeasonType = SeasonType.Winter,
                    Year = currentYear,
                    PrimaryColor = "#7c3aed",
                    SecondaryColor = "#ddd6fe",
                    TextColor = "#ffffff",
                    ButtonText = "Shop Winter",
                    MetaDescription = "Shop our Winter Warmth Collection for cozy, stylish winter fashion.",
                    DisplayOrder = 5,
                    IsActive = true,
                    IsFeatured = false,
                    StartDate = new DateTime(currentYear, 11, 1),
                    EndDate = new DateTime(currentYear + 1, 2, 28),
                    ShowCountdown = false
                },
                new()
                {
                    Name = "Pohela Boishakh Collection",
                    Slug = "pohela-boishakh-collection",
                    Tagline = "Welcome the Bengali New Year in style",
                    Description = "Celebrate Pohela Boishakh with our exclusive collection of red and white traditional wear. Sarees, panjabis, and accessories for the perfect new year celebration.",
                    SeasonType = SeasonType.PohelaBoishakh,
                    Year = currentYear,
                    PrimaryColor = "#ef4444",
                    SecondaryColor = "#ffffff",
                    TextColor = "#ffffff",
                    ButtonText = "Shop Boishakh Collection",
                    MetaDescription = "Shop our Pohela Boishakh Collection for Bengali New Year celebration wear.",
                    DisplayOrder = 6,
                    IsActive = true,
                    IsFeatured = false,
                    StartDate = new DateTime(currentYear, 3, 15),
                    EndDate = new DateTime(currentYear, 4, 30),
                    ShowCountdown = false
                },
                new()
                {
                    Name = "Wedding Season Collection",
                    Slug = "wedding-season-collection",
                    Tagline = "Stunning outfits for the wedding season",
                    Description = "From bride to guest, find your perfect wedding outfit in our comprehensive Wedding Season Collection. Lehengas, sarees, sherwanis, and more.",
                    SeasonType = SeasonType.Wedding,
                    Year = currentYear,
                    PrimaryColor = "#ec4899",
                    SecondaryColor = "#fbcfe8",
                    TextColor = "#ffffff",
                    ButtonText = "Shop Wedding Collection",
                    MetaDescription = "Shop our Wedding Season Collection for stunning wedding and celebration wear.",
                    DisplayOrder = 7,
                    IsActive = true,
                    IsFeatured = true,
                    StartDate = DateTime.UtcNow.AddDays(-60),
                    EndDate = DateTime.UtcNow.AddDays(120),
                    ShowCountdown = false
                }
            };
            db.SeasonalCollections.AddRange(seasonalCollections);
            await db.SaveChangesAsync();
        }
    }
}

// Seed admin roles with default permissions
public static class SeedAdminRoles
{
    public static async Task InitializeAsync(ApplicationDbContext db)
    {
        // Check if admin roles already exist
        if (await db.AdminRoles.AnyAsync())
        {
            return;
        }

        // Create system roles with default permissions
        var roles = new List<AdminRole>
        {
            // Product Manager - manages products, categories, inventory, reviews
            new AdminRole
            {
                Name = "Product Manager",
                Description = "Manages products, categories, inventory, and reviews",
                IsSystemRole = true,
                Permissions = new List<AdminPermission>
                {
                    new() { Module = "Dashboard", CanView = true },
                    new() { Module = "Products", CanView = true, CanCreate = true, CanEdit = true, CanDelete = true },
                    new() { Module = "Categories", CanView = true, CanCreate = true, CanEdit = true, CanDelete = true },
                    new() { Module = "Inventory", CanView = true, CanCreate = true, CanEdit = true, CanDelete = true },
                    new() { Module = "Reviews", CanView = true, CanEdit = true, CanDelete = true }
                }
            },
            // Order Manager - manages orders, transactions, delivery
            new AdminRole
            {
                Name = "Order Manager",
                Description = "Manages orders, transactions, and delivery charges",
                IsSystemRole = true,
                Permissions = new List<AdminPermission>
                {
                    new() { Module = "Dashboard", CanView = true },
                    new() { Module = "Orders", CanView = true, CanCreate = true, CanEdit = true, CanDelete = false },
                    new() { Module = "Transactions", CanView = true },
                    new() { Module = "DeliveryCharges", CanView = true, CanCreate = true, CanEdit = true, CanDelete = true }
                }
            },
            // Support Team - manages support tickets, chat, contact inquiries
            new AdminRole
            {
                Name = "Support Team",
                Description = "Handles customer support tickets, live chat, and contact inquiries",
                IsSystemRole = true,
                Permissions = new List<AdminPermission>
                {
                    new() { Module = "Dashboard", CanView = true },
                    new() { Module = "SupportTickets", CanView = true, CanCreate = true, CanEdit = true, CanDelete = false },
                    new() { Module = "SupportChat", CanView = true, CanCreate = true, CanEdit = true },
                    new() { Module = "ContactInquiries", CanView = true, CanEdit = true, CanDelete = false },
                    new() { Module = "Orders", CanView = true } // Support needs to view orders for inquiries
                }
            },
            // Content Manager - manages blog, styling guides, site settings
            new AdminRole
            {
                Name = "Content Manager",
                Description = "Manages blog posts, styling guides, seasonal collections, and site content",
                IsSystemRole = true,
                Permissions = new List<AdminPermission>
                {
                    new() { Module = "Dashboard", CanView = true },
                    new() { Module = "Blog", CanView = true, CanCreate = true, CanEdit = true, CanDelete = true },
                    new() { Module = "StylingGuides", CanView = true, CanCreate = true, CanEdit = true, CanDelete = true },
                    new() { Module = "SeasonalCollections", CanView = true, CanCreate = true, CanEdit = true, CanDelete = true },
                    new() { Module = "SiteSettings", CanView = true, CanEdit = true },
                    new() { Module = "MenuManagement", CanView = true, CanCreate = true, CanEdit = true, CanDelete = true }
                }
            },
            // Marketing Manager - manages promotions, coupons, flash sales
            new AdminRole
            {
                Name = "Marketing Manager",
                Description = "Manages promotional campaigns, coupons, flash sales, and category promotions",
                IsSystemRole = true,
                Permissions = new List<AdminPermission>
                {
                    new() { Module = "Dashboard", CanView = true },
                    new() { Module = "Coupons", CanView = true, CanCreate = true, CanEdit = true, CanDelete = true },
                    new() { Module = "FlashSales", CanView = true, CanCreate = true, CanEdit = true, CanDelete = true },
                    new() { Module = "PromotionalCampaigns", CanView = true, CanCreate = true, CanEdit = true, CanDelete = true },
                    new() { Module = "CategoryPromotions", CanView = true, CanCreate = true, CanEdit = true, CanDelete = true },
                    new() { Module = "Reports", CanView = true } // For marketing analytics
                }
            },
            // Seller Coordinator - manages sellers and seller-related operations
            new AdminRole
            {
                Name = "Seller Coordinator",
                Description = "Manages sellers, seller payments, and seller communications",
                IsSystemRole = true,
                Permissions = new List<AdminPermission>
                {
                    new() { Module = "Dashboard", CanView = true },
                    new() { Module = "Sellers", CanView = true, CanCreate = true, CanEdit = true, CanDelete = false },
                    new() { Module = "SellerPayments", CanView = true, CanCreate = true, CanEdit = true },
                    new() { Module = "SellerMessages", CanView = true, CanEdit = true },
                    new() { Module = "Products", CanView = true } // To view seller products
                }
            },
            // Reports Viewer - can only view reports and analytics
            new AdminRole
            {
                Name = "Reports Viewer",
                Description = "View-only access to reports and analytics",
                IsSystemRole = true,
                Permissions = new List<AdminPermission>
                {
                    new() { Module = "Dashboard", CanView = true },
                    new() { Module = "Reports", CanView = true },
                    new() { Module = "Orders", CanView = true },
                    new() { Module = "Products", CanView = true },
                    new() { Module = "Users", CanView = true }
                }
            }
        };

        db.AdminRoles.AddRange(roles);
        await db.SaveChangesAsync();
    }
}