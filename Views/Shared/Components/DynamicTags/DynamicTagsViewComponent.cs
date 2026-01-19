using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bangaliyana.Views.Shared.Components.DynamicTags
{
    public class DynamicTagsViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _db;

        public DynamicTagsViewComponent(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Load all active categories and build the tree in memory
            var allCategories = await _db.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            // Build the hierarchy by assigning children to parents
            // First, clear any auto-populated Children from EF Core tracking
            foreach (var category in allCategories)
            {
                category.Children = null;
            }

            var categoryLookup = allCategories.ToDictionary(c => c.Id);
            foreach (var category in allCategories)
            {
                if (category.ParentId.HasValue && categoryLookup.TryGetValue(category.ParentId.Value, out var parent))
                {
                    parent.Children ??= new List<Category>();
                    parent.Children.Add(category);
                }
            }

            // Get root categories (those without parents)
            var rootCategories = allCategories
                .Where(c => c.ParentId == null)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToList();

            return View(rootCategories);
        }
    }
}
