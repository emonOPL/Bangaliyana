using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public class Union
    {
        [Key]
        public int Id { get; set; }
        
        public int UpazilaId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string? Type { get; set; } // City Corporation, Municipality, Union
        
        // Navigation property
        [ForeignKey("UpazilaId")]
        public virtual Upazila Upazila { get; set; } = null!;
    }
}