# Implementation Plan: Enhanced Authentication & Dynamic Menu System

## Overview
This plan covers implementing:
1. Email/Phone login support
2. OTP-based login (optional)
3. Password reset via Email/Phone
4. Dynamic menu system with role-based permissions
5. Admin UI for user role management

---

## Phase 1: Database Models & Migrations

### 1.1 Create OTP Model (`Models/OtpCode.cs`)
```csharp
public class OtpCode
{
    public int Id { get; set; }
    public string Identifier { get; set; }  // Email or Phone
    public OtpType Type { get; set; }       // Login, Registration, PasswordReset
    public string Code { get; set; }        // 6-digit code
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public int AttemptCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum OtpType { Login, Registration, PasswordReset }
```

### 1.2 Create Menu/Permission Models (`Models/MenuItem.cs`)
```csharp
public class MenuItem
{
    public int Id { get; set; }
    public string Name { get; set; }           // Display name
    public string? Icon { get; set; }          // FontAwesome icon class
    public string? Url { get; set; }           // URL or route
    public string? Area { get; set; }          // MVC Area
    public string? Controller { get; set; }
    public string? Action { get; set; }
    public int? ParentId { get; set; }         // For nested menus
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public MenuLocation Location { get; set; } // Navbar, Sidebar, Footer
    public string? RequiredRoles { get; set; } // Comma-separated: "Admin,Seller"
    public bool IsDefault { get; set; }        // Show to all users by default

    public virtual MenuItem? Parent { get; set; }
    public virtual ICollection<MenuItem> Children { get; set; }
    public virtual ICollection<MenuPermission> Permissions { get; set; }
}

public class MenuPermission
{
    public int Id { get; set; }
    public int MenuItemId { get; set; }
    public string RoleName { get; set; }       // "Admin", "Seller", "User"
    public bool CanAccess { get; set; }

    public virtual MenuItem MenuItem { get; set; }
}

public enum MenuLocation { Navbar, AdminSidebar, CustomerSidebar, Footer }
```

### 1.3 Update ApplicationUser (if needed)
Already has:
- PhoneVerificationCode
- PhoneVerificationExpiry

Add:
- `LoginMethod` enum (Email, Phone, OTP) - optional for tracking

---

## Phase 2: Services

### 2.1 Create IOtpService / OtpService
```csharp
public interface IOtpService
{
    Task<string> GenerateOtpAsync(string identifier, OtpType type);
    Task<bool> ValidateOtpAsync(string identifier, string code, OtpType type);
    Task<bool> SendOtpViaEmailAsync(string email, string code);
    Task<bool> SendOtpViaSmsAsync(string phone, string code);
    Task CleanupExpiredOtpsAsync();
}
```

### 2.2 Create IMenuService / MenuService
```csharp
public interface IMenuService
{
    Task<List<MenuItem>> GetMenuItemsForUserAsync(ClaimsPrincipal user, MenuLocation location);
    Task<List<MenuItem>> GetAllMenuItemsAsync();
    Task<MenuItem> CreateMenuItemAsync(MenuItem item);
    Task<MenuItem> UpdateMenuItemAsync(MenuItem item);
    Task DeleteMenuItemAsync(int id);
    Task UpdateMenuPermissionsAsync(int menuItemId, List<string> roles);
    Task SeedDefaultMenuItemsAsync();
}
```

---

## Phase 3: Authentication Updates

### 3.1 Update Login Page (`Login.cshtml` / `Login.cshtml.cs`)
- Add tabs: "Email Login" | "Phone Login" | "OTP Login"
- Email Login: Existing flow (email + password)
- Phone Login: Phone + password (find user by phone)
- OTP Login: Email/Phone + OTP code (passwordless)

### 3.2 Update Registration Page
- Already supports email + phone (phone optional)
- Make phone required OR email required (at least one)
- Add option to verify phone via OTP

### 3.3 Update ForgotPassword
- Support both email and phone for password reset
- Send reset link via email OR OTP via SMS

---

## Phase 4: Admin UI for Role & Menu Management

### 4.1 Enhance RoleManagementController
- Add: Assign roles to users (User → Seller, User → Admin)
- Add: View/Edit user details
- Add: Manage menu permissions per role

