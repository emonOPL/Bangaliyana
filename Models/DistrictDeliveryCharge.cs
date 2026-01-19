using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    /// <summary>
    /// Stores delivery charges for specific districts.
    /// Districts not in this table will use the default delivery charge from SiteSettings.
    /// </summary>
    public class DistrictDeliveryCharge
    {
        public int Id { get; set; }

        [Required]
        public int DistrictId { get; set; }

        [ForeignKey("DistrictId")]
        public virtual District? District { get; set; }

        [Required]
        [Display(Name = "Delivery Charge")]
        [Range(0, 10000)]
        public decimal DeliveryCharge { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
