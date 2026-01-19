namespace Bangaliyana.Models
{
    public class CartItem
    {
        public required Products Product { get; set; }
        public int Quantity { get; set; }

        // Variant fields
        public int? ProductVariantId { get; set; }
        public string? Size { get; set; }
        public string? Color { get; set; }
        public string? ColorCode { get; set; }
        public decimal AdditionalPrice { get; set; } = 0;
        public string? VariantSKU { get; set; }

        // Computed property for effective price (base price + variant additional price)
        public decimal EffectivePrice =>
            (Product.HasDiscount && Product.DiscountPrice.HasValue
                ? Product.DiscountPrice.Value
                : Product.Price) + AdditionalPrice;

        // Computed property for variant display string
        public string? VariantInfo
        {
            get
            {
                var parts = new List<string>();
                if (!string.IsNullOrEmpty(Size)) parts.Add($"Size: {Size}");
                if (!string.IsNullOrEmpty(Color)) parts.Add($"Color: {Color}");
                return parts.Count > 0 ? string.Join(", ", parts) : null;
            }
        }
    }
}
