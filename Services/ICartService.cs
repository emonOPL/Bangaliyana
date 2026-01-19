using Bangaliyana.Models;

namespace Bangaliyana.Services
{
    /// <summary>
    /// Result of add to cart operation
    /// </summary>
    public class AddToCartResult
    {
        public bool Success { get; set; }
        public bool ShopConflict { get; set; }
        public string? ExistingShopName { get; set; }
        public int? ExistingSellerId { get; set; }
        public string? Message { get; set; }
        public bool RequiresVariant { get; set; }
    }

    public interface ICartService
    {
        Task<List<CartItem>> GetCartItemsAsync(string? userId);
        Task<AddToCartResult> AddToCartAsync(int productId, int quantity, string? userId, int? variantId = null);
        Task UpdateQuantityAsync(int productId, int quantity, string? userId, int? variantId = null);
        Task RemoveFromCartAsync(int productId, string? userId, int? variantId = null);
        Task ClearCartAsync(string? userId);
        Task MergeCartsAsync(string userId);
        Task<int> GetCartCountAsync(string? userId);
        List<CartItem> GetSessionCart();
        void SaveSessionCart(List<CartItem> cart);
        void ClearSessionCart();

        /// <summary>
        /// Clear existing cart and add the new product (used when user confirms shop change)
        /// </summary>
        Task<AddToCartResult> ClearAndAddToCartAsync(int productId, int quantity, string? userId, int? variantId = null);

        /// <summary>
        /// Get the seller ID of items currently in cart (null if cart is empty)
        /// </summary>
        Task<int?> GetCartSellerIdAsync(string? userId);
    }
}
