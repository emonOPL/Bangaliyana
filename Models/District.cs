using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bangaliyana.Models
{
    public class District
    {
        [Key]
        public int Id { get; set; }
        
        public int DivisionId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        
        [Column(TypeName = "decimal(10,8)")]
        public decimal? Lat { get; set; }
        
        [Column(TypeName = "decimal(11,8)")]
        public decimal? Lon { get; set; }
        
        // Navigation properties
        [ForeignKey("DivisionId")]
        public virtual Division Division { get; set; } = null!;
        
        public virtual ICollection<Upazila> Upazilas { get; set; } = new List<Upazila>();
    }
}