### 4.2 Create MenuManagementController
```
Areas/Admin/Controllers/MenuManagementController.cs
Areas/Admin/Views/MenuManagement/Index.cshtml    - List all menus
Areas/Admin/Views/MenuManagement/Create.cshtml   - Create menu item
Areas/Admin/Views/MenuManagement/Edit.cshtml     - Edit menu item
Areas/Admin/Views/MenuManagement/Permissions.cshtml - Manage role permissions
```

### 4.3 Update Admin Layout
- Add "Menu Management" to admin sidebar
- Add "User Management" with role assignment UI

---

## Phase 5: Dynamic Menu Rendering

### 5.1 Create MenuViewComponent
```csharp
public class DynamicMenuViewComponent : ViewComponent
{
    private readonly IMenuService _menuService;

    public async Task<IViewComponentResult> InvokeAsync(MenuLocation location)
    {
        var items = await _menuService.GetMenuItemsForUserAsync(UserClaimsPrincipal, location);
        return View(items);
    }
}
```

### 5.2 Update _Layout.cshtml
Replace hardcoded navbar items with:
```razor
@await Component.InvokeAsync("DynamicMenu", new { location = MenuLocation.Navbar })
```

### 5.3 Create Default Menu Items (Seed Data)
```
- Home (Default: All)
- Categories (Default: All)
- Admin (Roles: Admin)
  - Products
  - Orders
  - Settings
  - Users
  - Menu Management
- Seller Dashboard (Roles: Seller)
  - My Products
  - My Orders
  - Analytics
```

---

## Phase 6: Database Migration

Run after all models are created:
```bash
dotnet ef migrations add EnhancedAuthAndMenuSystem
dotnet ef database update
```

---

## Files to Create/Modify

### New Files:
1. `Models/OtpCode.cs` - OTP model
2. `Models/MenuItem.cs` - Menu and permission models
3. `Services/IOtpService.cs` - OTP service interface
4. `Services/OtpService.cs` - OTP service implementation
5. `Services/IMenuService.cs` - Menu service interface
6. `Services/MenuService.cs` - Menu service implementation
7. `ViewComponents/DynamicMenuViewComponent.cs`
8. `Views/Shared/Components/DynamicMenu/Default.cshtml`
9. `Areas/Admin/Controllers/MenuManagementController.cs`
10. `Areas/Admin/Views/MenuManagement/*.cshtml`
11. `Areas/Admin/Views/RoleManagement/ManageUsers.cshtml` (update)

### Modified Files:
1. `Data/ApplicationDbContext.cs` - Add DbSets
2. `Program.cs` - Register services
3. `Areas/Identity/Pages/Account/Login.cshtml` - Add phone/OTP tabs
4. `Areas/Identity/Pages/Account/Login.cshtml.cs` - Handle phone/OTP login
5. `Areas/Identity/Pages/Account/ForgotPassword.cshtml` - Add phone option
6. `Areas/Identity/Pages/Account/ForgotPassword.cshtml.cs` - Handle phone reset
7. `Views/Shared/_Layout.cshtml` - Use dynamic menu component

---

## Role Hierarchy

```
SuperAdmin (future) → Full access to everything
Admin → Manage users, products, orders, settings, menus
Seller → Manage own products, view own orders
User/Customer → Browse, purchase, view own orders
```

---

## Default Permissions

| Menu Item | User | Seller | Admin |
|-----------|------|--------|-------|
| Home | ✓ | ✓ | ✓ |
| Categories | ✓ | ✓ | ✓ |
| My Orders | ✓ | ✓ | ✓ |
| Seller Dashboard | ✗ | ✓ | ✓ |
| Admin Panel | ✗ | ✗ | ✓ |
| Menu Management | ✗ | ✗ | ✓ |
| User Management | ✗ | ✗ | ✓ |

---

## Implementation Order

1. **Models** (OtpCode, MenuItem, MenuPermission)
2. **DbContext** updates
3. **Migration**
4. **Services** (OtpService, MenuService)
5. **Register services** in Program.cs
6. **Seed default menus**
7. **Login/Register** updates
8. **Admin UI** for menu management
9. **DynamicMenu** view component
10. **Update layouts** to use dynamic menus
11. **Testing**

---

## Notes

- OTP expiry: 5 minutes
- Max OTP attempts: 3
- SMS integration: Can use placeholder/mock for now
- Menu caching: Consider caching menu items per role
- Security: Rate limit OTP requests
