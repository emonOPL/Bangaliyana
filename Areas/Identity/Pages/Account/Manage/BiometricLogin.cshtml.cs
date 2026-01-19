using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Bangaliyana.Areas.Identity.Pages.Account.Manage
{
    public class BiometricLoginModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public BiometricLoginModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [TempData]
        public string? StatusMessage { get; set; }

        public int CredentialCount { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            CredentialCount = await _context.WebAuthnCredentials
                .CountAsync(c => c.UserId == user.Id && c.IsActive);

            return Page();
        }
    }
}
