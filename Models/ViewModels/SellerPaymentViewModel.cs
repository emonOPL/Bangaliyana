namespace Bangaliyana.Models.ViewModels
{
    public class SellerPaymentViewModel
    {
        public int SellerId { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public string SellerEmail { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public decimal TotalSales { get; set; }
        public decimal CommissionRate { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal PayableAmount { get; set; }
        public bool HasBankAccount { get; set; }
        public bool HasMobileBanking { get; set; }

        // Monthly Report integration fields
        public decimal CurrentMonthSales { get; set; }
        public decimal CurrentMonthPayable { get; set; }
        public int PendingPayoutReportsCount { get; set; }
        public decimal CarryForwardAmount { get; set; }
    }
}
