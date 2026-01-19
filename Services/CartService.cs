using Bangaliyana.Data;
using Bangaliyana.Models;
using Bangaliyana.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Bangaliyana.Services
{
    /// <summary>
    /// Service to handle cart operations for both authenticated and guest users.
    /// Guest users: Cart stored in session
    /// Authenticated users: Cart stored in database
    /// Enforces single-shop-per-cart restriction.
    /// Supports product variants (size/color).
    /// </summary>
    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CartService> _logger;

        public CartService(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor, ILogger<CartService> logger)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Get cart items for a user (from session if guest, from database if authenticated)
        /// </summary>
        public async Task<List<CartItem>> GetCartItemsAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                // Guest user - return session cart
                return GetSessionCart();
            }

            // Authenticated user - get from database with variant info
            var persistentCart = await _db.PersistentCarts
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                    .ThenInclude(p => p!.Category)
                .Include(c => c.Product)
                    .ThenInclude(p => p!.Seller)
                .Include(c => c.ProductVariant)
                .ToListAsync();

            return persistentCart.Select(pc => new CartItem
            {
                Product = pc.Product!,
                Quantity = pc.Quantity,
                ProductVariantId = pc.ProductVariantId,
                Size = pc.ProductVariant?.Size,
                Color = pc.ProductVariant?.Color,
                ColorCode = pc.ProductVariant?.ColorCode,
                AdditionalPrice = pc.ProductVariant?.AdditionalPrice ?? 0,
                VariantSKU = pc.ProductVariant?.SKU
            }).ToList();
        }

        /// <summary>
        /// Get the seller ID of the current cart items
        /// </summary>
        public async Task<int?> GetCartSellerIdAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                // Guest user - check session cart
                var sessionCart = GetSessionCart();
                if (sessionCart.Any())
                {
                    return sessionCart.First().Product.SellerId;
                }
                return null;
            }

            // Authenticated user - check database
            var firstItem = await _db.PersistentCarts
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                .FirstOrDefaultAsync();

            return firstItem?.Product?.SellerId;
        }

        /// <summary>
        /// Add item to cart with single-shop validation and variant support
        /// </summary>
        public async Task<AddToCartResult> AddToCartAsync(int productId, int quantity, string? userId, int? variantId = null)
        {
            // Validate product using helper method
            var (product, variant, error) = await ValidateProductForCartAsync(productId, quantity, variantId);
            if (error != null) return error;

            if (string.IsNullOrEmpty(userId))
            {
                // Guest user - add to session with shop validation
                return AddToSessionCartWithValidation(product!, quantity, variant);
            }
            else
            {
                // Authenticated user - save to database with shop validation
                return await AddToDatabaseCartWithValidation(product!, quantity, userId, variant);
            }
        }

        private AddToCartResult AddToSessionCartWithValidation(Products product, int quantity, ProductVariant? variant)
        {
            var cart = GetSessionCart();

            // Check for shop conflict using helper method
            if (cart.Any())
            {
                var existingSellerId = cart.First().Product.SellerId;
                var existingShopName = cart.First().Product.Seller?.ShopName;
                var conflictResult = CheckShopConflict(existingSellerId, existingShopName, product);
                if (conflictResult != null) return conflictResult;
            }

            // No conflict - add to cart (match by ProductId AND VariantId)
            var existingItem = cart.FirstOrDefault(c =>
                c.Product.Id == product.Id &&
                c.ProductVariantId == variant?.Id);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    Product = product,
                    Quantity = quantity,
                    ProductVariantId = variant?.Id,
                    Size = variant?.Size,
                    Color = variant?.Color,
                    ColorCode = variant?.ColorCode,
                    AdditionalPrice = variant?.AdditionalPrice ?? 0,
                    VariantSKU = variant?.SKU
                });
            }

            SaveSessionCart(cart);

            return new AddToCartResult
            {
                Success = true,
                Message = "Product added to cart"
            };
        }

        private async Task<AddToCartResult> AddToDatabaseCartWithValidation(Products product, int quantity, string userId, ProductVariant? variant)
        {
            // Check for shop conflict using helper method
            var existingItems = await _db.PersistentCarts
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                    .ThenInclude(p => p!.Seller)
                .ToListAsync();

            if (existingItems.Any())
            {
                var existingSellerId = existingItems.First().Product?.SellerId;
                var existingShopName = existingItems.First().Product?.Seller?.ShopName;
                var conflictResult = CheckShopConflict(existingSellerId, existingShopName, product);
                if (conflictResult != null) return conflictResult;
            }

            // No conflict - add to cart (match by ProductId AND VariantId)
            var existingCartItem = existingItems.FirstOrDefault(c =>
                c.ProductId == product.Id &&
                c.ProductVariantId == variant?.Id);

            if (existingCartItem != null)
            {
                existingCartItem.Quantity += quantity;
                existingCartItem.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.PersistentCarts.Add(new PersistentCart
                {
                    UserId = userId,
                    ProductId = product.Id,
                    ProductVariantId = variant?.Id,
                    Quantity = quantity,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();

            return new AddToCartResult
            {
                Success = true,
                Message = "Product added to cart"
            };
        }

        /// <summary>
        /// Clear existing cart and add new product (user confirmed shop change)
        /// </summary>
        public async Task<AddToCartResult> ClearAndAddToCartAsync(int productId, int quantity, string? userId, int? variantId = null)
        {
            // Validate product using helper method
            var (product, variant, error) = await ValidateProductForCartAsync(productId, quantity, variantId);
            if (error != null) return error;

            // Clear existing cart
            await ClearCartAsync(userId);

            if (string.IsNullOrEmpty(userId))
            {
                // Guest user - add to session
                var cart = new List<CartItem>
                {
                    new CartItem
                    {
                        Product = product!,
                        Quantity = quantity,
                        ProductVariantId = variant?.Id,
                        Size = variant?.Size,
                        Color = variant?.Color,
                        ColorCode = variant?.ColorCode,
                        AdditionalPrice = variant?.AdditionalPrice ?? 0,
                        VariantSKU = variant?.SKU
                    }
                };
                SaveSessionCart(cart);
            }
            else
            {
                // Authenticated user - add to database
                _db.PersistentCarts.Add(new PersistentCart
                {
                    UserId = userId,
                    ProductId = productId,
                    ProductVariantId = variant?.Id,
                    Quantity = quantity,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }

            return new AddToCartResult
            {
                Success = true,
                Message = "Cart cleared and product added"
            };
        }

        /// <summary>
        /// Update quantity of a cart item (identified by productId + variantId)
        /// </summary>
        public async Task UpdateQuantityAsync(int productId, int quantity, string? userId, int? variantId = null)
        {
            if (string.IsNullOrEmpty(userId))
            {
                // Guest user - update session
                var cart = GetSessionCart();
                var item = cart.FirstOrDefault(c =>
                    c.Product.Id == productId &&
                    c.ProductVariantId == variantId);

                if (item != null)
                {
                    if (quantity > 0)
                    {
                        item.Quantity = quantity;
                    }
                    else
                    {
                        cart.Remove(item);
                    }
                    SaveSessionCart(cart);
                }
            }
            else
            {
                // Authenticated user - update database
                var item = await _db.PersistentCarts
                    .FirstOrDefaultAsync(c =>
                        c.UserId == userId &&
                        c.ProductId == productId &&
                        c.ProductVariantId == variantId);

                if (item != null)
                {
                    if (quantity > 0)
                    {
                        item.Quantity = quantity;
                        item.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        _db.PersistentCarts.Remove(item);
                    }
                    await _db.SaveChangesAsync();
                }
            }
        }

        /// <summary>
        /// Remove item from cart (identified by productId + variantId)
        /// </summary>
        public async Task RemoveFromCartAsync(int productId, string? userId, int? variantId = null)
        {
            await UpdateQuantityAsync(productId, 0, userId, variantId);
        }

        /// <summary>
        /// Clear entire cart (used after order completion)
        /// </summary>
        public async Task ClearCartAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                // Guest user - clear session
                ClearSessionCart();
            }
            else
            {
                // Authenticated user - clear database entries
                var cartItems = await _db.PersistentCarts
                    .Where(c => c.UserId == userId)
                    .ToListAsync();

                _db.PersistentCarts.RemoveRange(cartItems);
                await _db.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Merge session cart with database cart when user logs in
        /// Enforces single-shop: if session cart has different shop, session cart wins
        /// Preserves variant information during merge
        /// </summary>
        public async Task MergeCartsAsync(string userId)
        {
            try
            {
                var sessionCart = GetSessionCart();

                if (sessionCart.Any())
                {
                    _logger.LogInformation($"Merging {sessionCart.Count} items from session cart for user {userId}");

                    var sessionSellerId = sessionCart.First().Product.SellerId;

                    // Get existing database cart items
                    var existingItems = await _db.PersistentCarts
                        .Where(c => c.UserId == userId)
                        .Include(c => c.Product)
                        .ToListAsync();

                    // Check if database cart has items from different shop (semi-strict mode)
                    if (existingItems.Any())
                    {
                        var dbSellerId = existingItems.First().Product?.SellerId;
                        // Clear if sellers are different (including null vs non-null cases)
                        // But keep if both are null (both are no-shop products)
                        if (sessionSellerId != dbSellerId && (sessionSellerId.HasValue || dbSellerId.HasValue))
                        {
                            // Different shop - clear database cart (session cart wins)
                            _db.PersistentCarts.RemoveRange(existingItems);
                            existingItems.Clear();
                            _logger.LogInformation($"Cleared database cart due to shop conflict for user {userId}");
                        }
                    }

                    foreach (var sessionItem in sessionCart)
                    {
                        // Match by ProductId AND VariantId
                        var existingItem = existingItems.FirstOrDefault(c =>
                            c.ProductId == sessionItem.Product.Id &&
                            c.ProductVariantId == sessionItem.ProductVariantId);

                        if (existingItem != null)
                        {
                            // Item exists in both carts - add quantities
                            existingItem.Quantity += sessionItem.Quantity;
                            existingItem.UpdatedAt = DateTime.UtcNow;
                        }
                        else
                        {
                            // Item only in session cart - add to database with variant info
                            _db.PersistentCarts.Add(new PersistentCart
                            {
                                UserId = userId,
                                ProductId = sessionItem.Product.Id,
                                ProductVariantId = sessionItem.ProductVariantId,
                                Quantity = sessionItem.Quantity,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            });
                        }
                    }

                    await _db.SaveChangesAsync();

                    // Clear session cart after successful merge
                    ClearSessionCart();
                    _logger.LogInformation($"Successfully merged carts for user {userId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error merging carts for user {userId}");
            }
        }

        /// <summary>
        /// Get total cart count
        /// </summary>
        public async Task<int> GetCartCountAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                // Guest user - count from session
                return GetSessionCart().Sum(c => c.Quantity);
            }

            // Authenticated user - count from database
            return await _db.PersistentCarts
                .Where(c => c.UserId == userId)
                .SumAsync(c => c.Quantity);
        }

        /// <summary>
        /// Get cart from session
        /// </summary>
        public List<CartItem> GetSessionCart()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null) return new List<CartItem>();

            return session.Get<List<CartItem>>("cart") ?? new List<CartItem>();
        }

        /// <summary>
        /// Save cart to session
        /// </summary>
        public void SaveSessionCart(List<CartItem> cart)
        {
            _httpContextAccessor.HttpContext?.Session?.Set("cart", cart);
        }

        /// <summary>
        /// Clear session cart
        /// </summary>
        public void ClearSessionCart()
        {
            _httpContextAccessor.HttpContext?.Session?.Remove("cart");
        }

        #region Private Helper Methods

        /// <summary>
        /// Check for shop conflict between cart items and a new product
        /// </summary>
        private AddToCartResult? CheckShopConflict(int? existingSellerId, string? existingShopName, Products product)
        {
            // Warn if sellers are different (including null vs non-null cases)
            // But allow if both are null (both are no-shop products)
            if (product.SellerId != existingSellerId && (product.SellerId.HasValue || existingSellerId.HasValue))
            {
                var existingShopDisplay = existingSellerId.HasValue
                    ? (existingShopName ?? "a shop")
                    : "non-shop products";
                var newProductType = product.SellerId.HasValue
                    ? $"from {product.Seller?.ShopName ?? "a shop"}"
                    : "a non-shop product";

                return new AddToCartResult
                {
                    Success = false,
                    ShopConflict = true,
                    ExistingShopName = existingShopDisplay,
                    ExistingSellerId = existingSellerId,
                    Message = $"Your cart contains {(existingSellerId.HasValue ? $"items from {existingShopDisplay}" : existingShopDisplay)}. Adding {newProductType} will remove all existing items."
                };
            }

            return null; // No conflict
        }

        /// <summary>
        /// Validate product for adding to cart (null check, suspended seller, variant requirements)
        /// </summary>
        private async Task<(Products? product, ProductVariant? variant, AddToCartResult? error)> ValidateProductForCartAsync(int productId, int quantity, int? variantId)
        {
            var product = await _db.Products
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                return (null, null, new AddToCartResult
                {
                    Success = false,
                    Message = "Product not found"
                });
            }

            // Block adding products from suspended sellers
            if (product.Seller != null && product.Seller.Status == SellerStatus.Suspended)
            {
                return (product, null, new AddToCartResult
                {
                    Success = false,
                    Message = "This product is currently unavailable as the seller is suspended."
                });
            }

            // Validate variant requirement
            var availableVariants = product.Variants.Where(v => v.IsAvailable).ToList();
            if (availableVariants.Any() && !variantId.HasValue)
            {
                return (product, null, new AddToCartResult
                {
                    Success = false,
                    RequiresVariant = true,
                    Message = "Please select a size and/or color before adding to cart."
                });
            }

            // Validate and get variant info if provided
            ProductVariant? variant = null;
            if (variantId.HasValue)
            {
                variant = availableVariants.FirstOrDefault(v => v.Id == variantId.Value);
                if (variant == null)
                {
                    return (product, null, new AddToCartResult
                    {
                        Success = false,
                        Message = "Selected variant is not available."
                    });
                }

                // Check variant stock
                if (variant.Stock < quantity)
                {
                    return (product, null, new AddToCartResult
                    {
                        Success = false,
                        Message = $"Only {variant.Stock} items available for this variant."
                    });
                }
            }

            return (product, variant, null); // No error
        }

        #endregion
    }
}
