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
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace Bangaliyana.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,Admin,Seller")]
    public class ProductController : Controller
    {
        private ApplicationDbContext _db;
        private IHostingEnvironment _he;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPriceAlertService _priceAlertService;
        private readonly IShopFollowService _shopFollowService;
        private readonly IProductBulkImportService _bulkImportService;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly IFileValidationService _fileValidationService;

        public ProductController(ApplicationDbContext db, IHostingEnvironment he, UserManager<ApplicationUser> userManager, IPriceAlertService priceAlertService, IShopFollowService shopFollowService, IProductBulkImportService bulkImportService, IStringLocalizer<SharedResources> localizer, IFileValidationService fileValidationService)
        {
            _db = db;
            _he = he;
            _userManager = userManager;
            _priceAlertService = priceAlertService;
            _shopFollowService = shopFollowService;
            _bulkImportService = bulkImportService;
            _localizer = localizer;
            _fileValidationService = fileValidationService;
        }

        // Helper method to get current user's seller ID
        private async Task<int?> GetCurrentSellerIdAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return null;

            var seller = await _db.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
            return seller?.Id;
        }

        // Helper method to check if current user is Admin or SuperAdmin
        private bool IsAdmin()
        {
            return User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
        }

        public async Task<IActionResult> Index(int? sellerId, string? search, int? categoryId, string? status, int page = 1)
        {
            const int pageSize = 15;
            var products = _db.Products.Include(p => p.Category).Include(p => p.Seller).AsQueryable();

            // If Seller role, filter to only their products
            if (!IsAdmin())
            {
                var currentSellerId = await GetCurrentSellerIdAsync();
                if (currentSellerId != null)
                {
                    products = products.Where(p => p.SellerId == currentSellerId);
                }
                else
                {
                    products = products.Where(p => false);
                }
            }
            else if (sellerId.HasValue)
            {
                // Admin filtering by specific seller
                products = products.Where(p => p.SellerId == sellerId);
                var seller = await _db.Sellers.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == sellerId);
                ViewBag.FilteredSeller = seller;
            }

            // Search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                products = products.Where(p => p.Name.Contains(search) || (p.SKU != null && p.SKU.Contains(search)));
            }

            // Category filter
            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryId == categoryId);
            }

            // Status filter
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ProductStatus>(status, out var productStatus))
            {
                products = products.Where(p => p.Status == productStatus);
            }

            var totalProducts = await products.CountAsync();
            var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);

            var productList = await products
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Get categories for filter dropdown
            var categories = await _db.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();

            // Get sellers for filter dropdown (only for Admin/Moderator/SuperAdmin)
            // Show all shops except rejected ones
            var canFilterBySeller = User.IsInRole("SuperAdmin") || User.IsInRole("Admin") || User.IsInRole("Moderator");
            List<Models.Seller>? sellers = null;
            if (canFilterBySeller)
            {
                sellers = await _db.Sellers
                    .Where(s => s.Status != SellerStatus.Rejected)
                    .OrderBy(s => s.ShopName)
                    .ToListAsync();
            }

            ViewBag.IsAdmin = IsAdmin();
            ViewBag.CanFilterBySeller = canFilterBySeller;
            ViewBag.Sellers = sellers;
            ViewBag.SellerId = sellerId;
            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.Categories = categories;

            return View(productList);
        }

        public async Task<IActionResult> Create()
        {
            // Sellers need a seller profile to create products
            if (!IsAdmin())
            {
                var sellerId = await GetCurrentSellerIdAsync();
                if (sellerId == null)
                {
                    TempData["Error"] = _localizer["SellerProfileRequiredForProducts"].Value;
                    return RedirectToAction("Index");
                }
            }

            // Get root categories for cascading dropdown
            ViewData["rootCategories"] = new SelectList(_db.Categories.Where(c => c.IsActive && c.ParentId == null).OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToList(), "Id", "Name");
            ViewData["productStatuses"] = new SelectList(Enum.GetValues(typeof(ProductStatus)).Cast<ProductStatus>().Select(s => new { Id = s.ToString(), Name = s.GetDisplayName() }), "Id", "Name");
            ViewBag.IsAdmin = IsAdmin();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Products product, IFormFile? ImageUrl, List<IFormFile>? AdditionalImages, string? VariantsJson)
        {
            if (ModelState.IsValid)
            {
                // For Sellers, auto-assign their seller ID
                if (!IsAdmin())
                {
                    var sellerId = await GetCurrentSellerIdAsync();
                    if (sellerId == null)
                    {
                        TempData["Error"] = _localizer["SellerProfileRequiredForProducts"].Value;
                        return RedirectToAction("Index");
                    }
                    product.SellerId = sellerId;
                }

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

                // Handle main image with validation
                if (ImageUrl != null)
                {
                    // Validate image file
                    if (!_fileValidationService.ValidateImage(ImageUrl, 5, out var imageError))
                    {
                        ModelState.AddModelError("ImageUrl", imageError);
                        ViewData["rootCategories"] = new SelectList(_db.Categories.Where(c => c.IsActive && c.ParentId == null).OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToList(), "Id", "Name");
                        ViewData["productStatuses"] = new SelectList(Enum.GetValues(typeof(ProductStatus)).Cast<ProductStatus>().Select(s => new { Id = s.ToString(), Name = s.GetDisplayName() }), "Id", "Name");
                        ViewBag.IsAdmin = IsAdmin();
                        return View(product);
                    }

                    // Validate file signature (magic bytes)
                    if (!_fileValidationService.ValidateFileSignature(ImageUrl, out var signatureError))
                    {
                        ModelState.AddModelError("ImageUrl", signatureError);
                        ViewData["rootCategories"] = new SelectList(_db.Categories.Where(c => c.IsActive && c.ParentId == null).OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToList(), "Id", "Name");
                        ViewData["productStatuses"] = new SelectList(Enum.GetValues(typeof(ProductStatus)).Cast<ProductStatus>().Select(s => new { Id = s.ToString(), Name = s.GetDisplayName() }), "Id", "Name");
                        ViewBag.IsAdmin = IsAdmin();
                        return View(product);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageUrl.FileName);
                    var name = Path.Combine(_he.WebRootPath + "/images/products", uniqueFileName);
                    using (var stream = new FileStream(name, FileMode.Create))
                    {
                        await ImageUrl.CopyToAsync(stream);
                    }
                    product.ImageUrl = "images/products/" + uniqueFileName;
                }
                else
                {
                    product.ImageUrl = "images/products/noimage.jpg";
                }

                product.CreatedAt = DateTime.UtcNow;
                product.UpdatedAt = DateTime.UtcNow;

                TempData["onSave"] = _localizer["ProductCreatedSuccessfully"].Value;
                _db.Products.Add(product);
                await _db.SaveChangesAsync();

                // Handle product variants
                if (!string.IsNullOrWhiteSpace(VariantsJson))
                {
                    try
                    {
                        var options = new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            MaxDepth = 10 // Prevent deeply nested objects
                        };
                        var variants = System.Text.Json.JsonSerializer.Deserialize<List<ProductVariant>>(VariantsJson, options);
                        if (variants != null && variants.Count > 0)
                        {
                            int displayOrder = 1;
                            foreach (var variant in variants)
                            {
                                variant.Id = 0; // Ensure new record
                                variant.ProductId = product.Id;
                                variant.DisplayOrder = displayOrder++;
                                variant.CreatedAt = DateTime.UtcNow;
                                variant.UpdatedAt = DateTime.UtcNow;
                                _db.ProductVariants.Add(variant);
                            }
                            await _db.SaveChangesAsync();
                        }
                    }
                    catch (System.Text.Json.JsonException ex)
                    {
                        // Log variant parsing errors for debugging
                        TempData["Warning"] = _localizer["VariantParsingError"].Value;
                        System.Diagnostics.Debug.WriteLine($"Variant JSON parsing error: {ex.Message}");
                    }
                }

                // Handle additional images with validation
                if (AdditionalImages != null && AdditionalImages.Count > 0)
                {
                    int order = 1;
                    foreach (var image in AdditionalImages)
                    {
                        if (image != null && image.Length > 0)
                        {
                            // Validate each additional image
                            if (!_fileValidationService.ValidateImage(image, 5, out _) ||
                                !_fileValidationService.ValidateFileSignature(image, out _))
                            {
                                continue; // Skip invalid images
                            }

                            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                            var imagePath = Path.Combine(_he.WebRootPath + "/images/products", uniqueFileName);
                            using (var stream = new FileStream(imagePath, FileMode.Create))
                            {
                                await image.CopyToAsync(stream);
                            }

                            var productImage = new ProductImage
                            {
                                ProductId = product.Id,
                                ImageUrl = "images/products/" + uniqueFileName,
                                DisplayOrder = order++,
                                CreatedAt = DateTime.UtcNow
                            };
                            _db.ProductImages.Add(productImage);
                        }
                    }
                    await _db.SaveChangesAsync();
                }

                // Notify shop followers about new product (only if product is Active and has a seller)
                if (product.Status == ProductStatus.Active && product.SellerId.HasValue)
                {
                    await _shopFollowService.NotifyFollowersNewProductAsync(
                        product.SellerId.Value,
                        product.Id,
                        product.Name);
                }

                return RedirectToAction("Index");
            }
            ViewData["rootCategories"] = new SelectList(_db.Categories.Where(c => c.IsActive && c.ParentId == null).OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToList(), "Id", "Name");
            ViewData["productStatuses"] = new SelectList(Enum.GetValues(typeof(ProductStatus)).Cast<ProductStatus>().Select(s => new { Id = s.ToString(), Name = s.GetDisplayName() }), "Id", "Name");
            ViewBag.IsAdmin = IsAdmin();
            return View(product);
        }

        // API endpoint to get child categories by parent (for cascading dropdowns)
        [HttpGet]
        public IActionResult GetCategoriesByParent(int? parentId)
        {
            var categories = _db.Categories
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
            var category = await _db.Categories.FindAsync(categoryId);

            while (category != null)
            {
                path.Insert(0, new { id = category.Id, name = category.Name });
                category = category.ParentId.HasValue
                    ? await _db.Categories.FindAsync(category.ParentId)
                    : null;
            }

            return Json(path);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var product = _db.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            // Sellers can only edit their own products
            if (!IsAdmin())
            {
                var sellerId = await GetCurrentSellerIdAsync();
                if (product.SellerId != sellerId)
                {
                    TempData["Error"] = _localizer["CanOnlyEditOwnProducts"].Value;
                    return RedirectToAction("Index");
                }
            }

            ViewData["rootCategories"] = new SelectList(_db.Categories.Where(c => c.IsActive && c.ParentId == null).OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToList(), "Id", "Name");
            ViewData["productStatuses"] = new SelectList(Enum.GetValues(typeof(ProductStatus)).Cast<ProductStatus>().Select(s => new { Id = s.ToString(), Name = s.GetDisplayName() }), "Id", "Name");
            ViewBag.IsAdmin = IsAdmin();
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Products product, IFormFile? ImageFile, List<IFormFile>? AdditionalImages)
        {
            // Get old product data for price tracking
            var existingProduct = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == product.Id);

            // Sellers can only edit their own products
            if (!IsAdmin())
            {
                var sellerId = await GetCurrentSellerIdAsync();
                if (existingProduct == null || existingProduct.SellerId != sellerId)
                {
                    TempData["Error"] = _localizer["CanOnlyEditOwnProducts"].Value;
                    return RedirectToAction("Index");
                }
                // Preserve the seller ID for the product
                product.SellerId = sellerId;
            }

            if (ModelState.IsValid)
            {
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

                if (ImageFile != null)
                {
                    // Validate image file
                    if (!_fileValidationService.ValidateImage(ImageFile, 5, out var imageError))
                    {
                        ModelState.AddModelError("ImageFile", imageError);
                        ViewData["rootCategories"] = new SelectList(_db.Categories.Where(c => c.IsActive && c.ParentId == null).OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToList(), "Id", "Name");
                        ViewData["productStatuses"] = new SelectList(Enum.GetValues(typeof(ProductStatus)).Cast<ProductStatus>().Select(s => new { Id = s.ToString(), Name = s.GetDisplayName() }), "Id", "Name");
                        ViewBag.IsAdmin = IsAdmin();
                        return View(product);
                    }

                    // Validate file signature (magic bytes)
                    if (!_fileValidationService.ValidateFileSignature(ImageFile, out var signatureError))
                    {
                        ModelState.AddModelError("ImageFile", signatureError);
                        ViewData["rootCategories"] = new SelectList(_db.Categories.Where(c => c.IsActive && c.ParentId == null).OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToList(), "Id", "Name");
                        ViewData["productStatuses"] = new SelectList(Enum.GetValues(typeof(ProductStatus)).Cast<ProductStatus>().Select(s => new { Id = s.ToString(), Name = s.GetDisplayName() }), "Id", "Name");
                        ViewBag.IsAdmin = IsAdmin();
                        return View(product);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                    var name = Path.Combine(_he.WebRootPath + "/images/products", uniqueFileName);
                    using (var stream = new FileStream(name, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }
                    product.ImageUrl = "images/products/" + uniqueFileName;
                }

                // Track price changes before updating
                decimal oldPrice = existingProduct?.Price ?? 0;
                decimal? oldDiscountPrice = existingProduct?.DiscountPrice;
                decimal newPrice = product.Price;
                decimal? newDiscountPrice = product.DiscountPrice;

                bool priceChanged = oldPrice != newPrice || oldDiscountPrice != newDiscountPrice;

                product.UpdatedAt = DateTime.UtcNow;
                product.CreatedAt = existingProduct?.CreatedAt ?? DateTime.UtcNow;

                TempData["onEdit"] = _localizer["ProductUpdatedSuccessfully"].Value;
                _db.Products.Update(product);
                await _db.SaveChangesAsync();

                // Handle additional images with validation
                if (AdditionalImages != null && AdditionalImages.Count > 0)
                {
                    var existingImageCount = await _db.ProductImages.CountAsync(i => i.ProductId == product.Id);
                    int order = existingImageCount + 1;
                    foreach (var image in AdditionalImages)
                    {
                        if (image != null && image.Length > 0)
                        {
                            // Validate each additional image
                            if (!_fileValidationService.ValidateImage(image, 5, out _) ||
                                !_fileValidationService.ValidateFileSignature(image, out _))
                            {
                                continue; // Skip invalid images
                            }

                            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                            var imagePath = Path.Combine(_he.WebRootPath + "/images/products", uniqueFileName);
                            using (var stream = new FileStream(imagePath, FileMode.Create))
                            {
                                await image.CopyToAsync(stream);
                            }

                            var productImage = new ProductImage
                            {
                                ProductId = product.Id,
                                ImageUrl = "images/products/" + uniqueFileName,
                                DisplayOrder = order++,
                                CreatedAt = DateTime.UtcNow
                            };
                            _db.ProductImages.Add(productImage);
                        }
                    }
                    await _db.SaveChangesAsync();
                }

                // Record price change and send notifications if price decreased
                if (priceChanged)
                {
                    var userId = _userManager.GetUserId(User);
                    await _priceAlertService.RecordPriceChangeAsync(
                        product.Id,
                        oldPrice,
                        newPrice,
                        oldDiscountPrice,
                        newDiscountPrice,
                        userId);

                    // Process price drop notifications (only sends if price actually dropped)
                    await _priceAlertService.ProcessPriceDropNotificationsAsync(product.Id);

                    // Notify shop followers about price drop (check if price actually dropped)
                    decimal effectiveOldPrice = oldDiscountPrice ?? oldPrice;
                    decimal effectiveNewPrice = newDiscountPrice ?? newPrice;

                    if (effectiveNewPrice < effectiveOldPrice && product.SellerId.HasValue)
                    {
                        await _shopFollowService.NotifyFollowersPriceDropAsync(
                            product.SellerId.Value,
                            product.Id,
                            product.Name,
                            effectiveOldPrice,
                            effectiveNewPrice);
                    }
                }

                return RedirectToAction("Index");
            }
            ViewData["rootCategories"] = new SelectList(_db.Categories.Where(c => c.IsActive && c.ParentId == null).OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToList(), "Id", "Name");
            ViewData["productStatuses"] = new SelectList(Enum.GetValues(typeof(ProductStatus)).Cast<ProductStatus>().Select(s => new { Id = s.ToString(), Name = s.GetDisplayName() }), "Id", "Name");
            ViewBag.IsAdmin = IsAdmin();
            return View(product);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = _db.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .Include(p => p.Seller)
                .Include(p => p.Reviews)
                .FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            // Sellers can only view details of their own products
            if (!IsAdmin())
            {
                var sellerId = await GetCurrentSellerIdAsync();
                if (product.SellerId != sellerId)
                {
                    TempData["Error"] = _localizer["CanOnlyViewOwnProducts"].Value;
                    return RedirectToAction("Index");
                }
            }

            ViewBag.IsAdmin = IsAdmin();
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var product = _db.Products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            // Sellers can only delete their own products
            if (!IsAdmin())
            {
                var sellerId = await GetCurrentSellerIdAsync();
                if (product.SellerId != sellerId)
                {
                    TempData["Error"] = _localizer["CanOnlyDeleteOwnProducts"].Value;
                    return RedirectToAction("Index");
                }
            }

            _db.Products.Remove(product);
            await _db.SaveChangesAsync();
            TempData["onDelete"] = _localizer["ProductDeletedSuccessfully"].Value;
            return RedirectToAction("Index");
        }

        // API: Delete a single product image
        [HttpPost]
        public async Task<IActionResult> DeleteImage(int imageId, int productId)
        {
            var product = await _db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null)
            {
                return Json(new { success = false, message = _localizer["ProductNotFound"].Value });
            }

            // Sellers can only manage their own products
            if (!IsAdmin())
            {
                var sellerId = await GetCurrentSellerIdAsync();
                if (product.SellerId != sellerId)
                {
                    return Json(new { success = false, message = _localizer["CanOnlyManageOwnProducts"].Value });
                }
            }

            // Count total images (main image + gallery images)
            int totalImages = (string.IsNullOrEmpty(product.ImageUrl) || product.ImageUrl == "images/products/noimage.jpg" ? 0 : 1) + product.Images.Count;

            // Ensure at least one image remains
            if (totalImages <= 1)
            {
                return Json(new { success = false, message = _localizer["CannotDeleteLastImage"].Value });
            }

            var image = await _db.ProductImages.FindAsync(imageId);
            if (image == null || image.ProductId != productId)
            {
                return Json(new { success = false, message = _localizer["ImageNotFound"].Value });
            }

            // If this was the primary image, set the main product image as primary or another image
            if (image.IsPrimary)
            {
                // Find another image to make primary
                var nextImage = product.Images.FirstOrDefault(i => i.Id != imageId);
                if (nextImage != null)
                {
                    nextImage.IsPrimary = true;
                }
            }

            // Delete the physical file
            try
            {
                var filePath = Path.Combine(_he.WebRootPath, image.ImageUrl);
                if (System.IO.File.Exists(filePath) && !image.ImageUrl.Contains("noimage.jpg"))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch (IOException ex)
            {
                // Log file deletion errors - file might be in use or locked
                System.Diagnostics.Debug.WriteLine($"Failed to delete image file: {ex.Message}");
            }

            _db.ProductImages.Remove(image);
            await _db.SaveChangesAsync();

            return Json(new { success = true, message = _localizer["ImageDeletedSuccessfully"].Value });
        }

        // API: Delete main product image (convert to noimage.jpg or gallery image)
        [HttpPost]
        public async Task<IActionResult> DeleteMainImage(int productId)
        {
            var product = await _db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null)
            {
                return Json(new { success = false, message = _localizer["ProductNotFound"].Value });
            }

            // Sellers can only manage their own products
            if (!IsAdmin())
            {
                var sellerId = await GetCurrentSellerIdAsync();
                if (product.SellerId != sellerId)
                {
                    return Json(new { success = false, message = _localizer["CanOnlyManageOwnProducts"].Value });
                }
            }

            // Count total images
            int totalImages = (string.IsNullOrEmpty(product.ImageUrl) || product.ImageUrl == "images/products/noimage.jpg" ? 0 : 1) + product.Images.Count;

            if (totalImages <= 1)
            {
                return Json(new { success = false, message = _localizer["CannotDeleteLastImage"].Value });
            }

            // Delete the physical file
            try
            {
                var filePath = Path.Combine(_he.WebRootPath, product.ImageUrl);
                if (System.IO.File.Exists(filePath) && !product.ImageUrl.Contains("noimage.jpg"))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch (IOException ex)
            {
                // Log file deletion errors - file might be in use or locked
                System.Diagnostics.Debug.WriteLine($"Failed to delete main image file: {ex.Message}");
            }

            // If there are gallery images, promote the first one to main image
            var firstGalleryImage = product.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault();
            if (firstGalleryImage != null)
            {
                product.ImageUrl = firstGalleryImage.ImageUrl;
                _db.ProductImages.Remove(firstGalleryImage);
            }
            else
            {
                product.ImageUrl = "images/products/noimage.jpg";
            }

            await _db.SaveChangesAsync();

            return Json(new { success = true, message = _localizer["MainImageDeletedSuccessfully"].Value, newMainImage = "/" + product.ImageUrl });
        }

        // API: Set an image as primary/main image
        [HttpPost]
        public async Task<IActionResult> SetPrimaryImage(int imageId, int productId)
        {
            var product = await _db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null)
            {
                return Json(new { success = false, message = _localizer["ProductNotFound"].Value });
            }

            // Sellers can only manage their own products
            if (!IsAdmin())
            {
                var sellerId = await GetCurrentSellerIdAsync();
                if (product.SellerId != sellerId)
                {
                    return Json(new { success = false, message = _localizer["CanOnlyManageOwnProducts"].Value });
                }
            }

            var image = await _db.ProductImages.FindAsync(imageId);
            if (image == null || image.ProductId != productId)
            {
                return Json(new { success = false, message = _localizer["ImageNotFound"].Value });
            }

            // Swap: current main image becomes gallery image, selected gallery image becomes main
            var oldMainImageUrl = product.ImageUrl;

            // Set new main image
            product.ImageUrl = image.ImageUrl;

            // If old main image was not noimage, add it to gallery
            if (!string.IsNullOrEmpty(oldMainImageUrl) && !oldMainImageUrl.Contains("noimage.jpg"))
            {
                // Update the selected image's URL to the old main image (effectively swapping)
                image.ImageUrl = oldMainImageUrl;
                image.IsPrimary = false;
            }
            else
            {
                // Just remove the image from gallery since it's now the main image
                _db.ProductImages.Remove(image);
            }

            // Reset all IsPrimary flags
            foreach (var img in product.Images)
            {
                img.IsPrimary = false;
            }

            await _db.SaveChangesAsync();

            return Json(new { success = true, message = _localizer["PrimaryImageUpdatedSuccessfully"].Value, newMainImage = "/" + product.ImageUrl });
        }

        // API: Get product images for AJAX
        [HttpGet]
        public async Task<IActionResult> GetProductImages(int productId)
        {
            var product = await _db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null)
            {
                return Json(new { success = false, message = _localizer["ProductNotFound"].Value });
            }

            var images = new List<object>();

            // Add main image
            if (!string.IsNullOrEmpty(product.ImageUrl) && !product.ImageUrl.Contains("noimage.jpg"))
            {
                images.Add(new { id = 0, url = "/" + product.ImageUrl, isMain = true });
            }

            // Add gallery images
            foreach (var img in product.Images.OrderBy(i => i.DisplayOrder))
            {
                images.Add(new { id = img.Id, url = "/" + img.ImageUrl, isMain = false });
            }

            return Json(new { success = true, images });
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
                if (variant.ProductId <= 0)
                {
                    return Json(new { success = false, message = _localizer["InvalidProductId"].Value });
                }

                var product = await _db.Products.FindAsync(variant.ProductId);
                if (product == null)
                {
                    return Json(new { success = false, message = _localizer["ProductNotFound"].Value });
                }

                variant.Id = 0;
                variant.CreatedAt = DateTime.UtcNow;
                variant.UpdatedAt = DateTime.UtcNow;

                // Set display order
                var maxOrder = await _db.ProductVariants
                    .Where(v => v.ProductId == variant.ProductId)
                    .MaxAsync(v => (int?)v.DisplayOrder) ?? 0;
                variant.DisplayOrder = maxOrder + 1;

                _db.ProductVariants.Add(variant);
                await _db.SaveChangesAsync();

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
                var existingVariant = await _db.ProductVariants.FindAsync(variant.Id);
                if (existingVariant == null)
                {
                    return Json(new { success = false, message = _localizer["VariantNotFound"].Value });
                }

                existingVariant.Size = variant.Size;
                existingVariant.Color = variant.Color;
                existingVariant.ColorCode = variant.ColorCode;
                existingVariant.SKU = variant.SKU;
                existingVariant.Stock = variant.Stock;
                existingVariant.AdditionalPrice = variant.AdditionalPrice;
                existingVariant.IsAvailable = variant.IsAvailable;
                existingVariant.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

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
                var variant = await _db.ProductVariants.FindAsync(id);
                if (variant == null)
                {
                    return Json(new { success = false, message = _localizer["VariantNotFound"].Value });
                }

                _db.ProductVariants.Remove(variant);
                await _db.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // =============================================
        // BULK UPLOAD PRODUCTS
        // =============================================

        // GET: /Admin/Product/BulkUpload
        public async Task<IActionResult> BulkUpload()
        {
            // Sellers need a seller profile to bulk upload
            if (!IsAdmin())
            {
                var sellerId = await GetCurrentSellerIdAsync();
                if (sellerId == null)
                {
                    TempData["Error"] = _localizer["SellerProfileRequiredForBulkUpload"].Value;
                    return RedirectToAction("Index");
                }
            }

            ViewBag.Categories = await _db.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            return View();
        }

        // GET: /Admin/Product/DownloadTemplate
        public IActionResult DownloadTemplate()
        {
            var templateBytes = _bulkImportService.GenerateTemplate();
            return File(templateBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "BulkProductUpload_Template.xlsx");
        }

        // POST: /Admin/Product/BulkUpload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkUpload(IFormFile? excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                TempData["Error"] = _localizer["PleaseSelectExcelFile"].Value;
                return RedirectToAction("BulkUpload");
            }

            // Get seller ID for the upload
            int? sellerId = null;
            if (!IsAdmin())
            {
                sellerId = await GetCurrentSellerIdAsync();
                if (sellerId == null)
                {
                    TempData["Error"] = _localizer["SellerProfileRequiredForBulkUpload"].Value;
                    return RedirectToAction("Index");
                }
            }

            // For Admin, they can optionally upload products without a seller (platform products)
            // or we could require them to select a seller. For now, let them upload without seller.

            using var stream = new MemoryStream();
            await excelFile.CopyToAsync(stream);
            stream.Position = 0;

            // Validate file
            var validationResult = _bulkImportService.ValidateFile(stream);
            if (!validationResult.IsValid)
            {
                TempData["Error"] = validationResult.ErrorMessage;
                return RedirectToAction("BulkUpload");
            }

            // Reset stream position for import
            stream.Position = 0;

            // Import products
            var result = await _bulkImportService.ImportProductsAsync(stream, sellerId ?? 0);

            if (result.Success)
            {
                var message = $"Successfully imported {result.SuccessCount} product(s).";
                if (result.FailedCount > 0)
                {
                    message += $" {result.FailedCount} row(s) failed.";
                }
                if (result.Errors.Any())
                {
                    TempData["ImportErrors"] = result.Errors;
                }
                TempData["Success"] = message;
            }
            else
            {
                TempData["Error"] = result.Errors.FirstOrDefault() ?? "Import failed. Please check your file and try again.";
                if (result.Errors.Count > 1)
                {
                    TempData["ImportErrors"] = result.Errors.Skip(1).ToList();
                }
            }

            return RedirectToAction("BulkUpload");
        }
    }
}
