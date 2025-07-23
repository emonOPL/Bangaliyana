using System.ComponentModel.DataAnnotations;

namespace Bangaliyana.Models
{
    public class ProductTypes
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Product Type")]
        public string ProductType { get; set; }

        [Required]
        [Display(Name = "Description")]
        public string Description { get; set; }

    }
}
