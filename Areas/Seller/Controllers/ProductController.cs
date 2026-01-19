using Bangaliyana.Data;
using Bangaliyana.Extensions;
using Bangaliyana.Models;
using Bangaliyana.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Bangaliyana.Areas.Seller.Controllers
{
    [Area("Seller")]
    [Authorize(Roles = "SuperAdmin,Seller,Admin")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IProductBulkImportService _bulkImportService;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public ProductController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment,
            IProductBulkImportService bulkImportService,
            IStringLocalizer<SharedResources> localizer)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _bulkImportService = bulkImportService;
            _localizer = localizer;
        }

        private async Task<int?> GetCurrentSellerIdAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return null;

            var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
            return seller?.Id;
        }

        // GET: /Seller/Product
        public async Task<IActionResult> Index(string? search, int? categoryId, string? status, int page = 1)
        {
            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null)
            {
                return RedirectToAction("Apply", "Registration", new { area = "Seller" });
            }

            var query = _context.Products
                .Include(p => p.Category)
                .Where(p => p.SellerId == sellerId);

            // Search
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search) ||
                                        (p.SKU != null && p.SKU.Contains(search)));
            }

            // Category filter
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId);
            }

            // Status filter
            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<ProductStatus>(status, out var productStatus))
                {
                    query = query.Where(p => p.Status == productStatus);
                }
            }

            // Pagination
            var pageSize = 20;
            var totalProducts = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

            var products = await query
                .OrderByDescending(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Get categories for filter
            var categories = await _context.Categories.ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.Status = status;
            ViewBag.Categories = categories;

            return View(products);
        }

        // GET: /Seller/Product/Create
        public async Task<IActionResult> Create()
        {
            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null)
            {
                TempData["Error"] = _localizer["SetupSellerProfileBeforeAddingProducts"].Value;
                return RedirectToAction("Apply", "Registration", new { area = "Seller" });
            }

            ViewData["rootCategories"] = new SelectList(await _context.Categories.Where(c => c.IsActive && c.ParentId == null).OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToListAsync(), "Id", "Name");
            ViewData["productStatuses"] = new SelectList(Enum.GetValues(typeof(ProductStatus)).Cast<ProductStatus>().Select(s => new { Id = s.ToString(), Name = s.GetDisplayName() }), "Id", "Name");

            return View(new Products());
        }

        // POST: /Seller/Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Products product, IFormFile? ImageUrl, List<IFormFile>? AdditionalImages, string? VariantsJson)
        {
            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null)
            {
                TempData["Error"] = _localizer["SetupSellerProfileBeforeAddingProducts"].Value;
                return RedirectToAction("Apply", "Registration", new { area = "Seller" });
            }

            // Remove SellerId from ModelState since we'll set it manually
            ModelState.Remove("SellerId");

            if (ModelState.IsValid)
            {
                product.SellerId = sellerId;
                product.CreatedAt = DateTime.UtcNow;
                product.UpdatedAt = DateTime.UtcNow;

                // Auto set availability based on stock and status
                if (product.Stock <= 0)
                {
                    product.IsAvailable = false;
                    product.Status = ProductStatus.OutOfStock;
                }
                else
                {
                    // Set IsAvailable based on Status - only Active products with stock are available
                    product.IsAvailable = product.Status == ProductStatus.Active;
                }

                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                if (ImageUrl != null && ImageUrl.Length > 0)
                {
                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageUrl.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageUrl.CopyToAsync(stream);
                    }

                    product.ImageUrl = "/images/products/" + uniqueFileName;
                }
                else
                {
                    product.ImageUrl = "/images/products/noimage.jpg";
                }

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // Handle additional images
                if (AdditionalImages != null && AdditionalImages.Count > 0)
                {
                    int displayOrder = 1;
                    foreach (var additionalImage in AdditionalImages)
                    {
                        if (additionalImage.Length > 0)
                        {
                            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(additionalImage.FileName);
                            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await additionalImage.CopyToAsync(stream);
                            }

                            var productImage = new ProductImage
                            {
                                ProductId = product.Id,
                                ImageUrl = "/images/products/" + uniqueFileName,
                                DisplayOrder = displayOrder++,
                                IsPrimary = false,
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.ProductImages.Add(productImage);
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                // Handle product variants
                if (!string.IsNullOrWhiteSpace(VariantsJson))
                {
                    try
                    {
                        var variants = System.Text.Json.JsonSerializer.Deserialize<List<ProductVariant>>(VariantsJson);
                        if (variants != null && variants.Count > 0)
                        {
                            int displayOrder = 1;
                            foreach (var variant in variants)
                            {
                                variant.Id = 0;
                                variant.ProductId = product.Id;
                                variant.DisplayOrder = displayOrder++;
                                variant.CreatedAt = DateTime.UtcNow;
                                variant.UpdatedAt = DateTime.UtcNow;
                                _context.ProductVariants.Add(variant);
                            }
                            await _context.SaveChangesAsync();
                        }
                    }
                    catch { /* Ignore variant parsing errors */ }
                }

                TempData["Success"] = _localizer["ProductCreatedSuccess"].Value;
                return RedirectToAction(nameof(Index));
            }

            ViewData["rootCategories"] = new SelectList(await _context.Categories.Where(c => c.IsActive && c.ParentId == null).OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToListAsync(), "Id", "Name");
            ViewData["productStatuses"] = new SelectList(Enum.GetValues(typeof(ProductStatus)).Cast<ProductStatus>().Select(s => new { Id = s.ToString(), Name = s.GetDisplayName() }), "Id", "Name");

            return View(product);
        }

        // GET: /Seller/Product/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null) return RedirectToAction("Apply", "Registration", new { area = "Seller" });

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id && p.SellerId == sellerId);

            if (product == null)
            {
                TempData["Error"] = _localizer["ProductNotFoundOrNoPermissionToEdit"].Value;
                return RedirectToAction(nameof(Index));
            }

            ViewData["rootCategories"] = new SelectList(await _context.Categories.Where(c => c.IsActive && c.ParentId == null).OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToListAsync(), "Id", "Name");
            ViewData["productStatuses"] = new SelectList(Enum.GetValues(typeof(ProductStatus)).Cast<ProductStatus>().Select(s => new { Id = s.ToString(), Name = s.GetDisplayName() }), "Id", "Name");

            return View(product);
        }

        // POST: /Seller/Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Products product, IFormFile? ImageUrl, List<IFormFile>? AdditionalImages)
        {
            if (id != product.Id) return NotFound();

            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null) return RedirectToAction("Apply", "Registration", new { area = "Seller" });

            var existingProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.SellerId == sellerId);

            if (existingProduct == null)
            {
                TempData["Error"] = _localizer["ProductNotFoundOrNoPermissionToEdit"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Remove SellerId from ModelState since we'll preserve it
            ModelState.Remove("SellerId");

            if (ModelState.IsValid)
            {
                // Update only allowed fields
                existingProduct.Name = product.Name;
                existingProduct.SKU = product.SKU;
                existingProduct.Price = product.Price;
                existingProduct.DiscountPrice = product.DiscountPrice;
                existingProduct.CategoryId = product.CategoryId;
                existingProduct.Description = product.Description;
                existingProduct.Stock = product.Stock;
                existingProduct.Status = product.Status;
                existingProduct.UpdatedAt = DateTime.UtcNow;

                // Auto set availability based on stock and status
                if (existingProduct.Stock <= 0)
                {
                    existingProduct.IsAvailable = false;
                    existingProduct.Status = ProductStatus.OutOfStock;
                }
                else
                {
                    // Set IsAvailable based on Status - only Active products with stock are available
                    existingProduct.IsAvailable = existingProduct.Status == ProductStatus.Active;
                }

                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                if (ImageUrl != null && ImageUrl.Length > 0)
                {
                    // Delete old image if it's not the default
                    if (!string.IsNullOrEmpty(existingProduct.ImageUrl) &&
                        !existingProduct.ImageUrl.Contains("noimage.jpg"))
                    {
                        var oldPath = Path.Combine(_webHostEnvironment.WebRootPath, existingProduct.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath))
                        {
                            System.IO.File.Delete(oldPath);
                        }
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageUrl.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageUrl.CopyToAsync(stream);
                    }

                    existingProduct.ImageUrl = "/images/products/" + uniqueFileName;
                }

                // Handle additional images
                if (AdditionalImages != null && AdditionalImages.Count > 0)
                {
                    var maxDisplayOrder = await _context.ProductImages
                        .Where(pi => pi.ProductId == id)
                        .MaxAsync(pi => (int?)pi.DisplayOrder) ?? 0;

                    foreach (var additionalImage in AdditionalImages)
                    {
                        if (additionalImage.Length > 0)
                        {
                            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(additionalImage.FileName);
                            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await additionalImage.CopyToAsync(stream);
                            }

                            var productImage = new ProductImage
                            {
                                ProductId = id,
                                ImageUrl = "/images/products/" + uniqueFileName,
                                DisplayOrder = ++maxDisplayOrder,
                                IsPrimary = false,
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.ProductImages.Add(productImage);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = _localizer["ProductUpdatedSuccess"].Value;
                return RedirectToAction(nameof(Index));
            }

            ViewData["rootCategories"] = new SelectList(await _context.Categories.Where(c => c.IsActive && c.ParentId == null).OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToListAsync(), "Id", "Name");
            ViewData["productStatuses"] = new SelectList(Enum.GetValues(typeof(ProductStatus)).Cast<ProductStatus>().Select(s => new { Id = s.ToString(), Name = s.GetDisplayName() }), "Id", "Name");

            return View(product);
        }

        // API endpoint to get child categories by parent (for cascading dropdowns)
        [HttpGet]
        public IActionResult GetCategoriesByParent(int? parentId)
        {
            var categories = _context.Categories
                .Where(c => c.ParentId == parentId && c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .Select(c => new { id = c.Id, name = c.Name, hasChildren = c.Children.Any(ch => ch.IsActive) })
                .ToList();
            return Json(categories);
        }

        // API endpoint to get category path (for breadcrumbs)
        [HttpGet]
        public async Task<IActionResult> GetCategoryPath(int categoryId)
        {
            var path = new List<object>();
            var category = await _context.Categories.FindAsync(categoryId);

            while (category != null)
            {
                path.Insert(0, new { id = category.Id, name = category.Name });
                category = category.ParentId.HasValue
                    ? await _context.Categories.FindAsync(category.ParentId)
                    : null;
            }

            return Json(path);
        }

        // GET: /Seller/Product/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null) return RedirectToAction("Apply", "Registration", new { area = "Seller" });

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id && p.SellerId == sellerId);

            if (product == null)
            {
                TempData["Error"] = _localizer["ProductNotFoundOrNoPermissionToView"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Get product stats
            var orderCount = await _context.OrderItems
                .Where(oi => oi.ProductId == id)
                .CountAsync();

            var reviewCount = await _context.ProductReviews
                .Where(r => r.ProductId == id)
                .CountAsync();

            ViewBag.OrderCount = orderCount;
            ViewBag.ReviewCount = reviewCount;

            return View(product);
        }

        // POST: /Seller/Product/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null) return RedirectToAction("Apply", "Registration", new { area = "Seller" });

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.SellerId == sellerId);

            if (product == null)
            {
                TempData["Error"] = _localizer["ProductNotFoundOrNoPermissionToDelete"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Check if product has orders
            var hasOrders = await _context.OrderItems.AnyAsync(oi => oi.ProductId == id);
            if (hasOrders)
            {
                TempData["Error"] = _localizer["CannotDeleteProductWithOrders"].Value;
                return RedirectToAction(nameof(Index));
            }

            // Delete product image
            if (!string.IsNullOrEmpty(product.ImageUrl) && !product.ImageUrl.Contains("noimage.jpg"))
            {
                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, product.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            TempData["Success"] = _localizer["ProductDeletedSuccess"].Value;
            return RedirectToAction(nameof(Index));
        }

        // POST: /Seller/Product/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null) return Json(new { success = false, message = "Not authorized" });

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.SellerId == sellerId);

            if (product == null)
            {
                return Json(new { success = false, message = "Product not found" });
            }

            // Toggle between Active and Inactive status
            if (product.Status == ProductStatus.Active)
            {
                product.Status = ProductStatus.Inactive;
                product.IsAvailable = false;
            }
            else if (product.Stock > 0)
            {
                // Only allow activation if there's stock
                product.Status = ProductStatus.Active;
                product.IsAvailable = true;
            }
            else
            {
                return Json(new { success = false, message = "Cannot activate product with zero stock" });
            }

            product.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { success = true, isAvailable = product.IsAvailable, status = product.Status.GetDisplayName() });
        }

        // =============================================
        // PRODUCT VARIANTS MANAGEMENT
        // =============================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVariant([FromBody] ProductVariant variant)
        {
            try
            {
                var sellerId = await GetCurrentSellerIdAsync();
                if (sellerId == null)
                    return Json(new { success = false, message = "Not authorized" });

                if (variant.ProductId <= 0)
                    return Json(new { success = false, message = "Invalid product ID." });

                var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == variant.ProductId && p.SellerId == sellerId);
                if (product == null)
                    return Json(new { success = false, message = "Product not found or you don't have permission." });

                variant.Id = 0;
                variant.CreatedAt = DateTime.UtcNow;
                variant.UpdatedAt = DateTime.UtcNow;

                var maxOrder = await _context.ProductVariants
                    .Where(v => v.ProductId == variant.ProductId)
                    .MaxAsync(v => (int?)v.DisplayOrder) ?? 0;
                variant.DisplayOrder = maxOrder + 1;

                _context.ProductVariants.Add(variant);
                await _context.SaveChangesAsync();

                return Json(new { success = true, variantId = variant.Id });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateVariant([FromBody] ProductVariant variant)
        {
            try
            {
                var sellerId = await GetCurrentSellerIdAsync();
                if (sellerId == null)
                    return Json(new { success = false, message = "Not authorized" });

                var existingVariant = await _context.ProductVariants
                    .Include(v => v.Product)
                    .FirstOrDefaultAsync(v => v.Id == variant.Id);

                if (existingVariant == null)
                    return Json(new { success = false, message = "Variant not found." });

                if (existingVariant.Product?.SellerId != sellerId)
                    return Json(new { success = false, message = "You don't have permission to edit this variant." });

                existingVariant.Size = variant.Size;
                existingVariant.Color = variant.Color;
                existingVariant.ColorCode = variant.ColorCode;
                existingVariant.SKU = variant.SKU;
                existingVariant.Stock = variant.Stock;
                existingVariant.AdditionalPrice = variant.AdditionalPrice;
                existingVariant.IsAvailable = variant.IsAvailable;
                existingVariant.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVariant(int id)
        {
            try
            {
                var sellerId = await GetCurrentSellerIdAsync();
                if (sellerId == null)
                    return Json(new { success = false, message = "Not authorized" });

                var variant = await _context.ProductVariants
                    .Include(v => v.Product)
                    .FirstOrDefaultAsync(v => v.Id == id);

                if (variant == null)
                    return Json(new { success = false, message = "Variant not found." });

                if (variant.Product?.SellerId != sellerId)
                    return Json(new { success = false, message = "You don't have permission to delete this variant." });

                _context.ProductVariants.Remove(variant);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // =============================================
        // PRODUCT IMAGES MANAGEMENT
        // =============================================

        // POST: /Seller/Product/DeleteMainImage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMainImage(int productId)
        {
            try
            {
                var sellerId = await GetCurrentSellerIdAsync();
                if (sellerId == null)
                    return Json(new { success = false, message = "Not authorized" });

                var product = await _context.Products
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.Id == productId && p.SellerId == sellerId);

                if (product == null)
                    return Json(new { success = false, message = "Product not found or you don't have permission." });

                // Check if there are gallery images to promote
                var galleryImages = product.Images.OrderBy(i => i.DisplayOrder).ToList();

                if (!galleryImages.Any())
                    return Json(new { success = false, message = "Cannot delete the last image. Every product must have at least one image." });

                // Delete the main image file
                if (!string.IsNullOrEmpty(product.ImageUrl) && !product.ImageUrl.Contains("noimage.jpg"))
                {
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, product.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                // Promote the first gallery image to main image
                var firstGalleryImage = galleryImages.First();
                product.ImageUrl = firstGalleryImage.ImageUrl;

                // Remove the promoted image from gallery
                _context.ProductImages.Remove(firstGalleryImage);

                product.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Main image deleted and replaced successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /Seller/Product/DeleteImage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int imageId, int productId)
        {
            try
            {
                var sellerId = await GetCurrentSellerIdAsync();
                if (sellerId == null)
                    return Json(new { success = false, message = "Not authorized" });

                var product = await _context.Products
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.Id == productId && p.SellerId == sellerId);

                if (product == null)
                    return Json(new { success = false, message = "Product not found or you don't have permission." });

                var image = await _context.ProductImages
                    .FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId);

                if (image == null)
                    return Json(new { success = false, message = "Image not found." });

                // Count total images (main + gallery)
                var totalImages = 1 + product.Images.Count; // 1 for main image
                if (totalImages <= 1)
                    return Json(new { success = false, message = "Cannot delete the last image. Every product must have at least one image." });

                // Delete image file
                if (!string.IsNullOrEmpty(image.ImageUrl))
                {
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, image.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _context.ProductImages.Remove(image);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Image deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /Seller/Product/SetPrimaryImage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPrimaryImage(int imageId, int productId)
        {
            try
            {
                var sellerId = await GetCurrentSellerIdAsync();
                if (sellerId == null)
                    return Json(new { success = false, message = "Not authorized" });

                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == productId && p.SellerId == sellerId);

                if (product == null)
                    return Json(new { success = false, message = "Product not found or you don't have permission." });

                var newPrimaryImage = await _context.ProductImages
                    .FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId);

                if (newPrimaryImage == null)
                    return Json(new { success = false, message = "Image not found." });

                // Swap the images: current main becomes gallery, gallery becomes main
                var oldMainImageUrl = product.ImageUrl;

                // Set new main image
                product.ImageUrl = newPrimaryImage.ImageUrl;

                // Move old main image to gallery (if it's not the default)
                if (!string.IsNullOrEmpty(oldMainImageUrl) && !oldMainImageUrl.Contains("noimage.jpg"))
                {
                    newPrimaryImage.ImageUrl = oldMainImageUrl;
                }
                else
                {
                    // If old main was noimage.jpg, just remove the new primary from gallery
                    _context.ProductImages.Remove(newPrimaryImage);
                }

                product.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Primary image updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // =============================================
        // BULK UPLOAD
        // =============================================

        // GET: /Seller/Product/BulkUpload
        public async Task<IActionResult> BulkUpload()
        {
            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null)
            {
                TempData["Error"] = _localizer["SetupSellerProfileBeforeUploadingProducts"].Value;
                return RedirectToAction("Apply", "Registration", new { area = "Seller" });
            }

            return View();
        }

        // GET: /Seller/Product/DownloadTemplate
        public IActionResult DownloadTemplate()
        {
            var templateBytes = _bulkImportService.GenerateTemplate();
            return File(templateBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ProductImportTemplate.xlsx");
        }

        // POST: /Seller/Product/BulkUpload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkUpload(IFormFile? excelFile)
        {
            var sellerId = await GetCurrentSellerIdAsync();
            if (sellerId == null)
            {
                TempData["Error"] = _localizer["SetupSellerProfileBeforeUploadingProducts"].Value;
                return RedirectToAction("Apply", "Registration", new { area = "Seller" });
            }

            if (excelFile == null || excelFile.Length == 0)
            {
                TempData["Error"] = _localizer["PleaseSelectExcelFileToUpload"].Value;
                return View();
            }

            // Validate file extension
            var extension = Path.GetExtension(excelFile.FileName).ToLowerInvariant();
            if (extension != ".xlsx" && extension != ".xls")
            {
                TempData["Error"] = _localizer["PleaseUploadValidExcelFile"].Value;
                return View();
            }

            // Validate file size (max 5MB)
            if (excelFile.Length > 5 * 1024 * 1024)
            {
                TempData["Error"] = _localizer["FileSizeExceeds5MBLimit"].Value;
                return View();
            }

            using var stream = excelFile.OpenReadStream();

            // Validate file structure
            var (isValid, errorMessage) = _bulkImportService.ValidateFile(stream);
            if (!isValid)
            {
                TempData["Error"] = errorMessage;
                return View();
            }

            // Reset stream position for import
            stream.Position = 0;

            // Import products
            var result = await _bulkImportService.ImportProductsAsync(stream, sellerId.Value);

            ViewBag.ImportResult = result;

            if (result.Success)
            {
                TempData["Success"] = _localizer["SuccessfullyImportedProducts"].Value.Replace("{0}", result.SuccessCount.ToString());
            }
            else if (result.SuccessCount > 0)
            {
                TempData["Warning"] = _localizer["ImportedProductsWithErrors"].Value.Replace("{0}", result.SuccessCount.ToString()).Replace("{1}", result.FailedCount.ToString());
            }
            else
            {
                TempData["Error"] = _localizer["NoProductsWereImported"].Value;
            }

            return View();
        }
    }
}
