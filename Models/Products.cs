using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public class Products
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Product Description is required")]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Product Price is required")]
        [Display(Name = "Price")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero")]
        public decimal Price { get; set; }

        [Display(Name = "Image")]
        public string ImageUrl { get; set; }

        [Required(ErrorMessage = "Please Select the Availability")]
        [Display(Name = "Available")]
        public bool IsAvailable { get; set; }

        [Required(ErrorMessage = "Product Type is required")]
        [Display(Name = "Type")]
        public int ProductTypeId { get; set; }
        [ForeignKey("ProductTypeId")]
        public ProductTypes ProductTypes { get; set; }

        [Required(ErrorMessage = "Special Tag is required")]
        [Display(Name = "Tag")]
        public int SpecialTagId { get; set; }
        [ForeignKey("SpecialTagId")]
        public SpecialTags SpecialTags { get; set; }
    }
}
