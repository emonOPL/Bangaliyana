using System.ComponentModel.DataAnnotations;

namespace Bangaliyana.Models
{
    public enum PaymentMethod
    {
        [Display(Name = "Cash on Delivery")]
        CashOnDelivery = 0,

        [Display(Name = "Online Payment")]
        OnlinePayment = 1,

        [Display(Name = "bKash")]
        bKash = 2,

        [Display(Name = "Nagad")]
        Nagad = 3,

        [Display(Name = "Rocket")]
        Rocket = 4,

        [Display(Name = "Credit/Debit Card")]
        CreditDebitCard = 5,

        [Display(Name = "Upay")]
        Upay = 6
    }
}
