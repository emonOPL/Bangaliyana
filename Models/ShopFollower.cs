using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    /// <summary>
    /// Represents a user following a seller's shop
    /// </summary>
    public class ShopFollower
    {
        public int Id { get; set; }

        /// <summary>
        /// The user who is following the shop
        /// </summary>
        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        /// <summary>
        /// The seller whose shop is being followed
        /// </summary>
        [Required]
        public int SellerId { get; set; }

        [ForeignKey("SellerId")]
        public virtual Seller? Seller { get; set; }

        /// <summary>
        /// When the user started following
        /// </summary>
        public DateTime FollowedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Whether to receive notifications for new products from this shop
        /// </summary>
        public bool NotifyOnNewProducts { get; set; } = true;

        /// <summary>
        /// Whether to receive notifications for sales/discounts from this shop
        /// </summary>
        public bool NotifyOnSales { get; set; } = true;
    }
}
