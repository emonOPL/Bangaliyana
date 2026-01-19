using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public class Upazila
    {
        [Key]
        public int Id { get; set; }
        
        public int DistrictId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        
        // Navigation properties
        [ForeignKey("DistrictId")]
        public virtual District District { get; set; } = null!;
        
        public virtual ICollection<Union> Unions { get; set; } = new List<Union>();
    }
}