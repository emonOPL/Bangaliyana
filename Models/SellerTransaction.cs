using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public enum SellerTransactionType
    {
        [Display(Name = "Commission")]
        Commission,           // Deducted on COD delivery

        [Display(Name = "Order Payout")]
        OrderPayout,          // Added on online payment delivery

        [Display(Name = "Refund Deduction")]
        RefundDeduction,      // Deducted on approved return

        [Display(Name = "Withdrawal")]
        Withdrawal,           // Manual withdrawal by seller

        [Display(Name = "Adjustment")]
        Adjustment            // Admin adjustment
    }

    public class SellerTransaction
    {
        public int Id { get; set; }

        [Required]
        public int SellerId { get; set; }

        [ForeignKey("SellerId")]
        public virtual Seller? Seller { get; set; }

        public int? OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [Display(Name = "Transaction Type")]
        public SellerTransactionType Type { get; set; }

        [Display(Name = "Amount")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Display(Name = "Balance After")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BalanceAfter { get; set; }

        [StringLength(500)]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
